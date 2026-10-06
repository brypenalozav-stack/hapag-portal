namespace HapagPortal.Application.WebService;

using FluentValidation;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.ResponsibilityLetter;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

/// <summary>Cliente del canal autenticado: organización, alcances, límite y firmante configurado.</summary>
public sealed record WsClientInfoDto(
    Guid ClientId,
    string Name,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationTaxId,
    IReadOnlyList<string> Scopes,
    int RateLimitPerMinute,
    bool SignatoryConfigured,
    string ResponsibilityLetterTermsVersion);

/// <summary>
/// Estado actual de lo que creó una solicitud del canal: el documento (carta de responsabilidad), el cambio de almacén o la
/// solicitud masiva, leído del portal en el momento (mismos estados que en la interfaz).
/// </summary>
public sealed record WsRequestTargetDto(
    string Type,
    Guid Id,
    string? Reference,
    string Status,
    string? BlNumber,
    string? ContainerNumber,
    decimal? Amount,
    string? Currency,
    bool? IsFree,
    int? TotalItems,
    int? ProcessedItems,
    int? SucceededItems,
    int? FailedItems,
    DateTime? CompletedAt,
    string? VerificationCode,
    IReadOnlyList<WsBatchItemDto>? Items);

public sealed record WsBatchItemDto(
    int LineNumber,
    string BlNumber,
    string? ContainerNumber,
    string Status,
    string? ErrorCode,
    Guid? WarehouseChangeId);

/// <summary>Solicitud recibida por el canal (bitácora NF-14) con el estado actual de lo que creó.</summary>
public sealed record WsRequestDto(
    Guid Id,
    string Operation,
    string? IdempotencyKey,
    string Outcome,
    int? StatusCode,
    string? ErrorCode,
    string? BlNumber,
    DateTime ReceivedAt,
    DateTime? CompletedAt,
    WsRequestTargetDto? Target);

/// <summary>Datos del cliente autenticado (prueba de conexión).</summary>
public sealed record GetWsClientInfoQuery : IQuery<WsClientInfoDto>;

/// <summary>Solicitudes del cliente (carta, cambio de almacén individual y masivo), más recientes primero.</summary>
public sealed record GetWsRequestsQuery(
    string? Operation = null,
    string? Outcome = null,
    string? BlNumber = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<WsRequestDto>>;

/// <summary>Estado de una solicitud del cliente (con el detalle por línea de una solicitud masiva).</summary>
public sealed record GetWsRequestQuery(Guid Id) : IQuery<WsRequestDto>;

/// <summary>
/// Carta de responsabilidad por el canal (M3-17, M6-06): los datos del firmante son los que la organización configuró
/// para el canal (no se aceptan otros) y la aceptación de los términos vigentes se registra a su nombre. Aplica la misma
/// regla que el portal: solo el consignee Freight Forwarder (o quien la reciba por un acceso otorgado).
/// </summary>
public sealed record SubmitWsResponsibilityLetterCommand(
    string BlNumber,
    bool AcceptTerms,
    string TermsVersion,
    string? CargoDescription,
    string? Observations,
    string? ContactPhone) : ICommand<ShipmentDocumentDto>;

public sealed class GetWsRequestsQueryValidator : AbstractValidator<GetWsRequestsQuery>
{
    public GetWsRequestsQueryValidator()
    {
        RuleFor(x => x.Operation).Must(o => o is null || ApiClientOperations.Submissions.Contains(o))
            .WithMessage($"Operation must be one of: {string.Join(", ", ApiClientOperations.Submissions)}.");
        RuleFor(x => x.Outcome)
            .Must(o => o is null or ApiClientRequestOutcomes.Accepted or ApiClientRequestOutcomes.Rejected or ApiClientRequestOutcomes.Failed or ApiClientRequestOutcomes.Processing)
            .WithMessage("Unknown outcome.");
        RuleFor(x => x.BlNumber).MaximumLength(50);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class SubmitWsResponsibilityLetterCommandValidator : AbstractValidator<SubmitWsResponsibilityLetterCommand>
{
    public SubmitWsResponsibilityLetterCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TermsVersion).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CargoDescription).MaximumLength(500);
        RuleFor(x => x.Observations).MaximumLength(1000);
        RuleFor(x => x.ContactPhone).MaximumLength(30);
    }
}

