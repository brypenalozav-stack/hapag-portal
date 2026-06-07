namespace HapagPortal.UnitTests.Application.Auth;

using FluentAssertions;
using HapagPortal.Application.Auth.ConfirmEmail;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class ConfirmEmailCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly ConfirmEmailCommandHandler _handler;

    public ConfirmEmailCommandHandlerTests()
    {
        _handler = new ConfirmEmailCommandHandler(_dbContext);
    }

    [Fact]
    public async Task ValidToken_ShouldConfirmEmail()
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
            IsEmailConfirmed = false
        };

        var user = new User
        {
            Username = "test@example.com",
            Email = "test@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL",
            IsActive = true,
            EmailConfirmationToken = "confirm-token",
            ClientId = client.Id,
            Client = client
        };

        _dbContext.UserList.Add(user);
        _dbContext.ClientList.Add(client);

        var command = new ConfirmEmailCommand("test@example.com", "confirm-token");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        client.IsEmailConfirmed.Should().BeTrue();
        user.EmailConfirmationToken.Should().BeNull();
        _dbContext.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UserNotFound_ShouldReturnFailure()
    {
        var command = new ConfirmEmailCommand("nonexistent@example.com", "token");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task InvalidToken_ShouldReturnFailure()
    {
        var user = new User
        {
            Username = "test@example.com",
            Email = "test@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL",
            IsActive = true,
            EmailConfirmationToken = "correct-token"
        };

        _dbContext.UserList.Add(user);

        var command = new ConfirmEmailCommand("test@example.com", "wrong-token");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidToken");
    }
}
