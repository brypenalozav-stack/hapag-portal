namespace HapagPortal.UnitTests.Application.Auth;

using FluentAssertions;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Auth.RefreshToken;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(_dbContext, _jwtTokenService);
    }

    [Fact]
    public async Task ValidRefreshToken_ShouldReturnNewTokens()
    {
        var user = new User
        {
            Username = "test@example.com",
            Email = "test@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL",
            IsActive = true,
            Client = new Client
            {
                Name = "Test Client",
                TaxId = "12.345.678-9",
                TaxIdType = "RUT",
                Country = "CL",
                Email = "test@example.com",
                ClientType = "Client",
                IsActive = true
            }
        };

        _dbContext.UserList.Add(user);
        _dbContext.UserRoleList.Add(new UserRole { UserId = user.Id, RoleName = "Client" });

        _jwtTokenService.GetEmailFromExpiredToken("expired-jwt").Returns("test@example.com");
        _jwtTokenService.GenerateToken(Arg.Any<User>(), Arg.Any<IList<string>>()).Returns("new-jwt-token");

        var command = new RefreshTokenCommand("expired-jwt", "valid-refresh-token");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("new-jwt-token");
        result.Value.ExpiresIn.Should().Be(60);
        result.Value.User.Should().NotBeNull();
    }

    [Fact]
    public async Task UserNotFound_ShouldReturnFailure()
    {
        _jwtTokenService.GetEmailFromExpiredToken("expired-jwt").Returns("nonexistent@example.com");

        var command = new RefreshTokenCommand("expired-jwt", "refresh-token");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidCredentials");
    }

    [Fact]
    public async Task InvalidRefreshToken_ShouldReturnFailure()
    {
        _jwtTokenService.GetEmailFromExpiredToken("invalid-jwt").Returns((string?)null);

        var command = new RefreshTokenCommand("invalid-jwt", "refresh-token");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidCredentials");
    }

    [Fact]
    public async Task InactiveUser_ShouldReturnFailure()
    {
        var user = new User
        {
            Username = "test@example.com",
            Email = "test@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL",
            IsActive = false
        };

        _dbContext.UserList.Add(user);

        _jwtTokenService.GetEmailFromExpiredToken("expired-jwt").Returns("test@example.com");

        var command = new RefreshTokenCommand("expired-jwt", "refresh-token");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.Inactive");
    }
}
