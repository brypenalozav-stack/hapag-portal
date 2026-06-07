namespace HapagPortal.UnitTests.Application.Clients;

using FluentAssertions;
using HapagPortal.Application.Clients.Read.GetAllClients;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class GetAllClientsQueryHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly GetAllClientsQueryHandler _handler;

    public GetAllClientsQueryHandlerTests()
    {
        _handler = new GetAllClientsQueryHandler(_dbContext);
    }

    [Fact]
    public async Task NoFilter_ShouldReturnAll()
    {
        _dbContext.ClientList.AddRange(new[]
        {
            new Client { Name = "Client A", TaxId = "111", TaxIdType = "RUT", Country = "CL", Email = "a@test.com", ClientType = "Client", IsActive = true },
            new Client { Name = "Client B", TaxId = "222", TaxIdType = "NIT", Country = "BO", Email = "b@test.com", ClientType = "Client", IsActive = true },
            new Client { Name = "Client C", TaxId = "333", TaxIdType = "RUT", Country = "CL", Email = "c@test.com", ClientType = "Agent", IsActive = true }
        });

        var query = new GetAllClientsQuery(null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task FilterByCountry_ShouldReturnFiltered()
    {
        _dbContext.ClientList.AddRange(new[]
        {
            new Client { Name = "Client A", TaxId = "111", TaxIdType = "RUT", Country = "CL", Email = "a@test.com", ClientType = "Client", IsActive = true },
            new Client { Name = "Client B", TaxId = "222", TaxIdType = "NIT", Country = "BO", Email = "b@test.com", ClientType = "Client", IsActive = true },
            new Client { Name = "Client C", TaxId = "333", TaxIdType = "RUT", Country = "CL", Email = "c@test.com", ClientType = "Client", IsActive = true }
        });

        var query = new GetAllClientsQuery("CL");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(c => c.Country == "CL");
    }
}
