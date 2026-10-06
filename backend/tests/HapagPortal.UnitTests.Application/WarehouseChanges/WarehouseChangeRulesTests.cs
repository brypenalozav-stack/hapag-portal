namespace HapagPortal.UnitTests.Application.WarehouseChanges;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.WarehouseChanges.Bulk;
using HapagPortal.Application.WarehouseChanges.Requests;
using HapagPortal.Domain.Constants;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Cambio de almacén con autogestión del cambio gratuito (M3-04), tarifa vigente KTE/KTF (M8-01) y
/// solicitudes masivas procesadas por tramos con avance consultable (M3-05, NF-19).
/// </summary>
public sealed class WarehouseChangeRulesTests
{
    private static readonly DateOnly From = new(2026, 1, 1);

    private readonly ChargeRulesFixture _f = new();

    public WarehouseChangeRulesTests()
    {
        _f.AddTariff(ChargeConceptCodes.WarehouseChange, 9940m, "CLP", code: "KTE", validFrom: new DateOnly(2026, 10, 1));
        _f.AddTariff(ChargeConceptCodes.WarehouseChange, 110910m, "CLP", code: "KTF", validFrom: new DateOnly(2026, 10, 1));
    }

    private RequestWarehouseChangeCommandHandler Request() =>
        new(_f.Db, _f.CurrentUser, _f.Evaluator, _f.WarehouseChanges());

    private static RequestWarehouseChangeCommand Change(string bl, string? tariffCode = null) =>
        new(bl, null, "STI San Antonio", "Bodega Central Santiago", tariffCode);

    [Fact]
    public async Task FreeEntitlementByInternalRule_ShouldCompleteWithoutChargeAndRespectUsesPerBl()
    {
        _f.OwnBl("BL-WC");
        _f.AddRule(InternalChargeRuleTypes.FreeWarehouseChange, CountryCodes.Chile, null, "MC100010", maxUses: 1);

        var free = await Request().Handle(Change("BL-WC"), CancellationToken.None);
        var second = await Request().Handle(Change("BL-WC"), CancellationToken.None);

        free.IsSuccess.Should().BeTrue();
        free.Value.IsFree.Should().BeTrue();
        free.Value.Amount.Should().Be(0m);
        free.Value.Status.Should().Be(WarehouseChangeStatus.Completed);
        free.Value.RequiresPayment.Should().BeFalse();
        free.Value.EntitlementSource.Should().Be(RuleSources.Portal);
        free.Value.CompletedAt.Should().NotBeNull();

        second.Value.IsFree.Should().BeFalse();
        second.Value.Amount.Should().Be(9940m);
        second.Value.TariffCode.Should().Be("KTE");
        second.Value.RequiresPayment.Should().BeTrue();
    }

    [Fact]
    public async Task FreeEntitlementFromNexus_ShouldCompleteWithNexusSource()
    {
        _f.OwnBl("BL-WC");
        _f.Exempt("76123456-7", new ExemptionInfo(ChargeConceptCodes.WarehouseChange, null, null, From, null));

        var result = await Request().Handle(Change("BL-WC"), CancellationToken.None);

        result.Value.IsFree.Should().BeTrue();
        result.Value.EntitlementSource.Should().Be(RuleSources.Nexus);
    }

    [Theory]
    [InlineData(null, "KTE", 9940)]
    [InlineData("KTF", "KTF", 110910)]
    public async Task WithoutEntitlement_ShouldChargeTheTariffInForce(string? requested, string expectedCode, decimal expected)
    {
        _f.OwnBl("BL-WC");

        var result = await Request().Handle(Change("BL-WC", requested), CancellationToken.None);

        result.Value.IsFree.Should().BeFalse();
        result.Value.Status.Should().Be(WarehouseChangeStatus.PendingPayment);
        result.Value.TariffCode.Should().Be(expectedCode);
        result.Value.Amount.Should().Be(expected);
        result.Value.Currency.Should().Be("CLP");
        result.Value.TariffSource.Should().Be(RuleSources.Portal);
    }

    [Fact]
    public async Task Quote_ShouldListTariffsAndEntitlement()
    {
        _f.OwnBl("BL-WC");

        var result = await new GetWarehouseChangeQuoteQueryHandler(_f.Db, _f.Evaluator, _f.WarehouseChanges())
            .Handle(new GetWarehouseChangeQuoteQuery("BL-WC"), CancellationToken.None);

        result.Value.Entitlement.IsFree.Should().BeFalse();
        result.Value.Tariffs.Select(t => t.Code).Should().Equal("KTE", "KTF");
        result.Value.DefaultTariffCode.Should().Be("KTE");
        result.Value.CanRequest.Should().BeTrue();
    }

