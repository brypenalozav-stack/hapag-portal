namespace HapagPortal.UnitTests.Application.Demurrage;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Demurrage.Advance;
using HapagPortal.Application.Demurrage.Calculate;
using HapagPortal.Application.Demurrage.State;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Demurrage por estado del BL (M3-18), MHD como concepto pagable (M3-02) y demoras anticipadas de
/// Bolivia con bloqueo del CLD y descuento del MHD (M3-16).
/// </summary>
public sealed class DemurrageStateTests
{
    private readonly ChargeRulesFixture _f = new();

    private GetDemurrageStatusQueryHandler Status() => new(_f.Db, _f.Evaluator, _f.DemurrageStatus());

    private CalculateDemurrageCommandHandler Calculator() =>
        new(_f.Db, _f.CurrentUser, _f.Evaluator, _f.TariffResolver(), _f.DemurrageStatus());

    private RequestAdvanceDemurrageCommandHandler Advance() =>
        new(_f.Db, _f.Evaluator, _f.ExchangeRates(), _f.DemurrageStatus());

    private DemurrageCharge AddLine(BillOfLading bl, string status, string? invoice = null, decimal total = 225000m)
    {
        var line = new DemurrageCharge
        {
            BillOfLadingId = bl.Id,
            ContainerNumber = "HLXU0000001",
            FreeDays = 7,
            DemurrageDays = 5,
            DailyRate = 45000m,
            TotalAmount = total,
            Currency = "CLP",
            StartDate = bl.ETA!.Value,
            EndDate = bl.ETA!.Value.AddDays(12),
            Status = status,
            InvoiceNumber = invoice,
            InvoicedAt = invoice is null ? null : new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc),
            InvoiceDueDate = invoice is null ? null : new DateTime(2026, 10, 15, 3, 0, 0, DateTimeKind.Utc)
        };
        _f.Db.DemurrageChargeList.Add(line);
        return line;
    }

    private void DemurrageTariff()
    {
        _f.AddTariff(ChargeConceptCodes.Demurrage, 0m, "CLP", tierUnit: TariffTierUnits.CalendarDays,
            tierMode: TariffTierModes.PerUnit, tiers: [(1, 7, 0m), (8, 14, 45000m), (15, null, 65000m)]);
    }

    [Fact]
    public async Task InvoicedWithDebt_ShouldOfferPayAndBlockTheCalculator()
    {
        var bl = _f.OwnBl("BL-INV");
        AddLine(bl, DemurrageChargeStatus.Invoiced, "FAC-DEM-1", 510000m);

        var status = (await Status().Handle(new GetDemurrageStatusQuery("BL-INV"), CancellationToken.None)).Value;

        status.State.Should().Be(DemurrageStates.InvoicedWithDebt);
        status.Action.Should().Be(ChargeActions.Pay);
        status.ActionAllowed.Should().BeTrue();
        status.CalculatorEnabled.Should().BeFalse();
        status.Invoices.Should().ContainSingle(i =>
            i.InvoiceNumber == "FAC-DEM-1" && i.Amount == 510000m && i.Currency == "CLP" && i.DueDate != null);
    }

    [Fact]
    public async Task CalculatedUnpaid_ShouldOfferAddToCart()
    {
        var bl = _f.OwnBl("BL-CALC");
        AddLine(bl, DemurrageChargeStatus.Pending);

        var status = (await Status().Handle(new GetDemurrageStatusQuery("BL-CALC"), CancellationToken.None)).Value;

        status.State.Should().Be(DemurrageStates.CalculatedUnpaid);
        status.Action.Should().Be(ChargeActions.AddToCart);
        status.CalculatorEnabled.Should().BeTrue();
        status.Invoices.Should().BeEmpty();
    }

    [Fact]
    public async Task NotCalculated_ShouldOfferCalculateWithTheInputs()
    {
        var bl = _f.OwnBl("BL-NOCALC");
        _f.AddContainer(bl, "HLXU0000002", "40HC");
        DemurrageTariff();

        var status = (await Status().Handle(new GetDemurrageStatusQuery("BL-NOCALC"), CancellationToken.None)).Value;

        status.State.Should().Be(DemurrageStates.NotCalculated);
        status.Action.Should().Be(ChargeActions.Calculate);
        status.CalculationInputs!.Containers.Should().ContainSingle(c => c.ContainerNumber == "HLXU0000002");
        status.CalculationInputs.DischargeDate.Should().Be(bl.ETA);
        status.CalculationInputs.TariffAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task NoDemurrage_ShouldStateThatThereIsNoDebt()
    {
        var notArrived = _f.OwnBl("BL-SEA", eta: DateTime.UtcNow.AddDays(10));
        var paid = _f.OwnBl("BL-PAID");
        AddLine(paid, DemurrageChargeStatus.Paid, "FAC-DEM-2");

        foreach (var number in new[] { notArrived.BLNumber, paid.BLNumber })
        {
            var status = (await Status().Handle(new GetDemurrageStatusQuery(number), CancellationToken.None)).Value;

            status.State.Should().Be(DemurrageStates.NoDemurrage);
            status.Action.Should().Be(ChargeActions.None);
            status.MessageCode.Should().Be("NO_DEBT");
            status.CalculatorEnabled.Should().BeFalse();
        }
    }

    [Fact]
    public async Task CustomerWithoutConsigneeRole_ShouldNotSeeImportDemurrage()
    {
        _f.OwnBl("BL-CUSTOMER-ONLY", consignee: false);

        var result = await Status().Handle(new GetDemurrageStatusQuery("BL-CUSTOMER-ONLY"), CancellationToken.None);

        result.Error.Should().Be(Error.Forbidden);
    }

    [Fact]
    public async Task Calculate_WithInvoice_ShouldBeRejected()
    {
        var bl = _f.OwnBl("BL-INV");
        AddLine(bl, DemurrageChargeStatus.Invoiced, "FAC-DEM-1");
        DemurrageTariff();

        var result = await Calculator().Handle(new CalculateDemurrageCommand("BL-INV"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Demurrage.InvoiceExists");
    }

    [Fact]
    public async Task Calculate_ShouldApplyDayTiersWithSourceFreeDaysAndSave()
    {
        var bl = _f.OwnBl("BL-NOCALC", eta: new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc));
        _f.AddContainer(bl, "HLXU0000002", "40HC");
        DemurrageTariff();
        _f.Source.GetByBlNumberAsync("BL-NOCALC", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<ShipmentRecord?>.Success(new ShipmentRecord(
                "BL-NOCALC", null, "IMPORT", null, null, null, null, 7, "SAAM", null, null, null))));

        // 17 días: 7 libres, 7 a 45.000 y 3 a 65.000.
        var result = await Calculator().Handle(
            new CalculateDemurrageCommand("BL-NOCALC", UntilDate: new DateOnly(2026, 9, 6)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var line = result.Value.Lines.Single();
        line.ElapsedDays.Should().Be(17);
        line.FreeDays.Should().Be(7);
        line.DemurrageDays.Should().Be(10);
        line.TotalAmount.Should().Be(510000m);
        line.TariffSource.Should().Be(RuleSources.Portal);
        result.Value.Saved.Should().BeTrue();
        _f.Db.DemurrageChargeList.Should().ContainSingle(d => d.TotalAmount == 510000m && d.Status == DemurrageChargeStatus.Pending);
        result.Value.Status!.State.Should().Be(DemurrageStates.CalculatedUnpaid);
    }

    [Fact]
    public async Task Calculate_WithoutTariff_ShouldFail()
    {
        var bl = _f.OwnBl("BL-NOTARIFF");
        _f.AddContainer(bl, "HLXU0000002", "40HC");

        var result = await Calculator().Handle(
            new CalculateDemurrageCommand("BL-NOTARIFF", UntilDate: new DateOnly(2026, 9, 20)), CancellationToken.None);

        result.Error.Code.Should().Be("Tariff.NotInForce");
    }

    [Fact]
    public async Task Mhd_ShouldBeAPayableConceptOfTheDemurrageTab()
    {
        var bl = _f.OwnBl("BL-MHD");
        var mhd = _f.AddCharge(bl, ChargeConceptCodes.Mhd, 85000m);

        var status = (await Status().Handle(new GetDemurrageStatusQuery("BL-MHD"), CancellationToken.None)).Value;

        var concept = status.OtherConcepts.Single();
        concept.ChargeId.Should().Be(mhd.Id);
        concept.Action.Should().Be(ChargeActions.AddToCart);
        concept.PayableTotal.Should().Be(101150m);
        status.Mhd!.NetPayable.Should().Be(101150m);
    }

    [Fact]
    public async Task AdvanceDemurrage_SubjectAccount_ShouldBlockCldUntilPaid()
    {
        _f.Organization.Country = CountryCodes.Bolivia;
        _f.Organization.TaxId = "1023456017";
        var bl = _f.OwnBl("BL-BO", country: CountryCodes.Bolivia);
        _f.AddContainer(bl, "HLXU0000003", "20DV");
        _f.AddRule(InternalChargeRuleTypes.AdvanceDemurrageRequired, CountryCodes.Bolivia, "1023456017", null);
        _f.AddTariff(ChargeConceptCodes.AdvanceDemurrageBo, 150m, "USD", country: CountryCodes.Bolivia);

        var status = (await Status().Handle(new GetDemurrageStatusQuery("BL-BO"), CancellationToken.None)).Value;

        status.Advance.Required.Should().BeTrue();
        status.Advance.Status.Should().Be(AdvanceDemurrageStatus.NotRequested);
        status.Advance.Amount.Should().Be(150m);
        status.Advance.Currency.Should().Be("USD");
        status.Advance.CldBlocked.Should().BeTrue();
        status.Requirements.Should().ContainSingle(r => r.Code == ProcessRequirements.AdvanceDemurrage && r.BlocksProcess);
    }

    [Fact]
    public async Task AdvanceDemurrage_Request_ShouldCreateTheChargeAndRecordTheExchangeRate()
    {
        _f.Organization.Country = CountryCodes.Bolivia;
        _f.Organization.TaxId = "1023456017";
        var bl = _f.OwnBl("BL-BO", country: CountryCodes.Bolivia);
        _f.AddContainer(bl, "HLXU0000003", "20DV");
        _f.AddContainer(bl, "HLXU0000004", "40HC");
        _f.AddRule(InternalChargeRuleTypes.AdvanceDemurrageRequired, CountryCodes.Bolivia, "1023456017", null);
        _f.AddTariff(ChargeConceptCodes.AdvanceDemurrageBo, 150m, "USD", country: CountryCodes.Bolivia);

        var first = await Advance().Handle(new RequestAdvanceDemurrageCommand("BL-BO"), CancellationToken.None);
        var second = await Advance().Handle(new RequestAdvanceDemurrageCommand("BL-BO"), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        var charge = _f.Db.LocalChargeList.Should().ContainSingle(c => c.ChargeType == ChargeConceptCodes.AdvanceDemurrageBo).Subject;
        charge.TotalAmount.Should().Be(300m);
        charge.Status.Should().Be(ChargeStatus.Pending);
        first.Value.Advance.Status.Should().Be(AdvanceDemurrageStatus.Pending);
        first.Value.Advance.CldBlocked.Should().BeTrue();

        var rate = _f.Db.ExchangeRateRecordList.Should().ContainSingle().Subject;
        rate.TransactionType.Should().Be(ExchangeRateTransactionTypes.LocalCharge);
        rate.TransactionId.Should().Be(charge.Id);
        rate.FromCurrency.Should().Be("USD");
        rate.ToCurrency.Should().Be("BOB");
        rate.Rate.Should().Be(6.91m);
        rate.ConvertedAmount.Should().Be(2073m);
    }

    [Fact]
    public async Task AdvanceDemurrage_Paid_ShouldUnblockCldAndBeDeductedFromMhd()
    {
        _f.Organization.Country = CountryCodes.Bolivia;
        _f.Organization.TaxId = "1023456017";
        var bl = _f.OwnBl("BL-BO", country: CountryCodes.Bolivia);
        _f.AddRule(InternalChargeRuleTypes.AdvanceDemurrageRequired, CountryCodes.Bolivia, "1023456017", null);
        _f.AddCharge(bl, ChargeConceptCodes.AdvanceDemurrageBo, 150m, currency: "USD", taxRate: 0m, status: ChargeStatus.Paid);
        _f.AddCharge(bl, ChargeConceptCodes.Mhd, 450m, currency: "USD", taxRate: 0m);

        var status = (await Status().Handle(new GetDemurrageStatusQuery("BL-BO"), CancellationToken.None)).Value;

        status.Advance.Status.Should().Be(AdvanceDemurrageStatus.Paid);
        status.Advance.CldBlocked.Should().BeFalse();
        status.Mhd!.Total.Should().Be(450m);
        status.Mhd.AdvanceDeducted.Should().Be(150m);
        status.Mhd.NetPayable.Should().Be(300m);
        status.OtherConcepts.Single(c => c.ConceptCode == ChargeConceptCodes.Mhd).PayableTotal.Should().Be(300m);
    }

    [Fact]
    public async Task AdvanceDemurrage_AccountNotSubject_ShouldNotBeRequired()
    {
        _f.Organization.Country = CountryCodes.Bolivia;
        var bl = _f.OwnBl("BL-BO", country: CountryCodes.Bolivia);
        _f.AddTariff(ChargeConceptCodes.AdvanceDemurrageBo, 150m, "USD", country: CountryCodes.Bolivia);

        var status = (await Status().Handle(new GetDemurrageStatusQuery("BL-BO"), CancellationToken.None)).Value;
        var request = await Advance().Handle(new RequestAdvanceDemurrageCommand("BL-BO"), CancellationToken.None);

        status.Advance.Required.Should().BeFalse();
        status.Advance.CldBlocked.Should().BeFalse();
        status.Requirements.Should().BeEmpty();
        request.Error.Code.Should().Be("Demurrage.AdvanceNotRequired");
        _f.Db.LocalChargeList.Should().NotContain(c => c.BillOfLadingId == bl.Id);
    }
}
