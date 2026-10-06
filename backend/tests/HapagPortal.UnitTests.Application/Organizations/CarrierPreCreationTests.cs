namespace HapagPortal.UnitTests.Application.Organizations;

using FluentAssertions;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Auth.Login;
using HapagPortal.Application.Auth.Register;
using HapagPortal.Application.Auth.RequestMembership;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Organizations.Carriers;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Pre-creación de transportistas (M1-09): perfil sin cuenta con BL asignados como accesos pendientes, sin duplicados,
/// detección al registrarse (dirige al ingreso) y vinculación automática en el primer ingreso.
/// </summary>
public sealed class CarrierPreCreationTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly FakeNotificationPublisher _notifications = new();
    private readonly ThirdPartyTestData.Actor _customer;
    private readonly BillOfLading _bl;

    public CarrierPreCreationTests()
    {
        _hasher.Hash(Arg.Any<string>()).Returns("hash");
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _customer = ThirdPartyTestData.Organization(_db, name: "Importadora");
        _bl = AccessTestData.AddBl(_db, _customer.Organization.Id, "BL-CARRIER", bookingNumber: "BKG-CARRIER");
    }

    private PreCreateCarrierCommandHandler PreCreate(ThirdPartyTestData.Actor actor) =>
        new(_db, actor.CurrentUser, actor.Evaluator(_db), _hasher, _email);

    private static PreCreateCarrierCommand Command(string taxId = "77.123.321-5", string email = "contacto@cordillera.cl", params string[] bls) =>
        new("Transportes Cordillera", taxId, email, BlNumbers: bls, DurationDays: 90);

    private LoginCommandHandler Login()
    {
        var permissions = Substitute.For<IPermissionResolver>();
        permissions.ResolveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns([AccessPermissions.OperateShipments]);
        return new LoginCommandHandler(_db, _hasher, Substitute.For<IJwtTokenService>(), permissions, _notifications);
    }

    [Fact]
    public async Task PreCreate_ShouldCreateThePendingCarrier_InviteItsUser_AndAssignPendingAccess()
    {
        var result = await PreCreate(_customer).Handle(Command(bls: "BL-CARRIER"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        result.Value.Created.Should().BeTrue();
        result.Value.InvitationSent.Should().BeTrue();
        result.Value.Assigned.Should().Be(1);

        var carrier = _db.ClientList.Single(c => c.OrganizationType == OrganizationTypes.Carrier);
        carrier.RegistrationStatus.Should().Be(OrganizationStatus.PreCreated);
        var user = _db.UserList.Single(u => u.ClientId == carrier.Id);
        user.Email.Should().Be("contacto@cordillera.cl");
        user.PasswordResetToken.Should().NotBeNullOrEmpty();
        _db.UserRoleList.Should().Contain(r => r.UserId == user.Id && r.RoleName == RoleCodes.OrgAdmin);

        var grant = _db.AccessGrantList.Should().ContainSingle().Subject;
        grant.Status.Should().Be(AccessGrantStatus.PendingActivation);
        grant.GranteeClientId.Should().Be(carrier.Id);
        grant.DurationDays.Should().Be(90);
        result.Value.PreRegistration.Assignments.Should().ContainSingle().Which.Status.Should().Be(AccessGrantStatus.PendingActivation);
        _db.AccessAuditEntryList.Select(e => e.EventType).Should().Contain([AccessAuditEvents.CarrierPreCreated, AccessAuditEvents.GrantCreated]);
        await _email.Received(1).SendEmailAsync("contacto@cordillera.cl", Arg.Any<string>(), Arg.Is<string>(b => b.Contains(user.PasswordResetToken!)), Arg.Any<CancellationToken>());

        // Mientras no ingresa, el transportista no ve nada.
        var carrierActor = new ThirdPartyTestData.Actor(carrier, user, AccessTestData.CurrentUser(user, AccessPermissions.OperateShipments));
        var evaluator = carrierActor.Evaluator(_db);
        var scope = await evaluator.GetScopeAsync();
        evaluator.FilterAccessible(_db.BillsOfLading, scope).Should().BeEmpty();
    }

    [Fact]
    public async Task PreCreate_TheSameCarrier_ShouldReuseTheProfile()
    {
        await PreCreate(_customer).Handle(Command(), CancellationToken.None);
        var other = ThirdPartyTestData.Organization(_db, name: "Exportadora");
        var otherBl = AccessTestData.AddBl(_db, other.Organization.Id, "BL-OTHER");

        // Mismo RUT con otro formato y otro correo: se reconoce el perfil pre-creado.
        var second = await PreCreate(other).Handle(Command("77123321-5", "otro@cordillera.cl", "BL-OTHER"), CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        second.Value.Created.Should().BeFalse();
        second.Value.InvitationSent.Should().BeFalse();
        _db.ClientList.Count(c => c.OrganizationType == OrganizationTypes.Carrier).Should().Be(1);
        _db.UserList.Count(u => u.Email.EndsWith("@cordillera.cl")).Should().Be(1);
        _db.CarrierPreRegistrationList.Should().HaveCount(2);
        _db.AccessGrantList.Should().ContainSingle(g => g.BillOfLadingId == otherBl.Id);
    }

    [Fact]
    public async Task PreCreate_AnAlreadyRegisteredOrNonCarrierOrganization_ShouldFail()
    {
        var registered = AccessTestData.AddOrganization(_db, OrganizationTypes.Carrier, name: "Registrado");
        var customsAgency = AccessTestData.AddOrganization(_db, OrganizationTypes.CustomsAgency);

        var byRegistered = await PreCreate(_customer).Handle(Command(registered.TaxId), CancellationToken.None);
        var byAgency = await PreCreate(_customer).Handle(Command(customsAgency.TaxId), CancellationToken.None);
        var byUserEmail = await PreCreate(_customer).Handle(Command("11.111.111-1", _customer.User.Email), CancellationToken.None);

        byRegistered.Error.Code.Should().Be("CarrierPreCreation.AlreadyRegistered");
        byAgency.Error.Code.Should().Be("CarrierPreCreation.NotACarrier");
        byUserEmail.Error.Code.Should().Be("AccessGrant.SelfGrant");
        _db.CarrierPreRegistrationList.Should().BeEmpty();
    }

    [Fact]
    public async Task PreCreate_ByACarrier_ShouldNotBeAllowed()
    {
        var carrier = ThirdPartyTestData.Organization(_db, OrganizationTypes.Carrier);

        var result = await PreCreate(carrier).Handle(Command(), CancellationToken.None);

        result.Error.Code.Should().Be("CarrierPreCreation.NotAllowed");
    }

    [Fact]
    public async Task Registering_ThePreCreatedCarrier_ShouldBeDirectedToLogin()
    {
        await PreCreate(_customer).Handle(Command(), CancellationToken.None);
        var register = new RegisterCommandHandler(_db, _hasher, _email);
        var join = new RequestOrganizationMembershipCommandHandler(_db, _hasher, _notifications);
        var organizations = _db.ClientList.Count;

        var byEmail = await register.Handle(new RegisterCommand(
            "Cordillera", "99.999.999-9", CountryCodes.Chile, "CONTACTO@cordillera.cl", "Password1!", "Client", null, null,
            OrganizationTypes.Carrier), CancellationToken.None);
        var byTaxId = await register.Handle(new RegisterCommand(
            "Cordillera", "77123321-5", CountryCodes.Chile, "nuevo@cordillera.cl", "Password1!", "Client", null, null,
            OrganizationTypes.Carrier), CancellationToken.None);
        var byJoin = await join.Handle(new RequestOrganizationMembershipCommand(
            "77.123.321-5", CountryCodes.Chile, "nuevo2@cordillera.cl", "Password1!", "N", "U", null), CancellationToken.None);

        byEmail.Error.Code.Should().Be("Registration.PreCreatedAccountExists");
        byTaxId.Error.Code.Should().Be("Registration.PreCreatedAccountExists");
        byJoin.Error.Code.Should().Be("Registration.PreCreatedAccountExists");
        _db.ClientList.Should().HaveCount(organizations);
    }

    [Fact]
    public async Task FirstLogin_ShouldActivateTheCarrier_AndLinkItToTheAssignedShipments()
    {
        await PreCreate(_customer).Handle(Command(bls: "BL-CARRIER"), CancellationToken.None);
        var carrier = _db.ClientList.Single(c => c.OrganizationType == OrganizationTypes.Carrier);
        var user = _db.UserList.Single(u => u.ClientId == carrier.Id);
        user.Client = carrier;

        var login = await Login().Handle(new LoginCommand(user.Email, "Password1!"), CancellationToken.None);

        login.IsSuccess.Should().BeTrue();
        login.Value.Organization!.Status.Should().Be(OrganizationStatus.Approved);
        carrier.RegistrationStatus.Should().Be(OrganizationStatus.Approved);
        _db.CarrierPreRegistrationList.Single().Status.Should().Be(CarrierPreRegistrationStatus.Activated);
        var grant = _db.AccessGrantList.Single();
        grant.Status.Should().Be(AccessGrantStatus.Active);
        grant.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddDays(90), TimeSpan.FromMinutes(1));
        _db.AccessAuditEntryList.Should().Contain(e => e.EventType == AccessAuditEvents.GrantActivated && e.AccessGrantId == grant.Id);
        _db.AccessAuditEntryList.Should().Contain(e => e.EventType == AccessAuditEvents.CarrierActivated);
        _notifications.Published.Should().Contain(n => n.Type == NotificationTypes.CarrierActivated && n.UserId == _customer.User.Id);

        // Ya ve el BL asignado, como tercero de la Ola B.
        var carrierActor = new ThirdPartyTestData.Actor(carrier, user, AccessTestData.CurrentUser(user, AccessPermissions.OperateShipments));
        var evaluator = carrierActor.Evaluator(_db);
        var scope = await evaluator.GetScopeAsync();
        evaluator.FilterAccessible(_db.BillsOfLading, scope).Select(b => b.BLNumber).Should().Equal("BL-CARRIER");

        // Un segundo ingreso no vuelve a activar nada.
        _notifications.Published.Clear();
        await Login().Handle(new LoginCommand(user.Email, "Password1!"), CancellationToken.None);
        _notifications.Published.Should().BeEmpty();
    }

    [Fact]
    public async Task Assign_MoreReferences_BeforeActivation_ShouldAddPendingAccess_AndSkipDuplicates()
    {
        var created = await PreCreate(_customer).Handle(Command(bls: "BL-CARRIER"), CancellationToken.None);
        var second = AccessTestData.AddBl(_db, _customer.Organization.Id, "BL-SECOND", bookingNumber: "BKG-SECOND");

        var result = await new AssignCarrierReferencesCommandHandler(_db, _customer.CurrentUser, _customer.Evaluator(_db)).Handle(
            new AssignCarrierReferencesCommand(created.Value.PreRegistration.Id, ["BL-CARRIER"], ["BKG-SECOND"]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Assigned.Should().Be(1);
        result.Value.Skipped.Should().ContainSingle().Which.Code.Should().Be("CarrierPreCreation.AlreadyAssigned");
        _db.AccessGrantList.Should().HaveCount(2).And.OnlyContain(g => g.Status == AccessGrantStatus.PendingActivation);
        _db.AccessGrantList.Should().Contain(g => g.BillOfLadingId == second.Id);
    }

    [Fact]
    public async Task ResendInvitation_ShouldOnlyActForPendingPreCreatedAccounts()
    {
        await PreCreate(_customer).Handle(Command(), CancellationToken.None);
        var user = _db.UserList.Single(u => u.Email == "contacto@cordillera.cl");
        user.Client = _db.ClientList.Single(c => c.Id == user.ClientId);
        var firstToken = user.PasswordResetToken;
        _email.ClearReceivedCalls();
        var handler = new ResendCarrierInvitationCommandHandler(_db, _email);

        var unknown = await handler.Handle(new ResendCarrierInvitationCommand("nadie@x.cl"), CancellationToken.None);
        var resent = await handler.Handle(new ResendCarrierInvitationCommand("Contacto@Cordillera.cl"), CancellationToken.None);

        unknown.IsSuccess.Should().BeTrue();
        resent.IsSuccess.Should().BeTrue();
        user.PasswordResetToken.Should().NotBe(firstToken);
        await _email.Received(1).SendEmailAsync("contacto@cordillera.cl", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
