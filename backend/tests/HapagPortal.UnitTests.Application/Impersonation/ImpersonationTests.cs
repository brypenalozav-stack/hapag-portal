namespace HapagPortal.UnitTests.Application.Impersonation;

using System.Text.Json;
using FluentAssertions;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Auth.Logout;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Impersonation;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// «Vista como cliente» (M8-08): solo usuarios internos autorizados, sobre usuarios de organizaciones cliente, sin
/// anidamiento, con vencimiento, solo consulta en el servidor y auditoría de cada solicitud con la identidad del actor;
/// nunca modifica las credenciales del cliente.
/// </summary>
public sealed class ImpersonationTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IPermissionResolver _permissions = Substitute.For<IPermissionResolver>();
    private readonly ImpersonationSettings _settings = new();
    private readonly User _admin;
    private readonly Client _client;
    private readonly User _clientUser;

    public ImpersonationTests()
    {
        var hapag = AccessTestData.AddOrganization(_db, OrganizationTypes.Internal, name: "Hapag-Lloyd");
        _admin = AccessTestData.AddMember(_db, hapag, profile: RoleCodes.Administrador);
        _client = AccessTestData.AddOrganization(_db, name: "Cliente");
        _clientUser = AccessTestData.AddMember(_db, _client);
        _clientUser.RefreshToken = "client-refresh-token";
        _clientUser.PasswordHash = "client-hash";

        _permissions.ResolveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns([AccessPermissions.OperateShipments, AccessPermissions.ManageThirdPartyAccess]);
        _jwt.GenerateImpersonationToken(Arg.Any<User>(), Arg.Any<IList<string>>(), Arg.Any<IList<string>>(), Arg.Any<ImpersonationSession>())
            .Returns("impersonation-token");
    }

    private ICurrentUserService AdminUser(bool impersonating = false)
    {
        var current = AccessTestData.CurrentUser(_admin, AdministrationPermissions.UseImpersonation);
        if (impersonating)
            current.ImpersonationSessionId.Returns(Guid.NewGuid());
        current.IsImpersonating.Returns(impersonating);
        return current;
    }

    private StartImpersonationCommandHandler Start(ICurrentUserService actor) => new(_db, actor, _permissions, _jwt, _settings);

    private async Task<ImpersonationSession> StartedSessionAsync()
    {
        var result = await Start(AdminUser()).Handle(
            new StartImpersonationCommand(_client.Id, _clientUser.Id, "Ticket CS-1"), CancellationToken.None);
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        return _db.ImpersonationSessionList.Single(s => s.Id == result.Value.Session.Id);
    }

    [Fact]
    public async Task Start_ShouldIssueAClientTokenMarkedAsImpersonation_WithoutTouchingCredentials()
    {
        var result = await Start(AdminUser()).Handle(
            new StartImpersonationCommand(_client.Id, _clientUser.Id, "Ticket CS-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Auth.Token.Should().Be("impersonation-token");
        result.Value.Auth.RefreshToken.Should().BeNull();
        result.Value.Auth.User!.Id.Should().Be(_clientUser.Id);
        result.Value.Auth.Organization!.Id.Should().Be(_client.Id);
        result.Value.Session.ReadOnly.Should().BeTrue();
        result.Value.Session.Actor.UserId.Should().Be(_admin.Id);
        result.Value.Session.Subject.UserId.Should().Be(_clientUser.Id);

        var session = _db.ImpersonationSessionList.Should().ContainSingle().Subject;
        session.Status.Should().Be(ImpersonationStatus.Active);
        session.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromMinutes(1));
        _jwt.Received(1).GenerateImpersonationToken(_clientUser, Arg.Any<IList<string>>(), Arg.Any<IList<string>>(), session);
        _jwt.DidNotReceive().GenerateRefreshToken();

        _clientUser.RefreshToken.Should().Be("client-refresh-token");
        _clientUser.PasswordHash.Should().Be("client-hash");
        _clientUser.LastLoginAt.Should().BeNull();

        var audit = _db.AuditLogList.Should().ContainSingle().Subject;
        audit.EntityName.Should().Be(ImpersonationAuditActions.EntityName);
        audit.Action.Should().Be(ImpersonationAuditActions.Started);
        audit.UserId.Should().Be(_admin.Id.ToString());
        audit.NewValues.Should().Contain(_clientUser.Email).And.Contain("Ticket CS-1");
    }

    [Fact]
    public async Task Start_ShouldNotBeNested()
    {
        var result = await Start(AdminUser(impersonating: true)).Handle(
            new StartImpersonationCommand(_client.Id, _clientUser.Id, "x"), CancellationToken.None);

        result.Error.Code.Should().Be("Impersonation.Nested");
        _db.ImpersonationSessionList.Should().BeEmpty();
    }

    [Fact]
    public async Task Start_ByAClientUserWithThePermission_ShouldFail()
    {
        var other = AccessTestData.AddOrganization(_db);
        var clientAdmin = AccessTestData.AddMember(_db, other);

        var result = await Start(AccessTestData.CurrentUser(clientAdmin, AdministrationPermissions.UseImpersonation)).Handle(
            new StartImpersonationCommand(_client.Id, _clientUser.Id, "x"), CancellationToken.None);

        result.Error.Code.Should().Be("Impersonation.NotInternalActor");
    }

    [Fact]
    public async Task Start_OnInternalUsersOrOrganizations_ShouldFail()
    {
        var hapag = _db.ClientList.Single(c => c.OrganizationType == OrganizationTypes.Internal);
        var otherAdmin = AccessTestData.AddMember(_db, hapag, profile: RoleCodes.Administrador);
        var clientWithInternalRole = AccessTestData.AddMember(_db, _client, profile: RoleCodes.Supervisor);
        var inactive = AccessTestData.AddMember(_db, _client, isActive: false);

        var internalOrg = await Start(AdminUser()).Handle(new StartImpersonationCommand(hapag.Id, otherAdmin.Id, "x"), CancellationToken.None);
        var internalRole = await Start(AdminUser()).Handle(new StartImpersonationCommand(_client.Id, clientWithInternalRole.Id, "x"), CancellationToken.None);
        var inactiveUser = await Start(AdminUser()).Handle(new StartImpersonationCommand(_client.Id, inactive.Id, "x"), CancellationToken.None);
        var wrongOrg = await Start(AdminUser()).Handle(new StartImpersonationCommand(_client.Id, otherAdmin.Id, "x"), CancellationToken.None);

        internalOrg.Error.Code.Should().Be("Impersonation.TargetNotAllowed");
        internalRole.Error.Code.Should().Be("Impersonation.TargetNotAllowed");
        inactiveUser.Error.Code.Should().Be("Impersonation.TargetNotAllowed");
        wrongOrg.Error.Code.Should().Be("Impersonation.TargetNotFound");
        _db.ImpersonationSessionList.Should().BeEmpty();
    }

    [Fact]
    public async Task Start_ShouldReplaceThePreviousActiveSessionOfTheActor()
    {
        var first = await StartedSessionAsync();
        var second = await StartedSessionAsync();

        first.Status.Should().Be(ImpersonationStatus.Ended);
        first.EndReason.Should().Be(ImpersonationEndReasons.Replaced);
        second.Status.Should().Be(ImpersonationStatus.Active);
    }

    [Theory]
    [InlineData("GET", "/api/v1/shipments", true)]
    [InlineData("HEAD", "/api/v1/shipments", true)]
    [InlineData("POST", "/api/v1/cart/items", false)]
    [InlineData("PUT", "/api/v1/access/grants/1", false)]
    [InlineData("DELETE", "/api/v1/organizations/me/parent-company", false)]
    [InlineData("POST", "/api/v1/impersonation/end", true)]
    [InlineData("POST", "/api/v1/auth/logout", true)]
    public async Task Guard_ShouldBlockWritesWhileImpersonating(string method, string path, bool allowed)
    {
        var session = await StartedSessionAsync();

        var decision = await new ImpersonationGuard(_db, _settings).CheckAsync(session.Id, method, path, CancellationToken.None);

        decision.Allowed.Should().Be(allowed);
        if (!allowed)
        {
            decision.Error!.Code.Should().Be("Impersonation.ReadOnly");
            decision.SessionEnded.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Guard_ShouldAllowOnlyConfiguredWrites()
    {
        var settings = new ImpersonationSettings { AllowedActions = ["POST /api/v1/notifications"] };
        var session = await StartedSessionAsync();
        var guard = new ImpersonationGuard(_db, settings);

        (await guard.CheckAsync(session.Id, "POST", "/api/v1/notifications/abc/read", CancellationToken.None)).Allowed.Should().BeTrue();
        (await guard.CheckAsync(session.Id, "POST", "/api/v1/notifications-other", CancellationToken.None)).Allowed.Should().BeFalse();
        (await guard.CheckAsync(session.Id, "PUT", "/api/v1/notifications/preferences", CancellationToken.None)).Allowed.Should().BeFalse();
    }

    [Fact]
    public async Task Guard_ShouldEndExpiredOrClosedSessions()
    {
        var session = await StartedSessionAsync();
        session.StartedAt = DateTime.UtcNow.AddMinutes(-31);
        session.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        var guard = new ImpersonationGuard(_db, _settings);

        var expired = await guard.CheckAsync(session.Id, "GET", "/api/v1/shipments", CancellationToken.None);
        var unknown = await guard.CheckAsync(Guid.NewGuid(), "GET", "/api/v1/shipments", CancellationToken.None);

        expired.Allowed.Should().BeFalse();
        expired.SessionEnded.Should().BeTrue();
        expired.Error!.Code.Should().Be("Impersonation.Ended");
        session.Status.Should().Be(ImpersonationStatus.Expired);
        session.EndReason.Should().Be(ImpersonationEndReasons.Expired);
        session.EndedAt.Should().Be(session.ExpiresAt);
        session.DurationSeconds.Should().BeInRange(29 * 60, 31 * 60);
        unknown.SessionEnded.Should().BeTrue();
        _db.AuditLogList.Should().Contain(a => a.Action == ImpersonationAuditActions.Ended);
    }

    [Fact]
    public async Task Requests_ShouldBeAuditedWithTheActorIdentity()
    {
        var session = await StartedSessionAsync();
        var guard = new ImpersonationGuard(_db, _settings);

        await guard.RecordAsync(session.Id, "GET", "/api/v1/shipments", 200, blocked: false, "10.0.0.1", CancellationToken.None);
        await guard.RecordAsync(session.Id, "POST", "/api/v1/cart/items", 403, blocked: true, "10.0.0.1", CancellationToken.None);

        session.RequestCount.Should().Be(1);
        session.BlockedCount.Should().Be(1);
        session.SourceAddress.Should().Be("10.0.0.1");
        var requests = _db.AuditLogList.Where(a => a.Action is ImpersonationAuditActions.Request or ImpersonationAuditActions.BlockedWrite).ToList();
        requests.Should().HaveCount(2).And.OnlyContain(a => a.UserId == _admin.Id.ToString() && a.EntityId == session.Id.ToString());

        var listed = await new GetImpersonationRequestsQueryHandler(_db).Handle(new GetImpersonationRequestsQuery(session.Id), CancellationToken.None);
        listed.Value.Items.Should().Contain(r => r.Action == ImpersonationAuditActions.BlockedWrite && r.Path == "/api/v1/cart/items" && r.StatusCode == 403);
    }

    [Fact]
    public async Task End_ShouldRecordDuration_AndLogoutShouldEndWithoutRevokingTheClientsRefreshToken()
    {
        var manual = await StartedSessionAsync();
        var actor = Substitute.For<ICurrentUserService>();
        actor.ImpersonationSessionId.Returns(manual.Id);
        actor.UserId.Returns(_clientUser.Id);

        var ended = await new EndCurrentImpersonationCommandHandler(_db, actor, _settings).Handle(new EndCurrentImpersonationCommand(), CancellationToken.None);

        ended.Value.Status.Should().Be(ImpersonationStatus.Ended);
        ended.Value.EndReason.Should().Be(ImpersonationEndReasons.Manual);
        ended.Value.DurationSeconds.Should().NotBeNull();

        var byLogout = await StartedSessionAsync();
        var logoutUser = Substitute.For<ICurrentUserService>();
        logoutUser.ImpersonationSessionId.Returns(byLogout.Id);
        logoutUser.UserId.Returns(_clientUser.Id);

        var logout = await new LogoutCommandHandler(_db, logoutUser).Handle(new LogoutCommand(), CancellationToken.None);

        logout.IsSuccess.Should().BeTrue();
        byLogout.Status.Should().Be(ImpersonationStatus.Ended);
        byLogout.EndReason.Should().Be(ImpersonationEndReasons.Logout);
        _clientUser.RefreshToken.Should().Be("client-refresh-token");
    }

    [Fact]
    public async Task Sessions_ShouldListActorClientOrganizationAndDuration()
    {
        var session = await StartedSessionAsync();
        await new EndImpersonationSessionCommandHandler(_db, _settings).Handle(new EndImpersonationSessionCommand(session.Id), CancellationToken.None);

        var listed = await new GetImpersonationSessionsQueryHandler(_db, _settings)
            .Handle(new GetImpersonationSessionsQuery(OrganizationId: _client.Id), CancellationToken.None);

        var item = listed.Value.Items.Should().ContainSingle().Subject;
        item.Actor.Email.Should().Be(_admin.Email);
        item.Subject.Email.Should().Be(_clientUser.Email);
        item.Organization.Name.Should().Be("Cliente");
        item.EndReason.Should().Be(ImpersonationEndReasons.Admin);
        item.DurationSeconds.Should().NotBeNull();

        var details = JsonDocument.Parse(_db.AuditLogList.Single(a => a.Action == ImpersonationAuditActions.Ended).NewValues!);
        details.RootElement.GetProperty("details").GetProperty("reason").GetString().Should().Be(ImpersonationEndReasons.Admin);
    }

    [Fact]
    public async Task Targets_ShouldFlagEligibleUsers()
    {
        AccessTestData.AddMember(_db, _client, profile: RoleCodes.Supervisor);

        var targets = await new GetImpersonationTargetsQueryHandler(_db).Handle(new GetImpersonationTargetsQuery(_client.Id), CancellationToken.None);

        targets.Value.Should().HaveCount(2);
        targets.Value.Single(t => t.UserId == _clientUser.Id).Eligible.Should().BeTrue();
        targets.Value.Single(t => t.UserId != _clientUser.Id).Eligible.Should().BeFalse();
    }
}
