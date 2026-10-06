namespace HapagPortal.UnitTests.Infrastructure.Persistence;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Tariffs.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using HapagPortal.Infrastructure.Persistence;
using HapagPortal.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>
/// Semilla de Fase 1 Ola C sobre un proveedor EF real (InMemory): catálogo coherente con los recargos
/// sembrados, tarifas KTE/KTF, tramos, registro de cambios (NF-15), reglas internas y demurrage facturado.
/// </summary>
public sealed class ChargeRulesSeedTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public ChargeRulesSeedTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Catalog_ShouldCoverEverySeededLocalChargeCode()
    {
        var concepts = await _context.ChargeConcepts.Select(c => c.Code).ToListAsync();
        var chargeCodes = await _context.LocalCharges.Select(c => c.ChargeType).Distinct().ToListAsync();

        chargeCodes.Should().OnlyContain(code => concepts.Contains(code));
        concepts.Should().Contain([
            ChargeConceptCodes.GateIn, ChargeConceptCodes.Eds, ChargeConceptCodes.GateOut, ChargeConceptCodes.Ipo,
            ChargeConceptCodes.Mhd, ChargeConceptCodes.Demurrage, ChargeConceptCodes.WarehouseChange,
            ChargeConceptCodes.AdvanceDemurrageBo]);
    }

    [Fact]
    public async Task WarehouseChangeTariffs_KteAndKtf_ShouldBeInForceFromOctoberFirst()
    {
        var nexus = Substitute.For<ITariffProvider>();
        nexus.GetTariffsAsync(default!, default!, default, default)
            .ReturnsForAnyArgs(Task.FromResult(Result<IReadOnlyList<TariffItem>>.Success([])));
        var resolver = new TariffResolver(_context, nexus);

        var before = await resolver.GetInForceAsync(new TariffLookup("CL", ChargeConceptCodes.WarehouseChange, new DateOnly(2026, 9, 30)));
        var after = await resolver.GetInForceAsync(new TariffLookup("CL", ChargeConceptCodes.WarehouseChange, new DateOnly(2026, 10, 1)));

        before.Should().BeEmpty();
        after.Select(t => (t.Code, t.Amount, t.Currency)).Should().Equal(("KTE", 9940m, "CLP"), ("KTF", 110910m, "CLP"));
    }

    [Fact]
    public async Task SeededTariffs_ShouldHaveTheirCreationInTheChangeLog()
    {
        var tariffs = await _context.Tariffs.Include(t => t.Tiers).ToListAsync();
        var logs = await _context.MaintainerChangeLogs.Where(l => l.Maintainer == MaintainerNames.Tariff).ToListAsync();

        logs.Select(l => l.EntityId).Should().BeEquivalentTo(tariffs.Select(t => t.Id));
        var lateArrival = tariffs.Single(t => t.Id == SeedDataIds.TariffLateArrivalCL);
        lateArrival.Tiers.Should().HaveCount(3);
        var snapshot = MaintainerChangeLogger.Read<TariffSnapshot>(logs.Single(l => l.EntityId == lateArrival.Id).NewValue)!;
        snapshot.TierUnit.Should().Be(TariffTierUnits.Hours);
        snapshot.Tiers.Should().HaveCount(3);
    }

    [Fact]
    public async Task DemoData_ShouldShowInvoicedDemurrageAndTheBolivianAdvanceRule()
    {
        var invoiced = await _context.DemurrageCharges.SingleAsync(d => d.Id == SeedDataIds.Demurrage04);
        var rule = await _context.InternalChargeRules.SingleAsync(r => r.Id == SeedDataIds.RuleAdvanceDemurrageBO);
        var mhd = await _context.LocalCharges.Where(c => c.ChargeType == ChargeConceptCodes.Mhd).Select(c => c.BillOfLadingId).ToListAsync();

        invoiced.InvoiceNumber.Should().NotBeNull();
        invoiced.Status.Should().Be(DemurrageChargeStatus.Invoiced);
        rule.RuleType.Should().Be(InternalChargeRuleTypes.AdvanceDemurrageRequired);
        rule.TaxId.Should().Be("1023456017");
        mhd.Should().Contain([SeedDataIds.BL01, SeedDataIds.BL04, SeedDataIds.BL09]);
    }

    [Fact]
    public async Task MaintainerChangeLog_ShouldBeAppendOnly()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor(currentUser))
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var log = await context.MaintainerChangeLogs.FirstAsync();
        log.ChangedBy = "tampered";

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    public void Dispose() => _context.Dispose();
}
