namespace HapagPortal.Application.WebService;

using System.Text.Json;
using FluentValidation;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Clave de un cliente: solo su prefijo y fechas (nunca el valor ni el hash).</summary>
public sealed record ApiClientKeyDto(
    Guid Id,
    string Prefix,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? ExpiresAt,
    DateTime? RevokedAt,
    string? RevokedBy,
    DateTime? LastUsedAt,
    bool Active);

/// <summary>Firmante de la carta de responsabilidad designado por la organización para el canal.</summary>
public sealed record ApiClientSignatoryDto(string? Name, string? TaxId, string? Position, string? Email);

/// <summary>Cliente del canal Web Service tal como lo administra el área interna (M3-17).</summary>
public sealed record ApiClientDto(
    Guid Id,
    string Name,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationTaxId,
    Guid TechnicalUserId,
    string TechnicalUserEmail,
    string Status,
    IReadOnlyList<string> Scopes,
    int RateLimitPerMinute,
    ApiClientSignatoryDto Signatory,
    string? TechnicalContactEmail,
    string? Notes,
    DateTime? LastUsedAt,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? ModifiedAt,
    DateTime? RevokedAt,
    string? RevokedBy,
    string? RevocationReason,
    IReadOnlyList<ApiClientKeyDto> Keys);

/// <summary>Cliente con la clave recién emitida: <c>ApiKey</c> se muestra solo en esta respuesta y no se puede recuperar.</summary>
public sealed record ApiClientSecretDto(ApiClientDto Client, string ApiKey, string KeyPrefix);

/// <summary>Fila de la bitácora del canal para el área interna.</summary>
public sealed record ApiClientRequestLogDto(
    Guid Id,
    Guid? KeyId,
    string Operation,
    string Method,
    string Path,
    string? IdempotencyKey,
    string Outcome,
    int? StatusCode,
    string? ErrorCode,
    string? TargetType,
    Guid? TargetId,
    string? TargetReference,
    string? BlNumber,
    string? SourceAddress,
    DateTime ReceivedAt,
    DateTime? CompletedAt,
    int? DurationMs);

public sealed record GetApiClientsQuery(Guid? OrganizationId = null, string? Status = null) : IQuery<IReadOnlyList<ApiClientDto>>;

public sealed record GetApiClientQuery(Guid Id) : IQuery<ApiClientDto>;

/// <summary>
/// Alta de un cliente del canal para una organización cliente aprobada: crea su usuario técnico (perfil que opera, sin
/// contraseña utilizable) y su primera clave, que se muestra una sola vez.
/// </summary>
public sealed record CreateApiClientCommand(
    Guid OrganizationId,
    string Name,
    IReadOnlyList<string> Scopes,
    int RateLimitPerMinute,
    ApiClientSignatoryDto? Signatory,
    string? TechnicalContactEmail,
    string? Notes) : ICommand<ApiClientSecretDto>;

public sealed record UpdateApiClientCommand(
    Guid Id,
    string Name,
    IReadOnlyList<string> Scopes,
    int RateLimitPerMinute,
    ApiClientSignatoryDto? Signatory,
    string? TechnicalContactEmail,
    string? Notes) : ICommand<ApiClientDto>;

/// <summary>
/// Rotación sin despliegue (NF-09): emite una clave nueva (se muestra una sola vez) y deja las vigentes hasta el fin del
/// período de gracia (<c>GraceMinutes</c>, 0 = revocadas de inmediato).
/// </summary>
public sealed record RotateApiClientKeyCommand(Guid Id, int GraceMinutes = 0) : ICommand<ApiClientSecretDto>;

public sealed record RevokeApiClientKeyCommand(Guid Id, Guid KeyId) : ICommand<ApiClientDto>;

/// <summary>Revoca el cliente: todas sus claves dejan de servir y su usuario técnico se desactiva.</summary>
public sealed record RevokeApiClientCommand(Guid Id, string Reason) : ICommand<ApiClientDto>;

public sealed record GetApiClientRequestsQuery(Guid Id, int Page = 1, int PageSize = 50) : IQuery<PagedResult<ApiClientRequestLogDto>>;

