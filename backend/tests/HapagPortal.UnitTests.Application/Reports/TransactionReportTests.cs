namespace HapagPortal.UnitTests.Application.Reports;

using System.Text;
using FluentAssertions;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Reports.Transactions;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Reportería general (M9-01): transacciones confirmadas por servicio con totales por moneda, y excepciones aplicadas
/// (Gate In, EDS, Gate Out, IPO, cambio de almacén gratuito, imputación a crédito) con su BL y su cliente.
/// </summary>
public sealed class TransactionReportTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly IChargeRulesService _rules = Substitute.For<IChargeRulesService>();
    private readonly Client _creditCustomer;
    private readonly Client _cashCustomer;
    private readonly BillOfLading _bl1;
    private readonly BillOfLading _bl2;
    private readonly DateTime _now = DateTime.UtcNow;

    public TransactionReportTests()
    {
        _creditCustomer = AccessTestData.AddOrganization(_db, name: "Credito");
        _cashCustomer = AccessTestData.AddOrganization(_db, name: "Contado");
        _bl1 = AccessTestData.AddBl(_db, _creditCustomer.Id, "BL-1");
        _bl2 = AccessTestData.AddBl(_db, _cashCustomer.Id, "BL-2");

        _rules.GetConditionsAsync(Arg.Is<Client>(c => c.Id == _creditCustomer.Id), Arg.Any<CancellationToken>())
            .Returns(Conditions(_creditCustomer, credit: true));
        _rules.GetConditionsAsync(Arg.Is<Client>(c => c.Id == _cashCustomer.Id), Arg.Any<CancellationToken>())
            .Returns(Conditions(_cashCustomer, credit: false));

        _db.ChargeConceptList.Add(new ChargeConcept { Code = ChargeConceptCodes.Thc, Name = "THC", Category = ChargeCategories.LocalCharge });
        _db.ChargeConceptList.Add(new ChargeConcept { Code = ChargeConceptCodes.SealManagement, Name = "Sellos", Category = ChargeCategories.Service });
        _db.ServiceDefinitionList.Add(new ServiceDefinition
        {
            Code = "SEALS",
            NameEs = "Gestión de sellos",
            NameEn = "Seals",
            Operations = "Import",
            Countries = "CL",
            ActionCode = ShipmentActionCodes.PayOnDemandLocalCharges,
            InputSchemaJson = "[]",
            PricingMode = "Tariff"
        });
    }

    private static CommercialConditionsDto Conditions(Client client, bool credit) =>
        new(true, RuleSources.Nexus, client.TaxId, client.MatchCode, credit, credit ? 30 : null, [], null, null, false, false, credit, null);

    private TransactionReportBuilder Builder() => new(_db, _rules);

    private Payment AddPayment(Client payer, string currency, string status = PaymentStatus.Confirmed, string origin = PaymentOrigins.Cart,
        DateTime? confirmedAt = null, string number = "PAY")
    {
        var payment = new Payment
        {
            PaymentNumber = $"{number}-{_db.PaymentList.Count + 1}",
            PaymentType = "Cart",
            PaymentMethod = "KHIPU",
            Currency = currency,
            Status = status,
            Country = CountryCodes.Chile,
            Origin = origin,
            ClientId = payer.Id,
            ConfirmedAt = status == PaymentStatus.Confirmed ? confirmedAt ?? _now.AddHours(-1) : null,
            PaymentDate = confirmedAt ?? _now.AddHours(-1)
        };
        _db.PaymentList.Add(payment);
        return payment;
    }

    private PaymentDetail AddDetail(Payment payment, string itemType, string concept, decimal amount, decimal tax, BillOfLading bl, Guid? sourceId = null)
    {
        var detail = new PaymentDetail
        {
            PaymentId = payment.Id,
            ItemType = itemType,
            ConceptType = concept,
            Amount = amount,
            TaxAmount = tax,
            Currency = payment.Currency,
            BillOfLadingId = bl.Id,
            BlNumber = bl.BLNumber,
            SourceId = sourceId ?? Guid.NewGuid()
        };
        _db.PaymentDetailList.Add(detail);
        return detail;
    }

    private void SeedTransactions()
    {
        var clp = AddPayment(_creditCustomer, "CLP");
        AddDetail(clp, PayableItemTypes.LocalCharge, ChargeConceptCodes.Thc, 100m, 19m, _bl1);
        var serviceCharge = Guid.NewGuid();
        AddDetail(clp, PayableItemTypes.LocalCharge, ChargeConceptCodes.SealManagement, 50m, 9.5m, _bl1, serviceCharge);
        var request = new ServiceRequest
        {
            RequestNumber = "SRV-1",
            DefinitionCode = "SEALS",
            BlNumber = _bl1.BLNumber,
            Country = CountryCodes.Chile,
            Operation = "Import",
            Status = ServiceRequestStatus.Paid
        };
        _db.ServiceRequestList.Add(request);
        _db.ServiceRequestChargeList.Add(new ServiceRequestCharge { ServiceRequestId = request.Id, LocalChargeId = serviceCharge });

        var usd = AddPayment(_cashCustomer, "USD");
        AddDetail(usd, PayableItemTypes.Freight, PaymentConcepts.Freight, 1000m, 0m, _bl2);
        AddDetail(usd, PayableItemTypes.Demurrage, ChargeConceptCodes.Demurrage, 200m, 0m, _bl2);

        // Fuera del reporte: pago no confirmado, imputación a crédito y pago anterior al rango.
        AddDetail(AddPayment(_cashCustomer, "CLP", PaymentStatus.Pending), PayableItemTypes.LocalCharge, ChargeConceptCodes.Thc, 999m, 0m, _bl2);
        AddDetail(AddPayment(_creditCustomer, "CLP", origin: PaymentOrigins.CreditLine, number: "CRI"), PayableItemTypes.LocalCharge,
            ChargeConceptCodes.Isps, 300m, 57m, _bl1);
        AddDetail(AddPayment(_cashCustomer, "CLP", confirmedAt: _now.AddYears(-2)), PayableItemTypes.LocalCharge, ChargeConceptCodes.Thc, 777m, 0m, _bl2);
    }

    [Fact]
    public async Task Transactions_ShouldGroupByServiceAndCurrency_ExcludingCreditImputationsAndOtherStates()
    {
        SeedTransactions();

        var result = await new GetTransactionReportQueryHandler(Builder()).Handle(new GetTransactionReportQuery(
            DateOnly.FromDateTime(_now.AddDays(-30)), DateOnly.FromDateTime(_now.AddDays(1))), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value;
        report.Items.Total.Should().Be(4);
        report.Summary.Should().BeEquivalentTo(
        [
            new TransactionServiceSummaryDto(TransactionServiceCategories.LocalCharge, ChargeConceptCodes.Thc, "THC", "CLP", 1, 1, 100m, 19m, 119m),
            new TransactionServiceSummaryDto(TransactionServiceCategories.OnDemandService, "SEALS", "Gestión de sellos", "CLP", 1, 1, 50m, 9.5m, 59.5m),
            new TransactionServiceSummaryDto(TransactionServiceCategories.Demurrage, ChargeConceptCodes.Demurrage, ChargeConceptCodes.Demurrage, "USD", 1, 1, 200m, 0m, 200m),
            new TransactionServiceSummaryDto(TransactionServiceCategories.Freight, PaymentConcepts.Freight, "Flete", "USD", 1, 1, 1000m, 0m, 1000m)
        ]);
        report.Totals.Should().BeEquivalentTo(
        [
            new ReportCurrencyTotalDto("CLP", 1, 2, 178.5m),
            new ReportCurrencyTotalDto("USD", 1, 2, 1200m)
        ]);
        report.Items.Items.Single(i => i.Service == "SEALS").ServiceRequestNumber.Should().Be("SRV-1");
        report.Items.Items.Should().OnlyContain(i => i.Organization != null && i.BlNumber != null);
    }

    [Fact]
    public async Task Transactions_ShouldFilterByCategoryCurrencyOrganizationAndBl()
    {
        SeedTransactions();
        var from = DateOnly.FromDateTime(_now.AddDays(-30));
        var to = DateOnly.FromDateTime(_now.AddDays(1));
        var handler = new GetTransactionReportQueryHandler(Builder());

        var freight = await handler.Handle(new GetTransactionReportQuery(from, to, Category: TransactionServiceCategories.Freight), CancellationToken.None);
        var clp = await handler.Handle(new GetTransactionReportQuery(from, to, Currency: "clp"), CancellationToken.None);
        var organization = await handler.Handle(new GetTransactionReportQuery(from, to, OrganizationId: _cashCustomer.Id), CancellationToken.None);
        var paged = await handler.Handle(new GetTransactionReportQuery(from, to, Page: 2, PageSize: 3), CancellationToken.None);

        freight.Value.Items.Items.Should().ContainSingle().Which.Total.Should().Be(1000m);
        clp.Value.Items.Total.Should().Be(2);
        organization.Value.Items.Items.Should().OnlyContain(i => i.Organization!.Id == _cashCustomer.Id);
        paged.Value.Items.Items.Should().ContainSingle();
        paged.Value.Summary.Should().HaveCount(4);   // el resumen cubre todo el filtro, no solo la página
    }

    [Fact]
    public async Task Exceptions_ShouldIdentifyTheShipmentAndCustomerOfEachException()
    {
        _db.AppliedExemptionList.Add(new AppliedExemption
        {
            BillOfLadingId = _bl1.Id, BillOfLading = _bl1, ConceptCode = ChargeConceptCodes.GateIn, ExemptParty = ExemptionParties.FinalClient,
            PartyTaxId = _creditCustomer.TaxId, ExemptAmount = 95000m, Currency = "CLP", Source = RuleSources.Nexus,
            PayerClientId = _creditCustomer.Id, AppliedAt = _now.AddHours(-2)
        });
        _db.AppliedExemptionList.Add(new AppliedExemption
        {
            BillOfLadingId = _bl2.Id, BillOfLading = _bl2, ConceptCode = ChargeConceptCodes.Eds, ExemptParty = ExemptionParties.MasterConsignee,
            PartyTaxId = "99", ExemptAmount = 38000m, Currency = "CLP", ConditionAmount = 20000m, ConditionCurrency = "CLP",
            Source = RuleSources.Nexus, AppliedAt = _now.AddHours(-3)
        });
        _db.AppliedExemptionList.Add(new AppliedExemption
        {
            BillOfLadingId = _bl2.Id, BillOfLading = _bl2, ConceptCode = ChargeConceptCodes.GateOut, ExemptParty = ExemptionParties.FinalClient,
            PartyTaxId = "99", ExemptAmount = 60000m, Currency = "CLP", Source = RuleSources.Nexus, AppliedAt = _now.AddHours(-4)
        });
        _db.WarehouseChangeList.Add(new WarehouseChange
        {
            BillOfLadingId = _bl2.Id, FromWarehouse = "A", ToWarehouse = "B", Amount = 0m, Currency = "CLP", Status = WarehouseChangeStatus.Completed,
            Country = CountryCodes.Chile, IsFree = true, EntitlementSource = RuleSources.Portal, RequestedByClientId = _cashCustomer.Id,
            CreatedAt = _now.AddHours(-5)
        });
        // IPO de un cliente con crédito vigente (excluido) y de uno sin crédito (se cobra: no es excepción).
        _db.LocalChargeList.Add(new LocalCharge
        {
            ChargeType = ChargeConceptCodes.Ipo, Amount = 150m, TotalAmount = 150m, Currency = "USD", Status = ChargeStatus.Pending,
            BillOfLadingId = _bl1.Id, CreatedAt = _now.AddHours(-6)
        });
        _db.LocalChargeList.Add(new LocalCharge
        {
            ChargeType = ChargeConceptCodes.Ipo, Amount = 150m, TotalAmount = 150m, Currency = "USD", Status = ChargeStatus.Pending,
            BillOfLadingId = _bl2.Id, CreatedAt = _now.AddHours(-6)
        });
        AddDetail(AddPayment(_creditCustomer, "CLP", origin: PaymentOrigins.CreditLine, number: "CRI"), PayableItemTypes.LocalCharge,
            ChargeConceptCodes.Isps, 300m, 57m, _bl1);

        var result = await new GetExceptionReportQueryHandler(Builder()).Handle(new GetExceptionReportQuery(
            DateOnly.FromDateTime(_now.AddDays(-30)), DateOnly.FromDateTime(_now.AddDays(1))), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value;
        report.IpoExclusionsAvailable.Should().BeTrue();
        report.Items.Items.Select(i => i.Type).Should().BeEquivalentTo(
        [
            ReportExceptionTypes.GateInExemption, ReportExceptionTypes.EdsExemption, ReportExceptionTypes.GateOutExemption,
            ReportExceptionTypes.FreeWarehouseChange, ReportExceptionTypes.IpoExclusion, ReportExceptionTypes.CreditImputation
        ]);
        report.Items.Items.Should().OnlyContain(i => i.BlNumber != null && i.Organization != null);
        report.Items.Items.Single(i => i.Type == ReportExceptionTypes.IpoExclusion).BlNumber.Should().Be("BL-1");
        report.Items.Items.Single(i => i.Type == ReportExceptionTypes.CreditImputation).Amount.Should().Be(357m);
        report.Items.Items.Single(i => i.Type == ReportExceptionTypes.GateInExemption).Organization!.Id.Should().Be(_creditCustomer.Id);
        report.Summary.Single(s => s.Type == ReportExceptionTypes.GateInExemption).Amount.Should().Be(95000m);

        var onlyIpo = await new GetExceptionReportQueryHandler(Builder()).Handle(new GetExceptionReportQuery(
            DateOnly.FromDateTime(_now.AddDays(-30)), DateOnly.FromDateTime(_now.AddDays(1)), Type: ReportExceptionTypes.IpoExclusion), CancellationToken.None);
        onlyIpo.Value.Items.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Exceptions_WhenNexusIsDown_ShouldFlagIpoAsUnavailable()
    {
        _rules.GetConditionsAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>())
            .Returns(new CommercialConditionsDto(false, RuleSources.Nexus, "x", null, false, null, [], null, null, false, false, false, "Integration.Unavailable"));
        _db.LocalChargeList.Add(new LocalCharge
        {
            ChargeType = ChargeConceptCodes.Ipo, Amount = 150m, TotalAmount = 150m, Currency = "USD", Status = ChargeStatus.Pending,
            BillOfLadingId = _bl1.Id, CreatedAt = _now.AddHours(-1)
        });

        var result = await new GetExceptionReportQueryHandler(Builder()).Handle(new GetExceptionReportQuery(), CancellationToken.None);

        result.Value.IpoExclusionsAvailable.Should().BeFalse();
        result.Value.Items.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Export_ShouldProduceXlsxAndCsv()
    {
        SeedTransactions();
        var from = DateOnly.FromDateTime(_now.AddDays(-30));
        var to = DateOnly.FromDateTime(_now.AddDays(1));

        var xlsx = await new ExportTransactionReportQueryHandler(Builder()).Handle(new ExportTransactionReportQuery(from, to), CancellationToken.None);
        var csv = await new ExportTransactionReportQueryHandler(Builder()).Handle(
            new ExportTransactionReportQuery(from, to, Format: ReportExportFormats.Csv, Language: "en"), CancellationToken.None);
        var exceptions = await new ExportExceptionReportQueryHandler(Builder()).Handle(new ExportExceptionReportQuery(from, to), CancellationToken.None);

        xlsx.Value.FileName.Should().EndWith(".xlsx").And.StartWith("transacciones-");
        xlsx.Value.Content.Take(2).Should().Equal((byte)'P', (byte)'K');
        csv.Value.FileName.Should().StartWith("transactions-").And.EndWith(".csv");
        var text = Encoding.UTF8.GetString(csv.Value.Content);
        text.Should().Contain("Confirmed at").And.Contain("SEALS").And.Contain("1000");
        exceptions.Value.FileName.Should().StartWith("excepciones-");
    }

    [Fact]
    public void Validator_ShouldLimitTheRange()
    {
        var validator = new GetTransactionReportQueryValidator();

        validator.Validate(new GetTransactionReportQuery(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))).IsValid.Should().BeTrue();
        validator.Validate(new GetTransactionReportQuery(new DateOnly(2025, 1, 1), new DateOnly(2026, 12, 31))).IsValid.Should().BeFalse();
        validator.Validate(new GetTransactionReportQuery(new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1))).IsValid.Should().BeFalse();
        validator.Validate(new GetTransactionReportQuery(Country: "AR")).IsValid.Should().BeFalse();
    }
}
