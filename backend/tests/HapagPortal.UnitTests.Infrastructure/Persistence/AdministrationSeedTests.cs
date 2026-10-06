namespace HapagPortal.UnitTests.Infrastructure.Persistence;

using FluentAssertions;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Auth.Login;
using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Guides;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.Infrastructure.Integrations.Nexus;
using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

/// <summary>
/// Semilla de la Ola I sobre un proveedor EF real (InMemory): permisos internos nuevos, comunicados, guía del carro,
/// empresa matriz que ve los BL de su filial, transportista pre-creado que se activa al primer ingreso, Counter de
/// Bolivia, sesión de impersonación auditada y notificaciones con acción pendiente.
/// </summary>
public sealed class AdministrationSeedTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public AdministrationSeedTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    private ShipmentAccessEvaluator EvaluatorFor(Guid userId, params string[] permissions)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        currentUser.HasPermission(Arg.Any<string>()).Returns(call => permissions.Contains(call.Arg<string>()));
        return new ShipmentAccessEvaluator(_context, currentUser);
    }

    [Fact]
    public async Task Permissions_AndRoleAssignments_ShouldBeSeeded()
    {
        var codes = await _context.Permissions.AsNoTracking().Select(p => p.Code).ToListAsync();

        codes.Should().Contain(
        [
            AdministrationPermissions.AccessAdminArea, AdministrationPermissions.UseImpersonation, AdministrationPermissions.ManageAnnouncements,
            AdministrationPermissions.ManageCounter, AdministrationPermissions.ViewTransactionsReport
        ]);
        var resolver = new PermissionResolver(_context);
        (await resolver.ResolveAsync([RoleCodes.Coordinador])).Should().Contain(AdministrationPermissions.ManageCounter)
            .And.NotContain(AdministrationPermissions.UseImpersonation);
        (await resolver.ResolveAsync([RoleCodes.OrgAdmin])).Should().NotContain(AdministrationPermissions.UseImpersonation);
    }

    [Fact]
    public async Task Announcements_AndCartGuide_ShouldBeSeeded()
    {
        var announcements = await _context.Announcements.AsNoTracking().ToListAsync();
        var guide = await _context.GuideDefinitions.AsNoTracking().SingleAsync();
        var logs = await _context.MaintainerChangeLogs.AsNoTracking()
            .CountAsync(l => l.Maintainer == MaintainerNames.Announcement || l.Maintainer == MaintainerNames.Guide);

        announcements.Should().HaveCount(3);
        announcements.Single(a => a.Id == SeedDataIds.AnnouncementCL)
            .Should().Match<Domain.Entities.Announcement>(a => a.Countries == "CL" && a.Operation == AnnouncementOperations.Import
                && a.Status == AnnouncementStatus.Published && a.PublishedAt != null);
        announcements.Single(a => a.Id == SeedDataIds.AnnouncementBO)
            .Should().Match<Domain.Entities.Announcement>(a => a.Countries == "BO" && a.Operation == AnnouncementOperations.Export);
        announcements.Single(a => a.Id == SeedDataIds.AnnouncementDraft).Status.Should().Be(AnnouncementStatus.Draft);
        guide.Code.Should().Be("cart-checkout");
        GuideSteps.Parse(guide.StepsJson).Select(s => s.ElementKey).Should().Equal(
            "cart.items", "cart.billing-tax-id", "cart.currency", "cart.payment-method", "cart.checkout");
        logs.Should().Be(4);
    }

    [Fact]
    public async Task Holding_ShouldSeeItsSubsidiaryShipments_AsParent_AndNotThePendingOne()
    {
        var evaluator = EvaluatorFor(SeedDataIds.HoldingUser, AccessPermissions.OperateShipments);
        var scope = await evaluator.GetScopeAsync();

        var bls = await evaluator.FilterAccessible(_context.BillsOfLading.AsNoTracking(), scope).ToListAsync();
        var sources = await evaluator.GetAccessSourcesAsync(scope, bls);
        var bl01 = bls.Single(b => b.Id == SeedDataIds.BL01);
        var permissions = await evaluator.EvaluateAsync(scope, bl01);

        bls.Select(b => b.BLNumber).Should().Contain(["HLCUVAL250100123", "HLCUVAL250200456"]);
        bls.Should().NotContain(b => b.ClientId == SeedDataIds.DemoClientBO);   // Comercial Altiplano: vínculo pendiente
        sources.Values.Should().OnlyContain(s => s == ShipmentAccessSources.Parent);
        permissions.OriginOrganizationId.Should().Be(SeedDataIds.DemoClientCL);
        permissions.Can(ShipmentActionCodes.ViewShipment).Should().BeTrue();
        permissions.Can(ShipmentActionCodes.PayFreight).Should().BeFalse();
    }

    [Fact]
    public async Task PreCreatedCarrier_ShouldBeActivatedByItsFirstLogin()
    {
        var before = EvaluatorFor(SeedDataIds.PreCreatedCarrierUser, AccessPermissions.OperateShipments);
        var beforeScope = await before.GetScopeAsync();
        (await before.FilterAccessible(_context.BillsOfLading.AsNoTracking(), beforeScope).CountAsync()).Should().Be(0);

        var permissions = Substitute.For<IPermissionResolver>();
        permissions.ResolveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns([AccessPermissions.OperateShipments]);
        var login = await new LoginCommandHandler(_context, new PasswordHasher(), Substitute.For<IJwtTokenService>(), permissions,
                Substitute.For<INotificationPublisher>())
            .Handle(new LoginCommand("contacto@transportescordillera.cl", "Admin123!"), CancellationToken.None);

        login.IsSuccess.Should().BeTrue();
        login.Value.Organization!.Status.Should().Be(OrganizationStatus.Approved);
        _context.ChangeTracker.Clear();
        var grant = await _context.AccessGrants.AsNoTracking().SingleAsync(g => g.Id == SeedDataIds.CarrierGrantBL02);
        grant.Status.Should().Be(AccessGrantStatus.Active);
        grant.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddDays(90), TimeSpan.FromMinutes(1));

        var after = EvaluatorFor(SeedDataIds.PreCreatedCarrierUser, AccessPermissions.OperateShipments);
        var scope = await after.GetScopeAsync();
        (await after.FilterAccessible(_context.BillsOfLading.AsNoTracking(), scope).Select(b => b.BLNumber).ToListAsync())
            .Should().Equal("HLCUVAL250200456");
    }

    [Fact]
    public async Task Counter_ImpersonationAndInbox_ShouldBeConsistent()
    {
        var records = await _context.CounterRecords.AsNoTracking().ToListAsync();
        var source = await new DummyCounterRecorder(NullLogger<DummyCounterRecorder>.Instance).GetAsync("HLCUARI260300830");
        var session = await _context.ImpersonationSessions.AsNoTracking().SingleAsync();
        var audit = await _context.AuditLogs.AsNoTracking()
            .Where(a => a.EntityName == ImpersonationAuditActions.EntityName).ToListAsync();
        var actionable = await _context.Notifications.AsNoTracking().Where(n => n.ActionType != null).ToListAsync();

        records.Should().HaveCount(2);
        records.Single(r => r.Id == SeedDataIds.CounterRecordBL08).SyncStatus.Should().Be(CounterSyncStatus.Synced);
        records.Single(r => r.Id == SeedDataIds.CounterRecordBL04).SyncStatus.Should().Be(CounterSyncStatus.Failed);
        source.Value!.SourceReference.Should().Be(records.Single(r => r.Id == SeedDataIds.CounterRecordBL08).SourceReference);
        session.Status.Should().Be(ImpersonationStatus.Ended);
        session.DurationSeconds.Should().Be(720);
        audit.Select(a => a.Action).Should().BeEquivalentTo(
            [ImpersonationAuditActions.Started, ImpersonationAuditActions.BlockedWrite, ImpersonationAuditActions.Ended]);
        audit.Should().OnlyContain(a => a.UserId == SeedDataIds.AdminUser.ToString());
        actionable.Select(n => n.ActionType).Should().BeEquivalentTo(
            [NotificationActionTypes.ApproveJoinRequest, NotificationActionTypes.ReviewParentLink]);
        (await _context.NotificationPreferences.CountAsync(p => p.UserId == SeedDataIds.DemoUserCL)).Should().Be(2);
        (await _context.ContactListChanges.CountAsync()).Should().Be(1);
    }

    public void Dispose() => _context.Dispose();
}
