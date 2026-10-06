namespace HapagPortal.UnitTests.Infrastructure.Persistence;

using FluentAssertions;
using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Access;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>
/// Semilla de Fase 1 Ola A sobre un proveedor EF real (InMemory): matriz de M1-11, perfiles y
/// embarques de demostración, y el evaluador de accesos traduciendo sus consultas.
/// </summary>
public sealed class OrganizationAccessSeedTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public OrganizationAccessSeedTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task AccessMatrix_ShouldBeSeededFromBaseline()
    {
        var actions = await _context.ShipmentActions.CountAsync();
        var rules = await _context.ShipmentAccessRules.CountAsync();

        actions.Should().Be(AccessMatrixBaseline.Actions.Count);
        rules.Should().Be(
            AccessMatrixBaseline.Actions.Count * ShipmentRoleCodes.MatrixColumns.Length
            + AccessMatrixBaseline.Actions.Sum(a => a.Overrides.Count));
    }

    [Fact]
    public async Task SeededOrganizations_ShouldBeApprovedWithMatchCode()
    {
        var demo = await _context.Clients.SingleAsync(c => c.Id == SeedDataIds.DemoClientCL);
        var pending = await _context.Clients.SingleAsync(c => c.Id == SeedDataIds.PendingOrgClient);

        demo.RegistrationStatus.Should().Be(OrganizationStatus.Approved);
        demo.MatchCode.Should().Be("MC100010");
        pending.RegistrationStatus.Should().Be(OrganizationStatus.PendingValidation);
        pending.OrganizationType.Should().Be(OrganizationTypes.FreightForwarder);
    }

    [Theory]
    [InlineData("D4E5F6A7-0004-0004-0004-000000000010", new[] { "HLCUVAL250100123", "HLCUVAL250200456", "HLCUSAI260300610", "HLCUVAP260300720", "HLCUSAI260400910", "HLCUSAI260401020", "HLCUSAI260501240", "HLCUSAI260601520", "HLCUSAI260901610" })]
    [InlineData("D4E5F6A7-0004-0004-0004-000000000020", new[] { "HLCUARI260100045", "HLCUIQQ260200078", "HLCUARI260300830", "HLCUARI260901720" })]
    public async Task Evaluator_ShouldListSeededShipmentsPerDemoOrganization(string userId, string[] expectedBls)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.Parse(userId));
        var evaluator = new ShipmentAccessEvaluator(_context, currentUser);

        var scope = await evaluator.GetScopeAsync();
        var bls = await evaluator.FilterAccessible(_context.BillsOfLading.AsNoTracking(), scope)
            .Select(b => b.BLNumber)
            .ToListAsync();

        scope.IsOperational.Should().BeTrue();
        bls.Should().BeEquivalentTo(expectedBls);
    }

    [Fact]
    public async Task Evaluator_DemoShipperOnlyBl_ShouldHideFreight()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(SeedDataIds.DemoUserCL);
        var evaluator = new ShipmentAccessEvaluator(_context, currentUser);
        var bl = await _context.BillsOfLading.AsNoTracking().SingleAsync(b => b.Id == SeedDataIds.BL07);

        var scope = await evaluator.GetScopeAsync();
        var permissions = await evaluator.EvaluateAsync(scope, bl);

        permissions.Roles.Should().Equal(ShipmentRoleCodes.Shipper);
        permissions.Can(ShipmentActionCodes.ViewShipment).Should().BeTrue();
        permissions.Can(ShipmentActionCodes.PayFreight).Should().BeFalse();
    }

    [Fact]
    public async Task OrganizationProfiles_ShouldGrantOrganizationPermissions()
    {
        var resolver = new HapagPortal.Infrastructure.Authentication.PermissionResolver(_context);

        var admin = await resolver.ResolveAsync([RoleCodes.OrgAdmin]);
        var viewer = await resolver.ResolveAsync([RoleCodes.OrgViewer]);

        admin.Should().Contain([AccessPermissions.ManageOrganizationUsers, AccessPermissions.ApproveJoinRequests, AccessPermissions.OperateShipments]);
        admin.Should().NotContain(AccessPermissions.ViewAllShipments);
        viewer.Should().BeEmpty();
    }

    public void Dispose() => _context.Dispose();
}
