namespace HapagPortal.UnitTests.Application.WebService;

using FluentAssertions;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Auth.Login;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Organizations.Users;
using HapagPortal.Application.WarehouseChanges.Requests;
using HapagPortal.Application.WebService;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Canal de requerimientos vía Web Service (M3-17): alta de clientes con usuario técnico y clave mostrada una sola vez
/// (solo se guarda el hash), autenticación con claves vigentes, revocadas y rotadas (NF-09), alcances, límite por cliente,
/// idempotencia, la misma matriz de accesos que el portal (M1-11, NF-05) y la trazabilidad del canal (NF-14).
/// </summary>
public sealed class WebServiceChannelTests
{
    private readonly DocumentsFixture _f = new();
    private readonly IPermissionResolver _permissions = Substitute.For<IPermissionResolver>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ICurrentUserService _internal;

    public WebServiceChannelTests()
    {
        _permissions.ResolveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(call => (IReadOnlyList<string>)(call.Arg<IEnumerable<string>>().Contains(RoleCodes.OrgOperator)
                ? [AccessPermissions.OperateShipments]
                : []));
        _hasher.Hash(Arg.Any<string>()).Returns("$2a$12$unusable");
        _internal = AccessTestData.CurrentUser(
            AccessTestData.AddMember(_f.Db, AccessTestData.AddOrganization(_f.Db, OrganizationTypes.Internal), profile: null),
            ApiClientPermissions.Manage);
    }

    private MockApplicationDbContext Db => _f.Db;

    private async Task<ApiClientSecretDto> CreateClientAsync(
        Client organization,
        IReadOnlyList<string>? scopes = null,
        int rateLimit = 60,
        ApiClientSignatoryDto? signatory = null)
    {
        var created = await new CreateApiClientCommandHandler(Db, _internal, _hasher).Handle(new CreateApiClientCommand(
            organization.Id,
            "ERP de prueba",
            scopes ?? ApiClientScopes.All,
            rateLimit,
            signatory ?? new ApiClientSignatoryDto("Felipe Forwarder", "12.345.678-5", "Gerente", "felipe@ffww.test"),
            "ti@cliente.test",
            null), CancellationToken.None);
        created.IsSuccess.Should().BeTrue(created.IsFailure ? created.Error.Message : null);
        return created.Value;
    }

    private Task<ApiClientIdentity?> AuthenticateAsync(string key, DateTime? now = null) =>
        new ApiClientAuthenticator(Db, _permissions).AuthenticateAsync(key, now ?? DateTime.UtcNow, CancellationToken.None);

    /// <summary>Usuario actual del canal: el usuario técnico del cliente, con su clave.</summary>
    private PaymentsFixture.Actor TechnicalActor(ApiClientSecretDto secret)
    {
        var user = Db.UserList.Single(u => u.Id == secret.Client.TechnicalUserId);
        var organization = Db.ClientList.Single(c => c.Id == secret.Client.OrganizationId);
        var currentUser = AccessTestData.CurrentUser(user, AccessPermissions.OperateShipments);
        currentUser.ApiClientId.Returns(secret.Client.Id);
        return new PaymentsFixture.Actor(organization, user, currentUser);
    }

    private static ApiClientRequestStart Start(
        ApiClientIdentity identity,
        string? key = "key-1",
        string hash = "hash-1",
        bool creates = true,
        string? scope = ApiClientScopes.WarehouseChange) =>
        new(identity, ApiClientOperations.WarehouseChange, "POST", "/api/ws/v1/warehouse-changes", key, hash, "10.0.0.1", creates, scope);

