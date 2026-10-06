using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using HapagPortal.Application.WebService;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HapagPortal.Infrastructure.Authentication;

/// <summary>Esquema de autenticación del canal Web Service (M3-17): clave en el encabezado <c>X-Api-Key</c>.</summary>
public static class ApiKeyDefaults
{
    public const string Scheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    /// <summary>Clave de <c>HttpContext.Items</c> con la <see cref="ApiClientIdentity"/> autenticada.</summary>
    public const string IdentityItem = "ws.identity";
}

/// <summary>
/// Autenticación de los clientes del canal Web Service por su clave (NF-09). Solo la piden las rutas del canal
/// (<c>/api/ws/v1</c>); el portal sigue con el token JWT (el esquema por defecto), de modo que una clave no abre el portal
/// ni un token abre el canal. El principal es el usuario técnico del cliente, con los roles y permisos de su perfil, más los
/// claims del cliente, la clave, sus alcances y el canal: el resto del portal (evaluador de accesos, registro de NF-14) lo
/// trata como a un usuario de esa organización.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyDefaults.HeaderName, out var values) || string.IsNullOrWhiteSpace(values.ToString()))
            return AuthenticateResult.NoResult();

        var authenticator = Context.RequestServices.GetRequiredService<ApiClientAuthenticator>();
        var identity = await authenticator.AuthenticateAsync(values.ToString(), DateTime.UtcNow, Context.RequestAborted);
        if (identity is null)
            return AuthenticateResult.Fail(DomainErrors.ApiClient.InvalidKey.Message);

        Context.Items[ApiKeyDefaults.IdentityItem] = identity;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, identity.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, identity.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, identity.UserEmail),
            new("clientId", identity.OrganizationId.ToString()),
            new("country", identity.Country),
            new(ApiClientClaims.ClientId, identity.ApiClientId.ToString()),
            new(ApiClientClaims.KeyId, identity.KeyId.ToString()),
            new(ApiClientClaims.Channel, DocumentChannels.WebService)
        };
        claims.AddRange(identity.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(identity.Permissions.Select(p => new Claim("permission", p)));
        claims.AddRange(identity.Scopes.Select(s => new Claim(ApiClientClaims.Scope, s)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ApiKeyDefaults.Scheme));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, ApiKeyDefaults.Scheme));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        WriteProblemAsync(StatusCodes.Status401Unauthorized, DomainErrors.ApiClient.InvalidKey.Code, DomainErrors.ApiClient.InvalidKey.Message);

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        WriteProblemAsync(StatusCodes.Status403Forbidden, "Error.Forbidden", "The client is not allowed to perform this operation.");

    private Task WriteProblemAsync(int status, string title, string detail)
    {
        Response.StatusCode = status;
        Response.ContentType = "application/problem+json";
        if (status == StatusCodes.Status401Unauthorized)
            Response.Headers.WWWAuthenticate = $"{ApiKeyDefaults.Scheme} header=\"{ApiKeyDefaults.HeaderName}\"";

        return Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title, Detail = detail }, Context.RequestAborted);
    }
}