public sealed class CreateApiClientCommandValidator : AbstractValidator<CreateApiClientCommand>
{
    public CreateApiClientCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Scopes).Must(ApiClientFieldRules.ValidScopes).WithMessage(ApiClientFieldRules.ScopesMessage);
        RuleFor(x => x.RateLimitPerMinute).InclusiveBetween(1, ApiClientFieldRules.MaxRateLimitPerMinute);
        RuleFor(x => x.TechnicalContactEmail).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.TechnicalContactEmail));
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Signatory!).SetValidator(new ApiClientSignatoryValidator()).When(x => x.Signatory is not null);
    }
}

public sealed class UpdateApiClientCommandValidator : AbstractValidator<UpdateApiClientCommand>
{
    public UpdateApiClientCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Scopes).Must(ApiClientFieldRules.ValidScopes).WithMessage(ApiClientFieldRules.ScopesMessage);
        RuleFor(x => x.RateLimitPerMinute).InclusiveBetween(1, ApiClientFieldRules.MaxRateLimitPerMinute);
        RuleFor(x => x.TechnicalContactEmail).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.TechnicalContactEmail));
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Signatory!).SetValidator(new ApiClientSignatoryValidator()).When(x => x.Signatory is not null);
    }
}

public sealed class ApiClientSignatoryValidator : AbstractValidator<ApiClientSignatoryDto>
{
    public ApiClientSignatoryValidator()
    {
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.TaxId).MaximumLength(30);
        RuleFor(x => x.Position).MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

/// <summary>Reglas comunes del alta y la edición de un cliente del canal.</summary>
public static class ApiClientFieldRules
{
    public const int MaxRateLimitPerMinute = 600;

    public static readonly string ScopesMessage = $"Scopes must include at least one of: {string.Join(", ", ApiClientScopes.All)}.";

    public static bool ValidScopes(IReadOnlyList<string>? scopes) =>
        scopes is { Count: > 0 } && scopes.All(ApiClientScopes.All.Contains);
}

public sealed class RotateApiClientKeyCommandValidator : AbstractValidator<RotateApiClientKeyCommand>
{
    public const int MaxGraceMinutes = 10080;

    public RotateApiClientKeyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.GraceMinutes).InclusiveBetween(0, MaxGraceMinutes);
    }
}

