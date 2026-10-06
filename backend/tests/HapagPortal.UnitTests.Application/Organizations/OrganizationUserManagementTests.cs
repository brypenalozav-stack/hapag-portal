namespace HapagPortal.UnitTests.Application.Organizations;

using FluentAssertions;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Organizations.Users;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class OrganizationUserManagementTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly Client _org;
    private readonly User _admin;
    private readonly ICurrentUserService _currentUser;

    public OrganizationUserManagementTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hash");
        _org = AccessTestData.AddOrganization(_db);
        _admin = AccessTestData.AddMember(_db, _org, profile: RoleCodes.OrgAdmin);
        _currentUser = AccessTestData.CurrentUser(_admin, AccessPermissions.ManageOrganizationUsers);
    }

    [Fact]
    public async Task Create_ShouldAddUserToSameOrganizationWithProfileAndInvitation()
    {
        var handler = new CreateOrganizationUserCommandHandler(_db, _currentUser, _passwordHasher, _emailService);

        var result = await handler.Handle(
            new CreateOrganizationUserCommand("Pedro", "Pago", "Pedro@Org.cl", RoleCodes.OrgOperator, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Profile.Should().Be(RoleCodes.OrgOperator);
        var created = _db.UserList.Single(u => u.Email == "pedro@org.cl");
        created.ClientId.Should().Be(_org.Id);
        created.MembershipStatus.Should().Be(MembershipStatus.Active);
        created.PasswordResetToken.Should().NotBeNullOrEmpty();
        _db.UserRoleList.Should().Contain(r => r.UserId == created.Id && r.RoleName == RoleCodes.OrgOperator);
        await _emailService.Received(1).SendEmailAsync("pedro@org.cl", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_InPendingOrganization_ShouldFail()
    {
        _org.RegistrationStatus = OrganizationStatus.PendingValidation;
        var handler = new CreateOrganizationUserCommandHandler(_db, _currentUser, _passwordHasher, _emailService);

        var result = await handler.Handle(
            new CreateOrganizationUserCommand("A", "B", "a@b.cl", RoleCodes.OrgViewer, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Organization.NotOperational");
    }

    [Fact]
    public void CreateValidator_ShouldRejectNonOrganizationProfiles()
    {
        var validator = new CreateOrganizationUserCommandValidator();

        validator.Validate(new CreateOrganizationUserCommand("A", "B", "a@b.cl", RoleCodes.Administrador, null))
            .IsValid.Should().BeFalse();
        validator.Validate(new CreateOrganizationUserCommand("A", "B", "a@b.cl", RoleCodes.OrgViewer, null))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ShouldChangeProfileOfSameOrganizationUser()
    {
        var member = AccessTestData.AddMember(_db, _org, profile: RoleCodes.OrgViewer);
        var handler = new UpdateOrganizationUserCommandHandler(_db, _currentUser);

        var result = await handler.Handle(
            new UpdateOrganizationUserCommand(member.Id, "Nuevo", "Nombre", "+56 9", RoleCodes.OrgOperator),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Profile.Should().Be(RoleCodes.OrgOperator);
        member.FirstName.Should().Be("Nuevo");
        _db.UserRoleList.Where(r => r.UserId == member.Id).Select(r => r.RoleName).Should().Equal(RoleCodes.OrgOperator);
    }

    [Fact]
    public async Task Update_UserOfAnotherOrganization_ShouldReturnNotFound()
    {
        var foreign = AccessTestData.AddMember(_db, AccessTestData.AddOrganization(_db));
        var handler = new UpdateOrganizationUserCommandHandler(_db, _currentUser);

        var result = await handler.Handle(
            new UpdateOrganizationUserCommand(foreign.Id, "X", "Y", null, RoleCodes.OrgViewer),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task Update_OwnProfile_ShouldFail()
    {
        var handler = new UpdateOrganizationUserCommandHandler(_db, _currentUser);

        var result = await handler.Handle(
            new UpdateOrganizationUserCommand(_admin.Id, "Yo", "Mismo", null, RoleCodes.OrgViewer),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Organization.CannotChangeOwnAccount");
    }

    [Fact]
    public async Task Deactivate_ShouldRevokeSessionOfSameOrganizationUser()
    {
        var member = AccessTestData.AddMember(_db, _org, profile: RoleCodes.OrgOperator);
        member.RefreshToken = "rt";
        var handler = new SetOrganizationUserActiveCommandHandler(_db, _currentUser);

        var result = await handler.Handle(new SetOrganizationUserActiveCommand(member.Id, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        member.IsActive.Should().BeFalse();
        member.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task Deactivate_Self_ShouldFail()
    {
        var handler = new SetOrganizationUserActiveCommandHandler(_db, _currentUser);

        var result = await handler.Handle(new SetOrganizationUserActiveCommand(_admin.Id, false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _admin.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_UserOfAnotherOrganization_ShouldReturnNotFound()
    {
        var foreign = AccessTestData.AddMember(_db, AccessTestData.AddOrganization(_db));
        var handler = new SetOrganizationUserActiveCommandHandler(_db, _currentUser);

        var result = await handler.Handle(new SetOrganizationUserActiveCommand(foreign.Id, false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
        foreign.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task List_ShouldReturnOnlyActiveMembersOfOwnOrganization()
    {
        AccessTestData.AddMember(_db, _org, profile: RoleCodes.OrgViewer);
        AccessTestData.AddMember(_db, _org, MembershipStatus.Pending, profile: null);
        AccessTestData.AddMember(_db, AccessTestData.AddOrganization(_db));
        var handler = new GetOrganizationUsersQueryHandler(_db, _currentUser);

        var result = await handler.Handle(new GetOrganizationUsersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(u => u.Profile).Should().BeEquivalentTo([RoleCodes.OrgAdmin, RoleCodes.OrgViewer]);
    }
}
