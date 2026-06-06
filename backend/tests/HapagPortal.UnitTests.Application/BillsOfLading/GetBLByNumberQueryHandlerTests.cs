namespace HapagPortal.UnitTests.Application.BillsOfLading;

using FluentAssertions;
using HapagPortal.Application.BillsOfLading.Read.GetByNumber;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class GetBLByNumberQueryHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly GetBLByNumberQueryHandler _handler;

    public GetBLByNumberQueryHandlerTests()
    {
        _handler = new GetBLByNumberQueryHandler(_dbContext);
    }

    [Fact]
    public async Task ExistingBL_ShouldReturnSuccess()
    {
        var bl = new BillOfLading
        {
            BLNumber = "BL-001",
            ShipmentType = "Import",
            Vessel = "Test Vessel",
            Voyage = "V001",
            PortOfLoading = "Shanghai",
            PortOfDischarge = "Valparaiso",
            FreightAmount = 1500m,
            FreightCurrency = "USD",
            Status = "Active",
            Country = "CL",
            Containers = new List<BLContainer>
            {
                new()
                {
                    ContainerNumber = "CONT001",
                    ContainerType = "20GP",
                    SealNumber = "SEAL001",
                    Weight = 15000m,
                    Status = "Active"
                }
            }
        };

        _dbContext.BillsOfLadingList.Add(bl);

        var query = new GetBLByNumberQuery("BL-001");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BLNumber.Should().Be("BL-001");
        result.Value.Containers.Should().HaveCount(1);
    }

    [Fact]
    public async Task NonExistingBL_ShouldReturnNotFound()
    {
        var query = new GetBLByNumberQuery("BL-NONEXISTENT");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BillOfLading.NotFound");
    }
}
