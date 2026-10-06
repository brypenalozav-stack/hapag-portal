namespace HapagPortal.UnitTests.Application.Payments;

using FluentAssertions;
using HapagPortal.Application.AccountStatement;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.PostPayment;
using HapagPortal.Application.Invoices;
using HapagPortal.Application.Payments.Settlements;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Pago anticipado de Gate Out por la agencia de aduanas (M3-19): en exportación la agencia paga antes de la
/// factura; el portal emite un recibo que identifica embarque, unidades y pagador en el repositorio (M6-09); la
/// factura emitida tras el zarpe queda vinculada al recibo y el cargo no vuelve a presentarse como pendiente
/// (M7-01, M7-03), con el cruce disponible para conciliación (NF-04).
/// </summary>
public sealed class GateOutAdvanceTests
{
    private readonly FinanceFixture _f = new();
    private readonly PaymentsFixture.Actor _agency;
    private readonly BillOfLading _bl;
    private readonly LocalCharge _gateOut;

    public GateOutAdvanceTests()
    {
        _agency = _f.Payments.NewActor(OrganizationTypes.CustomsAgency);
        _f.Payments.NoConditions(_agency.Organization);
        _f.Payments.NoConditions(_f.Owner.Organization);
        _bl = _f.Services.ExportBl("BL-GO-EXP", DateTime.UtcNow.AddDays(3));
        _bl.FreightPaidAt = DateTime.UtcNow.AddDays(-1);
        AccessTestData.AddRole(_f.Db, _bl, _agency.Organization, ShipmentRoleCodes.CustomsAgency);
        _f.Rules.AddContainer(_bl, "HLXU3071801", "40HC");
        _gateOut = _f.Rules.AddCharge(_bl, ChargeConceptCodes.GateOut, 60000m);
    }

    private string AgencyTaxId => TaxIdNormalizer.Normalize(_agency.Organization.TaxId);

    [Fact]
    public async Task AdvancePayment_ShouldIssueAReceipt_AndTheLaterInvoiceShouldBeLinkedAndNotPending()
    {
        var payment = await _f.PayByCartAsync(_agency, AgencyTaxId, (PayableItemTypes.LocalCharge, _gateOut.Id));

        // Recibo del anticipo (además del cupón de retiro) con embarque, unidades y pagador.
        var settlement = _f.Db.ChargeSettlementList.Single();
        var receipt = _f.Db.ShipmentDocumentList.Single(d => d.DocumentType == ShipmentDocumentTypes.GateOutAdvanceReceipt);
        _f.Db.ShipmentDocumentList.Should().Contain(d => d.DocumentType == ShipmentDocumentTypes.GateOutCoupon);
        receipt.DocumentNumber.Should().StartWith(DocumentPrefixes.GateOutAdvanceReceipt);
        receipt.IssuedForOrganizationId.Should().Be(_agency.Organization.Id);
        receipt.ContainerNumbers.Should().Contain("HLXU3071801");
        receipt.TemplateJson.Should().Contain(_agency.Organization.Name).And.Contain(payment.PaymentNumber).And.Contain("BL-GO-EXP");
        settlement.Kind.Should().Be(SettlementKinds.Advance);
        settlement.PayerOrganizationId.Should().Be(_agency.Organization.Id);
        settlement.ReceiptDocumentId.Should().Be(receipt.Id);
        _gateOut.Status.Should().Be(ChargeStatus.Paid);

        // La factura se emite tras el zarpe; la actualización de facturas la cruza con el anticipo.
        var invoice = _f.AddInvoice(_agency.Organization, _bl, 60000m, 11400m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            ChargeConceptCodes.GateOut, sii: "100318");
        var provider = Substitute.For<IInvoiceProvider>();
        provider.GetAsync(default!, default).ReturnsForAnyArgs(Task.FromResult(Result<InvoiceDocument?>.Success(null)));
        var refreshed = await new RefreshInvoicesCommandHandler(_f.Db, _agency.Evaluator(_f.Db), provider)
            .Handle(new RefreshInvoicesCommand(null), CancellationToken.None);

        refreshed.IsSuccess.Should().BeTrue();
        settlement.Status.Should().Be(SettlementStatus.Matched);
        settlement.MatchedInvoiceId.Should().Be(invoice.Id);
        settlement.MatchedBy.Should().Be(RefreshInvoicesCommandHandler.MatchActor);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaymentId.Should().Be(payment.Id);

        // M7-01: la factura se presenta vinculada al recibo y no es pagable.
        var invoices = await new GetInvoicesQueryHandler(_f.Db, _agency.CurrentUser, _agency.Evaluator(_f.Db))
            .Handle(new GetInvoicesQuery(), CancellationToken.None);
        var view = invoices.Value.Items.Single(i => i.Id == invoice.Id);
        view.IsPayable.Should().BeFalse();
        view.CoveredBy!.ReceiptDocumentId.Should().Be(receipt.Id);
        view.CoveredBy.ReceiptDocumentNumber.Should().Be(receipt.DocumentNumber);
        view.CoveredBy.PaymentNumber.Should().Be(payment.PaymentNumber);

        // M7-03: cubierta, sin saldo; el cargo no vuelve a presentarse como pendiente ni se puede volver a pagar.
        var statement = (await _f.Statement(_agency).Handle(new GetAccountStatementQuery(), CancellationToken.None)).Value;
        statement.Lines.Single(l => l.SourceId == invoice.Id).Status.Should().Be(StatementStatuses.Covered);
        statement.Lines.Should().NotContain(l => l.SourceId == _gateOut.Id);
        statement.Summary.Sum(s => s.TotalBalance).Should().Be(0m);
        var again = await _f.Payments.Add(_agency).Handle(
            new AddCartItemCommand(PayableItemTypes.Invoice, invoice.Id, null, AgencyTaxId, null), CancellationToken.None);
        again.Error.Should().Be(DomainErrors.Cart.AlreadyPaid);

        // NF-04: el cruce queda disponible para Finanzas.
        var matched = await new GetSettlementsQueryHandler(_f.Db)
            .Handle(new GetSettlementsQuery(Status: SettlementStatus.Matched), CancellationToken.None);
        matched.Value.Single().MatchedInvoiceNumber.Should().Be("100318");
        matched.Value.Single().ReceiptDocumentNumber.Should().Be(receipt.DocumentNumber);
    }

