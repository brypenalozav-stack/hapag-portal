namespace HapagPortal.UnitTests.Application.BillsOfLading;

using FluentAssertions;
using HapagPortal.Application.BillsOfLading.Read.GetContainers;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class GetContainersByBLQueryHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly GetContainersByBLQueryHandler _handler;

    public GetContainersByBLQueryHandlerTests()
    {
        _handler = new GetContainersByBLQueryHandler(_dbContext);
    }

    [Fact]
    public async Task ExistingBL_ShouldReturnContainers()
    {
        var bl = new BillOfLading
        {
            BLNumber = "BL-001",
            ShipmentType = "Import",
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
                },
                new()
                {
                    ContainerNumber = "CONT002",
                    ContainerType = "40HC",
                    SealNumber = "SEAL002",
                    Weight = 25000m,
                    Status = "Active"
                }
            }
        };

        _dbContext.BillsOfLadingList.Add(bl);

        var query = new GetContainersByBLQuery("BL-001");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].ContainerNumber.Should().Be("CONT001");
        result.Value[1].ContainerNumber.Should().Be("CONT002");
    }

    [Fact]
    public async Task NonExistingBL_ShouldReturnFailure()
    {
        var query = new GetContainersByBLQuery("BL-NONEXISTENT");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BillOfLading.NotFound");
    }
}