/// <summary>Vistas de las solicitudes del canal con el estado actual de lo creado.</summary>
internal static class WsRequestViews
{
    public static async Task<Result<ApiClient>> CurrentClientAsync(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (currentUserService.ApiClientId is not { } clientId)
            return Result<ApiClient>.Failure(Error.Forbidden);

        var client = await dbContext.ApiClients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clientId && c.Status == ApiClientStatus.Active, cancellationToken);
        return client is null ? Result<ApiClient>.Failure(Error.Forbidden) : Result<ApiClient>.Success(client);
    }

    public static async Task<WsRequestDto> ToDtoAsync(
        IApplicationDbContext dbContext,
        ApiClientRequest row,
        bool includeItems,
        CancellationToken cancellationToken)
    {
        WsRequestTargetDto? target = null;
        if (row.TargetId is { } targetId)
        {
            target = row.TargetType switch
            {
                ApiClientTargetTypes.ShipmentDocument => await DocumentAsync(dbContext, targetId, cancellationToken),
                ApiClientTargetTypes.WarehouseChange => await WarehouseChangeAsync(dbContext, targetId, cancellationToken),
                ApiClientTargetTypes.WarehouseChangeBatch => await BatchAsync(dbContext, targetId, includeItems, cancellationToken),
                _ => null
            };
        }

        return new WsRequestDto(row.Id, row.Operation, row.IdempotencyKey, row.Outcome, row.StatusCode, row.ErrorCode, row.BlNumber,
            row.ReceivedAt, row.CompletedAt, target);
    }

    private static async Task<WsRequestTargetDto?> DocumentAsync(IApplicationDbContext dbContext, Guid id, CancellationToken cancellationToken)
    {
        var document = await dbContext.ShipmentDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return document is null
            ? null
            : new WsRequestTargetDto(ApiClientTargetTypes.ShipmentDocument, document.Id, document.DocumentNumber, document.Status,
                document.BlNumber, null, null, null, null, null, null, null, null, document.IssuedAt, document.VerificationCode, null);
    }

    private static async Task<WsRequestTargetDto?> WarehouseChangeAsync(IApplicationDbContext dbContext, Guid id, CancellationToken cancellationToken)
    {
        var change = await dbContext.WarehouseChanges.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (change is null)
            return null;

        var blNumber = await dbContext.BillsOfLading.AsNoTracking()
            .Where(b => b.Id == change.BillOfLadingId).Select(b => b.BLNumber).FirstOrDefaultAsync(cancellationToken);
        return new WsRequestTargetDto(ApiClientTargetTypes.WarehouseChange, change.Id, $"{change.FromWarehouse} -> {change.ToWarehouse}",
            change.Status, blNumber, change.ContainerNumber, change.Amount, change.Currency, change.IsFree, null, null, null, null,
            change.CompletedAt, null, null);
    }

    private static async Task<WsRequestTargetDto?> BatchAsync(IApplicationDbContext dbContext, Guid id, bool includeItems, CancellationToken cancellationToken)
    {
        var batch = await dbContext.WarehouseChangeBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (batch is null)
            return null;

        IReadOnlyList<WsBatchItemDto>? items = null;
        if (includeItems)
        {
            items = (await dbContext.WarehouseChangeBatchItems.AsNoTracking()
                    .Where(i => i.BatchId == batch.Id)
                    .ToListAsync(cancellationToken))
                .OrderBy(i => i.LineNumber)
                .Select(i => new WsBatchItemDto(i.LineNumber, i.BlNumber, i.ContainerNumber, i.Status, i.ErrorCode, i.WarehouseChangeId))
                .ToList();
        }

        return new WsRequestTargetDto(ApiClientTargetTypes.WarehouseChangeBatch, batch.Id, null, batch.Status, null, null, null, null, null,
            batch.TotalItems, batch.ProcessedItems, batch.SucceededItems, batch.FailedItems, batch.CompletedAt, null, items);
    }
}

public sealed class GetWsClientInfoQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetWsClientInfoQuery, WsClientInfoDto>
{
    public async Task<Result<WsClientInfoDto>> Handle(GetWsClientInfoQuery request, CancellationToken cancellationToken)
    {
        var current = await WsRequestViews.CurrentClientAsync(dbContext, currentUserService, cancellationToken);
        if (current.IsFailure)
            return Result<WsClientInfoDto>.Failure(current.Error);

        var client = current.Value;
        var organization = await dbContext.Clients.AsNoTracking().FirstAsync(c => c.Id == client.OrganizationId, cancellationToken);

        return Result<WsClientInfoDto>.Success(new WsClientInfoDto(
            client.Id,
            client.Name,
            organization.Id,
            organization.Name,
            TaxIdNormalizer.Normalize(organization.TaxId),
            ApiClientScopeList.Parse(client.Scopes),
            client.RateLimitPerMinute,
            WsSignatory.IsConfigured(client),
            ResponsibilityLetterTerms.Version));
    }
}

