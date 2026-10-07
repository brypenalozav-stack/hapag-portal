namespace HapagPortal.Application.Dashboard;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.Demurrage.Common;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Invoices;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Destino de navegación de un elemento del dashboard: el frontend arma la ruta con <c>Kind</c>
/// (<c>DashboardTargets</c>) y el BL o el identificador.
/// </summary>
public sealed record DashboardTargetDto(string Kind, string? BlNumber, Guid? Id);

public sealed record DashboardAmountDto(string Currency, decimal Total);

public sealed record DashboardCountDto(string Key, int Count);

/// <summary>Gestión de la organización (solicitud del portal) con su estado.</summary>
public sealed record DashboardRequestDto(
    string Kind,
    Guid Id,
    string Reference,
    string? BlNumber,
    string Status,
    bool InProgress,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    DashboardTargetDto Target);

public sealed record DashboardRequestsDto(int InProgress, IReadOnlyList<DashboardRequestDto> Items);

/// <summary>Servicio pendiente de pago: mismo <c>ItemType</c> y <c>SourceId</c> que acepta el carro.</summary>
public sealed record DashboardPayableDto(
    string ItemType,
    Guid SourceId,
    string? BlNumber,
    string? BookingNumber,
    string Country,
    string ConceptCode,
    string? Description,
    decimal TotalAmount,
    string Currency,
    string Status,
    DateOnly? DueDate,
    bool InCart,
    DashboardTargetDto Target);

public sealed record DashboardPaymentsDto(
    int Count,
    IReadOnlyList<DashboardAmountDto> Totals,
    IReadOnlyList<DashboardPayableDto> Items,
    bool Truncated);

public sealed record DashboardDocumentDto(
    Guid Id,
    string DocumentType,
    string DocumentNumber,
    string BlNumber,
    DateTime IssuedAt,
    string DownloadPath);

public sealed record DashboardShipmentDto(
    string BlNumber,
    string? BookingNumber,
    string Operation,
    string? Vessel,
    string? Voyage,
    string? Port,
    DateTime? Date);

public sealed record DashboardDemurrageItemDto(
    string BlNumber,
    string State,
    int PendingLines,
    IReadOnlyList<DashboardAmountDto> Amounts);

public sealed record DashboardDemurrageDto(int Count, IReadOnlyList<DashboardDemurrageItemDto> Items);

/// <summary>
/// Indicadores operativos de los embarques accesibles. Los conteos cubren todos los embarques; los bloques que
/// dependen de los permisos por BL se calculan sobre los <c>EvaluatedShipments</c> más recientes
/// (<c>ShipmentsTruncated</c> indica que hubo más).
/// </summary>
public sealed record DashboardIndicatorsDto(
    int TotalShipments,
    IReadOnlyList<DashboardCountDto> ByStatus,
    IReadOnlyList<DashboardCountDto> ByOperation,
    int WithPendingCharges,
    IReadOnlyList<DashboardShipmentDto> UpcomingArrivals,
    IReadOnlyList<DashboardShipmentDto> UpcomingDepartures,
    DashboardDemurrageDto DemurrageAtRisk,
    int EvaluatedShipments,
    bool ShipmentsTruncated);

/// <summary>Dashboard consolidado del cliente (M1-05).</summary>
public sealed record DashboardDto(
    Guid? OrganizationId,
    string? Country,
    string? Operation,
    DateTime GeneratedAt,
    DashboardRequestsDto Requests,
    DashboardPaymentsDto PendingPayments,
    IReadOnlyList<DashboardDocumentDto> Documents,
    DashboardIndicatorsDto Indicators);

/// <summary>Valores de <c>DashboardTargetDto.Kind</c> y de <c>DashboardRequestDto.Kind</c>.</summary>
public static class DashboardTargets
{
    public const string Shipment = "Shipment";
    public const string Charges = "Charges";
    public const string Demurrage = "Demurrage";
    public const string Invoice = "Invoice";
    public const string Documents = "Documents";
    public const string WarehouseChange = "WarehouseChange";
    public const string WarehouseChangeBatch = "WarehouseChangeBatch";
    public const string ServiceOrder = "ServiceOrder";
    public const string TatcBatch = "TatcBatch";
    public const string BlCopy = "BlCopy";
    public const string ResponsibilityLetter = "ResponsibilityLetter";

    /// <summary>Solicitud de servicio on demand (Ola G): <c>Id</c> de la solicitud.</summary>
    public const string ServiceRequest = "ServiceRequest";

