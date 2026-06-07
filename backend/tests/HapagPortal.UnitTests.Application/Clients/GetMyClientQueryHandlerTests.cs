namespace HapagPortal.UnitTests.Application.Clients;

using FluentAssertions;
using HapagPortal.Application.Clients.Read.GetMyClient;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class GetMyClientQueryHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetMyClientQueryHandler _handler;

    public GetMyClientQueryHandlerTests()
    {
        _handler = new GetMyClientQueryHandler(_dbContext, _currentUserService);
    }

    [Fact]
    public async Task AuthenticatedUser_ShouldReturnClient()
    {
        var client = new Client
        {
            Name = "Test Client",
            TaxId = "12.345.678-9",
            TaxIdType = "RUT",
            Country = "CL",
            Email = "test@example.com",
            ClientType = "Client",
            IsActive = true
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

        var query = new GetMyClientQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Client");
        result.Value.TaxId.Should().Be("12.345.678-9");
    }

    [Fact]
    public async Task UnauthenticatedUser_ShouldReturnFailure()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var query = new GetMyClientQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Unauthorized");
    }

    [Fact]
    public async Task UserNotFound_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);

        var query = new GetMyClientQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task UserWithNoClient_ShouldReturnFailure()
    {
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
            ClientId = null
        };

        _dbContext.UserList.Add(user);
        _currentUserService.UserId.Returns((Guid?)userId);

        var query = new GetMyClientQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Client.NotFound");
    }
}