    [Fact]
    public async Task CustomerWithoutConsigneeRole_ShouldNotRequest()
    {
        _f.OwnBl("BL-CUSTOMER-ONLY", consignee: false);

        var result = await Request().Handle(Change("BL-CUSTOMER-ONLY"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _f.Db.WarehouseChangeList.Should().BeEmpty();
    }

    [Fact]
    public async Task Bulk_ShouldValidateAccessOnSubmitAndProcessInChunks()
    {
        _f.OwnBl("BL-A");
        _f.OwnBl("BL-B");
        var foreign = AccessTestData.AddOrganization(_f.Db);
        AccessTestData.AddBl(_f.Db, foreign.Id, "BL-FOREIGN");
        _f.AddRule(InternalChargeRuleTypes.FreeWarehouseChange, CountryCodes.Chile, "76.123.456-7", null, maxUses: 1);

        var submit = await new SubmitWarehouseChangeBatchCommandHandler(_f.Db, _f.CurrentUser, _f.Evaluator).Handle(
            new SubmitWarehouseChangeBatchCommand(
            [
                new WarehouseChangeBatchItemRequest("BL-A", null, "STI", "Bodega 1"),
                new WarehouseChangeBatchItemRequest("BL-FOREIGN", null, "STI", "Bodega 1"),
                new WarehouseChangeBatchItemRequest("BL-B", null, "STI", "Bodega 2"),
            ]),
            CancellationToken.None);

        submit.IsSuccess.Should().BeTrue();
        submit.Value.Status.Should().Be(BulkRequestStatus.Queued);
        submit.Value.TotalItems.Should().Be(3);
        submit.Value.FailedItems.Should().Be(1);
        submit.Value.Items!.Single(i => i.BlNumber == "BL-FOREIGN").ErrorCode.Should().Be("BillOfLading.NotFound");
        _f.Db.WarehouseChangeList.Should().BeEmpty();

        var processor = new ProcessWarehouseChangeBatchesCommandHandler(_f.Db, _f.WarehouseChanges());
        var query = new GetWarehouseChangeBatchQueryHandler(_f.Db, _f.CurrentUser);

        (await processor.Handle(new ProcessWarehouseChangeBatchesCommand(MaxItems: 1), CancellationToken.None)).Value.Should().Be(1);
        var partial = (await query.Handle(new GetWarehouseChangeBatchQuery(submit.Value.Id), CancellationToken.None)).Value;
        partial.Status.Should().Be(BulkRequestStatus.Processing);
        partial.ProcessedItems.Should().Be(2);
        partial.ProgressPercent.Should().Be(66);

        (await processor.Handle(new ProcessWarehouseChangeBatchesCommand(), CancellationToken.None)).Value.Should().Be(1);
        var done = (await query.Handle(new GetWarehouseChangeBatchQuery(submit.Value.Id), CancellationToken.None)).Value;
        done.Status.Should().Be(BulkRequestStatus.CompletedWithErrors);
        done.ProgressPercent.Should().Be(100);
        done.SucceededItems.Should().Be(2);
        done.CompletedAt.Should().NotBeNull();
        done.Items!.Where(i => i.Status == BulkItemStatus.Succeeded).Should().OnlyContain(i => i.WarehouseChangeId != null);

        _f.Db.WarehouseChangeList.Should().HaveCount(2);
        _f.Db.WarehouseChangeList.Should().OnlyContain(w => w.IsFree && w.BatchId == submit.Value.Id && w.RequestedByUserId == _f.User.Id);

        var mine = await new GetWarehouseChangeBatchesQueryHandler(_f.Db, _f.CurrentUser)
            .Handle(new GetWarehouseChangeBatchesQuery(), CancellationToken.None);
        mine.Value.Should().ContainSingle(b => b.Id == submit.Value.Id);
    }

    [Fact]
    public void BulkValidator_ShouldLimitTheNumberOfLines()
    {
        var validator = new SubmitWarehouseChangeBatchCommandValidator();
        var tooMany = Enumerable.Range(1, 501)
            .Select(i => new WarehouseChangeBatchItemRequest($"BL-{i}", null, null, "Bodega"))
            .ToList();

        validator.Validate(new SubmitWarehouseChangeBatchCommand(tooMany)).IsValid.Should().BeFalse();
        validator.Validate(new SubmitWarehouseChangeBatchCommand([new("BL-1", null, null, "")])).IsValid.Should().BeFalse();
    }
}