    [Fact]
    public async Task InvoiceWithAnotherAmount_ShouldStayPending_UntilFinanceMatchesItManually()
    {
        await _f.PayByCartAsync(_agency, AgencyTaxId, (PayableItemTypes.LocalCharge, _gateOut.Id));
        var invoice = _f.AddInvoice(_agency.Organization, _bl, 65000m, 12350m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            ChargeConceptCodes.GateOut, sii: "100319");
        var settlement = _f.Db.ChargeSettlementList.Single();

        var automatic = await new MatchSettlementsCommandHandler(_f.Db, _f.Owner.CurrentUser).Handle(new MatchSettlementsCommand(), CancellationToken.None);
        var manual = await new MatchSettlementCommandHandler(_f.Db, _f.Owner.CurrentUser)
            .Handle(new MatchSettlementCommand(settlement.Id, invoice.Id, "Diferencia por tarifa"), CancellationToken.None);
        var twice = await new MatchSettlementCommandHandler(_f.Db, _f.Owner.CurrentUser)
            .Handle(new MatchSettlementCommand(settlement.Id, invoice.Id, null), CancellationToken.None);

        automatic.Value.Matched.Should().Be(0);
        manual.Value.Status.Should().Be(SettlementStatus.Matched);
        manual.Value.MatchNote.Should().Be("Diferencia por tarifa");
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        twice.Error.Should().Be(DomainErrors.Settlement.AlreadyMatched);
    }

    [Fact]
    public void CreditImputation_ShouldNotIssueAnAdvanceReceipt()
    {
        var detail = new PaymentDetail
        {
            ConceptType = ChargeConceptCodes.GateOut,
            Currency = "CLP",
            ItemType = PayableItemTypes.LocalCharge,
            SourceId = _gateOut.Id
        };
        var paid = new Payment { PaymentNumber = "PAY-1", PaymentType = PaymentOrigins.Cart, PaymentMethod = "KHIPU", Currency = "CLP", Status = PaymentStatus.Confirmed, Country = "CL", Origin = PaymentOrigins.Cart };
        var imputed = new Payment { PaymentNumber = "CRI-1", PaymentType = PaymentOrigins.CreditLine, PaymentMethod = PaymentMethodCodes.CreditLine, Currency = "CLP", Status = PaymentStatus.Confirmed, Country = "CL", Origin = PaymentOrigins.CreditLine };
        var import = AccessTestData.AddBl(_f.Db, _f.Owner.Organization.Id, "BL-GO-IMP");

        PaymentDocumentRules.DocumentTypesFor(paid, detail, _bl)
            .Should().Equal(ShipmentDocumentTypes.GateOutCoupon, ShipmentDocumentTypes.GateOutAdvanceReceipt);
        PaymentDocumentRules.DocumentTypesFor(imputed, detail, _bl).Should().Equal(ShipmentDocumentTypes.GateOutCoupon);
        PaymentDocumentRules.DocumentTypesFor(paid, detail, import).Should().Equal(ShipmentDocumentTypes.GateOutCoupon);
    }
}