    /// <summary>Carta de liberación y desconsolidado (M6-08): <c>Id</c> de la solicitud, que se sigue en su propia página.</summary>
    public const string ReleaseLetter = "ReleaseLetter";
}

/// <summary>
/// Dashboard del cliente (M1-05): gestiones en curso, servicios pendientes de pago con su destino, documentos
/// recientes para descargar e indicadores operativos de los embarques, en una sola vista. Todo sale de los datos
/// del portal y de sus puertos (sin acceso directo a sistemas de origen) y respeta los accesos del usuario
/// (M1-11, NF-05): embarques por el evaluador, cargos y documentos por la acción de la matriz de cada BL y
/// facturas por las reglas de M7-01. Filtros opcionales por operación (M2-07) y país (M1-04).
/// </summary>
public sealed record GetDashboardQuery(string? Operation = null, string? Country = null) : IQuery<DashboardDto>;

public sealed class GetDashboardQueryValidator : AbstractValidator<GetDashboardQuery>
{
    public GetDashboardQueryValidator()
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

public sealed class GetDashboardQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    IChargeRulesService chargeRulesService,
    FeatureSettings features)
    : IQueryHandler<GetDashboardQuery, DashboardDto>
{
    /// <summary>Embarques (los más recientes) cuyos permisos se evalúan uno a uno (NF-18).</summary>
    public const int MaxEvaluatedShipments = 100;

    public const int MaxPayables = 50;
    public const int MaxDocuments = 10;
    public const int MaxRequests = 20;
    public const int MaxUpcoming = 5;
    public const int UpcomingDays = 14;
    public const int RecentDays = 30;

    /// <summary>Recargos que se pagan desde la pestaña de demurrage (M3-02, M3-16).</summary>
    private static readonly string[] DemurrageTabConcepts = [ChargeConceptCodes.Mhd, ChargeConceptCodes.AdvanceDemurrageBo];

    public async Task<Result<DashboardDto>> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        var operation = string.IsNullOrWhiteSpace(request.Operation) ? null : ShipmentOperations.Normalize(request.Operation);
        var country = string.IsNullOrWhiteSpace(request.Country) ? null : request.Country.Trim().ToUpperInvariant();

        var shipments = accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope);
        if (operation is not null)
            shipments = shipments.Where(b => b.ShipmentType.ToUpper() == operation);
        if (country is not null)
            shipments = shipments.Where(b => b.Country == country);

        var candidates = await shipments
            .Include(b => b.LocalCharges)
            .Include(b => b.DemurrageCharges)
            .Include(b => b.Payments)
            .OrderByDescending(b => b.ETA ?? b.CreatedAt)
            .ThenBy(b => b.BLNumber)
            .Take(MaxEvaluatedShipments + 1)
            .ToListAsync(cancellationToken);

        var truncated = candidates.Count > MaxEvaluatedShipments;
        if (truncated)
            candidates = candidates.Take(MaxEvaluatedShipments).ToList();

        var permissions = new Dictionary<Guid, ShipmentPermissionSet>();
        foreach (var bl in candidates)
            permissions[bl.Id] = await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken);

        var organization = scope.OrganizationId is { } organizationId
            ? await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == organizationId, cancellationToken)
            : null;

        var inCart = await CartKeysAsync(scope, cancellationToken);
        var payables = await PayablesAsync(candidates, permissions, organization, inCart, cancellationToken);
        payables.AddRange(await InvoicesAsync(scope, country, inCart, now, cancellationToken));

        var orderedPayables = payables
            .OrderByDescending(p => p.Status == InvoiceStatus.Overdue)
            .ThenBy(p => p.DueDate ?? DateOnly.MaxValue)
            .ThenBy(p => p.BlNumber)
            .ThenBy(p => p.ConceptCode)
            .ToList();

        var totals = payables
            .GroupBy(p => p.Currency)
            .OrderBy(g => g.Key)
            .Select(g => new DashboardAmountDto(g.Key, g.Sum(p => p.TotalAmount)))
            .ToList();

        var indicators = await IndicatorsAsync(shipments, candidates, permissions, truncated, now, cancellationToken);
        var documents = await DocumentsAsync(shipments, scope, permissions, cancellationToken);
        var requests = await RequestsAsync(scope, now, cancellationToken);

        return Result<DashboardDto>.Success(new DashboardDto(
            scope.OrganizationId,
            country,
            operation,
            now,
            requests,
            new DashboardPaymentsDto(
                payables.Count,
                totals,
                orderedPayables.Take(MaxPayables).ToList(),
                Truncated: payables.Count > MaxPayables || truncated),
            documents,
            indicators));
    }

    private async Task<HashSet<(string, Guid)>> CartKeysAsync(AccessScope scope, CancellationToken cancellationToken)
    {
        if (scope.UserId is null || scope.OrganizationId is null)
            return [];

        var userId = scope.UserId.Value;
        var organizationId = scope.OrganizationId.Value;
        var cartIds = dbContext.Carts.Where(c => c.UserId == userId && c.OrganizationId == organizationId).Select(c => c.Id);

        var items = await dbContext.CartItems.AsNoTracking()
            .Where(i => cartIds.Contains(i.CartId))
            .Select(i => new { i.ItemType, i.SourceId })
            .ToListAsync(cancellationToken);

        return items.Select(i => (i.ItemType, i.SourceId)).ToHashSet();
    }

    /// <summary>Recargos, líneas de demurrage calculadas y flete pendientes, según la acción de M1-11 de cada BL.</summary>
    private async Task<List<DashboardPayableDto>> PayablesAsync(
        IReadOnlyList<BillOfLading> bls,
        IReadOnlyDictionary<Guid, ShipmentPermissionSet> permissions,
        Client? organization,
        HashSet<(string, Guid)> inCart,
        CancellationToken cancellationToken)
    {
        var items = new List<DashboardPayableDto>();
        bool? ipoExcluded = null;

        foreach (var bl in bls)
        {
            var allowed = permissions[bl.Id];

            if (allowed.Can(ShipmentActionCodes.PayMandatoryLocalCharges) || allowed.Can(ShipmentActionCodes.PayOnDemandLocalCharges))
            {
                foreach (var charge in bl.LocalCharges.Where(c => c.Status == ChargeStatus.Pending).OrderBy(c => c.ChargeType))
                {
                    // M4-03: el IPO no se presenta a clientes con crédito vigente en Nexus.
                    if (charge.ChargeType == ChargeConceptCodes.Ipo)
                    {
                        ipoExcluded ??= organization is not null
                            && (await chargeRulesService.GetConditionsAsync(organization, cancellationToken)).IpoExcluded;
                        if (ipoExcluded == true)
                            continue;
                    }

                    var target = DemurrageTabConcepts.Contains(charge.ChargeType)
                        ? DashboardTargets.Demurrage
                        : DashboardTargets.Charges;
                    items.Add(new DashboardPayableDto(
                        PayableItemTypes.LocalCharge, charge.Id, bl.BLNumber, bl.BookingNumber, bl.Country, charge.ChargeType,
                        charge.Description, charge.TotalAmount, charge.Currency, charge.Status, null,
                        inCart.Contains((PayableItemTypes.LocalCharge, charge.Id)),
                        new DashboardTargetDto(target, bl.BLNumber, charge.Id)));
                }
            }

            if (allowed.Can(ShipmentActionCodes.PayImportDemurrage))
            {
                foreach (var line in bl.DemurrageCharges.Where(l =>
                             !l.IsExempt && l.InvoiceNumber == null && l.Status == DemurrageChargeStatus.Pending))
                {
                    items.Add(new DashboardPayableDto(
                        PayableItemTypes.Demurrage, line.Id, bl.BLNumber, bl.BookingNumber, bl.Country, ChargeConceptCodes.Demurrage,
                        $"Demurrage {line.ContainerNumber}", line.TotalAmount, line.Currency, line.Status, null,
                        inCart.Contains((PayableItemTypes.Demurrage, line.Id)),
                        new DashboardTargetDto(DashboardTargets.Demurrage, bl.BLNumber, line.Id)));
                }
            }

            var freightPaid = bl.FreightPaidAt != null
                || bl.Payments.Any(p => p.PaymentType == PayableItemTypes.Freight && p.Status == PaymentStatus.Confirmed);
            if (allowed.Can(ShipmentActionCodes.PayFreight) && bl.FreightAmount > 0m && !freightPaid)
            {
                items.Add(new DashboardPayableDto(
                    PayableItemTypes.Freight, bl.Id, bl.BLNumber, bl.BookingNumber, bl.Country, PaymentConcepts.Freight,
                    null, bl.FreightAmount, bl.FreightCurrency, ChargeStatus.Pending, null,
                    inCart.Contains((PayableItemTypes.Freight, bl.Id)),
                    new DashboardTargetDto(DashboardTargets.Shipment, bl.BLNumber, bl.Id)));
            }
        }

        return items;
    }

    /// <summary>Facturas pendientes o vencidas visibles según M7-01 y pagables desde el carro.</summary>
    private async Task<List<DashboardPayableDto>> InvoicesAsync(
        AccessScope scope,
        string? country,
        HashSet<(string, Guid)> inCart,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (scope.IsAdmin)
            return [];

        var invoiceScope = await InvoiceAccess.LoadAsync(dbContext, accessEvaluator, cancellationToken);
        var organizationIds = invoiceScope.BlFilter.Keys.ToList();
        if (organizationIds.Count == 0)
            return [];

        var query = dbContext.CustomerInvoices.AsNoTracking()
            .Where(i => organizationIds.Contains(i.OrganizationId) && i.Status == InvoiceStatus.Pending);
        if (country is not null)
            query = query.Where(i => i.Country == country);

        var invoices = await query.ToListAsync(cancellationToken);

        return invoices
            .Where(i => InvoiceAccess.CanView(invoiceScope, i))
            .Select(i => (i.Country, Dto: InvoiceView.ToDto(
                i, BusinessCalendar.LocalDate(i.Country, now), inCart.Contains((PayableItemTypes.Invoice, i.Id)))))
            .Where(i => i.Dto.IsPayable)
            .Select(i => new DashboardPayableDto(
                PayableItemTypes.Invoice, i.Dto.Id, i.Dto.BlNumber, i.Dto.BookingNumber, i.Country, PaymentConcepts.Invoice,
                i.Dto.SiiNumber is null ? i.Dto.SourceNumber : $"{i.Dto.SourceNumber} (folio {i.Dto.SiiNumber})",
                i.Dto.TotalAmount, i.Dto.Currency, i.Dto.Status, i.Dto.DueDate, i.Dto.InCart,
                new DashboardTargetDto(DashboardTargets.Invoice, i.Dto.BlNumber, i.Dto.Id)))
            .ToList();
    }

    private async Task<DashboardIndicatorsDto> IndicatorsAsync(
        IQueryable<BillOfLading> shipments,
        IReadOnlyList<BillOfLading> candidates,
        IReadOnlyDictionary<Guid, ShipmentPermissionSet> permissions,
        bool truncated,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var total = await shipments.CountAsync(cancellationToken);

        var byStatus = (await shipments
                .GroupBy(b => b.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.Key)
            .Select(g => new DashboardCountDto(g.Key, g.Count))
            .ToList();

        var byOperation = (await shipments
                .GroupBy(b => b.ShipmentType.ToUpper())
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
            .OrderBy(g => g.Key)
            .Select(g => new DashboardCountDto(g.Key, g.Count))
            .ToList();

        var withPendingCharges = await shipments.CountAsync(
            b => b.LocalCharges.Any(lc => lc.Status == ChargeStatus.Pending), cancellationToken);

        var until = now.AddDays(UpcomingDays);
        var arrivals = await shipments
            .Where(b => b.ShipmentType.ToUpper() == ShipmentOperations.Import && b.ETA != null && b.ETA >= now && b.ETA <= until)
            .OrderBy(b => b.ETA)
            .Take(MaxUpcoming)
            .Select(b => new DashboardShipmentDto(
                b.BLNumber, b.BookingNumber, ShipmentOperations.Import, b.Vessel, b.Voyage, b.PortOfDischarge, b.ETA))
            .ToListAsync(cancellationToken);

        var departures = await shipments
            .Where(b => b.ShipmentType.ToUpper() == ShipmentOperations.Export && b.ETD != null && b.ETD >= now && b.ETD <= until)
            .OrderBy(b => b.ETD)
            .Take(MaxUpcoming)
            .Select(b => new DashboardShipmentDto(
                b.BLNumber, b.BookingNumber, ShipmentOperations.Export, b.Vessel, b.Voyage, b.PortOfLoading, b.ETD))
            .ToListAsync(cancellationToken);

        // Demurrage en riesgo: el mismo evaluador de estado de M3-18, solo donde la matriz permite ver demurrage.
        var atRisk = new List<DashboardDemurrageItemDto>();
        foreach (var bl in candidates.Where(b => DemurrageStateEvaluator.IsImport(b)
                     && permissions[b.Id].Can(ShipmentActionCodes.PayImportDemurrage)))
        {
            var (state, _, _) = DemurrageStateEvaluator.Evaluate(bl, bl.DemurrageCharges.ToList(), now);
            if (state == DemurrageStates.NoDemurrage)
                continue;

            var pending = bl.DemurrageCharges.Where(l => !l.IsExempt && l.Status != DemurrageChargeStatus.Paid).ToList();
            atRisk.Add(new DashboardDemurrageItemDto(
                bl.BLNumber,
                state,
                pending.Count,
                pending.GroupBy(l => l.Currency).OrderBy(g => g.Key)
                    .Select(g => new DashboardAmountDto(g.Key, g.Sum(l => l.TotalAmount))).ToList()));
        }

        return new DashboardIndicatorsDto(
            total,
            byStatus,
            byOperation,
            withPendingCharges,
            arrivals,
            departures,
            new DashboardDemurrageDto(atRisk.Count, atRisk.OrderBy(a => a.State).ThenBy(a => a.BlNumber).ToList()),
            candidates.Count,
            truncated);
    }

    /// <summary>Documentos emitidos más recientes de los embarques accesibles, solo los tipos que el usuario puede ver.</summary>
    private async Task<IReadOnlyList<DashboardDocumentDto>> DocumentsAsync(
        IQueryable<BillOfLading> shipments,
        AccessScope scope,
        Dictionary<Guid, ShipmentPermissionSet> permissions,
        CancellationToken cancellationToken)
    {
        var blIds = shipments.Select(b => b.Id);
        var documents = await dbContext.ShipmentDocuments.AsNoTracking()
            .Where(d => blIds.Contains(d.BillOfLadingId) && d.Status == ShipmentDocumentStatus.Issued)
            .OrderByDescending(d => d.IssuedAt)
            .Take(MaxDocuments * 5)
            .ToListAsync(cancellationToken);

        var visible = new List<DashboardDocumentDto>();
        foreach (var document in documents)
        {
            if (!permissions.TryGetValue(document.BillOfLadingId, out var allowed))
            {
                var bl = await shipments.FirstOrDefaultAsync(b => b.Id == document.BillOfLadingId, cancellationToken);
                allowed = bl is null ? ShipmentPermissionSet.None : await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken);
                permissions[document.BillOfLadingId] = allowed;
            }

            if (!ShipmentDocumentService.CanView(allowed, document.DocumentType))
                continue;

            visible.Add(new DashboardDocumentDto(
                document.Id,
                document.DocumentType,
                document.DocumentNumber,
                document.BlNumber,
                document.IssuedAt,
                $"/api/v1/documents/{Uri.EscapeDataString(document.BlNumber)}/{document.Id}/download"));

            if (visible.Count == MaxDocuments)
                break;
        }

        return visible;
    }

    /// <summary>
    /// Gestiones de la propia organización en curso y las terminadas en los últimos días: cambios de almacén y
    /// solicitudes masivas, copias de BL y cartas solicitadas, órdenes de servicio, solicitudes masivas de TATC y
    /// solicitudes de servicios on demand (Ola G; en curso mientras no terminen, incluidos los borradores).
    /// </summary>
    private async Task<DashboardRequestsDto> RequestsAsync(AccessScope scope, DateTime now, CancellationToken cancellationToken)
    {
        if (scope.IsAdmin || scope.OrganizationId is null || !scope.IsOperational)
            return new DashboardRequestsDto(0, []);

        var organizationId = scope.OrganizationId.Value;
        var since = now.AddDays(-RecentDays);
        var items = new List<DashboardRequestDto>();

        var changes = await dbContext.WarehouseChanges.AsNoTracking()
            .Include(w => w.BillOfLading)
            .Where(w => w.RequestedByClientId == organizationId
                && (w.Status == WarehouseChangeStatus.PendingPayment || w.CreatedAt >= since))
            .ToListAsync(cancellationToken);
        items.AddRange(changes.Select(w => new DashboardRequestDto(
            DashboardTargets.WarehouseChange, w.Id, $"{w.FromWarehouse} → {w.ToWarehouse}", w.BillOfLading?.BLNumber,
            w.Status, w.Status == WarehouseChangeStatus.PendingPayment, w.CreatedAt, w.CompletedAt,
            new DashboardTargetDto(DashboardTargets.WarehouseChange, w.BillOfLading?.BLNumber, w.Id))));

        var batches = await dbContext.WarehouseChangeBatches.AsNoTracking()
            .Where(b => b.ClientId == organizationId
                && (b.Status == BulkRequestStatus.Queued || b.Status == BulkRequestStatus.Processing || b.CreatedAt >= since))
            .ToListAsync(cancellationToken);
        items.AddRange(batches.Select(b => new DashboardRequestDto(
            DashboardTargets.WarehouseChangeBatch, b.Id, $"{b.SucceededItems}/{b.TotalItems}", null, b.Status,
            b.Status is BulkRequestStatus.Queued or BulkRequestStatus.Processing, b.CreatedAt, b.CompletedAt,
            new DashboardTargetDto(DashboardTargets.WarehouseChangeBatch, null, b.Id))));

        string[] requestedTypes =
            [ShipmentDocumentTypes.BlCopyValued, ShipmentDocumentTypes.BlCopyNonValued, ShipmentDocumentTypes.ResponsibilityLetter];
        var documents = await dbContext.ShipmentDocuments.AsNoTracking()
            .Where(d => d.IssuedForOrganizationId == organizationId
                && d.Origin == ShipmentDocumentOrigins.Request
                && requestedTypes.Contains(d.DocumentType)
                && d.IssuedAt >= since)
            .ToListAsync(cancellationToken);
        items.AddRange(documents.Select(d =>
        {
            var kind = d.DocumentType == ShipmentDocumentTypes.ResponsibilityLetter
                ? DashboardTargets.ResponsibilityLetter
                : DashboardTargets.BlCopy;
            return new DashboardRequestDto(
                kind, d.Id, d.DocumentNumber, d.BlNumber, d.Status, false, d.IssuedAt, d.IssuedAt,
                new DashboardTargetDto(DashboardTargets.Documents, d.BlNumber, d.Id));
        }));

        var orders = await dbContext.ServiceOrders.AsNoTracking()
            .Include(o => o.BillOfLading)
            .Where(o => o.ClientId == organizationId
                && (o.Status != ServiceOrderStatus.Completed && o.Status != ServiceOrderStatus.Cancelled || o.RequestedAt >= since))
            .ToListAsync(cancellationToken);
        items.AddRange(orders.Select(o => new DashboardRequestDto(
            DashboardTargets.ServiceOrder, o.Id, o.OrderNumber, o.BillOfLading?.BLNumber, o.Status,
            o.Status is not ServiceOrderStatus.Completed and not ServiceOrderStatus.Cancelled, o.RequestedAt, o.CompletedAt,
            new DashboardTargetDto(DashboardTargets.Shipment, o.BillOfLading?.BLNumber, o.Id))));

        var tatcBatches = await dbContext.TatcBatches.AsNoTracking()
            .Where(b => b.ClientId == organizationId && b.CreatedAt >= since)
            .ToListAsync(cancellationToken);
        items.AddRange(tatcBatches.Select(b => new DashboardRequestDto(
            DashboardTargets.TatcBatch, b.Id, $"{b.LocationCode} {b.AcceptedItems}/{b.TotalItems}", null, b.Status, false,
            b.CreatedAt, b.CompletedAt, new DashboardTargetDto(DashboardTargets.TatcBatch, null, b.Id))));

        // Cierre de Fase 1: solo las solicitudes cuyo flag está encendido (con los servicios on demand apagados, las cartas
        // de liberación). La carta se sigue en su propia página.
        var serviceRequests = await dbContext.ServiceRequests.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId
                && (!ServiceRequestStatus.Terminal.Contains(r.Status) || r.CreatedAt >= since || r.StatusChangedAt >= since))
            .VisibleFor(features)
            .ToListAsync(cancellationToken);
        items.AddRange(serviceRequests.Select(r => new DashboardRequestDto(
            DashboardTargets.ServiceRequest, r.Id, r.RequestNumber, r.BlNumber, r.Status,
            !ServiceRequestStatus.Terminal.Contains(r.Status), r.CreatedAt,
            r.CompletedAt ?? r.RejectedAt ?? r.CancelledAt,
            new DashboardTargetDto(
                r.DefinitionCode == ServiceDefinitionCodes.ReleaseLetter ? DashboardTargets.ReleaseLetter : DashboardTargets.ServiceRequest,
                r.BlNumber,
                r.Id))));

        var ordered = items
            .OrderByDescending(i => i.InProgress)
            .ThenByDescending(i => i.CreatedAt)
            .ToList();

        return new DashboardRequestsDto(ordered.Count(i => i.InProgress), ordered.Take(MaxRequests).ToList());
    }
}
