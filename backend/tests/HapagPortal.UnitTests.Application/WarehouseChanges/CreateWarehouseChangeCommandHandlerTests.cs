namespace HapagPortal.UnitTests.Application.WarehouseChanges;

using FluentAssertions;
using HapagPortal.Application.WarehouseChanges.Commands.Create;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class CreateWarehouseChangeCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly CreateWarehouseChangeCommandHandler _handler;

    public CreateWarehouseChangeCommandHandlerTests()
    {
        _handler = new CreateWarehouseChangeCommandHandler(_dbContext);
    }

    [Fact]
    public async Task ValidRequest_ShouldCreateChange()
    {
        var bl = new BillOfLading
        {
            BLNumber = "BL-001",
            ShipmentType = "Import",
            FreightAmount = 1500m,
            FreightCurrency = "USD",
            Status = "Active",
            Country = "CL"
        };

        _dbContext.BillsOfLadingList.Add(bl);

        var command = new CreateWarehouseChangeCommand("Warehouse A", "Warehouse B", bl.Id, "CL", 500m, "CLP");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FromWarehouse.Should().Be("Warehouse A");
        result.Value.ToWarehouse.Should().Be("Warehouse B");
        result.Value.Amount.Should().Be(500m);
        result.Value.Currency.Should().Be("CLP");
        result.Value.Status.Should().Be(PaymentStatus.Pending);
        result.Value.Country.Should().Be("CL");
        _dbContext.WarehouseChangeList.Should().HaveCount(1);
        _dbContext.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task BLNotFound_ShouldReturnFailure()
    {
        var command = new CreateWarehouseChangeCommand("Warehouse A", "Warehouse B", Guid.NewGuid(), "CL", 500m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BillOfLading.NotFound");
    }

    [Fact]
    public async Task DefaultsCurrencyFromCountry()
    {
        var bl = new BillOfLading
        {
            BLNumber = "BL-001",
            ShipmentType = "Import",
            FreightAmount = 1500m,
            FreightCurrency = "USD",
            Status = "Active",
            Country = "CL"
        };

        _dbContext.BillsOfLadingList.Add(bl);

        var command = new CreateWarehouseChangeCommand("Warehouse A", "Warehouse B", bl.Id, "CL", 500m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("CLP");
    }

    [Fact]
    public async Task DefaultsCurrencyFromCountry_Bolivia()
    {
        var bl = new BillOfLading
        {
            BLNumber = "BL-002",
            ShipmentType = "Import",
            FreightAmount = 1500m,
            FreightCurrency = "USD",
            Status = "Active",
            Country = "BO"
        };

        _dbContext.BillsOfLadingList.Add(bl);

        var command = new CreateWarehouseChangeCommand("Warehouse A", "Warehouse B", bl.Id, "BO", 500m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("BOB");
    }
}
