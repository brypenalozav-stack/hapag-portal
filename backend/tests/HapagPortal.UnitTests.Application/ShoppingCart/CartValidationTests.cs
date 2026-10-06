namespace HapagPortal.UnitTests.Application.ShoppingCart;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Validaciones al agregar al carro (M5-01): permisos y perfil (M1-11, M1-02), asociación previa
/// (M1-18), carta FFWW (M4-04), valor cero (M4-02), duplicados, moneda de pago (M5-04, M5-08), RUT de
/// facturación (M5-09) y exclusión de clientes con crédito (M5-07).
/// </summary>
public sealed class CartValidationTests
{
    private const string OwnTaxId = "76123456-7";
    private readonly PaymentsFixture _f = new();

    private Task<Result<HapagPortal.Application.Payments.Common.CartDto>> AddAsync(
        string itemType,
        Guid? sourceId,
        string billingTaxId = OwnTaxId,
        string? currency = null,
        PaymentsFixture.Actor? actor = null,
        string? reference = null) =>
        _f.Add(actor).Handle(new AddCartItemCommand(itemType, sourceId, reference, billingTaxId, currency), CancellationToken.None);

    [Fact]
    public async Task LocalCharge_ShouldBeAddedToItsCurrencySubCart_WithBillingTaxIdAndAudit()
    {
        var bl = _f.Rules.OwnBl("BL-CART");
        var thc = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        var ipo = _f.Rules.AddCharge(bl, ChargeConceptCodes.Ipo, 150m, currency: "USD", taxRate: 0m);

        (await AddAsync(PayableItemTypes.LocalCharge, thc.Id)).IsSuccess.Should().BeTrue();
        var cart = await AddAsync(PayableItemTypes.LocalCharge, ipo.Id);

        cart.IsSuccess.Should().BeTrue();
        cart.Value.Groups.Select(g => (g.Country, g.PaymentCurrency, g.Subtotal)).Should().Equal(
            (CountryCodes.Chile, "CLP", 220150m),
            (CountryCodes.Chile, "USD", 150m));
        var item = cart.Value.Groups[0].Items.Single();
        item.BillingTaxId.Should().Be(OwnTaxId);
        item.BlNumber.Should().Be("BL-CART");
        item.AllowedCurrencies.Should().Contain("CLP");
        _f.Db.CartItemList.Should().OnlyContain(i => i.AddedByUserId == _f.Owner.User.Id && i.OnBehalfOfClientId == null);
    }

    [Fact]
    public async Task UsdCharge_ConvertedToClp_ShouldUseNexusRateAndKeepOriginalAmount()
    {
        var bl = _f.Rules.OwnBl("BL-USD");
        var ipo = _f.Rules.AddCharge(bl, ChargeConceptCodes.Ipo, 150m, currency: "USD", taxRate: 0m);

        var cart = await AddAsync(PayableItemTypes.LocalCharge, ipo.Id, currency: "CLP");

        var item = cart.Value.Groups.Single().Items.Single();
        item.PaymentCurrency.Should().Be("CLP");
        item.Currency.Should().Be("USD");
        item.TotalAmount.Should().Be(150m);
        item.PaymentAmount.Should().Be(142500m);
        item.ExchangeRate!.Rate.Should().Be(950m);
        item.ExchangeRate.Source.Should().Be("TEST");
    }

    [Fact]
    public async Task EurCharge_InChile_ShouldOnlyBePayableInClp()
    {
        var bl = _f.Rules.OwnBl("BL-EUR");
        var invoice = _f.AddInvoice(_f.Owner.Organization, bl, 480m, currency: "EUR");

        var inEur = await AddAsync(PayableItemTypes.Invoice, invoice.Id, currency: "EUR");
        var byDefault = await AddAsync(PayableItemTypes.Invoice, invoice.Id);

        inEur.Error.Code.Should().Be("Cart.CurrencyNotAllowed");
        byDefault.IsSuccess.Should().BeTrue();
        var item = byDefault.Value.Groups.Single().Items.Single();
        item.PaymentCurrency.Should().Be("CLP");
        item.AllowedCurrencies.Should().Equal("CLP");
    }

