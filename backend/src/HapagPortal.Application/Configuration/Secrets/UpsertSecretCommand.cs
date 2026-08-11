namespace HapagPortal.Application.Configuration.Secrets;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Configuration.Common;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Crea o reemplaza una credencial. El valor se cifra antes de persistir y no se devuelve.</summary>
public sealed record UpsertSecretCommand(string Scope, Guid? ClientId, string Type, string Value, DateTime? ExpiresAt)
    : ICommand<SecretMetadataDto>;

public sealed class UpsertSecretCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser,
    ISecretProtector protector)
    : ICommandHandler<UpsertSecretCommand, SecretMetadataDto>
{
    public async Task<Result<SecretMetadataDto>> Handle(
        UpsertSecretCommand request,
        CancellationToken cancellationToken)
    {
        var denied = ConfigurationAccess.Check(currentUser, request.Scope, request.ClientId);
        if (denied is not null)
            return Result<SecretMetadataDto>.Failure(denied);

        if (string.IsNullOrWhiteSpace(request.Value))
            return Result<SecretMetadataDto>.Failure(
                new Error("Secret.EmptyValue", "The secret value is required."));

        var encrypted = protector.Protect(request.Value);

        var existing = await dbContext.SecretCredentials
            .FirstOrDefaultAsync(
                s => s.Scope == request.Scope && s.ClientId == request.ClientId && s.Type == request.Type,
                cancellationToken);

        if (existing is null)
        {
            dbContext.SecretCredentials.Add(new SecretCredential
            {
                Scope = request.Scope,
                ClientId = request.ClientId,
                Type = request.Type,
                EncryptedValue = encrypted,
                ExpiresAt = request.ExpiresAt
            });
        }
        else
        {
            existing.EncryptedValue = encrypted;
            existing.ExpiresAt = request.ExpiresAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<SecretMetadataDto>.Success(
            new SecretMetadataDto(request.Scope, request.ClientId, request.Type, request.ExpiresAt, DateTime.UtcNow));
    }
}
