namespace HapagPortal.Application.ThirdPartyAccess.Audit;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Historial de accesos (M1-23). Con <see cref="BlNumber"/> o <see cref="BookingNumber"/>: el de ese
/// embarque, para quien tenga "consultar la auditoría de accesos" sobre él (M1-11); un tercero ve solo
/// los eventos en que participa su organización. Sin filtros: los eventos de la propia organización
/// (acceso abierto, terceros por defecto, accesos otorgados y recibidos).
/// </summary>
public sealed record GetAccessAuditQuery(
    string? BlNumber = null,
    string? BookingNumber = null,
    int Page = 1,
    int PageSize = 50) : IQuery<PagedResult<AccessAuditEntryDto>>;

public sealed class GetAccessAuditQueryValidator : AbstractValidator<GetAccessAuditQuery>
{
    public GetAccessAuditQueryValidator()
    {
        RuleFor(x => x.BlNumber).MaximumLength(50);
        RuleFor(x => x.BookingNumber).MaximumLength(50);
    }
}

public sealed class GetAccessAuditQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetAccessAuditQuery, PagedResult<AccessAuditEntryDto>>
{
    private const int MaxPageSize = 200;
    private const int DefaultPageSize = 50;

    public async Task<Result<PagedResult<AccessAuditEntryDto>>> Handle(GetAccessAuditQuery request, CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<PagedResult<AccessAuditEntryDto>>.Failure(loaded.Error);

        var context = loaded.Value;
        var organizationId = context.OrganizationId;
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > MaxPageSize ? DefaultPageSize : request.PageSize;

        IQueryable<AccessAuditEntry> query = dbContext.AccessAuditEntries.AsNoTracking();
        var ownEventsOnly = true;

        var blNumber = request.BlNumber?.Trim();
        var bookingNumber = request.BookingNumber?.Trim();

        if (!string.IsNullOrEmpty(blNumber) || !string.IsNullOrEmpty(bookingNumber))
        {
            var bl = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), context.Scope)
                .FirstOrDefaultAsync(b => string.IsNullOrEmpty(blNumber)
                    ? b.BookingNumber == bookingNumber
                    : b.BLNumber == blNumber, cancellationToken);

            if (bl is not null)
            {
                var permissions = await accessEvaluator.EvaluateAsync(context.Scope, bl, cancellationToken);
                if (!permissions.Can(ShipmentActionCodes.ViewAccessAudit))
                    return Result<PagedResult<AccessAuditEntryDto>>.Failure(Error.Forbidden);

                var booking = bl.BookingNumber;
                query = query.Where(e => e.BillOfLadingId == bl.Id
                    || (e.BillOfLadingId == null && booking != null && e.BookingNumber == booking));

                // Las partes del embarque ven todo su historial; un tercero, lo que lo involucra.
                ownEventsOnly = permissions.AccessSource != ShipmentAccessSources.Own;
            }
            else if (!string.IsNullOrEmpty(blNumber))
            {
                return Result<PagedResult<AccessAuditEntryDto>>.Failure(DomainErrors.BillOfLading.NotFoundByNumber(blNumber));
            }
            else
            {
                // Booking todavía sin BL (M1-20): solo los eventos en que participa la organización.
                query = query.Where(e => e.BookingNumber == bookingNumber);
            }
        }

        if (ownEventsOnly)
        {
            query = query.Where(e => e.GrantorClientId == organizationId
                || e.GranteeClientId == organizationId
                || e.ActorClientId == organizationId);
        }

        var total = await query.CountAsync(cancellationToken);
        var entries = await query
            .OrderByDescending(e => e.OccurredAt)
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var organizations = await AccessGrantMapper.LoadOrganizationsAsync(
            dbContext,
            entries.SelectMany(e => new[] { e.GrantorClientId, e.GranteeClientId, e.ActorClientId })
                .Where(id => id is not null)
                .Select(id => id!.Value),
            cancellationToken);

        OrganizationRefDto? Ref(Guid? id) =>
            id is not null && organizations.TryGetValue(id.Value, out var organization) ? AccessGrantMapper.ToRef(organization) : null;

        var items = entries.Select(e => new AccessAuditEntryDto(
            e.Id,
            e.OccurredAt,
            e.EventType,
            e.BillOfLadingId,
            e.BlNumber,
            e.BookingNumber,
            e.AccessGrantId,
            e.VisibilityWideningId,
            Ref(e.GrantorClientId),
            Ref(e.GranteeClientId),
            e.ActorUserId,
            e.ActorEmail,
            Ref(e.ActorClientId),
            e.Details)).ToList();

        return Result<PagedResult<AccessAuditEntryDto>>.Success(new PagedResult<AccessAuditEntryDto>(items, total, page, pageSize));
    }
}
