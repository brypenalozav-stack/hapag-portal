namespace HapagPortal.Application.WarehouseChanges.History;

using FluentValidation;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Organización que solicitó o pagó (razón social y RUT/NIT).</summary>
public sealed record WarehouseChangePartyDto(Guid? OrganizationId, string? Name, string? TaxId);

/// <summary>
/// Solicitud de cambio de almacén en el historial (M3-06): fecha, estado, embarque, quién la solicitó y la
/// razón social y RUT de quien la pagó (pagador del pago confirmado, M7-02), con el RUT de facturación.
/// </summary>
public sealed record WarehouseChangeHistoryItemDto(
    Guid Id,
    DateTime CreatedAt,
    string Status,
    Guid BillOfLadingId,
    string BlNumber,
    string? BookingNumber,
    string Country,
    string TimeZone,
    string? ContainerNumber,
    string FromWarehouse,
    string ToWarehouse,
    decimal Amount,
    string Currency,
    bool IsFree,
    string? EntitlementSource,
    string? TariffCode,
    string? RequestedByEmail,
    WarehouseChangePartyDto? RequestedBy,
    Guid? BatchId,
    int? BatchLineNumber,
    WarehouseChangePartyDto? Payer,
    string? BillingTaxId,
    string? BillingName,
    Guid? PaymentId,
    string? PaymentNumber,
    string? PaymentStatus,
    string? ReceiptNumber,
    DateTime? PaidAt,
    DateTime? CompletedAt);

/// <summary>Hito de la trazabilidad: solicitud, derecho gratuito, cambios del pago, liberación y anulación.</summary>
public sealed record WarehouseChangeTimelineEventDto(
    DateTime OccurredAt,
    string Event,
    string? Status,
    string? Actor,
    string? Reference,
    string? Notes);

public sealed record WarehouseChangeTraceDto(
    WarehouseChangeHistoryItemDto Change,
    IReadOnlyList<WarehouseChangeTimelineEventDto> Timeline);

