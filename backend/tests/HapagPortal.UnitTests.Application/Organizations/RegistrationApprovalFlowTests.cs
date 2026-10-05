namespace HapagPortal.UnitTests.Application.Organizations;

using FluentAssertions;
using HapagPortal.Application.Admin.Organizations;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Auth.Login;
using HapagPortal.Application.Auth.Logout;
using HapagPortal.Application.Auth.RequestMembership;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Organizations.JoinRequests;
using HapagPortal.Application.Organizations.OperatingCountry;
using HapagPortal.Domain.Constants;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>Registro con aprobación (M1-07, M8-04), vinculación (M1-08), país (M1-04) y cierre de sesión (M1-10).</summary>
public sealed class RegistrationApprovalFlowTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly INotificationPublisher _notifications = Substitute.For<INotificationPublisher>();

    public RegistrationApprovalFlowTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hash");
    }

    private RequestOrganizationMembershipCommand JoinCommand(string taxId, string email = "nuevo@org.cl") =>
        new(taxId, "CL", email, "Password1!", "Nuevo", "Usuario", null);

    [Fact]
    public async Task RequestMembership_ShouldCreatePendingUserAndNotifyOrganizationAdmins()
    {
        var org = AccessTestData.AddOrganization(_db);
        var admin = AccessTestData.AddMember(_db, org, profile: RoleCodes.OrgAdmin);
        AccessTestData.AddMember(_db, org, profile: RoleCodes.OrgViewer);
        var handler = new RequestOrganizationMembershipCommandHandler(_db, _passwordHasher, _notifications);

        var result = await handler.Handle(JoinCommand(org.TaxId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(MembershipStatus.Pending);
        var applicant = _db.UserList.Single(u => u.Email == "nuevo@org.cl");
        applicant.ClientId.Should().Be(org.Id);
        applicant.MembershipStatus.Should().Be(MembershipStatus.Pending);
        await _notifications.Received(1).PublishAsync(
            Arg.Is<NotificationRequest>(n => n.UserId == admin.Id && n.Type == NotificationTypes.JoinRequestReceived),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestMembership_UnknownOrganization_ShouldFail()
    {
        var handler = new RequestOrganizationMembershipCommandHandler(_db, _passwordHasher, _notifications);

        var result = await handler.Handle(JoinCommand("99.999.999-9"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Organization.NotFound");
        _db.UserList.Should().BeEmpty();
    }

    [Fact]
    public async Task PendingApplicant_ShouldNotLogIn()
    {
        var org = AccessTestData.AddOrganization(_db);
        var applicant = AccessTestData.AddMember(_db, org, MembershipStatus.Pending, profile: null);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var handler = new LoginCommandHandler(
            _db, _passwordHasher, Substitute.For<IJwtTokenService>(), Substitute.For<IPermissionResolver>());

        var result = await handler.Handle(new LoginCommand(applicant.Email, "Password1!"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.PendingApproval");
    }

    [Fact]
    public async Task Login_ShouldReturnOrganizationStatusForPendingOrganization()
    {
        var org = AccessTestData.AddOrganization(_db, OrganizationTypes.FreightForwarder, OrganizationStatus.PendingValidation);
        var user = AccessTestData.AddMember(_db, org);
        user.Client = org;
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var permissions = Substitute.For<IPermissionResolver>();
        permissions.ResolveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns([AccessPermissions.OperateShipments]);
        var handler = new LoginCommandHandler(_db, _passwordHasher, Substitute.For<IJwtTokenService>(), permissions);

        var result = await handler.Handle(new LoginCommand(user.Email, "Password1!"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Organization.Should().NotBeNull();
        result.Value.Organization!.Status.Should().Be(OrganizationStatus.PendingValidation);
        result.Value.Organization.OrganizationType.Should().Be(OrganizationTypes.FreightForwarder);
        result.Value.Organization.Profile.Should().Be(RoleCodes.OrgAdmin);
        result.Value.Organization.CanOperate.Should().BeFalse();
    }

    [Fact]
    public async Task JoinRequests_ShouldListOnlyOwnOrganizationPending()
    {
        var org = AccessTestData.AddOrganization(_db, name: "Mi Org");
        var admin = AccessTestData.AddMember(_db, org);
        var mine = AccessTestData.AddMember(_db, org, MembershipStatus.Pending, profile: null);
        AccessTestData.AddMember(_db, AccessTestData.AddOrganization(_db), MembershipStatus.Pending, profile: null);
        var handler = new GetJoinRequestsQueryHandler(_db, AccessTestData.CurrentUser(admin));

        var result = await handler.Handle(new GetJoinRequestsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(r => r.UserId == mine.Id && r.OrganizationName == "Mi Org");
    }

    [Fact]
    public async Task ApproveJoinRequest_ShouldActivateAssignProfileAndNotifyApplicant()
    {
        var org = AccessTestData.AddOrganization(_db);
        var admin = AccessTestData.AddMember(_db, org);
        var applicant = AccessTestData.AddMember(_db, org, MembershipStatus.Pending, profile: null);
        var handler = new ApproveJoinRequestCommandHandler(_db, AccessTestData.CurrentUser(admin), _notifications);

        var result = await handler.Handle(new ApproveJoinRequestCommand(applicant.Id, RoleCodes.OrgOperator), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        applicant.MembershipStatus.Should().Be(MembershipStatus.Active);
        applicant.MembershipDecidedBy.Should().Be(admin.Email);
        _db.UserRoleList.Should().Contain(r => r.UserId == applicant.Id && r.RoleName == RoleCodes.OrgOperator);
        await _notifications.Received(1).PublishAsync(
            Arg.Is<NotificationRequest>(n => n.UserId == applicant.Id && n.Type == NotificationTypes.JoinRequestApproved),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectJoinRequest_ShouldRejectAndNotifyApplicant()
    {
        var org = AccessTestData.AddOrganization(_db);
        var admin = AccessTestData.AddMember(_db, org);
        var applicant = AccessTestData.AddMember(_db, org, MembershipStatus.Pending, profile: null);
        var handler = new RejectJoinRequestCommandHandler(_db, AccessTestData.CurrentUser(admin), _notifications);

        var result = await handler.Handle(new RejectJoinRequestCommand(applicant.Id, "No pertenece"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        applicant.MembershipStatus.Should().Be(MembershipStatus.Rejected);
        await _notifications.Received(1).PublishAsync(
            Arg.Is<NotificationRequest>(n => n.UserId == applicant.Id && n.Type == NotificationTypes.JoinRequestRejected),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveJoinRequest_OfAnotherOrganization_ShouldReturnNotFound()
    {
        var org = AccessTestData.AddOrganization(_db);
        var admin = AccessTestData.AddMember(_db, org);
        var foreignApplicant = AccessTestData.AddMember(_db, AccessTestData.AddOrganization(_db), MembershipStatus.Pending, profile: null);
        var handler = new ApproveJoinRequestCommandHandler(_db, AccessTestData.CurrentUser(admin), _notifications);

        var result = await handler.Handle(new ApproveJoinRequestCommand(foreignApplicant.Id, RoleCodes.OrgViewer), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JoinRequest.NotFound");
        foreignApplicant.MembershipStatus.Should().Be(MembershipStatus.Pending);
    }

    [Fact]
    public async Task ReviewFlow_ShouldValidateThenArCheckWithMatchCode()
    {
        var org = AccessTestData.AddOrganization(_db, status: OrganizationStatus.PendingValidation);
        var orgAdmin = AccessTestData.AddMember(_db, org);
        var reviewer = Substitute.For<ICurrentUserService>();
        reviewer.Email.Returns("revisor@hlag.com");

        // El Match Code no se asigna antes de validar al cliente.
        var arHandler = new CompleteOrganizationArCheckCommandHandler(_db, reviewer, _notifications);
        var early = await arHandler.Handle(
            new CompleteOrganizationArCheckCommand(org.Id, "MC900001", "AR-1", null, null), CancellationToken.None);
        early.IsFailure.Should().BeTrue();
        early.Error.Code.Should().Be("Organization.InvalidStatus");

        var validate = await new ValidateOrganizationCommandHandler(_db, reviewer)
            .Handle(new ValidateOrganizationCommand(org.Id, "RUT verificado"), CancellationToken.None);
        validate.IsSuccess.Should().BeTrue();
        org.RegistrationStatus.Should().Be(OrganizationStatus.PendingArCheck);
        org.ValidatedBy.Should().Be("revisor@hlag.com");

        var approved = await arHandler.Handle(
            new CompleteOrganizationArCheckCommand(org.Id, "mc900001", "AR-1", ["CL", "BO"], null), CancellationToken.None);

        approved.IsSuccess.Should().BeTrue();
        org.RegistrationStatus.Should().Be(OrganizationStatus.Approved);
        org.MatchCode.Should().Be("MC900001");
        org.ArCheckedBy.Should().Be("revisor@hlag.com");
        org.OperatingCountries.Should().Be("CL,BO");
        await _notifications.Received(1).PublishAsync(
            Arg.Is<NotificationRequest>(n => n.UserId == orgAdmin.Id && n.Type == NotificationTypes.OrganizationApproved),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ArCheck_DuplicateMatchCode_ShouldFail()
    {
        var existing = AccessTestData.AddOrganization(_db);
        existing.MatchCode = "MC1";
        var org = AccessTestData.AddOrganization(_db, status: OrganizationStatus.PendingArCheck);
        var handler = new CompleteOrganizationArCheckCommandHandler(_db, Substitute.For<ICurrentUserService>(), _notifications);

        var result = await handler.Handle(
            new CompleteOrganizationArCheckCommand(org.Id, "MC1", null, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Organization.MatchCodeExists");
        org.RegistrationStatus.Should().Be(OrganizationStatus.PendingArCheck);
    }

    [Fact]
    public async Task Reject_ShouldCloseRegistration()
    {
        var org = AccessTestData.AddOrganization(_db, status: OrganizationStatus.PendingValidation);
        var handler = new RejectOrganizationCommandHandler(_db, Substitute.For<ICurrentUserService>(), _notifications);

        var result = await handler.Handle(new RejectOrganizationCommand(org.Id, "Documentación incompleta"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        org.RegistrationStatus.Should().Be(OrganizationStatus.Rejected);
        org.ReviewNotes.Should().Be("Documentación incompleta");
    }

    [Fact]
    public async Task OperatingCountry_SingleCountryOrganization_ShouldBeFixed()
    {
        var org = AccessTestData.AddOrganization(_db, country: "BO");
        org.OperatingCountries = "BO";
        var user = AccessTestData.AddMember(_db, org);
        var currentUser = AccessTestData.CurrentUser(user);

        var current = await new GetOperatingCountryQueryHandler(_db, currentUser)
            .Handle(new GetOperatingCountryQuery(), CancellationToken.None);
        var change = await new SetOperatingCountryCommandHandler(_db, currentUser)
            .Handle(new SetOperatingCountryCommand("CL"), CancellationToken.None);

        current.Value.Country.Should().Be("BO");
        current.Value.CanChange.Should().BeFalse();
        change.IsFailure.Should().BeTrue();
        change.Error.Code.Should().Be("Organization.CountryNotAvailable");
    }

    [Fact]
    public async Task OperatingCountry_BothCountries_ShouldBeSelectable()
    {
        var org = AccessTestData.AddOrganization(_db);
        org.OperatingCountries = "CL,BO";
        var user = AccessTestData.AddMember(_db, org);
        var currentUser = AccessTestData.CurrentUser(user);

        var result = await new SetOperatingCountryCommandHandler(_db, currentUser)
            .Handle(new SetOperatingCountryCommand("BO"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Country.Should().Be("BO");
        result.Value.AvailableCountries.Should().Equal("CL", "BO");
        result.Value.CanChange.Should().BeTrue();
        user.Country.Should().Be("BO");
    }

    [Fact]
    public async Task Logout_ShouldRevokeRefreshToken()
    {
        var user = AccessTestData.AddMember(_db, AccessTestData.AddOrganization(_db));
        user.RefreshToken = "rt";
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1);

        var result = await new LogoutCommandHandler(_db, AccessTestData.CurrentUser(user))
            .Handle(new LogoutCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiryTime.Should().BeNull();
    }

    [Fact]
    public async Task Logout_WithExpiredAccessToken_ShouldRevokeByRefreshToken()
    {
        var user = AccessTestData.AddMember(_db, AccessTestData.AddOrganization(_db));
        user.RefreshToken = "rt-anon";

        var result = await new LogoutCommandHandler(_db, Substitute.For<ICurrentUserService>())
            .Handle(new LogoutCommand("rt-anon"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.RefreshToken.Should().BeNull();
    }
}