public sealed class RevokeApiClientCommandValidator : AbstractValidator<RevokeApiClientCommand>
{
    public RevokeApiClientCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class GetApiClientRequestsQueryValidator : AbstractValidator<GetApiClientRequestsQuery>
{
    public GetApiClientRequestsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

/// <summary>Vistas, emisión de claves y auditoría de los clientes del canal.</summary>
public static class ApiClients
{
    public const string AuditEntityName = "ApiClient";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Correo del usuario técnico: no recibe correo (dominio reservado .invalid) ni inicia sesión.</summary>
    public static string TechnicalEmail(Guid clientId) => $"ws-{clientId:N}@clients.ws.invalid";

    public static ApiClientKey NewKey(Guid clientId, GeneratedApiKey generated, string createdBy, DateTime now) => new()
    {
        ApiClientId = clientId,
        Prefix = generated.Prefix,
        KeyHash = generated.Hash,
        CreatedAt = now,
        CreatedBy = createdBy
    };

    public static void Audit(IApplicationDbContext dbContext, ApiClient client, string action, string? actorUserId, DateTime now, object details) =>
        dbContext.AuditLogs.Add(new AuditLog
        {
            EntityName = AuditEntityName,
            EntityId = client.Id.ToString(),
            Action = action,
            NewValues = JsonSerializer.Serialize(details, JsonOptions),
            UserId = actorUserId,
            Timestamp = now
        });

    public static async Task<ApiClientDto> ToDtoAsync(IApplicationDbContext dbContext, ApiClient client, CancellationToken cancellationToken)
    {
        var organization = await dbContext.Clients.AsNoTracking().FirstAsync(c => c.Id == client.OrganizationId, cancellationToken);
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == client.TechnicalUserId, cancellationToken);
        var keys = await dbContext.ApiClientKeys.AsNoTracking()
            .Where(k => k.ApiClientId == client.Id)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        return new ApiClientDto(
            client.Id,
            client.Name,
            organization.Id,
            organization.Name,
            TaxIdNormalizer.Normalize(organization.TaxId),
            client.TechnicalUserId,
            user?.Email ?? TechnicalEmail(client.Id),
            client.Status,
            ApiClientScopeList.Parse(client.Scopes),
            client.RateLimitPerMinute,
            new ApiClientSignatoryDto(client.SignatoryName, client.SignatoryTaxId, client.SignatoryPosition, client.SignatoryEmail),
            client.TechnicalContactEmail,
            client.Notes,
            keys.Max(k => k.LastUsedAt),
            client.CreatedAt,
            client.CreatedBy,
            client.ModifiedAt,
            client.RevokedAt,
            client.RevokedBy,
            client.RevocationReason,
            keys.OrderByDescending(k => k.CreatedAt)
                .Select(k => new ApiClientKeyDto(k.Id, k.Prefix, k.CreatedAt, k.CreatedBy, k.ExpiresAt, k.RevokedAt, k.RevokedBy, k.LastUsedAt,
                    client.Status == ApiClientStatus.Active && k.IsUsableAt(now)))
                .ToList());
    }

    public static void ApplyFields(
        ApiClient client,
        string name,
        IReadOnlyList<string> scopes,
        int rateLimit,
        ApiClientSignatoryDto? signatory,
        string? contact,
        string? notes)
    {
        client.Name = name.Trim();
        client.Scopes = ApiClientScopeList.Format(scopes);
        client.RateLimitPerMinute = rateLimit;
        client.SignatoryName = Trim(signatory?.Name);
        client.SignatoryTaxId = Trim(signatory?.TaxId);
        client.SignatoryPosition = Trim(signatory?.Position);
        client.SignatoryEmail = Trim(signatory?.Email);
        client.TechnicalContactEmail = Trim(contact);
        client.Notes = Trim(notes);
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class GetApiClientsQueryHandler(IApplicationDbContext dbContext) : IQueryHandler<GetApiClientsQuery, IReadOnlyList<ApiClientDto>>
{
    public async Task<Result<IReadOnlyList<ApiClientDto>>> Handle(GetApiClientsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.ApiClients.AsNoTracking();
        if (request.OrganizationId is { } organizationId)
            query = query.Where(c => c.OrganizationId == organizationId);
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(c => c.Status == request.Status);

        var clients = await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
        var items = new List<ApiClientDto>();
        foreach (var client in clients)
            items.Add(await ApiClients.ToDtoAsync(dbContext, client, cancellationToken));

        return Result<IReadOnlyList<ApiClientDto>>.Success(items);
    }
}

public sealed class GetApiClientQueryHandler(IApplicationDbContext dbContext) : IQueryHandler<GetApiClientQuery, ApiClientDto>
{
    public async Task<Result<ApiClientDto>> Handle(GetApiClientQuery request, CancellationToken cancellationToken)
    {
        var client = await dbContext.ApiClients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        return client is null
            ? Result<ApiClientDto>.Failure(DomainErrors.ApiClient.NotFound(request.Id))
            : Result<ApiClientDto>.Success(await ApiClients.ToDtoAsync(dbContext, client, cancellationToken));
    }
}

public sealed class CreateApiClientCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPasswordHasher passwordHasher)
    : ICommandHandler<CreateApiClientCommand, ApiClientSecretDto>
{
    public async Task<Result<ApiClientSecretDto>> Handle(CreateApiClientCommand request, CancellationToken cancellationToken)
    {
        var organization = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.OrganizationId, cancellationToken);
        if (organization is null)
            return Result<ApiClientSecretDto>.Failure(DomainErrors.Organization.NotFound(request.OrganizationId));
        if (!organization.IsActive || organization.RegistrationStatus != OrganizationStatus.Approved
            || organization.OrganizationType == OrganizationTypes.Internal)
            return Result<ApiClientSecretDto>.Failure(DomainErrors.ApiClient.OrganizationNotAllowed);

        var now = DateTime.UtcNow;
        var actor = currentUserService.Email ?? currentUserService.UserId?.ToString() ?? "internal";
        var client = new ApiClient
        {
            Name = request.Name.Trim(),
            OrganizationId = organization.Id,
            Status = ApiClientStatus.Active,
            Scopes = string.Empty
        };
        ApiClients.ApplyFields(client, request.Name, request.Scopes, request.RateLimitPerMinute, request.Signatory,
            request.TechnicalContactEmail, request.Notes);

        // Usuario técnico: la organización actúa por el canal con el perfil que opera (mismas reglas que el portal).
        var email = ApiClients.TechnicalEmail(client.Id);
        var user = new User
        {
            Username = email,
            Email = email,
            PasswordHash = passwordHasher.Hash(Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))),
            UserType = UserTypes.Technical,
            Country = organization.Country,
            FirstName = "Web Service",
            LastName = client.Name.Length > 100 ? client.Name[..100] : client.Name,
            IsActive = true,
            IsEmailConfirmed = true,
            MembershipStatus = MembershipStatus.Active,
            MembershipDecidedAt = now,
            MembershipDecidedBy = actor,
            ClientId = organization.Id
        };
        client.TechnicalUserId = user.Id;