public sealed class GetWsRequestsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetWsRequestsQuery, PagedResult<WsRequestDto>>
{
    public async Task<Result<PagedResult<WsRequestDto>>> Handle(GetWsRequestsQuery request, CancellationToken cancellationToken)
    {
        var current = await WsRequestViews.CurrentClientAsync(dbContext, currentUserService, cancellationToken);
        if (current.IsFailure)
            return Result<PagedResult<WsRequestDto>>.Failure(current.Error);

        var clientId = current.Value.Id;
        var query = dbContext.ApiClientRequests.AsNoTracking()
            .Where(r => r.ApiClientId == clientId && ApiClientOperations.Submissions.Contains(r.Operation));

        if (!string.IsNullOrWhiteSpace(request.Operation))
            query = query.Where(r => r.Operation == request.Operation);
        if (!string.IsNullOrWhiteSpace(request.Outcome))
            query = query.Where(r => r.Outcome == request.Outcome);
        if (!string.IsNullOrWhiteSpace(request.BlNumber))
        {
            var number = request.BlNumber.Trim();
            query = query.Where(r => r.BlNumber == number);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(r => r.ReceivedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = new List<WsRequestDto>();
        foreach (var row in rows)
            items.Add(await WsRequestViews.ToDtoAsync(dbContext, row, includeItems: false, cancellationToken));

        return Result<PagedResult<WsRequestDto>>.Success(new PagedResult<WsRequestDto>(items, total, request.Page, request.PageSize));
    }
}

public sealed class GetWsRequestQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetWsRequestQuery, WsRequestDto>
{
    public async Task<Result<WsRequestDto>> Handle(GetWsRequestQuery request, CancellationToken cancellationToken)
    {
        var current = await WsRequestViews.CurrentClientAsync(dbContext, currentUserService, cancellationToken);
        if (current.IsFailure)
            return Result<WsRequestDto>.Failure(current.Error);

        // NF-05: la solicitud de otro cliente no se distingue de una inexistente.
        var clientId = current.Value.Id;
        var row = await dbContext.ApiClientRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.ApiClientId == clientId && ApiClientOperations.Submissions.Contains(r.Operation),
                cancellationToken);

        return row is null
            ? Result<WsRequestDto>.Failure(DomainErrors.ApiClient.RequestNotFound(request.Id))
            : Result<WsRequestDto>.Success(await WsRequestViews.ToDtoAsync(dbContext, row, includeItems: true, cancellationToken));
    }
}

/// <summary>Firmante de la carta de responsabilidad configurado para el canal.</summary>
internal static class WsSignatory
{
    public static bool IsConfigured(ApiClient client) =>
        !string.IsNullOrWhiteSpace(client.SignatoryName)
        && !string.IsNullOrWhiteSpace(client.SignatoryTaxId)
        && !string.IsNullOrWhiteSpace(client.SignatoryPosition)
        && !string.IsNullOrWhiteSpace(client.SignatoryEmail);
}

public sealed class SubmitWsResponsibilityLetterCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ISender sender)
    : ICommandHandler<SubmitWsResponsibilityLetterCommand, ShipmentDocumentDto>
{
    public async Task<Result<ShipmentDocumentDto>> Handle(SubmitWsResponsibilityLetterCommand request, CancellationToken cancellationToken)
    {
        var current = await WsRequestViews.CurrentClientAsync(dbContext, currentUserService, cancellationToken);
        if (current.IsFailure)
            return Result<ShipmentDocumentDto>.Failure(current.Error);

        var client = current.Value;
        if (!WsSignatory.IsConfigured(client))
            return Result<ShipmentDocumentDto>.Failure(DomainErrors.ApiClient.SignatoryNotConfigured);

        // La carta se emite con el mismo comando del portal: mismas reglas de M1-11, M4-04 y M6-06 y el mismo registro.
        return await sender.Send(new IssueResponsibilityLetterCommand(
            request.BlNumber,
            client.SignatoryName!,
            client.SignatoryTaxId!,
            client.SignatoryPosition!,
            client.SignatoryEmail!,
            request.ContactPhone,
            request.CargoDescription,
            request.Observations,
            request.AcceptTerms,
            request.TermsVersion), cancellationToken);
    }
}
