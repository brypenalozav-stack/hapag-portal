namespace HapagPortal.Application.Shipments.LiberationStatus;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Charges;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Demurrage.Common;
using HapagPortal.Application.Demurrage.State;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.NoDebt;
using HapagPortal.Application.Documents.ReleaseLetter;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Application.Shipments.Detail;
using HapagPortal.Application.Shipments.Tatc;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

/// <summary>Monto pendiente de un paso por moneda.</summary>
public sealed record ReleaseAmountDto(string Currency, decimal Total);

/// <summary>Elemento de un paso (un cargo, una línea de demurrage, un bloqueo del CLD, una solicitud).</summary>
public sealed record ReleaseStepItemDto(
    string Code,
    string Label,
    string? Reference,
    string Status,
    bool Satisfied,
    decimal? Amount,
    string? Currency);

/// <summary>
/// Paso de la liberación: estado, motivo, acción disponible, montos pendientes y su detalle. <c>Status</c> según
/// <c>ReleaseStepStatuses</c>; <c>Reason</c> según <c>ReleaseStepReasons</c>; <c>Action</c> según <c>ReleaseStepActions</c>.
/// </summary>
public sealed record ReleaseStepDto(
    string Code,
    string Status,
    string? Reason,
    string Action,
    bool ActionAllowed,
    IReadOnlyList<ReleaseAmountDto> PendingAmounts,
    IReadOnlyList<ReleaseStepItemDto> Items);

/// <summary>Contenedor del BL con su demurrage y su TATC (detalle de contenedores de la consulta).</summary>
public sealed record ReleaseContainerDto(
    string ContainerNumber,
    string ContainerType,
    bool IsShipperOwned,
    string? DemurrageStatus,
    decimal? DemurrageAmount,
    string? DemurrageCurrency,
    string? TatcNumber,
    string? TatcStatus,
    DateTime? TatcIssuedAt,
    string? WarehouseCode,
    IReadOnlyList<string> TatcPendingReasons);

/// <summary>
/// TATC de la consulta: se presenta cuando los requisitos están cumplidos (<c>Unlocked</c>). Incluye la ventana de
/// disponibilidad (72 h, o 48 h desde Callao al norte de Chile), la última solicitud registrada y si el portal
/// puede solicitarlo (automáticamente al quedar listo).
/// </summary>
public sealed record ReleaseTatcDto(
    bool Unlocked,
    bool Available,
    string? Status,
    string? ErrorCode,
    DateTime? SourceUpdatedAt,
    DateTime RetrievedAt,
    int WindowHours,
    DateTime? AvailableFrom,
    bool WindowOpen,
    bool CanRequest,
    DateTime? LastRequestedAt,
    string? LastRequestStatus,
    string? LastRequestReason);

/// <summary>
/// Consulta de BL y TATC con el estado de liberación (M2-09, CL-IMP-13, BO-IMP-13): cada requisito con su estado y
/// su acción, el avance y, con todo cumplido, el TATC por contenedor. Solo importación (<c>Applicable</c>).
/// </summary>
public sealed record ReleaseStatusDto(
    Guid BlId,
    string BlNumber,
    string? BookingNumber,
    string Country,
    string Operation,
    string Status,
    string? Vessel,
    string? Voyage,
    string? PortOfLoading,
    string? PortOfDischarge,
    string? PortOfDischargeCode,
    string? FinalDestinationCode,
    DateTime? Eta,
    string? Consignee,
    string TimeZone,
    bool Applicable,
    IReadOnlyList<ReleaseStepDto> Steps,
    int CompletedSteps,
    int TotalSteps,
    bool Released,
    IReadOnlyList<ReleaseContainerDto> Containers,
    ReleaseTatcDto Tatc,
    IReadOnlyList<string> Notices,
    DateTime EvaluatedAt);

public sealed record GetReleaseStatusQuery(string BlNumber) : IQuery<ReleaseStatusDto>;

/// <summary>
/// Solicita el TATC de un BL cuyos requisitos están cumplidos: una solicitud de generación de un solo BL por su
/// puerto de descarga (o destino final), con las mismas validaciones de la generación masiva (M2-09).
/// </summary>
public sealed record RequestReleaseTatcCommand(string BlNumber) : ICommand<TatcBatchDto>;

