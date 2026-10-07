namespace HapagPortal.UnitTests.Application.Admin;

using FluentAssertions;
using HapagPortal.Application.Admin.Overview;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>Área de administración unificada (M8-05): secciones según los permisos del usuario interno, con sus pendientes.</summary>
public sealed class AdminOverviewQueryTests
{
    private readonly MockApplicationDbContext _db = new();

    private static ICurrentUserService UserWith(params string[] permissions)
    {
        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(Guid.NewGuid());
        user.HasPermission(Arg.Any<string>()).Returns(call => permissions.Contains(call.Arg<string>()));
        return user;
    }

    [Fact]
    public async Task Overview_ShouldShowOnlyTheSectionsTheUserCanUse_WithTheirCounters()
    {
        AccessTestData.AddOrganization(_db, status: OrganizationStatus.PendingValidation);
        AccessTestData.AddOrganization(_db, OrganizationTypes.Carrier, OrganizationStatus.PreCreated);
        _db.OrganizationParentLinkList.Add(new OrganizationParentLink { Status = ParentLinkStatus.Pending, RequestedBy = "x" });
        _db.CounterRecordList.Add(new CounterRecord { BlNumber = "BL", Country = "BO", SyncStatus = CounterSyncStatus.Failed, RecordedBy = "x" });

        var full = await new GetAdminOverviewQueryHandler(_db, UserWith(
                AccessPermissions.ReviewOrganizations, AdministrationPermissions.ManageCounter, AdministrationPermissions.UseImpersonation))
            .Handle(new GetAdminOverviewQuery(), CancellationToken.None);
        var counterOnly = await new GetAdminOverviewQueryHandler(_db, UserWith(AdministrationPermissions.ManageCounter))
            .Handle(new GetAdminOverviewQuery(), CancellationToken.None);

        full.Value.Sections.Select(s => s.Code).Should().BeEquivalentTo(
            [AdminSections.Organizations, AdminSections.ParentLinks, AdminSections.Counter, AdminSections.Impersonation]);
        var organizations = full.Value.Sections.Single(s => s.Code == AdminSections.Organizations).Counters;
        organizations["pendingValidation"].Should().Be(1);
        organizations["preCreatedCarriers"].Should().Be(1);
        full.Value.Sections.Single(s => s.Code == AdminSections.ParentLinks).Counters["pending"].Should().Be(1);
        full.Value.Sections.Single(s => s.Code == AdminSections.Counter).Counters["syncFailed"].Should().Be(1);
        counterOnly.Value.Sections.Should().ContainSingle().Which.Code.Should().Be(AdminSections.Counter);
    }
}
