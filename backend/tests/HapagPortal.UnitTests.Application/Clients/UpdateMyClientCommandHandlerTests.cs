namespace HapagPortal.UnitTests.Application.Clients;

using FluentAssertions;
using HapagPortal.Application.Clients.Update.UpdateMyClient;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class UpdateMyClientCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly UpdateMyClientCommandHandler _handler;

    public UpdateMyClientCommandHandlerTests()
    {
        _handler = new UpdateMyClientCommandHandler(_dbContext, _currentUserService);
    }

    [Fact]
    public async Task ValidUpdate_ShouldUpdateFields()
    {
        var client = new Client
        {
            Name = "Test Client",
            TaxId = "12.345.678-9",
            TaxIdType = "RUT",
            Country = "CL",
            Email = "test@example.com",
            ClientType = "Client",
            IsActive = true,
            Phone = "old-phone",
            Address = "old-address",
            City = "old-city"
        };

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "test@example.com",
            Email = "test@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL",
            IsActive = true,
            ClientId = client.Id
        };

        _dbContext.UserList.Add(user);
        _dbContext.ClientList.Add(client);
        _currentUserService.UserId.Returns((Guid?)userId);

        var command = new UpdateMyClientCommand("+56912345678", "New Address 123", "Santiago");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        client.Phone.Should().Be("+56912345678");
        client.Address.Should().Be("New Address 123");
        client.City.Should().Be("Santiago");
        _dbContext.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UnauthenticatedUser_ShouldReturnFailure()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var command = new UpdateMyClientCommand("+56912345678", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Unauthorized");
    }

    [Fact]
    public async Task OnlyUpdatesProvidedFields()
    {
        var client = new Client
        {
            Name = "Test Client",
            TaxId = "12.345.678-9",
            TaxIdType = "RUT",
            Country = "CL",
            Email = "test@example.com",
            ClientType = "Client",
            IsActive = true,
            Phone = "original-phone",
            Address = "original-address",
            City = "original-city"
        };

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "test@example.com",
            Email = "test@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL",
            IsActive = true,
            ClientId = client.Id
        };

        _dbContext.UserList.Add(user);
        _dbContext.ClientList.Add(client);
        _currentUserService.UserId.Returns((Guid?)userId);

        var command = new UpdateMyClientCommand("+56999999999", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        client.Phone.Should().Be("+56999999999");
        client.Address.Should().Be("original-address");
        client.City.Should().Be("original-city");
    }
}
