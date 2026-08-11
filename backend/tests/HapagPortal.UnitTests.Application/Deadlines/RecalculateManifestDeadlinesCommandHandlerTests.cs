namespace HapagPortal.UnitTests.Application.Deadlines;

using FluentAssertions;
using HapagPortal.Application.Deadlines.Recalculate;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class RecalculateManifestDeadlinesCommandHandlerTests
{
    private readonly MockApplicationDbContext _db = new();

    private CustomsManifest Seed()
    {
        var manifest = new CustomsManifest
        {
            Id = Guid.NewGuid(),
            VesselImo = "9321483",
            Voyage = "V1",
            Port = "CLVAP",
            Direction = CustomsDirections.Ingreso,
            EstimatedArrival = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        _db.CustomsManifestList.Add(manifest);

        _db.DeadlineRuleList.Add(new DeadlineRule
        {
            Id = Guid.NewGuid(), Code = "MANIFEST_HEADER_IN", Name = "Encabezado",
            BaseEvent = DeadlineBaseEvents.ArrivalEstimated, OffsetHours = -168, AtRiskWindowHours = 48,
            Direction = "Ingreso", Severity = DeadlineSeverity.High, Certainty = DeadlineCertainty.Confirmed
        });
        _db.DeadlineRuleList.Add(new DeadlineRule
        {
            Id = Guid.NewGuid(), Code = "BL_MASTER_IN", Name = "B/L Máster",
            BaseEvent = DeadlineBaseEvents.ArrivalEstimated, OffsetHours = -48, AtRiskWindowHours = 12,
            Direction = "Ingreso", BLType = "Master", Severity = DeadlineSeverity.High, Certainty = DeadlineCertainty.ToVerify
        });
        // Regla que no aplica: dirección Salida.
        _db.DeadlineRuleList.Add(new DeadlineRule
        {
            Id = Guid.NewGuid(), Code = "MANIFEST_HEADER_OUT", Name = "Encabezado salida",
            BaseEvent = DeadlineBaseEvents.DepartureEstimated, OffsetHours = -48, AtRiskWindowHours = 12,
            Direction = "Salida", Severity = DeadlineSeverity.High, Certainty = DeadlineCertainty.Confirmed
        });

        var bl = new BillOfLading
        {
            Id = Guid.NewGuid(), BLNumber = "HLCUM1", ShipmentType = "Import",
            FreightCurrency = "USD", Status = "Active", Country = "CL", BLType = "Master"
        };
        _db.BillsOfLadingList.Add(bl);
        _db.CustomsTransmissionList.Add(new CustomsTransmission
        {
            Id = Guid.NewGuid(), ManifestId = manifest.Id, BillOfLadingId = bl.Id,
            Stage = CustomsStages.BillOfLading, Kind = CustomsTransmissionKind.Original,
            Status = CustomsTransmissionStatus.Accepted
        });

        return manifest;
    }

    [Fact]
    public async Task Recalculate_CreatesManifestAndBLDeadlines_SkippingWrongDirection()
    {
        var manifest = Seed();
        var handler = new RecalculateManifestDeadlinesCommandHandler(_db);

        var result = await handler.Handle(
            new RecalculateManifestDeadlinesCommand(manifest.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Encabezado (nivel manifiesto) + B/L Máster; la regla de salida no aplica.
        _db.DeadlineInstanceList.Should().HaveCount(2);
        _db.DeadlineInstanceList.Should().Contain(d => d.BillOfLadingId == null);   // manifiesto
        _db.DeadlineInstanceList.Should().Contain(d => d.BillOfLadingId != null);   // B/L
    }

    [Fact]
    public async Task Recalculate_IsIdempotent()
    {
        var manifest = Seed();
        var handler = new RecalculateManifestDeadlinesCommandHandler(_db);

        await handler.Handle(new RecalculateManifestDeadlinesCommand(manifest.Id), CancellationToken.None);
        await handler.Handle(new RecalculateManifestDeadlinesCommand(manifest.Id), CancellationToken.None);

        _db.DeadlineInstanceList.Should().HaveCount(2);
    }

    [Fact]
    public async Task Recalculate_ManifestNotFound_Fails()
    {
        var handler = new RecalculateManifestDeadlinesCommandHandler(_db);

        var result = await handler.Handle(
            new RecalculateManifestDeadlinesCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customs.ManifestNotFound");
    }
}
