namespace HapagPortal.UnitTests.Application.Clients;

using FluentAssertions;
using HapagPortal.Application.Clients.Read.GetClientById;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class GetClientByIdQueryHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly GetClientByIdQueryHandler _handler;

    public GetClientByIdQueryHandlerTests()
    {
        _handler = new GetClientByIdQueryHandler(_dbContext);
    }

    [Fact]
    public async Task ExistingClient_ShouldReturnSuccess()
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

        var query = new GetClientByIdQuery(client.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Client");
        result.Value.Id.Should().Be(client.Id);
    }

    [Fact]
    public async Task NonExistingClient_ShouldReturnFailure()
    {
        var query = new GetClientByIdQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Client.NotFound");
    }
}
