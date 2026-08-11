namespace HapagPortal.UnitTests.Application.BillsOfLading;

using FluentAssertions;
using HapagPortal.Application.BillsOfLading.Import;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class ImportBillsOfLadingCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly Guid _clientId = Guid.NewGuid();

    public ImportBillsOfLadingCommandHandlerTests()
    {
        _dbContext.ClientList.Add(new Client
        {
            Id = _clientId,
            Name = "ACME",
            Country = "CL",
            TaxId = "76.111.111-1",
            TaxIdType = "RUT",
            Email = "acme@example.com",
            ClientType = "Company"
        });
    }

    private ImportBillRow ValidRow() => new(
        ClientId: _clientId,
        BLNumber: "HLCUABC123",
        ShipmentType: "Import",
        Country: "CL",
        PortOfLoading: "CNSHA",
        PortOfDischarge: "CLVAP",
        FreightCurrency: "USD",
        ConsigneeName: "Importadora Ltda",
        ConsigneeTaxId: "12.345.678-5",
        HsCode: "870323",
        GrossWeight: 15000m,
        ContainerNumber: "HLXU1234567",
        ContainerIsoType: "22G1");

    [Fact]
    public async Task ValidRow_CreatesBillWithPartyCargoAndContainer()
    {
        var handler = new ImportBillsOfLadingCommandHandler(_dbContext);

        var result = await handler.Handle(
            new ImportBillsOfLadingCommand([ValidRow()]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().Be(1);
        result.Value.Failed.Should().Be(0);
        _dbContext.BillsOfLadingList.Should().ContainSingle();
        _dbContext.BLPartyList.Should().ContainSingle();
        _dbContext.BLCargoItemList.Should().ContainSingle();
        _dbContext.BLContainerList.Should().ContainSingle();
        _dbContext.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task InvalidRut_IsRejectedAndNotPersisted()
    {
        var row = ValidRow() with { ConsigneeTaxId = "12.345.678-9" };
        var handler = new ImportBillsOfLadingCommandHandler(_dbContext);

        var result = await handler.Handle(
            new ImportBillsOfLadingCommand([row]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().Be(0);
        result.Value.Failed.Should().Be(1);
        result.Value.Errors.Should().ContainSingle();
        result.Value.Errors[0].Messages.Should().Contain(m => m.Contains("RUT"));
        _dbContext.BillsOfLadingList.Should().BeEmpty();
    }

    [Fact]
    public async Task UnknownClientAndMissingHs_AreRejected()
    {
        var row = ValidRow() with { ClientId = Guid.NewGuid(), HsCode = null };
        var handler = new ImportBillsOfLadingCommandHandler(_dbContext);

        var result = await handler.Handle(
            new ImportBillsOfLadingCommand([row]), CancellationToken.None);

        result.Value.Created.Should().Be(0);
        result.Value.Errors[0].Messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task MixedBatch_PersistsValidRowsOnly()
    {
        var bad = ValidRow() with { BLNumber = "HLCUXYZ999", Country = "XX" };
        var handler = new ImportBillsOfLadingCommandHandler(_dbContext);

        var result = await handler.Handle(
            new ImportBillsOfLadingCommand([ValidRow(), bad]), CancellationToken.None);

        result.Value.Created.Should().Be(1);
        result.Value.Failed.Should().Be(1);
        _dbContext.BillsOfLadingList.Should().ContainSingle();
    }
}
