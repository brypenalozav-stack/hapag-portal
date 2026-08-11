namespace HapagPortal.Application.Configuration.Secrets;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Configuration.Common;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed record GetSecretsQuery(string Scope, Guid? ClientId) : IQuery<List<SecretMetadataDto>>;

public sealed class GetSecretsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : IQueryHandler<GetSecretsQuery, List<SecretMetadataDto>>
{
    public async Task<Result<List<SecretMetadataDto>>> Handle(
        GetSecretsQuery request,
        CancellationToken cancellationToken)
    {
        var denied = ConfigurationAccess.Check(currentUser, request.Scope, request.ClientId);
        if (denied is not null)
            return Result<List<SecretMetadataDto>>.Failure(denied);

        // Solo metadatos: el valor cifrado NUNCA sale del servidor.
        var items = await dbContext.SecretCredentials
            .AsNoTracking()
            .Where(s => s.Scope == request.Scope && s.ClientId == request.ClientId)
            .Select(s => new SecretMetadataDto(s.Scope, s.ClientId, s.Type, s.ExpiresAt, s.ModifiedAt))
            .ToListAsync(cancellationToken);

        return Result<List<SecretMetadataDto>>.Success(items);
    }
}
