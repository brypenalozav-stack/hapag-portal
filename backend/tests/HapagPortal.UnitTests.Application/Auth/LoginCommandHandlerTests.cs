namespace HapagPortal.UnitTests.Application.Auth;

using FluentAssertions;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Auth.Login;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class LoginCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(_dbContext, _passwordHasher, _jwtTokenService);
    }

    [Fact]
    public async Task ValidCredentials_ShouldReturnSuccess()
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

        _passwordHasher.Verify("Password123", "hashed").Returns(true);
        _jwtTokenService.GenerateToken(Arg.Any<User>(), Arg.Any<IList<string>>()).Returns("jwt-token");
        _jwtTokenService.GenerateRefreshToken().Returns("refresh-token");

        var command = new LoginCommand("test@example.com", "Password123");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("jwt-token");
        result.Value.ExpiresIn.Should().Be(60);
        result.Value.User.Should().NotBeNull();
    }

    [Fact]
    public async Task UserNotFound_ShouldReturnFailure()
    {
        var command = new LoginCommand("nonexistent@example.com", "Password123");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidCredentials");
    }

    [Fact]
    public async Task InvalidPassword_ShouldReturnFailure()
    {
        var user = new User
        {
            Username = "test@example.com",
            Email = "test@example.com",
            PasswordHash = "hashed",
            UserType = "Client",
            Country = "CL",
            IsActive = true
        };

        _dbContext.UserList.Add(user);
        _passwordHasher.Verify("WrongPassword", "hashed").Returns(false);

        var command = new LoginCommand("test@example.com", "WrongPassword");

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

        var command = new LoginCommand("test@example.com", "Password123");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.Inactive");
    }
}
