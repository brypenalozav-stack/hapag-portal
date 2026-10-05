namespace HapagPortal.UnitTests.Application.Access;

using FluentAssertions;
using HapagPortal.Domain.Constants;
using HapagPortal.UnitTests.Application.TestHelpers;
using Microsoft.EntityFrameworkCore;

public sealed class ShipmentAccessEvaluatorTests
{
    private readonly MockApplicationDbContext _db = new();

    [Fact]
    public async Task Consignee_ShouldSeeDemurrageAndFreight()
    {
        var (org, _, _, evaluator) = AccessTestData.ClientContext(_db);
        var bl = AccessTestData.AddBl(_db, Guid.NewGuid(), "BL-CN");
        AccessTestData.AddRole(_db, bl, org, ShipmentRoleCodes.Consignee);

        var scope = await evaluator.GetScopeAsync();
        var permissions = await evaluator.EvaluateAsync(scope, bl);

        permissions.Roles.Should().Equal(ShipmentRoleCodes.Consignee);
        permissions.Can(ShipmentActionCodes.ViewShipment).Should().BeTrue();
        permissions.Can(ShipmentActionCodes.PayImportDemurrage).Should().BeTrue();   // O
        permissions.Can(ShipmentActionCodes.PayFreight).Should().BeTrue();           // O
        permissions.Can(ShipmentActionCodes.GenerateResponsibilityLetter).Should().BeFalse(); // X (cliente)
    }

    [Fact]
    public async Task Customer_ShouldNotSeeImportDemurrage()
    {
        // El titular heredado del BL (ClientId) es Customer.
        var (org, _, _, evaluator) = AccessTestData.ClientContext(_db);
        var bl = AccessTestData.AddBl(_db, org.Id, "BL-CU");

        var scope = await evaluator.GetScopeAsync();
        var permissions = await evaluator.EvaluateAsync(scope, bl);

        permissions.Roles.Should().Equal(ShipmentRoleCodes.Customer);
        permissions.Can(ShipmentActionCodes.PayFreight).Should().BeTrue();
        permissions.Can(ShipmentActionCodes.PayImportDemurrage).Should().BeFalse();  // X
        permissions.Can(ShipmentActionCodes.DownloadNoDebtCertificate).Should().BeFalse();
    }

    [Fact]
    public async Task Shipper_OnGrantFreight_ShouldBeDeniedWithoutExplicitGrant()
    {
        var (org, _, _, evaluator) = AccessTestData.ClientContext(_db);
        var bl = AccessTestData.AddBl(_db, Guid.NewGuid(), "BL-SH", shipmentType: "Export");
        AccessTestData.AddRole(_db, bl, org, ShipmentRoleCodes.Shipper);

        var scope = await evaluator.GetScopeAsync();
        var permissions = await evaluator.EvaluateAsync(scope, bl);

        permissions.Can(ShipmentActionCodes.PayFreight).Should().BeFalse();          // X (o)
        permissions.Can(ShipmentActionCodes.RequestValuedBlCopy).Should().BeFalse(); // X (o)
        permissions.Can(ShipmentActionCodes.RequestUnvaluedBlCopy).Should().BeTrue(); // O
        permissions.Can(ShipmentActionCodes.PayMandatoryLocalCharges).Should().BeTrue();
    }

    [Fact]
    public async Task MultipleRoles_ShouldBeUnion()
    {
        var (org, _, _, evaluator) = AccessTestData.ClientContext(_db);
        var bl = AccessTestData.AddBl(_db, org.Id, "BL-UNION");
        AccessTestData.AddRole(_db, bl, org, ShipmentRoleCodes.Consignee);

        var scope = await evaluator.GetScopeAsync();
        var permissions = await evaluator.EvaluateAsync(scope, bl);

        permissions.Roles.Should().Equal(ShipmentRoleCodes.Customer, ShipmentRoleCodes.Consignee);
        permissions.Can(ShipmentActionCodes.PayImportDemurrage).Should().BeTrue();
    }