    [Fact]
    public async Task BobCharge_FromChileOperation_ShouldNotBePayableInBob()
    {
        var bl = _f.Rules.OwnBl("BL-BOB");
        var charge = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 1280m, currency: "BOB", taxRate: 13m);

        var inBob = await AddAsync(PayableItemTypes.LocalCharge, charge.Id, currency: "BOB");
        var byDefault = await AddAsync(PayableItemTypes.LocalCharge, charge.Id);

        inBob.Error.Code.Should().Be("Cart.CurrencyNotAllowed");
        byDefault.Value.Groups.Single().PaymentCurrency.Should().Be("CLP");
    }

    [Fact]
    public async Task ConfiguredCurrencies_ShouldRestrictTheConcept()
    {
        _f.EnableCurrencies(CountryCodes.Chile, PaymentConcepts.Freight, "USD", "EUR", "CLP");
        _f.EnableCurrencies(CountryCodes.Chile, ChargeConceptCodes.GateIn, "CLP");
        var bl = _f.Rules.OwnBl("BL-CONF");
        var gateIn = _f.Rules.AddCharge(bl, ChargeConceptCodes.GateIn, 95000m);

        var options = await new GetCartItemOptionsQueryHandler(_f.Resolver(_f.Owner))
            .Handle(new GetCartItemOptionsQuery(PayableItemTypes.Freight, bl.Id, null), CancellationToken.None);
        var gateInUsd = await AddAsync(PayableItemTypes.LocalCharge, gateIn.Id, currency: "USD");

        options.Value.AllowedCurrencies.Should().Equal("USD", "CLP", "EUR");
        options.Value.DefaultPaymentCurrency.Should().Be("USD");
        gateInUsd.Error.Code.Should().Be("Cart.CurrencyNotAllowed");
    }

    [Fact]
    public async Task BillingTaxId_NotEnabledForTheUser_ShouldBeRejected()
    {
        var bl = _f.Rules.OwnBl("BL-RUT");
        var charge = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);

        var result = await AddAsync(PayableItemTypes.LocalCharge, charge.Id, billingTaxId: "99.999.999-9");

        result.Error.Code.Should().Be("Cart.BillingTaxIdNotAllowed");
        _f.Db.CartItemList.Should().BeEmpty();
    }

    [Fact]
    public async Task Mandatary_ShouldChooseOwnOrMandatorTaxId_AndRecordTheMandator()
    {
        var bl = _f.Rules.OwnBl("BL-MANDATE");
        var charge = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        var agency = _f.NewActor(OrganizationTypes.FreightForwarder);
        var grant = ThirdPartyTestData.AddGrant(_f.Db, _f.Owner.Organization, agency.Organization, bl,
            [ShipmentActionCodes.ViewShipment, ShipmentActionCodes.PayMandatoryLocalCharges], isMandate: true);

        var options = await new GetCartItemOptionsQueryHandler(_f.Resolver(agency))
            .Handle(new GetCartItemOptionsQuery(PayableItemTypes.LocalCharge, charge.Id, null), CancellationToken.None);
        var cart = await AddAsync(PayableItemTypes.LocalCharge, charge.Id, billingTaxId: "76.123.456-7", actor: agency);

        options.Value.BillingOptions.Select(o => (o.TaxId, o.Source)).Should().Equal(
            (HapagPortal.Application.Common.Helpers.TaxIdNormalizer.Normalize(agency.Organization.TaxId), "Own"),
            (OwnTaxId, "Grant"));
        cart.IsSuccess.Should().BeTrue();
        var item = _f.Db.CartItemList.Single();
        item.BillingTaxId.Should().Be(OwnTaxId);
        item.OnBehalfOfClientId.Should().Be(_f.Owner.Organization.Id);
        item.AccessGrantId.Should().Be(grant.Id);
    }

    [Fact]
    public async Task CustomsAgencyByOpenAccessOnly_ShouldAssociateBeforeAdding()
    {
        var bl = _f.Rules.OwnBl("BL-OPEN");
        var charge = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        // El comando de M1-17 siempre agrega "shipment.view" al conjunto.
        ThirdPartyTestData.EnableOpenAccess(_f.Db, _f.Owner.Organization,
            [ShipmentActionCodes.ViewShipment, ShipmentActionCodes.PayMandatoryLocalCharges]);
        var agency = _f.NewActor(OrganizationTypes.CustomsAgency);

        var result = await AddAsync(PayableItemTypes.LocalCharge, charge.Id, billingTaxId: agency.Organization.TaxId, actor: agency);

        result.Error.Code.Should().Be("Cart.AssociationRequired");
    }

    [Fact]
    public async Task FreightForwarder_WithoutResponsibilityLetter_ShouldBeBlocked()
    {
        var bl = _f.Rules.OwnBl("BL-FFWW");
        var charge = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        _f.Rules.Conditions(OwnTaxId, isFreightForwarder: true, credit: null);

        var result = await AddAsync(PayableItemTypes.LocalCharge, charge.Id);

        result.Error.Code.Should().Be("Cart.ResponsibilityLetterRequired");
    }

    [Fact]
    public async Task ExemptCharge_ShouldNotEnterTheCart()
    {
        var bl = _f.Rules.OwnBl("BL-EXEMPT");
        var gateIn = _f.Rules.AddCharge(bl, ChargeConceptCodes.GateIn, 95000m);
        _f.Rules.Exempt(OwnTaxId, new ExemptionInfo(ChargeConceptCodes.GateIn, null, null, new DateOnly(2026, 1, 1), null));

        var result = await AddAsync(PayableItemTypes.LocalCharge, gateIn.Id);

        result.Error.Code.Should().Be("Cart.ZeroValue");
    }

    [Fact]
    public async Task CreditCustomer_ShouldUseTheAccountView()
    {
        var bl = _f.Rules.OwnBl("BL-CREDIT");
        var charge = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        _f.Rules.Conditions(OwnTaxId, false, new CreditCondition(["LOCAL_CHARGES"], 30, new DateOnly(2026, 1, 1), null));

        var result = await AddAsync(PayableItemTypes.LocalCharge, charge.Id);

        result.Error.Code.Should().Be("Cart.CreditCustomer");
    }

    [Fact]
    public async Task NexusUnavailable_ShouldNotAddAnything()
    {
        var bl = _f.Rules.OwnBl("BL-NEXUS");
        var charge = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        _f.Rules.Credit.GetConditionsAsync(OwnTaxId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CustomerConditions?>.Failure(new Error("Integration.Unavailable", "down"))));

        var result = await AddAsync(PayableItemTypes.LocalCharge, charge.Id);

        result.Error.Code.Should().Be("ChargeRules.ConditionsUnavailable");
    }

    [Fact]
    public async Task SameItemTwice_ShouldBeRejectedAsDuplicate()
    {
        var bl = _f.Rules.OwnBl("BL-DUP");
        var charge = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);

        await AddAsync(PayableItemTypes.LocalCharge, charge.Id);
        var again = await AddAsync(PayableItemTypes.LocalCharge, charge.Id);

        again.Error.Code.Should().Be("CartItem.AlreadyExists");
        _f.Db.CartItemList.Should().ContainSingle();
    }

    [Fact]
    public async Task ViewerProfile_ShouldNotAdd()
    {
        var bl = _f.Rules.OwnBl("BL-VIEWER");
        var charge = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        var viewerUser = AccessTestData.AddMember(_f.Db, _f.Owner.Organization, profile: RoleCodes.OrgViewer);
        var viewer = new PaymentsFixture.Actor(_f.Owner.Organization, viewerUser, AccessTestData.CurrentUser(viewerUser));

        var result = await AddAsync(PayableItemTypes.LocalCharge, charge.Id, actor: viewer);

        result.Error.Should().Be(Error.Forbidden);
    }

    [Fact]
    public async Task ShipperOnly_ShouldNotPayFreight()
    {
        // M1-11: "Visualizar y pagar montos de flete" es X (o) para el shipper (antes lo cubría POST /payments).
        var bl = _f.Rules.OwnBl("BL-SHIPPER-FREIGHT", consignee: false);
        bl.ClientId = Guid.NewGuid();
        AccessTestData.AddRole(_f.Db, bl, _f.Owner.Organization, ShipmentRoleCodes.Shipper);

        var result = await AddAsync(PayableItemTypes.Freight, bl.Id);

        result.Error.Should().Be(Error.Forbidden);
        _f.Db.CartItemList.Should().BeEmpty();
    }

    [Fact]
    public async Task ShipperOnly_ShouldNotPayImportDemurrage()
    {
        var bl = _f.Rules.OwnBl("BL-SHIPPER", consignee: false);
        bl.ClientId = Guid.NewGuid();
        AccessTestData.AddRole(_f.Db, bl, _f.Owner.Organization, ShipmentRoleCodes.Shipper);
        var line = AddDemurrage(bl, invoice: null);

        var result = await AddAsync(PayableItemTypes.Demurrage, line.Id);

        result.Error.Should().Be(Error.Forbidden);
    }

    [Fact]
    public async Task InvoicedDemurrageLine_ShouldBePaidThroughItsInvoice()
    {
        var bl = _f.Rules.OwnBl("BL-DEM");
        var line = AddDemurrage(bl, invoice: "FAC-DEM-1");
        _f.AddInvoice(_f.Owner.Organization, bl, 510000m, source: "FAC-DEM-1");

        var asLine = await AddAsync(PayableItemTypes.Demurrage, line.Id);
        var asInvoice = await AddAsync(PayableItemTypes.Invoice, null, reference: "FAC-DEM-1");

        asLine.Error.Code.Should().Be("Cart.PayDemurrageInvoice");
        asInvoice.IsSuccess.Should().BeTrue();
        asInvoice.Value.Groups.Single().Items.Single().ItemType.Should().Be(PayableItemTypes.Invoice);
    }

    [Fact]
    public async Task ItemInAPaymentInProgress_ShouldNotBeAddedAgain()
    {
        var bl = _f.Rules.OwnBl("BL-INFLIGHT");
        var charge = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 185000m);
        var payment = new Payment
        {
            PaymentNumber = "PAY-1", PaymentType = "Cart", PaymentMethod = "DEPOSIT", Currency = "CLP",
            Status = PaymentStatus.PendingVerification, Country = "CL", ClientId = _f.Owner.Organization.Id
        };
        _f.Db.PaymentList.Add(payment);
        _f.Db.PaymentDetailList.Add(new PaymentDetail
        {
            PaymentId = payment.Id, ConceptType = "THC", Currency = "CLP", ItemType = PayableItemTypes.LocalCharge, SourceId = charge.Id
        });

        var result = await AddAsync(PayableItemTypes.LocalCharge, charge.Id);

        result.Error.Code.Should().Be("Cart.ItemInPayment");
    }

    private DemurrageCharge AddDemurrage(BillOfLading bl, string? invoice)
    {
        var line = new DemurrageCharge
        {
            BillOfLadingId = bl.Id,
            ContainerNumber = "HLXU0000001",
            FreeDays = 7,
            DemurrageDays = 10,
            DailyRate = 51000m,
            TotalAmount = 510000m,
            Currency = "CLP",
            Status = invoice is null ? DemurrageChargeStatus.Pending : DemurrageChargeStatus.Invoiced,
            InvoiceNumber = invoice
        };
        _f.Db.DemurrageChargeList.Add(line);
        return line;
    }
}