/// <summary>Eventos de la línea de tiempo de un cambio de almacén (M3-06).</summary>
public static class WarehouseChangeTimelineEvents
{
    public const string Requested = "Requested";
    public const string FreeEntitlementApplied = "FreeEntitlementApplied";
    public const string PaymentStatusChanged = "PaymentStatusChanged";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// Historial de cambios de almacén (M3-06, CL-IMP-12, BO-IMP-12): las solicitudes de la organización
/// (individuales y de solicitudes masivas) o, para el administrador interno, todas; filtrable por BL o booking,
/// estado y fecha local de la solicitud (NF-22).
/// </summary>
public sealed record GetWarehouseChangeHistoryQuery(
    string? BlNumber = null,
    string? Status = null,
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? OrganizationId = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<WarehouseChangeHistoryItemDto>>;

/// <summary>Trazabilidad de una solicitud de cambio de almacén, reconstruida desde la solicitud y sus pagos.</summary>
public sealed record GetWarehouseChangeTraceQuery(Guid Id) : IQuery<WarehouseChangeTraceDto>;

public sealed class GetWarehouseChangeHistoryQueryValidator : AbstractValidator<GetWarehouseChangeHistoryQuery>
{
    public GetWarehouseChangeHistoryQueryValidator()
    {
        RuleFor(x => x.BlNumber).MaximumLength(50);
        RuleFor(x => x.Status).MaximumLength(30);
        RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To).WithName("To").WithMessage("To must be on or after From.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

internal static class WarehouseChangeHistory
{
    /// <summary>
    /// Cambios visibles: los solicitados por la organización y, en los anteriores a la Ola C (sin solicitante
    /// registrado), los de sus propios BL. El administrador interno ve todos.
    /// </summary>
    public static async Task<Result<IQueryable<WarehouseChange>>> VisibleAsync(
        IApplicationDbContext dbContext,
        IShipmentAccessEvaluator accessEvaluator,
        Guid? organizationFilter,
        CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        var query = dbContext.WarehouseChanges.AsNoTracking();

        if (scope.IsAdmin)
        {
            if (organizationFilter is { } filter)
            {
                var ownedBls = dbContext.BillsOfLading.Where(b => b.ClientId == filter).Select(b => b.Id);
                query = query.Where(w => w.RequestedByClientId == filter || (w.RequestedByClientId == null && ownedBls.Contains(w.BillOfLadingId)));
            }

            return Result<IQueryable<WarehouseChange>>.Success(query);
        }

        if (!scope.IsOperational || scope.OrganizationId is null)
            return Result<IQueryable<WarehouseChange>>.Failure(Error.Forbidden);

        var organizationId = scope.OrganizationId.Value;
        var own = dbContext.BillsOfLading.Where(b => b.ClientId == organizationId).Select(b => b.Id);
        return Result<IQueryable<WarehouseChange>>.Success(query.Where(w =>
            w.RequestedByClientId == organizationId || (w.RequestedByClientId == null && own.Contains(w.BillOfLadingId))));
    }

    public static async Task<IReadOnlyList<WarehouseChangeHistoryItemDto>> ItemsAsync(
        IApplicationDbContext dbContext,
        IReadOnlyList<WarehouseChange> changes,
        CancellationToken cancellationToken)
    {
        if (changes.Count == 0)
            return [];

        var ids = changes.Select(w => w.Id).ToList();
        var blIds = changes.Select(w => w.BillOfLadingId).Distinct().ToList();
        var bls = await dbContext.BillsOfLading.AsNoTracking()
            .Where(b => blIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        var organizationIds = changes.Where(w => w.RequestedByClientId is not null).Select(w => w.RequestedByClientId!.Value)
            .Concat(bls.Values.Select(b => b.ClientId)).Distinct().ToList();
        var organizations = await dbContext.Clients.AsNoTracking()
            .Where(c => organizationIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var userIds = changes.Where(w => w.RequestedByUserId is not null).Select(w => w.RequestedByUserId!.Value).Distinct().ToList();
        var users = await dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        var batchLines = await dbContext.WarehouseChangeBatchItems.AsNoTracking()
            .Where(i => i.WarehouseChangeId != null && ids.Contains(i.WarehouseChangeId.Value))
            .ToListAsync(cancellationToken);

        var details = await dbContext.PaymentDetails.AsNoTracking()
            .Where(d => d.ItemType == PayableItemTypes.WarehouseChange && d.SourceId != null && ids.Contains(d.SourceId.Value))
            .ToListAsync(cancellationToken);
        var paymentIds = details.Select(d => d.PaymentId).Distinct().ToList();
        var payments = await dbContext.Payments.AsNoTracking()
            .Where(p => paymentIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return changes.Select(change =>
        {
            var bl = bls.GetValueOrDefault(change.BillOfLadingId);
            var requesterId = change.RequestedByClientId ?? bl?.ClientId;
            var requester = requesterId is { } rid ? organizations.GetValueOrDefault(rid) : null;

            // El pago que cuenta es el confirmado; si no hay, el último intento (para ver su estado).
            var candidates = details.Where(d => d.SourceId == change.Id)
                .Select(d => (Detail: d, Payment: payments.GetValueOrDefault(d.PaymentId)))
                .Where(x => x.Payment is not null)
                .OrderByDescending(x => x.Payment!.Status == PaymentStatus.Confirmed)
                .ThenByDescending(x => x.Payment!.CreatedAt)
                .ToList();
            PaymentDetail? detail = candidates.Count > 0 ? candidates[0].Detail : null;
            Payment? payment = candidates.Count > 0 ? candidates[0].Payment : null;
            var confirmed = payment?.Status == PaymentStatus.Confirmed;

            var payer = confirmed
                ? new WarehouseChangePartyDto(
                    payment!.ClientId,
                    payment.PayerName ?? organizations.GetValueOrDefault(payment.ClientId)?.Name,
                    payment.PayerTaxId is { } payerTaxId ? TaxIdNormalizer.Normalize(payerTaxId) : null)
                : null;

            var line = batchLines.FirstOrDefault(i => i.WarehouseChangeId == change.Id);

            return new WarehouseChangeHistoryItemDto(
                change.Id,
                change.CreatedAt,
                change.Status,
                change.BillOfLadingId,
                bl?.BLNumber ?? string.Empty,
                bl?.BookingNumber,
                change.Country,
                BusinessCalendar.TimeZoneId(change.Country),
                change.ContainerNumber,
                change.FromWarehouse,
                change.ToWarehouse,
                change.Amount,
                change.Currency,
                change.IsFree,
                change.EntitlementSource,
                change.TariffCode,
                change.RequestedByUserId is { } uid ? users.GetValueOrDefault(uid) : null,
                requester is null ? null : new WarehouseChangePartyDto(requester.Id, requester.Name, TaxIdNormalizer.Normalize(requester.TaxId)),
                change.BatchId,
                line?.LineNumber,
                payer,
                detail?.BillingTaxId,
                detail?.BillingName,
                payment?.Id,
                payment?.PaymentNumber,
                payment?.Status,
                payment?.ReceiptNumber,
                confirmed ? payment!.ConfirmedAt : null,
                change.CompletedAt);
        }).ToList();
    }
}

public sealed class GetWarehouseChangeHistoryQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetWarehouseChangeHistoryQuery, PagedResult<WarehouseChangeHistoryItemDto>>
{
    public async Task<Result<PagedResult<WarehouseChangeHistoryItemDto>>> Handle(GetWarehouseChangeHistoryQuery request, CancellationToken cancellationToken)
    {
        var visible = await WarehouseChangeHistory.VisibleAsync(dbContext, accessEvaluator, request.OrganizationId, cancellationToken);
        if (visible.IsFailure)
            return Result<PagedResult<WarehouseChangeHistoryItemDto>>.Failure(visible.Error);

        var query = visible.Value;
        if (!string.IsNullOrWhiteSpace(request.BlNumber))
        {
            var number = request.BlNumber.Trim();
            var blIds = dbContext.BillsOfLading.Where(b => b.BLNumber == number || b.BookingNumber == number).Select(b => b.Id);
            query = query.Where(w => blIds.Contains(w.BillOfLadingId));
        }
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            query = query.Where(w => w.Status == status);
        }

        // Fecha local de la solicitud en el país (NF-22): margen de un día en UTC y filtro exacto en memoria.
        if (request.From is { } from)
        {
            var lower = from.AddDays(-1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(w => w.CreatedAt >= lower);
        }
        if (request.To is { } to)
        {
            var upper = to.AddDays(2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(w => w.CreatedAt < upper);
        }

        var candidates = (await query.OrderByDescending(w => w.CreatedAt).ToListAsync(cancellationToken))
            .Where(w => request.From is null || BusinessCalendar.LocalDate(w.Country, w.CreatedAt) >= request.From)
            .Where(w => request.To is null || BusinessCalendar.LocalDate(w.Country, w.CreatedAt) <= request.To)
            .ToList();

        var page = candidates.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        var items = await WarehouseChangeHistory.ItemsAsync(dbContext, page, cancellationToken);

        return Result<PagedResult<WarehouseChangeHistoryItemDto>>.Success(
            new PagedResult<WarehouseChangeHistoryItemDto>(items, candidates.Count, request.Page, request.PageSize));
    }
}

public sealed class GetWarehouseChangeTraceQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetWarehouseChangeTraceQuery, WarehouseChangeTraceDto>
{
    public async Task<Result<WarehouseChangeTraceDto>> Handle(GetWarehouseChangeTraceQuery request, CancellationToken cancellationToken)
    {
        var visible = await WarehouseChangeHistory.VisibleAsync(dbContext, accessEvaluator, null, cancellationToken);
        if (visible.IsFailure)
            return Result<WarehouseChangeTraceDto>.Failure(visible.Error);

        var change = await visible.Value.FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);
        if (change is null)
            return Result<WarehouseChangeTraceDto>.Failure(DomainErrors.WarehouseChange.NotFound(request.Id));

        var item = (await WarehouseChangeHistory.ItemsAsync(dbContext, [change], cancellationToken))[0];
        var timeline = new List<WarehouseChangeTimelineEventDto>
        {
            new(change.CreatedAt, WarehouseChangeTimelineEvents.Requested, WarehouseChangeStatus.PendingPayment,
                item.RequestedByEmail ?? change.CreatedBy, item.BatchId is null ? null : $"BATCH:{item.BatchId}",
                item.BatchLineNumber is null ? null : $"Solicitud masiva, línea {item.BatchLineNumber}")
        };

        if (change.IsFree)
        {
            timeline.Add(new(change.CompletedAt ?? change.CreatedAt, WarehouseChangeTimelineEvents.FreeEntitlementApplied,
                WarehouseChangeStatus.Completed, null, change.EntitlementReference, change.EntitlementSource));
        }

        var paymentIds = await dbContext.PaymentDetails.AsNoTracking()
            .Where(d => d.ItemType == PayableItemTypes.WarehouseChange && d.SourceId == change.Id)
            .Select(d => d.PaymentId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var payments = await dbContext.Payments.AsNoTracking()
            .Where(p => paymentIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.PaymentNumber, cancellationToken);
        var history = await dbContext.PaymentStatusChanges.AsNoTracking()
            .Where(h => paymentIds.Contains(h.PaymentId))
            .ToListAsync(cancellationToken);

        timeline.AddRange(history.Select(h => new WarehouseChangeTimelineEventDto(
            h.ChangedAt, WarehouseChangeTimelineEvents.PaymentStatusChanged, h.ToStatus, h.ChangedBy,
            payments.GetValueOrDefault(h.PaymentId), h.Reason)));

        if (!change.IsFree && change.CompletedAt is { } completedAt)
            timeline.Add(new(completedAt, WarehouseChangeTimelineEvents.Completed, WarehouseChangeStatus.Completed, null, item.ReceiptNumber, null));

        if (change.Status == WarehouseChangeStatus.Cancelled)
            timeline.Add(new(change.ModifiedAt ?? change.CreatedAt, WarehouseChangeTimelineEvents.Cancelled, WarehouseChangeStatus.Cancelled, change.ModifiedBy, null, null));

        return Result<WarehouseChangeTraceDto>.Success(new WarehouseChangeTraceDto(
            item,
            timeline.OrderBy(e => e.OccurredAt).ToList()));
    }
}
