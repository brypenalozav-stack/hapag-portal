namespace HapagPortal.Application.Shipments.Search;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Application.Shipments.Issuance;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Listado único de los BL y bookings accesibles por el usuario (M2-06), con filtros por BL,
/// booking, nave, viaje, estado, operación importación/exportación (M2-07) y país (M1-04).
/// El universo lo define el evaluador de accesos (M1-11): propios, recibidos por acceso otorgado y
/// autoasociados (M1-12, M1-18), con su origen. La ausencia de un BL no implica ausencia de deuda.
/// Cada fila trae el último estado de emisión conocido (M2-02). Los BL no publicados por DIFU (M2-01) no
/// llegan a los clientes; el administrador los ve con el motivo y puede filtrarlos con <c>Published</c>.
/// </summary>
public sealed record SearchShipmentsQuery(
    string? BlNumber = null,
    string? BookingNumber = null,
    string? Vessel = null,
    string? Voyage = null,
    string? Status = null,
    string? Operation = null,
    string? Country = null,
    bool? Published = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ShipmentListItemDto>>;

public sealed class SearchShipmentsQueryValidator : AbstractValidator<SearchShipmentsQuery>
{
    public SearchShipmentsQueryValidator()
    {
        RuleFor(x => x.Operation)
            .Must(o => ShipmentOperations.All.Contains(o!.Trim().ToUpperInvariant()))
            .WithMessage("Operation must be IMPORT or EXPORT.")
            .When(x => !string.IsNullOrWhiteSpace(x.Operation));

        RuleFor(x => x.Country)
            .Must(c => CountryCodes.ValidCountries.Contains(c!.Trim().ToUpperInvariant()))
            .WithMessage("Country must be 'CL' or 'BO'.")
            .When(x => !string.IsNullOrWhiteSpace(x.Country));
    }
}

public sealed class SearchShipmentsQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<SearchShipmentsQuery, PagedResult<ShipmentListItemDto>>
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;
    private const string PendingStatus = "Pending";

    public async Task<Result<PagedResult<ShipmentListItemDto>>> Handle(
        SearchShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > MaxPageSize ? DefaultPageSize : request.PageSize;

        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        var query = accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope);

        if (!string.IsNullOrWhiteSpace(request.BlNumber))
        {
            var term = request.BlNumber.Trim().ToLower();
            query = query.Where(b => b.BLNumber.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.BookingNumber))
        {
            var term = request.BookingNumber.Trim().ToLower();
            query = query.Where(b => b.BookingNumber != null && b.BookingNumber.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Vessel))
        {
            var term = request.Vessel.Trim().ToLower();
            query = query.Where(b => b.Vessel != null && b.Vessel.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Voyage))
        {
            var term = request.Voyage.Trim().ToLower();
            query = query.Where(b => b.Voyage != null && b.Voyage.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim().ToLower();
            query = query.Where(b => b.Status.ToLower() == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Operation))
        {
            var operation = ShipmentOperations.Normalize(request.Operation);
            query = query.Where(b => b.ShipmentType.ToUpper() == operation);
        }

        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            var country = request.Country.Trim().ToUpperInvariant();
            query = query.Where(b => b.Country == country);
        }

        // M2-01: los clientes solo ven BL publicados (ya filtrados); el administrador puede separarlos.
        if (scope.IsAdmin && request.Published is { } published)
        {
            query = published
                ? ShipmentPublicationFilter.Published(query, dbContext.ShipmentPublicationRules)
                : ShipmentPublicationFilter.Unpublished(query, dbContext.ShipmentPublicationRules);
        }

        var total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(b => b.ETA ?? b.CreatedAt)
            .ThenBy(b => b.BLNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                Bl = b,
                // Recargos locales: visibles para todos los roles de la matriz base (M1-11).
                HasPendingCharges = b.LocalCharges.Any(lc => lc.Status == PendingStatus)
            })
            .ToListAsync(cancellationToken);

        var bls = rows.Select(r => r.Bl).ToList();
        var roles = await accessEvaluator.GetRolesAsync(scope, bls, cancellationToken);
        var sources = await accessEvaluator.GetAccessSourcesAsync(scope, bls, cancellationToken);

        var publications = new Dictionary<Guid, ShipmentPublicationDto>();
        if (scope.IsAdmin)
        {
            foreach (var bl in bls)
                publications[bl.Id] = ShipmentPublicationView.From(bl, await accessEvaluator.GetPublicationAsync(bl, cancellationToken));
        }

        var items = rows.Select(r =>
        {
            var blRoles = roles.GetValueOrDefault(r.Bl.Id) ?? [];
            return new ShipmentListItemDto(
                r.Bl.Id,
                r.Bl.BLNumber,
                r.Bl.BookingNumber,
                r.Bl.Vessel,
                r.Bl.Voyage,
                r.Bl.Status,
                ShipmentOperations.Normalize(r.Bl.ShipmentType),
                r.Bl.Country,
                r.Bl.PortOfLoading,
                r.Bl.PortOfDischarge,
                r.Bl.ETD,
                r.Bl.ETA,
                blRoles,
                sources.GetValueOrDefault(r.Bl.Id) ?? ShipmentAccessSources.Own,
                r.HasPendingCharges,
                ShipmentIssuanceReader.Summary(r.Bl),
                publications.GetValueOrDefault(r.Bl.Id));
        }).ToList();

        return Result<PagedResult<ShipmentListItemDto>>.Success(
            new PagedResult<ShipmentListItemDto>(items, total, page, pageSize));
    }
}