    [Fact]
    public async Task FreightForwarderConsignee_ShouldGetResponsibilityLetterException()
    {
        var (org, _, _, evaluator) = AccessTestData.ClientContext(_db, OrganizationTypes.FreightForwarder);
        var bl = AccessTestData.AddBl(_db, Guid.NewGuid(), "BL-FF");
        AccessTestData.AddRole(_db, bl, org, ShipmentRoleCodes.Consignee);

        var scope = await evaluator.GetScopeAsync();
        var permissions = await evaluator.EvaluateAsync(scope, bl);

        permissions.Can(ShipmentActionCodes.GenerateResponsibilityLetter).Should().BeTrue();
    }

    [Fact]
    public async Task CustomsAgency_ShouldUseItsOwnColumn()
    {
        var (org, _, _, evaluator) = AccessTestData.ClientContext(_db, OrganizationTypes.CustomsAgency);
        var bl = AccessTestData.AddBl(_db, org.Id, "BL-AG");

        var scope = await evaluator.GetScopeAsync();
        var permissions = await evaluator.EvaluateAsync(scope, bl);

        permissions.Roles.Should().Equal(ShipmentRoleCodes.CustomsAgency);
        permissions.Can(ShipmentActionCodes.ViewInvoicesAsPayer).Should().BeTrue();   // O solo agencia y transportista
        permissions.Can(ShipmentActionCodes.ViewAccountStatement).Should().BeFalse(); // X
        permissions.Can(ShipmentActionCodes.GrantAccess).Should().BeFalse();          // X
    }

    [Fact]
    public async Task OtherOrganizationsBl_ShouldNotBeAccessible()
    {
        var (_, _, _, evaluator) = AccessTestData.ClientContext(_db);
        var other = AccessTestData.AddOrganization(_db);
        var bl = AccessTestData.AddBl(_db, other.Id, "BL-OTHER");
        AccessTestData.AddRole(_db, bl, other, ShipmentRoleCodes.Consignee);

        var scope = await evaluator.GetScopeAsync();
        var visible = await evaluator.FilterAccessible(_db.BillsOfLading, scope).ToListAsync();
        var permissions = await evaluator.EvaluateAsync(scope, bl);

        visible.Should().BeEmpty();
        permissions.HasAccess.Should().BeFalse();
        permissions.Can(ShipmentActionCodes.ViewShipment).Should().BeFalse();
    }

    [Theory]
    [InlineData(OrganizationStatus.PendingValidation)]
    [InlineData(OrganizationStatus.PendingArCheck)]
    [InlineData(OrganizationStatus.Rejected)]
    public async Task NotApprovedOrganization_ShouldNotOperate(string status)
    {
        AccessTestData.SeedMatrix(_db);
        var org = AccessTestData.AddOrganization(_db, status: status);
        var user = AccessTestData.AddMember(_db, org);
        var evaluator = AccessTestData.Evaluator(_db, AccessTestData.CurrentUser(user, AccessPermissions.OperateShipments));
        var bl = AccessTestData.AddBl(_db, org.Id, "BL-PENDING");

        var scope = await evaluator.GetScopeAsync();

        scope.IsOperational.Should().BeFalse();
        (await evaluator.FilterAccessible(_db.BillsOfLading, scope).ToListAsync()).Should().BeEmpty();
        (await evaluator.EvaluateAsync(scope, bl)).HasAccess.Should().BeFalse();
    }

