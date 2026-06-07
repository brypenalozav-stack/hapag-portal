namespace HapagPortal.UnitTests.Application.Auth;

using FluentAssertions;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Auth.ResetPassword;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class ResetPasswordCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _handler = new ResetPasswordCommandHandler(_dbContext, _passwordHasher);
    }

    [Fact]
    public async Task ValidToken_ShouldResetPassword()
    {
        var user = new User
        {
            Username = "test@example.com",
            Email = "test@example.com",
            PasswordHash = "old-hash",
            UserType = "Client",
            Country = "CL",
            IsActive = true,
            PasswordResetToken = "valid-token",
            PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1)
        };

        _dbContext.UserList.Add(user);
        _passwordHasher.Hash("NewPassword1!").Returns("new-hash");

        var command = new ResetPasswordCommand("test@example.com", "valid-token", "NewPassword1!");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hash");
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiry.Should().BeNull();
        _dbContext.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UserNotFound_ShouldReturnFailure()
    {
        var command = new ResetPasswordCommand("nonexistent@example.com", "token", "NewPassword1!");

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
            PasswordResetToken = "correct-token",
            PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1)
        };

        _dbContext.UserList.Add(user);

        var command = new ResetPasswordCommand("test@example.com", "wrong-token", "NewPassword1!");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidToken");
    }

    [Fact]
    public async Task ExpiredToken_ShouldReturnFailure()
    {
        var user = new User
        {
            Username = "test@example.com",
            Email = "test@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL",
            IsActive = true,
            PasswordResetToken = "valid-token",
            PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(-1)
        };

        _dbContext.UserList.Add(user);

        var command = new ResetPasswordCommand("test@example.com", "valid-token", "NewPassword1!");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.TokenExpired");
    }
}
