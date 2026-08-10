namespace HapagPortal.UnitTests.Application.BillsOfLading;

using FluentAssertions;
using HapagPortal.Application.BillsOfLading.Read.GetAll;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class GetAllBLsQueryHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetAllBLsQueryHandler _handler;

    public GetAllBLsQueryHandlerTests()
    {
        // Estos tests ejercitan el comportamiento de Admin (ver todo / filtrar por clientId).
        _currentUser.Roles.Returns(new[] { "Admin" });
        _handler = new GetAllBLsQueryHandler(_dbContext, _currentUser);
    }

    private static BillOfLading CreateBL(string blNumber, string country, Guid? clientId = null) => new()
    {
        BLNumber = blNumber,
        ShipmentType = "Import",
        Vessel = "Test Vessel",
        Voyage = "V001",
        PortOfLoading = "Shanghai",
        PortOfDischarge = "Valparaiso",
        FreightAmount = 1500m,
        FreightCurrency = "USD",
        Status = "Active",
        Country = country,
        ClientId = clientId ?? Guid.NewGuid()
    };

    [Fact]
    public async Task NoFilters_ShouldReturnAll()
    {
        _dbContext.BillsOfLadingList.Add(CreateBL("BL-001", "CL"));
        _dbContext.BillsOfLadingList.Add(CreateBL("BL-002", "BO"));
        _dbContext.BillsOfLadingList.Add(CreateBL("BL-003", "CL"));

        var query = new GetAllBLsQuery(null, null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task FilterByCountry_ShouldReturnFiltered()
    {
        _dbContext.BillsOfLadingList.Add(CreateBL("BL-001", "CL"));
        _dbContext.BillsOfLadingList.Add(CreateBL("BL-002", "BO"));
        _dbContext.BillsOfLadingList.Add(CreateBL("BL-003", "CL"));

        var query = new GetAllBLsQuery("CL", null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(bl => bl.Country == "CL");
    }

    [Fact]
    public async Task FilterByClientId_ShouldReturnFiltered()
    {
        var clientId = Guid.NewGuid();
        _dbContext.BillsOfLadingList.Add(CreateBL("BL-001", "CL", clientId));
        _dbContext.BillsOfLadingList.Add(CreateBL("BL-002", "CL"));
        _dbContext.BillsOfLadingList.Add(CreateBL("BL-003", "CL", clientId));

        var query = new GetAllBLsQuery(null, clientId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(bl => bl.ClientId == clientId);
    }

    [Fact]
    public async Task EmptyList_ShouldReturnEmptyList()
    {
        var query = new GetAllBLsQuery(null, null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task NonAdmin_ShouldOnlySeeOwnClientBLs()
    {
        var ownClient = Guid.NewGuid();
        var nonAdmin = Substitute.For<ICurrentUserService>();
        nonAdmin.Roles.Returns([]);
        nonAdmin.ClientId.Returns(ownClient);
        var handler = new GetAllBLsQueryHandler(_dbContext, nonAdmin);

        _dbContext.BillsOfLadingList.Add(CreateBL("BL-001", "CL", ownClient));
        _dbContext.BillsOfLadingList.Add(CreateBL("BL-002", "CL")); // otro cliente
        _dbContext.BillsOfLadingList.Add(CreateBL("BL-003", "CL", ownClient));

        // Aunque pida el clientId de otro, se ignora y solo ve los suyos (BUG-7).
        var query = new GetAllBLsQuery(null, Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(bl => bl.ClientId == ownClient);
    }
}
