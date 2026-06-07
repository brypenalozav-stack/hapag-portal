namespace HapagPortal.UnitTests.Application.ServiceOrders;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.ServiceOrders.Commands.Create;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class CreateServiceOrderCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly CreateServiceOrderCommandHandler _handler;

    public CreateServiceOrderCommandHandlerTests()
    {
        _handler = new CreateServiceOrderCommandHandler(_dbContext, _currentUserService);
    }

    [Fact]
    public async Task ValidRequest_ShouldCreateOrder()
    {
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var bl = new BillOfLading
        {
            BLNumber = "BL-001",
            ShipmentType = "Import",
            FreightAmount = 1500m,
            FreightCurrency = "USD",
            Status = "Active",
            Country = "CL",
            ClientId = clientId
        };

        var user = new User
        {
            Id = userId,
            Username = "test@example.com",
            Email = "test@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL",
            IsActive = true,
            ClientId = clientId
        };

        _dbContext.UserList.Add(user);
        _dbContext.BillsOfLadingList.Add(bl);
        _currentUserService.UserId.Returns((Guid?)userId);

        var command = new CreateServiceOrderCommand("WarehouseChange", "Test description", bl.Id, "CL");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderType.Should().Be("WarehouseChange");
        result.Value.Status.Should().Be("Requested");
        result.Value.Country.Should().Be("CL");
        result.Value.BillOfLadingId.Should().Be(bl.Id);
        result.Value.ClientId.Should().Be(clientId);
        _dbContext.ServiceOrderList.Should().HaveCount(1);
        _dbContext.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UnauthenticatedUser_ShouldReturnFailure()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var command = new CreateServiceOrderCommand("WarehouseChange", null, Guid.NewGuid(), "CL");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Unauthorized");
    }

    [Fact]
    public async Task BLNotFound_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Username = "test@example.com",
            Email = "test@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL",
            IsActive = true,
            ClientId = clientId
        };

        _dbContext.UserList.Add(user);
        _currentUserService.UserId.Returns((Guid?)userId);

        var command = new CreateServiceOrderCommand("WarehouseChange", null, Guid.NewGuid(), "CL");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BillOfLading.NotFound");
    }
}