        dbContext.Users.Add(user);
        await OrganizationProfileAssigner.AssignAsync(dbContext, user.Id, RoleCodes.OrgOperator, cancellationToken);

        var generated = ApiKeys.Generate();
        dbContext.ApiClients.Add(client);
        dbContext.ApiClientKeys.Add(ApiClients.NewKey(client.Id, generated, actor, now));
        ApiClients.Audit(dbContext, client, "Created", currentUserService.UserId?.ToString(), now,
            new { client.Name, client.OrganizationId, client.Scopes, client.RateLimitPerMinute, keyPrefix = generated.Prefix });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ApiClientSecretDto>.Success(new ApiClientSecretDto(
            await ApiClients.ToDtoAsync(dbContext, client, cancellationToken), generated.Key, generated.Prefix));
    }
}

public sealed class UpdateApiClientCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : ICommandHandler<UpdateApiClientCommand, ApiClientDto>
{
    public async Task<Result<ApiClientDto>> Handle(UpdateApiClientCommand request, CancellationToken cancellationToken)
    {
        var client = await dbContext.ApiClients.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (client is null)
            return Result<ApiClientDto>.Failure(DomainErrors.ApiClient.NotFound(request.Id));
        if (client.Status == ApiClientStatus.Revoked)
            return Result<ApiClientDto>.Failure(DomainErrors.ApiClient.AlreadyRevoked);

        var previous = new { client.Name, client.Scopes, client.RateLimitPerMinute, client.SignatoryName };
        ApiClients.ApplyFields(client, request.Name, request.Scopes, request.RateLimitPerMinute, request.Signatory,
            request.TechnicalContactEmail, request.Notes);
        ApiClients.Audit(dbContext, client, "Updated", currentUserService.UserId?.ToString(), DateTime.UtcNow,
            new { previous, current = new { client.Name, client.Scopes, client.RateLimitPerMinute, client.SignatoryName } });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ApiClientDto>.Success(await ApiClients.ToDtoAsync(dbContext, client, cancellationToken));
    }
}

public sealed class RotateApiClientKeyCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : ICommandHandler<RotateApiClientKeyCommand, ApiClientSecretDto>
{
    public async Task<Result<ApiClientSecretDto>> Handle(RotateApiClientKeyCommand request, CancellationToken cancellationToken)
    {
        var client = await dbContext.ApiClients.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (client is null)
            return Result<ApiClientSecretDto>.Failure(DomainErrors.ApiClient.NotFound(request.Id));
        if (client.Status == ApiClientStatus.Revoked)
            return Result<ApiClientSecretDto>.Failure(DomainErrors.ApiClient.AlreadyRevoked);

        var now = DateTime.UtcNow;
        var actor = currentUserService.Email ?? currentUserService.UserId?.ToString() ?? "internal";
        var current = await dbContext.ApiClientKeys
            .Where(k => k.ApiClientId == client.Id && k.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var key in current.Where(k => k.IsUsableAt(now)))
        {
            if (request.GraceMinutes == 0)
            {
                key.RevokedAt = now;
                key.RevokedBy = actor;
            }
            else
            {
                var graceEnd = now.AddMinutes(request.GraceMinutes);
                key.ExpiresAt = key.ExpiresAt is { } expires && expires < graceEnd ? expires : graceEnd;
            }
        }

        var generated = ApiKeys.Generate();
        dbContext.ApiClientKeys.Add(ApiClients.NewKey(client.Id, generated, actor, now));
        ApiClients.Audit(dbContext, client, "KeyRotated", currentUserService.UserId?.ToString(), now,
            new { keyPrefix = generated.Prefix, request.GraceMinutes, previousKeys = current.Select(k => k.Prefix).ToList() });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ApiClientSecretDto>.Success(new ApiClientSecretDto(
            await ApiClients.ToDtoAsync(dbContext, client, cancellationToken), generated.Key, generated.Prefix));
    }
}