    private static ApiClientIdentity Identity(int rateLimit = 60, params string[] scopes) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "ERP", Guid.NewGuid(), Guid.NewGuid(), "ws@test", CountryCodes.Chile, [RoleCodes.OrgOperator],
            [AccessPermissions.OperateShipments], scopes.Length == 0 ? ApiClientScopes.All : scopes, rateLimit);

    // ── Alta y claves (NF-09) ─────────────────────────────────────

    [Fact]
    public async Task Create_ShouldReturnTheKeyOnce_StoreOnlyItsHash_AndCreateATechnicalUser()
    {
        var secret = await CreateClientAsync(_f.Payments.Owner.Organization);

        secret.ApiKey.Should().StartWith("hlws_");
        ApiKeys.PrefixOf(secret.ApiKey).Should().Be(secret.KeyPrefix);
        var key = Db.ApiClientKeyList.Single();
        key.KeyHash.Should().Be(ApiKeys.Hash(secret.ApiKey)).And.NotContain(secret.ApiKey);
        Db.AuditLogList.Should().ContainSingle(a => a.EntityName == ApiClients.AuditEntityName && a.Action == "Created")
            .Which.NewValues.Should().NotContain(secret.ApiKey);
        secret.Client.Keys.Should().ContainSingle(k => k.Prefix == secret.KeyPrefix && k.Active);

        var user = Db.UserList.Single(u => u.Id == secret.Client.TechnicalUserId);
        user.UserType.Should().Be(UserTypes.Technical);
        user.ClientId.Should().Be(_f.Payments.Owner.Organization.Id);
        Db.UserRoleList.Should().ContainSingle(r => r.UserId == user.Id && r.RoleName == RoleCodes.OrgOperator);

        // El usuario técnico no inicia sesión ni aparece en la administración de usuarios de la organización.
        var login = await new LoginCommandHandler(Db, _hasher, Substitute.For<IJwtTokenService>(), _permissions, new FakeNotificationPublisher())
            .Handle(new LoginCommand(user.Email, "any"), CancellationToken.None);
        login.Error.Should().Be(DomainErrors.User.InvalidCredentials);
        var users = await new GetOrganizationUsersQueryHandler(Db, _f.Payments.Owner.CurrentUser).Handle(new GetOrganizationUsersQuery(), CancellationToken.None);
        users.Value.Should().NotContain(u => u.Id == user.Id);
    }

    [Fact]
    public async Task Create_ShouldOnlyAcceptApprovedClientOrganizations()
    {
        var pending = AccessTestData.AddOrganization(Db, status: OrganizationStatus.PendingValidation);
        var hapag = AccessTestData.AddOrganization(Db, OrganizationTypes.Internal);

        foreach (var organization in new[] { pending, hapag })
        {
            var result = await new CreateApiClientCommandHandler(Db, _internal, _hasher).Handle(
                new CreateApiClientCommand(organization.Id, "ERP", ApiClientScopes.All, 60, null, null, null), CancellationToken.None);
            result.Error.Should().Be(DomainErrors.ApiClient.OrganizationNotAllowed);
        }
    }

    [Fact]
    public async Task Authentication_ShouldAcceptAValidKey_AsTheTechnicalUserOfTheOrganization()
    {
        var secret = await CreateClientAsync(_f.Payments.Owner.Organization, [ApiClientScopes.WarehouseChange], rateLimit: 30);

        var identity = await AuthenticateAsync(secret.ApiKey);

        identity.Should().NotBeNull();
        identity!.ApiClientId.Should().Be(secret.Client.Id);
        identity.OrganizationId.Should().Be(_f.Payments.Owner.Organization.Id);
        identity.UserId.Should().Be(secret.Client.TechnicalUserId);
        identity.Permissions.Should().Equal(AccessPermissions.OperateShipments);
        identity.Scopes.Should().Equal(ApiClientScopes.WarehouseChange);
        identity.RateLimitPerMinute.Should().Be(30);
        Db.ApiClientKeyList.Single().LastUsedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-key")]
    [InlineData("hlws_abcdefghijkm_ZmFrZS1zZWNyZXQtd2l0aC1lbm91Z2gtbGVuZ3RoLTQz")]
    public async Task Authentication_ShouldRejectMissingMalformedOrUnknownKeys(string key)
    {
        await CreateClientAsync(_f.Payments.Owner.Organization);

        (await AuthenticateAsync(key)).Should().BeNull();
    }

    [Fact]
    public async Task Authentication_ShouldRejectAKeyWithTheRightPrefixButAnotherSecret()
    {
        var secret = await CreateClientAsync(_f.Payments.Owner.Organization);
        var forged = $"hlws_{secret.KeyPrefix}_{new string('A', 43)}";

        (await AuthenticateAsync(forged)).Should().BeNull();
    }

    [Fact]
    public async Task RevokedKeyOrClient_ShouldNoLongerAuthenticate()
    {
        var first = await CreateClientAsync(_f.Payments.Owner.Organization);
        var keyId = Db.ApiClientKeyList.Single().Id;
        await new RevokeApiClientKeyCommandHandler(Db, _internal).Handle(new RevokeApiClientKeyCommand(first.Client.Id, keyId), CancellationToken.None);
        (await AuthenticateAsync(first.ApiKey)).Should().BeNull();

        var second = await CreateClientAsync(_f.Payments.Owner.Organization);
        (await AuthenticateAsync(second.ApiKey)).Should().NotBeNull();
        var revoked = await new RevokeApiClientCommandHandler(Db, _internal)
            .Handle(new RevokeApiClientCommand(second.Client.Id, "Contrato terminado"), CancellationToken.None);

        revoked.Value.Status.Should().Be(ApiClientStatus.Revoked);
        revoked.Value.Keys.Should().OnlyContain(k => !k.Active);
        Db.UserList.Single(u => u.Id == second.Client.TechnicalUserId).IsActive.Should().BeFalse();
        (await AuthenticateAsync(second.ApiKey)).Should().BeNull();
    }

    [Fact]
    public async Task Rotation_ShouldIssueANewKey_AndEndTheOldOneNowOrAfterTheGracePeriod()
    {
        var created = await CreateClientAsync(_f.Payments.Owner.Organization);
        var rotate = new RotateApiClientKeyCommandHandler(Db, _internal);

        var immediate = await rotate.Handle(new RotateApiClientKeyCommand(created.Client.Id), CancellationToken.None);

        immediate.Value.ApiKey.Should().NotBe(created.ApiKey);
        (await AuthenticateAsync(created.ApiKey)).Should().BeNull();
        (await AuthenticateAsync(immediate.Value.ApiKey)).Should().NotBeNull();

        var graceful = await rotate.Handle(new RotateApiClientKeyCommand(created.Client.Id, GraceMinutes: 60), CancellationToken.None);
        var now = DateTime.UtcNow;

        (await AuthenticateAsync(immediate.Value.ApiKey, now.AddMinutes(30))).Should().NotBeNull();
        (await AuthenticateAsync(immediate.Value.ApiKey, now.AddMinutes(61))).Should().BeNull();
        (await AuthenticateAsync(graceful.Value.ApiKey, now.AddMinutes(61))).Should().NotBeNull();
        Db.AuditLogList.Count(a => a.Action == "KeyRotated").Should().Be(2);
    }

    [Fact]
    public async Task SuspendedOrganization_ShouldStopItsClients()
    {
        var secret = await CreateClientAsync(_f.Payments.Owner.Organization);
        _f.Payments.Owner.Organization.IsActive = false;

        (await AuthenticateAsync(secret.ApiKey)).Should().BeNull();
    }

    // ── Alcance, límite e idempotencia ────────────────────────────

    [Fact]
    public async Task Scope_ShouldBeEnforcedAndTheRejectionLogged()
    {
        var log = new ApiClientRequestLog(Db);
        var identity = Identity(scopes: ApiClientScopes.ResponsibilityLetter);

        var gate = await log.BeginAsync(Start(identity), DateTime.UtcNow, CancellationToken.None);

        gate.Proceed.Should().BeFalse();
        gate.StatusCode.Should().Be(403);
        gate.Error!.Code.Should().Be("WebService.ScopeNotGranted");
        Db.ApiClientRequestList.Should().ContainSingle(r => r.Outcome == ApiClientRequestOutcomes.Rejected && r.ErrorCode == "WebService.ScopeNotGranted");
    }

    [Fact]
    public async Task RateLimit_ShouldRejectRequestsOverTheClientLimitWithinOneMinute()
    {
        var log = new ApiClientRequestLog(Db);
        var identity = Identity(rateLimit: 2);
        var now = DateTime.UtcNow;
        Db.ApiClientRequestList.Add(new ApiClientRequest
        {
            ApiClientId = identity.ApiClientId, OrganizationId = identity.OrganizationId, TechnicalUserId = identity.UserId,
            Operation = ApiClientOperations.ListRequests, Method = "GET", Path = "/api/ws/v1/requests",
            Outcome = ApiClientRequestOutcomes.Accepted, ReceivedAt = now.AddMinutes(-2)
        });

        var first = await log.BeginAsync(Start(identity, key: null, creates: false), now, CancellationToken.None);
        var second = await log.BeginAsync(Start(identity, key: null, creates: false), now, CancellationToken.None);
        var third = await log.BeginAsync(Start(identity, key: null, creates: false), now, CancellationToken.None);
        var otherClient = await log.BeginAsync(Start(Identity(rateLimit: 2), key: null, creates: false), now, CancellationToken.None);

        first.Proceed.Should().BeTrue();
        second.Proceed.Should().BeTrue();
        third.StatusCode.Should().Be(429);
        third.Error.Should().Be(DomainErrors.ApiClient.RateLimited);
        otherClient.Proceed.Should().BeTrue();
        (await log.BeginAsync(Start(identity, key: null, creates: false), now.AddSeconds(30), CancellationToken.None)).StatusCode.Should().Be(429);
        Db.ApiClientRequestList.Count(r => r.ApiClientId == identity.ApiClientId && r.ErrorCode == "WebService.RateLimited").Should().Be(2);

        // Pasado el minuto, el cliente vuelve a operar.
        (await log.BeginAsync(Start(identity, key: null, creates: false), now.AddSeconds(91), CancellationToken.None)).Proceed.Should().BeTrue();
    }

    [Fact]
    public async Task Idempotency_ShouldReplayTheSameRequest_RejectAReusedKey_AndAllowRetryingAFailure()
    {
        var log = new ApiClientRequestLog(Db);
        var identity = Identity();
        var now = DateTime.UtcNow;

        (await log.BeginAsync(Start(identity, key: null), now, CancellationToken.None)).Error
            .Should().Be(DomainErrors.ApiClient.IdempotencyKeyRequired);

        var first = await log.BeginAsync(Start(identity), now, CancellationToken.None);
        first.Proceed.Should().BeTrue();
        (await log.BeginAsync(Start(identity), now, CancellationToken.None)).Error.Should().Be(DomainErrors.ApiClient.IdempotencyInProgress);

        await log.CompleteAsync(first.RequestId!.Value,
            new ApiClientRequestCompletion(201, null, "{\"id\":\"1\"}", ApiClientTargetTypes.WarehouseChange, Guid.NewGuid(), "STI -> B1", "BL-1"),
            now, CancellationToken.None);

        var replay = await log.BeginAsync(Start(identity), now, CancellationToken.None);
        replay.IsReplay.Should().BeTrue();
        replay.StatusCode.Should().Be(201);
        replay.ReplayJson.Should().Be("{\"id\":\"1\"}");

        var reused = await log.BeginAsync(Start(identity, hash: "other-content"), now, CancellationToken.None);
        reused.StatusCode.Should().Be(409);
        reused.Error!.Code.Should().Be("IdempotencyKey.Conflict");

        var failed = await log.BeginAsync(Start(identity, key: "key-2"), now, CancellationToken.None);
        await log.CompleteAsync(failed.RequestId!.Value, new ApiClientRequestCompletion(500, "Error.Unexpected", null, null, null, null, null),
            now, CancellationToken.None);
        var retry = await log.BeginAsync(Start(identity, key: "key-2"), now, CancellationToken.None);
        retry.Proceed.Should().BeTrue();
        retry.RequestId.Should().Be(failed.RequestId);
    }

    // ── Mismas reglas del portal y trazabilidad (M1-11, NF-05, NF-14) ─

    [Fact]
    public async Task ResponsibilityLetter_ShouldUseTheConfiguredSignatory_AndBeTracedWithTheWebServiceChannel()
    {
        var ffww = _f.Payments.NewActor(OrganizationTypes.FreightForwarder);
        _f.FreightForwarder(ffww);
        var bl = _f.Payments.Rules.OwnBl("BL-WS-FFWW", consignee: false);
        AccessTestData.AddRole(Db, bl, ffww.Organization, ShipmentRoleCodes.Consignee);
        var secret = await CreateClientAsync(ffww.Organization, signatory: new ApiClientSignatoryDto("Firma Configurada", "9.876.543-2", "Apoderado", "firma@ffww.test"));
        var technical = TechnicalActor(secret);

        var sender = new TestSender().Register(_f.Letter(technical));
        var issued = await new SubmitWsResponsibilityLetterCommandHandler(Db, technical.CurrentUser, sender).Handle(
            new SubmitWsResponsibilityLetterCommand("BL-WS-FFWW", true, ResponsibilityLetterTerms.Version, "Muebles", null, null), CancellationToken.None);

        issued.IsSuccess.Should().BeTrue(issued.IsFailure ? issued.Error.Message : null);
        issued.Value.IssuedForOrganizationId.Should().Be(ffww.Organization.Id);
        _f.Renderer.Rendered.Single().Sections.Should().Contain(s => s.Heading == "Firmante"
            && s.Fields!.Any(f => f.Label == "Nombre" && f.Value == "Firma Configurada"));
        var document = Db.ShipmentDocumentList.Single();
        document.IssuedByUserId.Should().Be(secret.Client.TechnicalUserId);
        Db.ShipmentDocumentEventList.Should().ContainSingle(e => e.ShipmentDocumentId == document.Id
            && e.EventType == ShipmentDocumentEventTypes.Issued && e.Channel == DocumentChannels.WebService);
    }

    [Fact]
    public async Task ResponsibilityLetter_ShouldApplyTheSameAccessMatrix_AndNeedTheSignatory()
    {
        var owner = _f.Payments.Owner;
        _f.Payments.Rules.OwnBl("BL-WS-CUST");
        var secret = await CreateClientAsync(owner.Organization);
        var technical = TechnicalActor(secret);
        var sender = new TestSender().Register(_f.Letter(technical));
        var command = new SubmitWsResponsibilityLetterCommand("BL-WS-CUST", true, ResponsibilityLetterTerms.Version, null, null, null);

        // Un customer no FFWW no emite la carta, igual que en el portal.
        (await new SubmitWsResponsibilityLetterCommandHandler(Db, technical.CurrentUser, sender).Handle(command, CancellationToken.None))
            .Error.Should().Be(Error.Forbidden);

        var client = Db.ApiClientList.Single();
        client.SignatoryName = null;
        (await new SubmitWsResponsibilityLetterCommandHandler(Db, technical.CurrentUser, sender).Handle(command, CancellationToken.None))
            .Error.Should().Be(DomainErrors.ApiClient.SignatoryNotConfigured);

        // Fuera del canal (usuario del portal) la operación no existe.
        (await new SubmitWsResponsibilityLetterCommandHandler(Db, owner.CurrentUser, sender).Handle(command, CancellationToken.None))
            .Error.Should().Be(Error.Forbidden);
    }

    [Fact]
    public async Task WarehouseChange_OnAnotherOrganizationsBl_ShouldNotExistForTheClient()
    {
        var stranger = _f.Payments.NewActor();
        _f.Payments.Rules.OwnBl("BL-WS-OWNER");
        var secret = await CreateClientAsync(stranger.Organization);
        var technical = TechnicalActor(secret);

        var result = await new RequestWarehouseChangeCommandHandler(Db, technical.CurrentUser, technical.Evaluator(Db), _f.Payments.Rules.WarehouseChanges())
            .Handle(new RequestWarehouseChangeCommand("BL-WS-OWNER", null, null, "Bodega 1"), CancellationToken.None);

        result.Error.Should().Be(DomainErrors.BillOfLading.NotFoundByNumber("BL-WS-OWNER"));
        Db.WarehouseChangeList.Should().BeEmpty();
    }

    [Fact]
    public async Task Requests_ShouldListOnlyTheClientsOwnSubmissions_WithTheCurrentStatusOfWhatTheyCreated()
    {
        var mine = await CreateClientAsync(_f.Payments.Owner.Organization);
        var other = await CreateClientAsync(_f.Payments.NewActor().Organization);
        var bl = _f.Payments.Rules.OwnBl("BL-WS-REQ");
        var change = new WarehouseChange
        {
            BillOfLadingId = bl.Id, FromWarehouse = "STI", ToWarehouse = "Bodega 1", Currency = "CLP", Country = CountryCodes.Chile,
            Status = WarehouseChangeStatus.Completed, IsFree = true
        };
        Db.WarehouseChangeList.Add(change);

        ApiClientRequest Row(Guid clientId, string operation, Guid? targetId = null) => new()
        {
            ApiClientId = clientId, OrganizationId = Guid.NewGuid(), TechnicalUserId = Guid.NewGuid(), Operation = operation,
            Method = "POST", Path = "/api/ws/v1", Outcome = ApiClientRequestOutcomes.Accepted, StatusCode = 201, ReceivedAt = DateTime.UtcNow,
            TargetType = targetId is null ? null : ApiClientTargetTypes.WarehouseChange, TargetId = targetId, BlNumber = "BL-WS-REQ"
        };
        var own = Row(mine.Client.Id, ApiClientOperations.WarehouseChange, change.Id);
        var foreign = Row(other.Client.Id, ApiClientOperations.WarehouseChange);
        Db.ApiClientRequestList.AddRange(own, foreign, Row(mine.Client.Id, ApiClientOperations.ListRequests));

        var technical = TechnicalActor(mine);
        var list = await new GetWsRequestsQueryHandler(Db, technical.CurrentUser).Handle(new GetWsRequestsQuery(), CancellationToken.None);
        var detail = await new GetWsRequestQueryHandler(Db, technical.CurrentUser).Handle(new GetWsRequestQuery(own.Id), CancellationToken.None);
        var notMine = await new GetWsRequestQueryHandler(Db, technical.CurrentUser).Handle(new GetWsRequestQuery(foreign.Id), CancellationToken.None);

        list.Value.Items.Should().ContainSingle().Which.Id.Should().Be(own.Id);
        detail.Value.Target!.Status.Should().Be(WarehouseChangeStatus.Completed);
        detail.Value.Target.BlNumber.Should().Be("BL-WS-REQ");
        notMine.Error.Code.Should().Be("WebServiceRequest.NotFound");
    }
}
