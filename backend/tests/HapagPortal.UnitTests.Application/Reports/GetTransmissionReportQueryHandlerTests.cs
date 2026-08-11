namespace HapagPortal.UnitTests.Application.Reports;

using FluentAssertions;
using HapagPortal.Application.Reports.Transmissions;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class GetTransmissionReportQueryHandlerTests
{
    private readonly MockApplicationDbContext _db = new();

    public GetTransmissionReportQueryHandlerTests()
    {
        _db.CustomsTransmissionList.AddRange(
            new CustomsTransmission { Id = Guid.NewGuid(), Stage = CustomsStages.Header, Kind = CustomsTransmissionKind.Original, Status = CustomsTransmissionStatus.Accepted },
            new CustomsTransmission { Id = Guid.NewGuid(), Stage = CustomsStages.Header, Kind = CustomsTransmissionKind.Original, Status = CustomsTransmissionStatus.Rejected });
    }

    [Fact]
    public async Task ReturnsAllWithColumns()
    {
        var handler = new GetTransmissionReportQueryHandler(_db);

        var result = await handler.Handle(new GetTransmissionReportQuery(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().Contain("Estado");
        result.Value.Rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task FiltersByStatus()
    {
        var handler = new GetTransmissionReportQueryHandler(_db);

        var result = await handler.Handle(
            new GetTransmissionReportQuery(CustomsTransmissionStatus.Rejected), CancellationToken.None);

        result.Value.Rows.Should().ContainSingle();
    }
}