public sealed class RevokeApiClientKeyCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : ICommandHandler<RevokeApiClientKeyCommand, ApiClientDto>
{
    public async Task<Result<ApiClientDto>> Handle(RevokeApiClientKeyCommand request, CancellationToken cancellationToken)
    {
        var client = await dbContext.ApiClients.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (client is null)
            return Result<ApiClientDto>.Failure(DomainErrors.ApiClient.NotFound(request.Id));

        var key = await dbContext.ApiClientKeys.FirstOrDefaultAsync(k => k.Id == request.KeyId && k.ApiClientId == client.Id, cancellationToken);
        if (key is null)
            return Result<ApiClientDto>.Failure(DomainErrors.ApiClient.KeyNotFound(request.KeyId));

        if (key.RevokedAt is null)
        {
            var now = DateTime.UtcNow;
            key.RevokedAt = now;
            key.RevokedBy = currentUserService.Email ?? currentUserService.UserId?.ToString() ?? "internal";
            ApiClients.Audit(dbContext, client, "KeyRevoked", currentUserService.UserId?.ToString(), now, new { keyPrefix = key.Prefix });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<ApiClientDto>.Success(await ApiClients.ToDtoAsync(dbContext, client, cancellationToken));
    }
}

public sealed class RevokeApiClientCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : ICommandHandler<RevokeApiClientCommand, ApiClientDto>
{
    public async Task<Result<ApiClientDto>> Handle(RevokeApiClientCommand request, CancellationToken cancellationToken)
    {
        var client = await dbContext.ApiClients.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (client is null)
            return Result<ApiClientDto>.Failure(DomainErrors.ApiClient.NotFound(request.Id));
        if (client.Status == ApiClientStatus.Revoked)
            return Result<ApiClientDto>.Failure(DomainErrors.ApiClient.AlreadyRevoked);

        var now = DateTime.UtcNow;
        var actor = currentUserService.Email ?? currentUserService.UserId?.ToString() ?? "internal";
        client.Status = ApiClientStatus.Revoked;
        client.RevokedAt = now;
        client.RevokedBy = actor;
        client.RevocationReason = request.Reason.Trim();

        var keys = await dbContext.ApiClientKeys.Where(k => k.ApiClientId == client.Id && k.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var key in keys)
        {
            key.RevokedAt = now;
            key.RevokedBy = actor;
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == client.TechnicalUserId, cancellationToken);
        if (user is not null)
            user.IsActive = false;

        ApiClients.Audit(dbContext, client, "Revoked", currentUserService.UserId?.ToString(), now, new { reason = client.RevocationReason });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ApiClientDto>.Success(await ApiClients.ToDtoAsync(dbContext, client, cancellationToken));
    }
}

public sealed class GetApiClientRequestsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetApiClientRequestsQuery, PagedResult<ApiClientRequestLogDto>>
{
    public async Task<Result<PagedResult<ApiClientRequestLogDto>>> Handle(GetApiClientRequestsQuery request, CancellationToken cancellationToken)
    {
        if (!await dbContext.ApiClients.AsNoTracking().AnyAsync(c => c.Id == request.Id, cancellationToken))
            return Result<PagedResult<ApiClientRequestLogDto>>.Failure(DomainErrors.ApiClient.NotFound(request.Id));

        var query = dbContext.ApiClientRequests.AsNoTracking().Where(r => r.ApiClientId == request.Id);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(r => r.ReceivedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<ApiClientRequestLogDto>>.Success(new PagedResult<ApiClientRequestLogDto>(
            rows.Select(r => new ApiClientRequestLogDto(r.Id, r.ApiClientKeyId, r.Operation, r.Method, r.Path, r.IdempotencyKey, r.Outcome,
                r.StatusCode, r.ErrorCode, r.TargetType, r.TargetId, r.TargetReference, r.BlNumber, r.SourceAddress, r.ReceivedAt,
                r.CompletedAt, r.DurationMs)).ToList(),
            total, request.Page, request.PageSize));
    }
}
