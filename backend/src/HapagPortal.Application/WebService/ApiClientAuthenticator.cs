namespace HapagPortal.Application.WebService;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Identidad de una solicitud del canal Web Service (M3-17): el cliente y la clave usada, y el usuario técnico con que el
/// cliente actúa como su organización (sus roles y permisos son los de su perfil, como un usuario del portal).
/// </summary>
public sealed record ApiClientIdentity(
    Guid ApiClientId,
    Guid KeyId,
    string ClientName,
    Guid OrganizationId,
    Guid UserId,
    string UserEmail,
    string Country,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> Scopes,
    int RateLimitPerMinute);

/// <summary>
/// Autenticación por clave del canal Web Service (NF-09): la clave debe existir, no estar revocada ni vencida (las claves
/// rotadas sirven hasta el fin de su período de gracia), el cliente debe estar activo, su organización aprobada y activa, y
/// su usuario técnico activo. No distingue el motivo del rechazo. Registra el último uso (a lo sumo una vez por minuto).
/// </summary>
public sealed class ApiClientAuthenticator(IApplicationDbContext dbContext, IPermissionResolver permissionResolver)
{
    private static readonly TimeSpan LastUseResolution = TimeSpan.FromMinutes(1);

    public async Task<ApiClientIdentity?> AuthenticateAsync(string? presentedKey, DateTime now, CancellationToken cancellationToken)
    {
        var prefix = ApiKeys.PrefixOf(presentedKey);
        if (prefix is null)
            return null;

        var key = await dbContext.ApiClientKeys.FirstOrDefaultAsync(k => k.Prefix == prefix, cancellationToken);
        if (key is null || !ApiKeys.Matches(presentedKey!, key.KeyHash) || !key.IsUsableAt(now))
            return null;

        var client = await dbContext.ApiClients.FirstOrDefaultAsync(c => c.Id == key.ApiClientId, cancellationToken);
        if (client is null || client.Status != ApiClientStatus.Active)
            return null;

        var organization = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == client.OrganizationId, cancellationToken);
        if (organization is null || !organization.IsActive || organization.RegistrationStatus != OrganizationStatus.Approved)
            return null;

        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == client.TechnicalUserId, cancellationToken);
        if (user is null || !user.IsActive || user.MembershipStatus != MembershipStatus.Active
            || user.UserType != UserTypes.Technical || user.ClientId != organization.Id)
            return null;

        var roles = await dbContext.UserRoles.AsNoTracking()
            .Where(r => r.UserId == user.Id)
            .Select(r => r.RoleName)
            .ToListAsync(cancellationToken);
        var permissions = await permissionResolver.ResolveAsync(roles, cancellationToken) ?? [];

        if (key.LastUsedAt is null || now - key.LastUsedAt.Value >= LastUseResolution)
        {
            key.LastUsedAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new ApiClientIdentity(
            client.Id,
            key.Id,
            client.Name,
            organization.Id,
            user.Id,
            user.Email,
            organization.Country,
            roles,
            permissions.ToList(),
            ApiClientScopeList.Parse(client.Scopes),
            client.RateLimitPerMinute);
    }
}

/// <summary>Lista de alcances guardada como CSV.</summary>
public static class ApiClientScopeList
{
    public static IReadOnlyList<string> Parse(string? csv) =>
        (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(ApiClientScopes.All.Contains)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    public static string Format(IEnumerable<string> scopes) =>
        string.Join(',', ApiClientScopes.All.Where(scopes.Contains));
}
