namespace HapagPortal.UnitTests.Application.Users;

using FluentAssertions;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Users.Create;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class CreateUserCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed");
        _dbContext.RoleList.Add(new Role { Code = RoleCodes.Coordinador, Name = "Coordinador", IsSystem = true });
        _handler = new CreateUserCommandHandler(_dbContext, _passwordHasher);
    }

    private static CreateUserCommand Command(string email = "nuevo@hapag.cl", string role = RoleCodes.Coordinador) =>
        new("Ana", "Pérez", email, role, "+56911111111", null, "CL");

    [Fact]
    public async Task ValidData_ShouldCreateUserAndRole()
    {
        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("Ana Pérez");
        result.Value.DisplayId.Should().Be(1); // primer usuario
        result.Value.Roles.Should().ContainSingle().Which.Should().Be(RoleCodes.Coordinador);
        _dbContext.UserList.Should().ContainSingle();
        _dbContext.UserRoleList.Should().ContainSingle();
        _dbContext.UserList[0].Email.Should().Be("nuevo@hapag.cl");
        _dbContext.UserRoleList[0].RoleId.Should().NotBeNull();
    }

    [Fact]
    public async Task DuplicateEmail_ShouldReturnFailure()
    {
        _dbContext.UserList.Add(new User
        {
            Username = "nuevo@hapag.cl", Email = "nuevo@hapag.cl", PasswordHash = "h",
            UserType = "Coordinador", Country = "CL"
        });

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.EmailExists");
    }

    [Fact]
    public async Task UnknownRole_ShouldReturnFailure()
    {
        var result = await _handler.Handle(Command(role: "NoExiste"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Role.NotFound");
        _dbContext.UserList.Should().BeEmpty();
    }
}
