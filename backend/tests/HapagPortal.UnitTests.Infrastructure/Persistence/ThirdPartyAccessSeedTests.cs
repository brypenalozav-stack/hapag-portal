namespace HapagPortal.UnitTests.Infrastructure.Persistence;

using FluentAssertions;
using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Infrastructure.Persistence;
using HapagPortal.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>
/// Semilla de Fase 1 Ola B sobre un proveedor EF real (InMemory): acceso otorgado a la agencia con
/// permisos limitados, mandato, tercero por defecto y acceso abierto, traducidos por el evaluador.
/// </summary>
public sealed class ThirdPartyAccessSeedTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public ThirdPartyAccessSeedTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        // Las vigencias sembradas son fechas reales: se extienden para no depender del reloj.
        foreach (var grant in _context.AccessGrants)
            grant.ValidTo = DateTime.UtcNow.AddYears(1);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    private ShipmentAccessEvaluator EvaluatorFor(Guid userId, params string[] permissions)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        currentUser.HasPermission(Arg.Any<string>()).Returns(call => permissions.Contains(call.Arg<string>()));
        return new ShipmentAccessEvaluator(_context, currentUser);
    }

    [Fact]
    public async Task Agency_ShouldListTheBlsReceivedByGrant()
    {
        var evaluator = EvaluatorFor(SeedDataIds.AgentUserCL);
        var scope = await evaluator.GetScopeAsync();

        var bls = await evaluator.FilterAccessible(_context.BillsOfLading.AsNoTracking(), scope).ToListAsync();
        var sources = await evaluator.GetAccessSourcesAsync(scope, bls);

        bls.Select(b => b.BLNumber).Should().BeEquivalentTo(["HLCUVAL250100123", "HLCUVAL250200456"]);
        sources.Values.Should().OnlyContain(s => s == ShipmentAccessSources.Grant);
    }

    [Fact]
    public async Task Agency_ShouldSeeOnlyTheGrantedPermissions()
    {
        var evaluator = EvaluatorFor(SeedDataIds.AgentUserCL, AccessPermissions.OperateShipments);
        var scope = await evaluator.GetScopeAsync();
        var limited = await _context.BillsOfLading.AsNoTracking().SingleAsync(b => b.Id == SeedDataIds.BL01);
        var mandate = await _context.BillsOfLading.AsNoTracking().SingleAsync(b => b.Id == SeedDataIds.BL02);

        var limitedPermissions = await evaluator.EvaluateAsync(scope, limited);
        var mandatePermissions = await evaluator.EvaluateAsync(scope, mandate);

        limitedPermissions.Can(ShipmentActionCodes.PayImportDemurrage).Should().BeTrue();
        limitedPermissions.Can(ShipmentActionCodes.PayFreight).Should().BeFalse();
        mandatePermissions.Can(ShipmentActionCodes.PayFreight).Should().BeTrue();
        mandatePermissions.GrantFor(ShipmentActionCodes.PayFreight)!.IsMandate.Should().BeTrue();
    }

    [Fact]
    public async Task OpenAccess_ShouldExposeTheBlOnlyByNumber()
    {
        var evaluator = EvaluatorFor(SeedDataIds.DemoUserBO);
        var scope = await evaluator.GetScopeAsync();

        var listed = await evaluator.FilterAccessible(_context.BillsOfLading.AsNoTracking(), scope)
            .AnyAsync(b => b.Id == SeedDataIds.BL07);
        var byNumber = await evaluator.FilterOpenAccess(_context.BillsOfLading.AsNoTracking(), scope)
            .SingleAsync(b => b.BLNumber == "HLCUVAP260300720");
        var permissions = await evaluator.EvaluateAsync(scope, byNumber);

        listed.Should().BeFalse();
        permissions.AccessSource.Should().Be(ShipmentAccessSources.OpenAccess);
        permissions.Can(ShipmentActionCodes.ViewTracking).Should().BeTrue();
        permissions.Can(ShipmentActionCodes.PayFreight).Should().BeFalse();
    }

    [Fact]
    public async Task SeededDefaultsAndAudit_ShouldBePresent()
    {
        (await _context.DefaultGrantees.SingleAsync()).GranteeClientId.Should().Be(SeedDataIds.AgentClientCL);
        (await _context.AccessAuditEntries.CountAsync()).Should().Be(5);
    }

    [Fact]
    public async Task AccessAudit_ShouldBeAppendOnly()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor(currentUser))
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var entry = await context.AccessAuditEntries.FirstAsync();
        entry.Details = "tampered";

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    public void Dispose() => _context.Dispose();
}
