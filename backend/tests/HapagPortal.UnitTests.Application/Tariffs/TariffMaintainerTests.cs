namespace HapagPortal.UnitTests.Application.Tariffs;

using System.Text.Json;
using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Tariffs.Common;
using HapagPortal.Application.Tariffs.Maintainer;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Mantenedor de tarifas (M8-01): registro de cambios con valor anterior, usuario y fecha (NF-15),
/// tarifa vigente por fecha local del país (NF-22), tramos de tiempo y precedencia portal → Nexus.
/// </summary>
public sealed class TariffMaintainerTests
{
    private readonly ChargeRulesFixture _f = new();

    private static CreateTariffCommand Kte(decimal amount = 9940m, DateOnly? from = null, DateOnly? to = null) => new(
        ChargeConceptCodes.WarehouseChange, "KTE", "CL", "CLP", null, "Cambio de almacén (KTE)", amount,
        null, null, null, from ?? new DateOnly(2026, 10, 1), to);

    [Fact]
    public async Task Create_ShouldPersistTariffAndLogCreationWithUser()
    {
        var handler = new CreateTariffCommandHandler(_f.Db, _f.CurrentUser);

        var result = await handler.Handle(Kte(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(9940m);
        result.Value.TierUnit.Should().Be(TariffTierUnits.None);
        var log = _f.Db.MaintainerChangeLogList.Single();
        log.Maintainer.Should().Be(MaintainerNames.Tariff);
        log.Action.Should().Be(MaintainerActions.Created);
        log.EntityId.Should().Be(result.Value.Id);
        log.PreviousValue.Should().BeNull();
        log.ChangedByUserId.Should().Be(_f.User.Id);
        log.ChangedBy.Should().Be(_f.User.Email);
        MaintainerChangeLogger.Read<TariffSnapshot>(log.NewValue)!.Amount.Should().Be(9940m);
    }

    [Fact]
    public async Task Update_ShouldLogPreviousAndNewValue_AndHistoryShouldExposeThem()
    {
        var created = await new CreateTariffCommandHandler(_f.Db, _f.CurrentUser).Handle(Kte(), CancellationToken.None);
        var id = created.Value.Id;

        var updated = await new UpdateTariffCommandHandler(_f.Db, _f.CurrentUser).Handle(
            new UpdateTariffCommand(id, ChargeConceptCodes.WarehouseChange, "KTE", "CL", "CLP", null, "Cambio de almacén (KTE)",
                10500m, null, null, null, new DateOnly(2026, 10, 1), null),
            CancellationToken.None);

        updated.Value.Amount.Should().Be(10500m);

        var history = await new GetTariffHistoryQueryHandler(_f.Db).Handle(new GetTariffHistoryQuery(id), CancellationToken.None);

        history.Value.Should().HaveCount(2);
        var update = history.Value.Single(h => h.Action == MaintainerActions.Updated);
        update.Previous!.Amount.Should().Be(9940m);
        update.Current!.Amount.Should().Be(10500m);
        update.ChangedByUserId.Should().Be(_f.User.Id);
    }

    [Fact]
    public async Task Deactivate_ShouldKeepHistoryAndStopApplying()
    {
        var created = await new CreateTariffCommandHandler(_f.Db, _f.CurrentUser).Handle(Kte(), CancellationToken.None);

        var result = await new DeactivateTariffCommandHandler(_f.Db, _f.CurrentUser)
            .Handle(new DeactivateTariffCommand(created.Value.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _f.Db.TariffList.Single().IsActive.Should().BeFalse();
        _f.Db.MaintainerChangeLogList.Should().Contain(l => l.Action == MaintainerActions.Deactivated);
        var inForce = await _f.TariffResolver().GetInForceAsync(
            new TariffLookup("CL", ChargeConceptCodes.WarehouseChange, new DateOnly(2026, 10, 20), Code: "KTE"));
        inForce.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_OverlappingValidity_ShouldFail()
    {
        var handler = new CreateTariffCommandHandler(_f.Db, _f.CurrentUser);
        await handler.Handle(Kte(), CancellationToken.None);

        var overlapping = await handler.Handle(Kte(10000m, new DateOnly(2026, 11, 1)), CancellationToken.None);

        overlapping.IsFailure.Should().BeTrue();
        overlapping.Error.Code.Should().Be("Tariff.Overlaps");
    }

    [Fact]
    public async Task Create_UnknownConcept_ShouldFail()
    {
        var result = await new CreateTariffCommandHandler(_f.Db, _f.CurrentUser).Handle(
            Kte() with { ConceptCode = "UNKNOWN" }, CancellationToken.None);

        result.Error.Code.Should().Be("ChargeConcept.NotFound");
    }

    [Fact]
    public void Validator_ShouldRejectOverlappingTiersAndTiersWithoutUnit()
    {
        var validator = new CreateTariffCommandValidator();

        var overlapping = validator.Validate(Kte() with
        {
            TierUnit = TariffTierUnits.Hours,
            Tiers = [new TariffTierDto(0, 24, 100m), new TariffTierDto(20, null, 200m)]
        });
        var withoutUnit = validator.Validate(Kte() with { Tiers = [new TariffTierDto(0, null, 100m)] });
        var valid = validator.Validate(Kte() with
        {
            TierUnit = TariffTierUnits.Hours,
            Tiers = [new TariffTierDto(0, 24, 100m), new TariffTierDto(25, null, 200m)]
        });

        overlapping.IsValid.Should().BeFalse();
        withoutUnit.IsValid.Should().BeFalse();
        valid.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("2026-10-01T02:30:00Z", 9500)]   // 30-09 23:30 en Santiago (UTC-3): tarifa anterior
    [InlineData("2026-10-01T03:30:00Z", 9940)]   // 01-10 00:30 en Santiago: tarifa nueva
    public async Task InForce_ShouldUseTheCountryLocalDate(string at, decimal expected)
    {
        _f.AddTariff(ChargeConceptCodes.WarehouseChange, 9500m, "CLP", code: "KTE", validTo: new DateOnly(2026, 9, 30));
        _f.AddTariff(ChargeConceptCodes.WarehouseChange, 9940m, "CLP", code: "KTE", validFrom: new DateOnly(2026, 10, 1));

        var result = await new GetTariffsInForceQueryHandler(_f.TariffResolver()).Handle(
            new GetTariffsInForceQuery("CL", ChargeConceptCodes.WarehouseChange, At: DateTime.Parse(at).ToUniversalTime(), Code: "KTE"),
            CancellationToken.None);

        result.Value.Should().ContainSingle().Which.Amount.Should().Be(expected);
        result.Value[0].TimeZone.Should().Be("America/Santiago");
    }

    [Fact]
    public async Task InForce_SameInstantInBolivia_ShouldStillBeThePreviousDay()
    {
        _f.AddTariff(ChargeConceptCodes.WarehouseChange, 800m, "BOB", country: "BO", validTo: new DateOnly(2026, 9, 30));
        _f.AddTariff(ChargeConceptCodes.WarehouseChange, 850m, "BOB", country: "BO", validFrom: new DateOnly(2026, 10, 1));

        // 03:30 UTC es 00:30 en Santiago pero 23:30 del día anterior en La Paz (UTC-4).
        var result = await new GetTariffsInForceQueryHandler(_f.TariffResolver()).Handle(
            new GetTariffsInForceQuery("BO", ChargeConceptCodes.WarehouseChange, At: new DateTime(2026, 10, 1, 3, 30, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        result.Value.Single().Amount.Should().Be(800m);
        result.Value.Single().LocalDate.Should().Be(new DateOnly(2026, 9, 30));
    }

    [Theory]
    [InlineData(10, 100)]
    [InlineData(30, 200)]
    [InlineData(72, 350)]
    public async Task TimeTiers_ShouldApplyTheTierOfTheElapsedHours(int hours, decimal expected)
    {
        _f.AddTariff(ChargeConceptCodes.LateArrival, 0m, "USD",
            tierUnit: TariffTierUnits.Hours, tierMode: TariffTierModes.Flat,
            tiers: [(0, 24, 100m), (25, 48, 200m), (49, null, 350m)]);
        var at = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        var result = await new GetTariffsInForceQueryHandler(_f.TariffResolver()).Handle(
            new GetTariffsInForceQuery("CL", ChargeConceptCodes.LateArrival, At: at, ElapsedFrom: at.AddHours(-hours)),
            CancellationToken.None);

        var tariff = result.Value.Single();
        tariff.MeasuredUnits.Should().Be(hours);
        tariff.ComputedAmount.Should().Be(expected);
        tariff.Covered.Should().BeTrue();
    }

    [Fact]
    public async Task AsOf_ShouldReconstructTheMaintainerFromTheChangeLog()
    {
        var tariff = _f.AddTariff(ChargeConceptCodes.WarehouseChange, 10500m, "CLP", code: "KTE");
        var before = TariffSnapshot.From(new Tariff { ConceptCode = tariff.ConceptCode, Code = "KTE", Country = "CL", Currency = "CLP", Amount = 9940m, ValidFrom = tariff.ValidFrom });
        var after = TariffSnapshot.From(tariff);
        var options = MaintainerChangeLogger.JsonOptions;
        _f.Db.MaintainerChangeLogList.Add(new MaintainerChangeLog
        {
            Maintainer = MaintainerNames.Tariff, EntityId = tariff.Id, Action = MaintainerActions.Created,
            NewValue = JsonSerializer.Serialize(before, options), ChangedAt = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc), ChangedBy = "admin"
        });
        _f.Db.MaintainerChangeLogList.Add(new MaintainerChangeLog
        {
            Maintainer = MaintainerNames.Tariff, EntityId = tariff.Id, Action = MaintainerActions.Updated,
            PreviousValue = JsonSerializer.Serialize(before, options), NewValue = JsonSerializer.Serialize(after, options),
            ChangedAt = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc), ChangedBy = "admin"
        });
        var date = new DateOnly(2026, 10, 2);

        var asOfBefore = await _f.TariffResolver().GetInForceAsync(new TariffLookup("CL", ChargeConceptCodes.WarehouseChange, date,
            AsOfUtc: new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc)));
        var current = await _f.TariffResolver().GetInForceAsync(new TariffLookup("CL", ChargeConceptCodes.WarehouseChange, date));

        asOfBefore.Single().Amount.Should().Be(9940m);
        current.Single().Amount.Should().Be(10500m);
    }

    [Fact]
    public async Task Precedence_ShouldPreferPortalTariffAndFallBackToNexus()
    {
        var date = new DateOnly(2026, 10, 20);
        _f.NexusTariffs.GetTariffsAsync("CL", ChargeConceptCodes.WarehouseChange, date, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<TariffItem>>.Success(
                [new TariffItem(ChargeConceptCodes.WarehouseChange, null, 9000m, "CLP", new DateOnly(2026, 10, 1), null)])));

        var nexusOnly = await _f.TariffResolver().GetInForceAsync(new TariffLookup("CL", ChargeConceptCodes.WarehouseChange, date));
        _f.AddTariff(ChargeConceptCodes.WarehouseChange, 9940m, "CLP", code: "KTE");
        var withPortal = await _f.TariffResolver().GetInForceAsync(new TariffLookup("CL", ChargeConceptCodes.WarehouseChange, date));

        nexusOnly.Single().Source.Should().Be(RuleSources.Nexus);
        nexusOnly.Single().Amount.Should().Be(9000m);
        withPortal.Single().Source.Should().Be(RuleSources.Portal);
        withPortal.Single().Amount.Should().Be(9940m);
    }

    [Fact]
    public async Task ContainerType_ShouldPreferTheSpecificTariff()
    {
        _f.AddTariff(ChargeConceptCodes.Demurrage, 0m, "CLP", containerType: "20DV", tierUnit: TariffTierUnits.CalendarDays,
            tierMode: TariffTierModes.PerUnit, tiers: [(1, 7, 0m), (8, null, 35000m)]);
        _f.AddTariff(ChargeConceptCodes.Demurrage, 0m, "CLP", tierUnit: TariffTierUnits.CalendarDays,
            tierMode: TariffTierModes.PerUnit, tiers: [(1, 7, 0m), (8, null, 45000m)]);

        var dv = await _f.TariffResolver().GetInForceAsync(new TariffLookup("CL", ChargeConceptCodes.Demurrage, new DateOnly(2026, 10, 1), "20DV"));
        var hc = await _f.TariffResolver().GetInForceAsync(new TariffLookup("CL", ChargeConceptCodes.Demurrage, new DateOnly(2026, 10, 1), "40HC"));

        dv.Single().ContainerType.Should().Be("20DV");
        hc.Single().ContainerType.Should().BeNull();
    }
}
