namespace HapagPortal.UnitTests.Infrastructure.Persistence;

using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.ReleaseLetter;
using HapagPortal.Application.WebService;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.ServiceRequests;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

/// <summary>
/// Semilla e infraestructura de la Ola J sobre un proveedor EF real (InMemory): definiciones del certificado de flete y de
/// la carta de liberación con flujo propio, el certificado de Comercial Altiplano y la carta pendiente de aprobación con su
/// TATC, y el cliente Web Service de Importadora Demo (solo el hash de su clave) autenticado por el esquema <c>ApiKey</c>
/// como el usuario técnico de la organización.
/// </summary>
public sealed class DocumentRequestsAndWebServiceSeedTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public DocumentRequestsAndWebServiceSeedTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose() => _context.Dispose();

    private ShipmentAccessEvaluator EvaluatorFor(Guid userId)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        currentUser.HasPermission(Arg.Any<string>()).Returns(call => call.Arg<string>() == AccessPermissions.OperateShipments);
        return new ShipmentAccessEvaluator(_context, currentUser);
    }

    [Fact]
    public async Task Definitions_ShouldBeDedicatedBoliviaImportFlowsWithoutCharge()
    {
        var definitions = await _context.ServiceDefinitions.AsNoTracking()
            .Where(d => d.Code == ServiceDefinitionCodes.FreightCertificate || d.Code == ServiceDefinitionCodes.ReleaseLetter)
            .ToListAsync();

        definitions.Should().HaveCount(2).And.OnlyContain(d =>
            d.IsActive && d.Countries == CountryCodes.Bolivia && d.Operations == ServiceOperations.Import
            && d.PricingMode == ServicePricingModes.None && ServiceDefinitionCodes.DedicatedFlow.Contains(d.Code));
        definitions.Single(d => d.Code == ServiceDefinitionCodes.FreightCertificate).ActionCode.Should().Be(ShipmentActionCodes.GenerateFreightCertificate);
        var release = definitions.Single(d => d.Code == ServiceDefinitionCodes.ReleaseLetter);
        release.ActionCode.Should().Be(ShipmentActionCodes.GenerateReleaseLetter);
        release.ApprovalTeam.Should().Be(ServiceTeams.CustomerService);
        (await _context.Tariffs.AsNoTracking().AnyAsync(t =>
            t.ConceptCode == ChargeConceptCodes.FreightCertificate || t.ConceptCode == ChargeConceptCodes.ReleaseLetter)).Should().BeFalse();
    }

    [Fact]
    public async Task FreightCertificate_ShouldBeCompletedWithASignedTypeDocumentVisibleToTheConsigneeOnly()
    {
        var request = await _context.ServiceRequests.AsNoTracking().SingleAsync(r => r.Id == SeedDataIds.ServiceRequestFreightCertificateBL05);
        var document = await _context.ShipmentDocuments.AsNoTracking().SingleAsync(d => d.Id == SeedDataIds.DocumentFreightCertificateBL05);
        var bl = await _context.BillsOfLading.AsNoTracking().SingleAsync(b => b.Id == SeedDataIds.BL05);

        request.Status.Should().Be(ServiceRequestStatus.Completed);
        document.DocumentType.Should().Be(ShipmentDocumentTypes.FreightCertificate);
        document.GenerationKey.Should().Be(DocumentServiceRequests.GenerationKey(request.Id));
        ShipmentDocumentTypes.Signed.Should().Contain(document.DocumentType);
        JsonSerializer.Deserialize<PdfDocumentModel>(document.TemplateJson, ShipmentDocumentService.JsonOptions)!
            .SignatureNote.Should().NotBeNull();

        var altiplano = EvaluatorFor(SeedDataIds.DemoUserBO);
        var permissions = await altiplano.EvaluateAsync(await altiplano.GetScopeAsync(), bl);
        ShipmentDocumentService.CanView(permissions, ShipmentDocumentTypes.FreightCertificate).Should().BeTrue();

        var importadora = EvaluatorFor(SeedDataIds.DemoUserCL);
        var importadoraScope = await importadora.GetScopeAsync();
        (await importadora.FilterAccessible(_context.BillsOfLading.AsNoTracking(), importadoraScope).AnyAsync(b => b.Id == bl.Id))
            .Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseLetter_ShouldBePendingApprovalWithTheTatcRecordedOnSubmission()
    {
        var request = await _context.ServiceRequests.AsNoTracking().SingleAsync(r => r.Id == SeedDataIds.ServiceRequestReleaseLetterBL04);
        var letter = await _context.ReleaseLetterRequests.AsNoTracking().SingleAsync(l => l.ServiceRequestId == request.Id);
        var containers = await _context.BLContainers.AsNoTracking().Where(c => c.BillOfLadingId == SeedDataIds.BL04).Select(c => c.ContainerNumber).ToListAsync();
        var definition = await _context.ServiceDefinitions.AsNoTracking().SingleAsync(d => d.Id == request.DefinitionId);

        request.Status.Should().Be(ServiceRequestStatus.PendingApproval);
        request.AssignedTeam.Should().Be(ServiceTeams.CustomerService);
        containers.Should().Contain(request.ContainerNumbers!.Split(','));
        ServiceInputSchema.ValidateValues(ServiceInputSchema.Parse(definition.InputSchemaJson)!, request.InputValuesJson, containers, [], requireComplete: true)
            .IsValid.Should().BeTrue();
        letter.LegalEntityType.Should().Be(LegalEntityTypes.Company);
        letter.TatcStatusAtSubmission.Should().Be(TatcStatuses.NotIssued);
        JsonSerializer.Deserialize<List<ReleaseLetterTatcContainerDto>>(letter.TatcSnapshotAtSubmission!, ReleaseLetterService.JsonOptions)!
            .Should().ContainSingle(c => c.ContainerNumber == "HLXU8899001" && c.Status == TatcStatuses.NotIssued);
        letter.DocumentId.Should().BeNull();
    }

    [Fact]
    public async Task ApiClient_ShouldBeSeededWithItsTechnicalUserAndOnlyTheKeyHash()
    {
        var client = await _context.ApiClients.AsNoTracking().SingleAsync(c => c.Id == SeedDataIds.ApiClientImportadora);
        var key = await _context.ApiClientKeys.AsNoTracking().SingleAsync(k => k.ApiClientId == client.Id);
        var user = await _context.Users.AsNoTracking().SingleAsync(u => u.Id == client.TechnicalUserId);
        var roles = await _context.UserRoles.AsNoTracking().Where(r => r.UserId == user.Id).Select(r => r.RoleName).ToListAsync();

        client.Status.Should().Be(ApiClientStatus.Active);
        client.OrganizationId.Should().Be(SeedDataIds.DemoClientCL);
        ApiClientScopeList.Parse(client.Scopes).Should().BeEquivalentTo(ApiClientScopes.All);
        key.Prefix.Should().HaveLength(ApiKeys.PrefixLength);
        key.KeyHash.Should().MatchRegex("^[0-9a-f]{64}$");
        user.UserType.Should().Be(UserTypes.Technical);
        user.Email.Should().Be(ApiClients.TechnicalEmail(client.Id));
        roles.Should().Equal(RoleCodes.OrgOperator);

        var permissions = await _context.Permissions.AsNoTracking().Select(p => p.Code).ToListAsync();
        permissions.Should().Contain(ApiClientPermissions.Manage);
        (await new PermissionResolver(_context).ResolveAsync([RoleCodes.Administrador])).Should().Contain(ApiClientPermissions.Manage);
        (await new PermissionResolver(_context).ResolveAsync([RoleCodes.OrgAdmin])).Should().NotContain(ApiClientPermissions.Manage);
    }

    private (DefaultHttpContext Context, ServiceProvider Provider) HttpContextWithKey(string? key)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IApplicationDbContext>(_ => _context);
        services.AddScoped<IPermissionResolver, PermissionResolver>();
        services.AddScoped<ApiClientAuthenticator>();
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyDefaults.Scheme, _ => { });
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = provider };
        if (key is not null)
            context.Request.Headers[ApiKeyDefaults.HeaderName] = key;
        return (context, provider);
    }

    private async Task<string> IssueKeyForSeededClientAsync()
    {
        var generated = ApiKeys.Generate();
        _context.ApiClientKeys.Add(ApiClients.NewKey(SeedDataIds.ApiClientImportadora, generated, "test", DateTime.UtcNow));
        await _context.SaveChangesAsync();
        return generated.Key;
    }

    [Fact]
    public async Task ApiKeyScheme_ShouldAuthenticateTheTechnicalUserOfTheOrganization()
    {
        var key = await IssueKeyForSeededClientAsync();
        var (context, provider) = HttpContextWithKey(key);
        using var _ = provider;

        var result = await context.AuthenticateAsync(ApiKeyDefaults.Scheme);

        result.Succeeded.Should().BeTrue();
        var principal = result.Principal!;
        principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(SeedDataIds.ApiClientImportadoraUser.ToString());
        principal.FindFirstValue("clientId").Should().Be(SeedDataIds.DemoClientCL.ToString());
        principal.FindFirstValue(ApiClientClaims.ClientId).Should().Be(SeedDataIds.ApiClientImportadora.ToString());
        principal.FindFirstValue(ApiClientClaims.Channel).Should().Be(DocumentChannels.WebService);
        principal.FindAll("permission").Select(c => c.Value).Should().Contain(AccessPermissions.OperateShipments)
            .And.NotContain(AccessPermissions.ViewAllShipments);
        principal.FindAll(ApiClientClaims.Scope).Select(c => c.Value).Should().BeEquivalentTo(ApiClientScopes.All);
        context.Items[ApiKeyDefaults.IdentityItem].Should().BeOfType<ApiClientIdentity>();

        context.User = principal;
        var accessor = new HttpContextAccessor { HttpContext = context };
        var currentUser = new CurrentUserService(accessor);
        currentUser.ApiClientId.Should().Be(SeedDataIds.ApiClientImportadora);
        currentUser.UserId.Should().Be(SeedDataIds.ApiClientImportadoraUser);

        // El evaluador de accesos lo trata como a un usuario operativo de Importadora Demo (NF-05).
        var scope = await new ShipmentAccessEvaluator(_context, currentUser).GetScopeAsync();
        scope.OrganizationId.Should().Be(SeedDataIds.DemoClientCL);
        scope.IsAdmin.Should().BeFalse();
        scope.CanOperate.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("hlws_aaaaaaaaaaaa_bm90LWEtcmVhbC1zZWNyZXQtYnV0LWxvbmctZW5vdWdoLTQz", true)]
    public async Task ApiKeyScheme_ShouldNotAuthenticateWithoutAValidKey(string? key, bool failure)
    {
        var (context, provider) = HttpContextWithKey(key);
        using var _ = provider;

        var result = await context.AuthenticateAsync(ApiKeyDefaults.Scheme);

        result.Succeeded.Should().BeFalse();
        (result.Failure is not null).Should().Be(failure);
    }
}
