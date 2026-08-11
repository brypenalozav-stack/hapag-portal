using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace HapagPortal.Infrastructure.Secrets;

public sealed class SecretResolver(IApplicationDbContext dbContext, ISecretProtector protector) : ISecretResolver
{
    public async Task<string?> ResolveAsync(string type, Guid? clientId, CancellationToken cancellationToken = default)
    {
        // Precedencia: credencial del cliente (si la hay) antes que la global.
        if (clientId is not null)
        {
            var clientSecret = await dbContext.SecretCredentials
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.Type == type && s.Scope == ConfigurationScopes.Client && s.ClientId == clientId,
                    cancellationToken);

            if (clientSecret is not null)
                return protector.Unprotect(clientSecret.EncryptedValue);
        }

        var globalSecret = await dbContext.SecretCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Type == type && s.Scope == ConfigurationScopes.Global,
                cancellationToken);

        return globalSecret is null ? null : protector.Unprotect(globalSecret.EncryptedValue);
    }
}