    [Fact]
    public async Task PendingMember_ShouldNotAccessOrganizationData()
    {
        AccessTestData.SeedMatrix(_db);
        var org = AccessTestData.AddOrganization(_db);
        var user = AccessTestData.AddMember(_db, org, MembershipStatus.Pending, profile: null);
        var evaluator = AccessTestData.Evaluator(_db, AccessTestData.CurrentUser(user));
        AccessTestData.AddBl(_db, org.Id, "BL-OWN");

        var scope = await evaluator.GetScopeAsync();

        scope.IsOperational.Should().BeFalse();
        (await evaluator.FilterAccessible(_db.BillsOfLading, scope).ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ViewerProfile_ShouldSeeButNotExecute()
    {
        AccessTestData.SeedMatrix(_db);
        var org = AccessTestData.AddOrganization(_db);
        var user = AccessTestData.AddMember(_db, org, profile: RoleCodes.OrgViewer);
        var evaluator = AccessTestData.Evaluator(_db, AccessTestData.CurrentUser(user)); // sin shipments.operate
        var bl = AccessTestData.AddBl(_db, org.Id, "BL-VIEW");

        var scope = await evaluator.GetScopeAsync();
        var permissions = await evaluator.EvaluateAsync(scope, bl);

        permissions.Can(ShipmentActionCodes.PayFreight).Should().BeTrue();
        permissions.CanExecute(ShipmentActionCodes.PayFreight).Should().BeFalse();
        permissions.CanOperate.Should().BeFalse();
    }

    [Fact]
    public async Task InternalAdmin_ShouldSeeEverything()
    {
        var (_, _, evaluator) = AccessTestData.AdminContext(_db);
        var other = AccessTestData.AddOrganization(_db);
        var bl = AccessTestData.AddBl(_db, other.Id, "BL-ANY");

        var scope = await evaluator.GetScopeAsync();
        var visible = await evaluator.FilterAccessible(_db.BillsOfLading, scope).ToListAsync();
        var permissions = await evaluator.EvaluateAsync(scope, bl);

        scope.IsAdmin.Should().BeTrue();
        visible.Should().ContainSingle();
        permissions.IsAdmin.Should().BeTrue();
        permissions.Can(ShipmentActionCodes.PayImportDemurrage).Should().BeTrue();
        permissions.Can(ShipmentActionCodes.AccessAdministrationArea).Should().BeTrue();
    }

    [Fact]
    public async Task ClientUserWithViewAllPermission_ShouldNotBeAdmin()
    {
        // M8-06: la visibilidad total es solo para usuarios internos.
        AccessTestData.SeedMatrix(_db);
        var org = AccessTestData.AddOrganization(_db);
        var user = AccessTestData.AddMember(_db, org);
        var evaluator = AccessTestData.Evaluator(
            _db, AccessTestData.CurrentUser(user, AccessPermissions.ViewAllShipments));
        AccessTestData.AddBl(_db, AccessTestData.AddOrganization(_db).Id, "BL-OTHER");

        var scope = await evaluator.GetScopeAsync();

        scope.IsAdmin.Should().BeFalse();
        (await evaluator.FilterAccessible(_db.BillsOfLading, scope).ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task MatrixChange_ShouldApplyImmediately()
    {
        var (org, _, currentUser, _) = AccessTestData.ClientContext(_db);
        var bl = AccessTestData.AddBl(_db, org.Id, "BL-MATRIX");
        var action = _db.ShipmentActionList.Single(a => a.Code == ShipmentActionCodes.PayImportDemurrage);
        _db.ShipmentAccessRuleList
            .Single(r => r.ShipmentActionId == action.Id && r.Role == ShipmentRoleCodes.Customer && r.OrganizationType == null)
            .Level = AccessLevels.Allowed;

        var evaluator = AccessTestData.Evaluator(_db, currentUser);
        var scope = await evaluator.GetScopeAsync();
        var permissions = await evaluator.EvaluateAsync(scope, bl);

        permissions.Can(ShipmentActionCodes.PayImportDemurrage).Should().BeTrue();
    }

    [Fact]
    public async Task InactiveAction_ShouldNotBeAllowed()
    {
        var (org, _, currentUser, _) = AccessTestData.ClientContext(_db);
        var bl = AccessTestData.AddBl(_db, org.Id, "BL-INACTIVE");
        _db.ShipmentActionList.Single(a => a.Code == ShipmentActionCodes.PayFreight).IsActive = false;

        var evaluator = AccessTestData.Evaluator(_db, currentUser);
        var scope = await evaluator.GetScopeAsync();

        (await evaluator.EvaluateAsync(scope, bl)).Can(ShipmentActionCodes.PayFreight).Should().BeFalse();
    }
}
