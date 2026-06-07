namespace HapagPortal.UnitTests.Application.Admin;

using FluentAssertions;
using HapagPortal.Application.Admin.CreditClients.Commands.Create;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class CreateCreditClientCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly CreateCreditClientCommandHandler _handler;

    public CreateCreditClientCommandHandlerTests()
    {
        _currentUserService.Email.Returns("admin@example.com");
        _handler = new CreateCreditClientCommandHandler(_dbContext, _currentUserService);
    }

    [Fact]
    public async Task ValidRequest_ShouldCreateCreditClient()
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

        _dbContext.ClientList.Add(client);

        var command = new CreateCreditClientCommand(client.Id, "CL", 50000m, DateTime.UtcNow.AddYears(1));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Client");
        result.Value.CreditLimit.Should().Be(50000m);
        result.Value.Country.Should().Be("CL");
        result.Value.ApprovedBy.Should().Be("admin@example.com");
        _dbContext.CreditClientList.Should().HaveCount(1);
        _dbContext.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ClientNotFound_ShouldReturnFailure()
    {
        var command = new CreateCreditClientCommand(Guid.NewGuid(), "CL", 50000m, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Client.NotFound");
    }
}