public sealed class GetReleaseStatusQueryValidator : AbstractValidator<GetReleaseStatusQuery>
{
    public GetReleaseStatusQueryValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class RequestReleaseTatcCommandValidator : AbstractValidator<RequestReleaseTatcCommand>
{
    public RequestReleaseTatcCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class GetReleaseStatusQueryHandler(
    ISender sender,
    IApplicationDbContext dbContext,
    DocumentSettings documentSettings)
    : IQueryHandler<GetReleaseStatusQuery, ReleaseStatusDto>
{
    private static readonly string[] ClearedCharge = [ChargeStatus.Paid, ChargeStatus.Exempt, ChargeStatus.CreditImputed];
    private static readonly string[] ClearedOutcomes = [ChargeOutcomes.Paid, ChargeOutcomes.Exempt, ChargeOutcomes.CreditImputed];

    /// <summary>Solicitudes de carta de liberación que ya no avanzan.</summary>
    private static readonly string[] ClosedRequest = [ServiceRequestStatus.Rejected, ServiceRequestStatus.Cancelled, ServiceRequestStatus.Draft];

    public async Task<Result<ReleaseStatusDto>> Handle(GetReleaseStatusQuery request, CancellationToken cancellationToken)
    {
        var blNumber = request.BlNumber.Trim().ToUpperInvariant();

        // El detalle valida el acceso (NotFound si el BL no es accesible) y entrega permisos, flete y contenedores.
        var detailResult = await sender.Send(new GetShipmentDetailQuery(blNumber), cancellationToken);
        if (detailResult.IsFailure)
            return Result<ReleaseStatusDto>.Failure(detailResult.Error);
        var detail = detailResult.Value;

        if (!detail.AllowedActions.Contains(ShipmentActionCodes.ViewReleaseRequirements))
            return Result<ReleaseStatusDto>.Failure(Error.Forbidden);

        var bl = await dbContext.BillsOfLading.AsNoTracking()
            .Where(b => b.Id == detail.Id)
            .Select(b => new { b.FreightTerms, b.PortOfLoading, Containers = b.Containers.Select(c => new { c.ContainerNumber, c.ContainerType, c.IsShipperOwned }).ToList() })
            .FirstAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var isImport = detail.Operation == ShipmentOperations.Import;
        var isBolivia = detail.Country == CountryCodes.Bolivia;
        var timeZone = BusinessCalendar.TimeZoneId(detail.Country);

        if (!isImport)
        {
            return Result<ReleaseStatusDto>.Success(new ReleaseStatusDto(
                detail.Id, detail.BlNumber, detail.BookingNumber, detail.Country, detail.Operation, detail.Status,
                detail.Vessel, detail.Voyage, detail.PortOfLoading, detail.PortOfDischarge, detail.PortOfDischargeCode,
                detail.FinalDestinationCode, detail.Eta, detail.Consignee, timeZone, Applicable: false, Steps: [], 0, 0, Released: false, Containers: [],
                EmptyTatc(now), Notices: [], now));
        }

        var charges = await sender.Send(new GetShipmentChargesQuery(blNumber), cancellationToken);
        var documents = await sender.Send(new GetShipmentDocumentsQuery(blNumber), cancellationToken);
        var demurrage = await sender.Send(new GetDemurrageStatusQuery(blNumber), cancellationToken);
        var tatc = await sender.Send(new GetShipmentTatcQuery(blNumber), cancellationToken);

        var steps = new List<ReleaseStepDto>
        {
            FreightStep(detail, bl.FreightTerms),
            LocalChargesStep(charges),
            ResponsibilityLetterStep(documents, charges),
            DemurrageStep(demurrage),
        };

        if (isBolivia)
        {
            var advance = AdvanceDemurrageStep(demurrage);
            if (advance is not null)
                steps.Add(advance);

            var noDebt = await sender.Send(new GetNoDebtEligibilityQuery(blNumber), cancellationToken);
            steps.Add(NoDebtStep(noDebt, documents));

            var releaseLetter = await sender.Send(new GetReleaseLetterQuery(blNumber), cancellationToken);
            steps.Add(ReleaseLetterStep(releaseLetter, documentSettings.ReleaseLetterRequiresIssuedTatc));
        }

        // Con la carta de liberación a la espera del TATC (configuración que exige el TATC emitido para aprobarla),
        // la carta enviada cuenta como cumplida para no bloquear el TATC del que depende.
        var released = steps.All(s => ReleaseStepStatuses.Satisfied.Contains(s.Status)
            || (s.Code == ReleaseStepCodes.ReleaseLetter && s.Reason == ReleaseStepReasons.AwaitingTatc));
        var completed = steps.Count(s => ReleaseStepStatuses.Satisfied.Contains(s.Status));

        var tatcDto = tatc.IsSuccess ? tatc.Value : null;
        var containers = Containers(bl.Containers.Select(c => (c.ContainerNumber, c.ContainerType, c.IsShipperOwned)).ToList(), demurrage, tatcDto);

        var windowHours = WindowHours(bl.PortOfLoading, detail.PortOfDischargeCode ?? detail.FinalDestinationCode);
        var availableFrom = detail.Eta?.AddHours(-windowHours);
        var windowOpen = availableFrom is null || now >= availableFrom;

        var lastRequest = await dbContext.TatcBatchItems.AsNoTracking()
            .Where(i => i.BillOfLadingId == detail.Id)
            .OrderByDescending(i => i.Batch.CreatedAt)
            .Select(i => new { i.Batch.CreatedAt, i.Status, i.ReasonCode })
            .FirstOrDefaultAsync(cancellationToken);

        var tatcStatus = tatcDto?.Status;
        var alreadyIssued = tatcStatus is TatcStatuses.Issued or TatcStatuses.PreTatc or TatcStatuses.Cancelled;
        var onlyShipperOwned = containers.Count > 0 && containers.All(c => c.IsShipperOwned);
        var canRequest = released && windowOpen && !alreadyIssued && !onlyShipperOwned
            && tatcDto is { CanRequestGeneration: true }
            && (detail.PortOfDischargeCode ?? detail.FinalDestinationCode) is not null;

        var notices = new List<string> { windowHours == TatcWindowRules.NorthFromCallaoHours ? ReleaseNotices.TatcWindow48h : ReleaseNotices.TatcWindow72h };
        if (containers.Any(c => c.IsShipperOwned))
            notices.Add(ReleaseNotices.ShipperOwned);
        if (lastRequest?.Status == TatcBatchItemStatus.Accepted && !alreadyIssued)
            notices.Add(ReleaseNotices.TatcRequested);

        var tatcSection = new ReleaseTatcDto(
            Unlocked: released,
            Available: tatcDto?.Available ?? false,
            Status: tatcStatus,
            ErrorCode: tatc.IsSuccess ? tatcDto!.ErrorCode : tatc.Error.Code,
            SourceUpdatedAt: tatcDto?.SourceUpdatedAt,
            RetrievedAt: tatcDto?.RetrievedAt ?? now,
            WindowHours: windowHours,
            AvailableFrom: availableFrom,
            WindowOpen: windowOpen,
            CanRequest: canRequest,
            LastRequestedAt: lastRequest?.CreatedAt,
            LastRequestStatus: lastRequest?.Status,
            LastRequestReason: lastRequest?.ReasonCode);

        return Result<ReleaseStatusDto>.Success(new ReleaseStatusDto(
            detail.Id, detail.BlNumber, detail.BookingNumber, detail.Country, detail.Operation, detail.Status,
            detail.Vessel, detail.Voyage, detail.PortOfLoading, detail.PortOfDischarge, detail.PortOfDischargeCode,
            detail.FinalDestinationCode, detail.Eta, detail.Consignee, timeZone, Applicable: true, steps, completed, steps.Count, released,
            containers, tatcSection, notices, now));
    }

    private static ReleaseTatcDto EmptyTatc(DateTime now) =>
        new(false, false, null, null, null, now, TatcWindowRules.DefaultHours, null, false, false, null, null, null);

    /// <summary>Ventana del aviso: 48 h para carga embarcada en Callao con destino a Iquique, Angamos o Antofagasta; si no, 72 h.</summary>
    public static int WindowHours(string? portOfLoading, string? dischargeCode)
    {
        var fromCallao = portOfLoading is not null
            && (portOfLoading.Contains("callao", StringComparison.OrdinalIgnoreCase)
                || portOfLoading.Contains(TatcWindowRules.CallaoCode, StringComparison.OrdinalIgnoreCase));
        var toNorth = dischargeCode is not null && TatcWindowRules.NorthernChilePorts.Contains(dischargeCode.Trim().ToUpperInvariant());
        return fromCallao && toNorth ? TatcWindowRules.NorthFromCallaoHours : TatcWindowRules.DefaultHours;
    }

    private static ReleaseStepDto Step(
        string code, string status, string? reason = null, string action = ReleaseStepActions.None, bool actionAllowed = false,
        IReadOnlyList<ReleaseAmountDto>? amounts = null, IReadOnlyList<ReleaseStepItemDto>? items = null) =>
        new(code, status, reason, action, actionAllowed, amounts ?? [], items ?? []);

    private static IReadOnlyList<ReleaseAmountDto> Totals(IEnumerable<(string Currency, decimal Amount)> amounts) =>
        amounts.Where(a => a.Amount > 0).GroupBy(a => a.Currency).Select(g => new ReleaseAmountDto(g.Key, g.Sum(x => x.Amount))).ToList();

    private static ReleaseStepDto FreightStep(ShipmentDetailDto detail, string? freightTerms)
    {
        if (detail.Freight is null)
            return Step(ReleaseStepCodes.Freight, ReleaseStepStatuses.Unavailable, ReleaseStepReasons.NoPermission);

        var item = new ReleaseStepItemDto("FREIGHT", "FREIGHT", detail.BlNumber, detail.Freight.Status, detail.Freight.Status == "PAID",
            detail.Freight.Amount, detail.Freight.Currency);
        if (detail.Freight.Status == "PAID")
            return Step(ReleaseStepCodes.Freight, ReleaseStepStatuses.Done, items: [item]);

        // Flete prepagado: se paga en origen y no es requisito en destino.
        if (string.Equals(freightTerms, "Prepaid", StringComparison.OrdinalIgnoreCase))
            return Step(ReleaseStepCodes.Freight, ReleaseStepStatuses.NotRequired, ReleaseStepReasons.Prepaid, items: [item]);

        return Step(ReleaseStepCodes.Freight, ReleaseStepStatuses.Pending, action: ReleaseStepActions.PayFreight,
            actionAllowed: detail.CanOperate && detail.AllowedActions.Contains(ShipmentActionCodes.PayFreight),
            amounts: Totals([(detail.Freight.Currency, detail.Freight.Amount)]), items: [item]);
    }

    private static ReleaseStepDto LocalChargesStep(Result<ShipmentChargesDto> charges)
    {
        if (charges.IsFailure)
            return Step(ReleaseStepCodes.LocalCharges, ReleaseStepStatuses.Unavailable, ReasonOf(charges.Error));

        var c = charges.Value;
        if (!c.RulesAvailable)
            return Step(ReleaseStepCodes.LocalCharges, ReleaseStepStatuses.Unavailable, ReleaseStepReasons.SourceUnavailable);

        // Recargos del BL (Gate In, EDS y demás cargos locales); demurrage, MHD y demoras anticipadas tienen su paso.
        var local = c.Charges
            .Where(x => x.Category == ChargeCategories.LocalCharge && x.ConceptCode != ChargeConceptCodes.AdvanceDemurrageBo)
            .OrderBy(x => x.ConceptCode is ChargeConceptCodes.GateIn or ChargeConceptCodes.Eds ? 0 : 1)
            .ToList();
        var items = local.Select(x =>
        {
            var cleared = ClearedOutcomes.Contains(x.Outcome) || ClearedCharge.Contains(x.Status);
            return new ReleaseStepItemDto(x.ConceptCode, x.ConceptName, x.Description, cleared ? x.Outcome : x.Status, cleared,
                cleared ? x.TotalAmount : x.PayableTotal, x.Currency);
        }).ToList();

        if (items.All(i => i.Satisfied))
            return Step(ReleaseStepCodes.LocalCharges, ReleaseStepStatuses.Done, items: items);

        var pending = local.Where(x => !(ClearedOutcomes.Contains(x.Outcome) || ClearedCharge.Contains(x.Status))).ToList();
        var allowed = pending.Any(x => x.Action is ChargeActions.Pay or ChargeActions.AddToCart);
        return Step(ReleaseStepCodes.LocalCharges, ReleaseStepStatuses.Pending, action: ReleaseStepActions.PayCharges, actionAllowed: allowed,
            amounts: Totals(pending.Select(x => (x.Currency, x.PayableTotal))), items: items);
    }

    private static ReleaseStepDto ResponsibilityLetterStep(Result<ShipmentDocumentsDto> documents, Result<ShipmentChargesDto> charges)
    {
        if (documents.IsFailure)
            return Step(ReleaseStepCodes.ResponsibilityLetter, ReleaseStepStatuses.Unavailable, ReasonOf(documents.Error));

        var state = documents.Value.ResponsibilityLetter;
        var issued = documents.Value.Documents
            .Where(d => d.DocumentType == ShipmentDocumentTypes.ResponsibilityLetter && d.Status == ShipmentDocumentStatus.Issued)
            .Select(d => new ReleaseStepItemDto("DOCUMENT", d.DocumentNumber, d.DocumentNumber, d.Status, true, null, null))
            .ToList();

        // Requisito que bloquea solo cuando Nexus lo exige (FFWW, M4-04); para el resto es gestión documental (M6-06).
        var required = state?.Required ?? charges is { IsSuccess: true } && charges.Value.Requirements.Any(r => r.Code == ProcessRequirements.ResponsibilityLetter);
        var fulfilled = state?.Status == ProcessRequirementStatus.Fulfilled || issued.Count > 0;

        if (fulfilled)
            return Step(ReleaseStepCodes.ResponsibilityLetter, ReleaseStepStatuses.Done, items: issued);
        if (!required)
            return Step(ReleaseStepCodes.ResponsibilityLetter, ReleaseStepStatuses.NotRequired, ReleaseStepReasons.NotFreightForwarder,
                ReleaseStepActions.IssueResponsibilityLetter, documents.Value.Actions.CanIssueResponsibilityLetter);

        return Step(ReleaseStepCodes.ResponsibilityLetter, ReleaseStepStatuses.Pending, action: ReleaseStepActions.IssueResponsibilityLetter,
            actionAllowed: documents.Value.Actions.CanIssueResponsibilityLetter);
    }

    private static ReleaseStepDto DemurrageStep(Result<DemurrageStatusDto> demurrage)
    {
        if (demurrage.IsFailure)
            return Step(ReleaseStepCodes.Demurrage, ReleaseStepStatuses.Unavailable, ReasonOf(demurrage.Error));

        var d = demurrage.Value;
        var lines = d.Lines.Select(l => new ReleaseStepItemDto(
            "CONTAINER", l.ContainerNumber, l.InvoiceNumber, l.IsExempt ? ChargeStatus.Exempt : l.Status,
            l.IsExempt || l.Status == DemurrageChargeStatus.Paid, l.TotalAmount, l.Currency)).ToList();
        var concepts = d.OtherConcepts
            .Where(o => o.ConceptCode != ChargeConceptCodes.AdvanceDemurrageBo)
            .Select(o => new ReleaseStepItemDto(o.ConceptCode, o.ConceptName, o.Description, o.Status,
                ClearedCharge.Contains(o.Status), o.PayableTotal, o.Currency))
            .ToList();
        var items = lines.Concat(concepts).ToList();
        var pendingAmounts = Totals(items.Where(i => !i.Satisfied && i.Amount is not null).Select(i => (i.Currency!, i.Amount!.Value)));

        if (d.State == DemurrageStates.NotCalculated)
            return Step(ReleaseStepCodes.Demurrage, ReleaseStepStatuses.Pending, ReleaseStepReasons.NotCalculated,
                ReleaseStepActions.CalculateDemurrage, d.ActionAllowed, items: items);

        if (d.State is DemurrageStates.CalculatedUnpaid or DemurrageStates.InvoicedWithDebt)
            return Step(ReleaseStepCodes.Demurrage, ReleaseStepStatuses.Pending,
                d.State == DemurrageStates.InvoicedWithDebt ? ReleaseStepReasons.InvoicedWithDebt : ReleaseStepReasons.CalculatedUnpaid,
                ReleaseStepActions.PayDemurrage, d.ActionAllowed, pendingAmounts, items);

        // Sin deuda de sobreestadía: el MHD (M3-02) sigue siendo requisito si está pendiente.
        if (concepts.Any(c => !c.Satisfied))
            return Step(ReleaseStepCodes.Demurrage, ReleaseStepStatuses.Pending, ReleaseStepReasons.CalculatedUnpaid,
                ReleaseStepActions.PayDemurrage, d.OtherConcepts.Any(o => o.Action is ChargeActions.Pay or ChargeActions.AddToCart),
                pendingAmounts, items);

        return Step(ReleaseStepCodes.Demurrage, ReleaseStepStatuses.Done, items.Count == 0 ? ReleaseStepReasons.NoDemurrage : null, items: items);
    }

    private static ReleaseStepDto? AdvanceDemurrageStep(Result<DemurrageStatusDto> demurrage)
    {
        if (demurrage.IsFailure)
            return null;

        var a = demurrage.Value.Advance;
        if (!a.Required)
            return null;

        var item = new ReleaseStepItemDto(ChargeConceptCodes.AdvanceDemurrageBo, ChargeConceptCodes.AdvanceDemurrageBo,
            a.RuleReason, a.Status, a.Status == AdvanceDemurrageStatus.Paid, a.Amount, a.Currency);
        if (a.Status == AdvanceDemurrageStatus.Paid)
            return Step(ReleaseStepCodes.AdvanceDemurrage, ReleaseStepStatuses.Done, items: [item]);

        return Step(ReleaseStepCodes.AdvanceDemurrage, ReleaseStepStatuses.Pending, action: ReleaseStepActions.PayAdvanceDemurrage,
            actionAllowed: a.ActionBlockedReason is null && a.Action != ChargeActions.None,
            amounts: a.Amount is { } amount && a.Currency is { } currency ? Totals([(currency, amount)]) : [], items: [item]);
    }

    private static ReleaseStepDto NoDebtStep(Result<NoDebtEligibilityDto> noDebt, Result<ShipmentDocumentsDto> documents)
    {
        var issued = documents.IsSuccess
            ? documents.Value.Documents
                .Where(d => d.DocumentType == ShipmentDocumentTypes.NoDebtCertificate && d.Status == ShipmentDocumentStatus.Issued)
                .Select(d => new ReleaseStepItemDto("DOCUMENT", d.DocumentNumber, d.DocumentNumber, d.Status, true, null, null))
                .ToList()
            : [];
        if (issued.Count > 0)
            return Step(ReleaseStepCodes.NoDebtCertificate, ReleaseStepStatuses.Done, items: issued);

        if (noDebt.IsFailure)
            return Step(ReleaseStepCodes.NoDebtCertificate, ReleaseStepStatuses.Unavailable, ReasonOf(noDebt.Error));

        var n = noDebt.Value;
        var blockers = n.Blockers.Select(b => new ReleaseStepItemDto(b.Code, b.Code, string.Join(", ", b.References), "Blocked", false,
            b.Amounts.Count == 1 ? b.Amounts[0].Total : null, b.Amounts.Count == 1 ? b.Amounts[0].Currency : null)).ToList();

        return n.Eligible
            ? Step(ReleaseStepCodes.NoDebtCertificate, ReleaseStepStatuses.Pending, action: ReleaseStepActions.RequestNoDebtCertificate, actionAllowed: n.CanRequest)
            : Step(ReleaseStepCodes.NoDebtCertificate, ReleaseStepStatuses.Pending, ReleaseStepReasons.Blocked,
                amounts: Totals(n.Blockers.SelectMany(b => b.Amounts).Select(a => (a.Currency, a.Total))), items: blockers);
    }

    private static ReleaseStepDto ReleaseLetterStep(Result<ReleaseLetterContextDto> letter, bool requiresIssuedTatc)
    {
        if (letter.IsFailure)
            return Step(ReleaseStepCodes.ReleaseLetter, ReleaseStepStatuses.Unavailable, ReasonOf(letter.Error));

        var l = letter.Value;
        var issued = l.Documents
            .Where(d => d.DocumentType == ShipmentDocumentTypes.ReleaseLetter && d.Status == ShipmentDocumentStatus.Issued)
            .Select(d => new ReleaseStepItemDto("DOCUMENT", d.DocumentNumber, d.DocumentNumber, d.Status, true, null, null))
            .ToList();
        if (issued.Count > 0)
            return Step(ReleaseStepCodes.ReleaseLetter, ReleaseStepStatuses.Done, items: issued);

        var requests = l.Requests.Select(r => new ReleaseStepItemDto("REQUEST", r.RequestNumber, r.RequestNumber, r.Status,
            r.Status is ServiceRequestStatus.Approved or ServiceRequestStatus.Completed, null, null)).ToList();
        var open = l.Requests.FirstOrDefault(r => !ClosedRequest.Contains(r.Status));
        if (open is not null)
        {
            if (open.Status is ServiceRequestStatus.Approved or ServiceRequestStatus.Completed)
                return Step(ReleaseStepCodes.ReleaseLetter, ReleaseStepStatuses.Done, items: requests);

            return Step(ReleaseStepCodes.ReleaseLetter, ReleaseStepStatuses.InProgress,
                requiresIssuedTatc ? ReleaseStepReasons.AwaitingTatc : ReleaseStepReasons.AwaitingApproval, items: requests);
        }

        var rejected = l.Requests.Any(r => r.Status == ServiceRequestStatus.Rejected);
        return Step(ReleaseStepCodes.ReleaseLetter, ReleaseStepStatuses.Pending, rejected ? ReleaseStepReasons.Rejected : null,
            ReleaseStepActions.RequestReleaseLetter, l.CanRequest, items: requests);
    }

    private static string ReasonOf(Error error) =>
        error.Code == Error.Forbidden.Code ? ReleaseStepReasons.NoPermission : ReleaseStepReasons.SourceUnavailable;

    private static List<ReleaseContainerDto> Containers(
        IReadOnlyList<(string Number, string Type, bool Sow)> containers,
        Result<DemurrageStatusDto> demurrage,
        ShipmentTatcDto? tatc)
    {
        // Un contenedor puede tener más de una línea (p. ej. una pagada y un recálculo posterior): se muestra la que
        // aún está pendiente o, si no hay, la más reciente.
        var lines = demurrage.IsSuccess
            ? demurrage.Value.Lines
                .GroupBy(l => l.ContainerNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(l => l.IsExempt || l.Status == DemurrageChargeStatus.Paid ? 1 : 0).ThenByDescending(l => l.EndDate).First(),
                    StringComparer.OrdinalIgnoreCase)
            : [];
        var tatcs = tatc?.Containers
            .GroupBy(t => t.ContainerNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase) ?? [];
        return containers.Select(c =>
        {
            lines.TryGetValue(c.Number, out var line);
            tatcs.TryGetValue(c.Number, out var t);
            return new ReleaseContainerDto(
                c.Number, c.Type, c.Sow,
                line is null ? null : line.IsExempt ? ChargeStatus.Exempt : line.Status,
                line?.TotalAmount, line?.Currency,
                t?.TatcNumber, t?.Status, t?.IssuedAt, t?.WarehouseCode, t?.PendingReasons ?? []);
        }).ToList();
    }
}

public sealed class RequestReleaseTatcCommandHandler(ISender sender)
    : ICommandHandler<RequestReleaseTatcCommand, TatcBatchDto>
{
    public async Task<Result<TatcBatchDto>> Handle(RequestReleaseTatcCommand request, CancellationToken cancellationToken)
    {
        var status = await sender.Send(new GetReleaseStatusQuery(request.BlNumber), cancellationToken);
        if (status.IsFailure)
            return Result<TatcBatchDto>.Failure(status.Error);
        if (!status.Value.Tatc.CanRequest)
            return Result<TatcBatchDto>.Failure(DomainErrors.Release.TatcNotRequestable);

        var location = status.Value.PortOfDischargeCode ?? status.Value.FinalDestinationCode!;
        return await sender.Send(new RequestTatcBatchCommand(location, [status.Value.BlNumber]), cancellationToken);
    }
}
