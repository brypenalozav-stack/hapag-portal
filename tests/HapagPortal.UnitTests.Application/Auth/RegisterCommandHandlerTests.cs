namespace HapagPortal.UnitTests.Application.Auth;

using FluentAssertions;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Auth.Register;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class RegisterCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _handler = new RegisterCommandHandler(_dbContext, _passwordHasher, _emailService);
    }

    [Fact]
    public async Task ValidRegistration_ShouldCreateClientAndUser()
    {
        _passwordHasher.Hash("Password1!").Returns("hashed-password");

        var command = new RegisterCommand(
            Name: "Test Client",
            TaxId: "12.345.678-9",
            Country: "CL",
            Email: "test@example.com",
            Password: "Password1!",
            ClientType: "Client",
            Phone: "+56912345678",
            AgentCode: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Client");
        result.Value.TaxId.Should().Be("12.345.678-9");
        result.Value.Country.Should().Be("CL");
        _dbContext.ClientList.Should().HaveCount(1);
        _dbContext.UserList.Should().HaveCount(1);
        _dbContext.UserRoleList.Should().HaveCount(1);
        _dbContext.SaveChangesCallCount.Should().Be(1);
        await _emailService.Received(1).SendEmailAsync(
            "test@example.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateTaxId_ShouldReturnFailure()
    {
        _dbContext.ClientList.Add(new Client
        {
            Name = "Existing",
            TaxId = "12.345.678-9",
            TaxIdType = "RUT",
            Country = "CL",
            Email = "existing@example.com",
            ClientType = "Client",
            IsActive = true
        });

        var command = new RegisterCommand(
            Name: "Test Client",
            TaxId: "12.345.678-9",
            Country: "CL",
            Email: "test@example.com",
            Password: "Password1!",
            ClientType: "Client",
            Phone: null,
            AgentCode: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Client.AlreadyExists");
    }
}
