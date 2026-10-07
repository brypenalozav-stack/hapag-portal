namespace HapagPortal.Application.Documents.ReleaseLetter;

using System.Globalization;
using System.Text.Json;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.Shipments;
using Microsoft.EntityFrameworkCore;

/// <summary>Estado TATC de una unidad seleccionada (M2-09): estado del portal, código del origen, número y motivos.</summary>
public sealed record ReleaseLetterTatcContainerDto(
    string ContainerNumber,
    string Status,
    string? SourceStatus,
    string? TatcNumber,
    IReadOnlyList<string> PendingReasons);

/// <summary>
/// Consulta del TATC de las unidades de la carta: <c>Available</c> = el sistema de TATC respondió (si no, <c>ErrorCode</c>,
/// NF-11); <c>Status</c> = agregado de las unidades seleccionadas (<c>TatcStatuses</c>).
/// </summary>
public sealed record ReleaseLetterTatcDto(
    bool Available,
    string? Status,
    string? ErrorCode,
    DateTime CheckedAt,
    IReadOnlyList<ReleaseLetterTatcContainerDto> Containers);

/// <summary>Registro de Counter del BL (M8-09), insumo de la carta; solo en la vista interna.</summary>
public sealed record ReleaseLetterCounterDto(
    DateOnly? ExchangeDate,
    bool HblReceived,
    DateOnly? HblReceivedAt,
    bool Deconsolidated,
    DateOnly? DeconsolidatedAt);

/// <summary>
/// Carta de liberación y desconsolidado (M6-08): la solicitud (número, estado, datos ingresados y línea de tiempo), el tipo
/// de sociedad, el transportista registrado elegido, el TATC al enviar y al aprobar, la carta emitida y la regla de TATC
/// vigente. La vista interna agrega el TATC consultado ahora y el registro de Counter.
/// </summary>
public sealed record ReleaseLetterRequestDto(
    ServiceRequestDetailDto Request,
    string LegalEntityType,
    Guid? CarrierOrganizationId,
    ReleaseLetterTatcDto TatcAtSubmission,
    ReleaseLetterTatcDto? TatcAtApproval,
    ReleaseLetterTatcDto? TatcNow,
    ReleaseLetterCounterDto? Counter,
    bool RequiresIssuedTatc,
    ShipmentDocumentDto? Document);

/// <summary>
/// Carta de liberación y desconsolidado (M6-08, BO-IMP-11). Definiciones de esta entrega (provisorias hasta su validación
/// con el área legal, documentadas en el contrato de la Ola J):
/// <list type="bullet">
/// <item>Tipo de sociedad: empresa (razón social, NIT, domicilio, representante legal y su documento) o persona natural
/// (nombre y documento de identidad).</item>
/// <item>Transportista: una organización transportista registrada vinculada al BL por la organización (acceso otorgado o
/// pre-creada, M1-09) o sus datos libres (nombre y NIT/RUT).</item>
/// <item>Circuito de aprobación: Customer Service en la bandeja interna de solicitudes (Ola G); al aprobar se emite la
/// carta y la solicitud se completa; el rechazo lleva su motivo.</item>
/// <item>Vínculo con el TATC (M2-09): se consulta y registra al enviar y al aprobar el estado de cada unidad seleccionada
/// y se imprime en la carta; con <c>Documents:ReleaseLetterRequiresIssuedTatc</c> la aprobación exige el TATC emitido de
/// todas ellas.</item>
/// </list>
/// No guarda: lo hace el llamador.
/// </summary>
public sealed class ReleaseLetterService(
    IApplicationDbContext dbContext,
    ITatcProvider tatcProvider,
    ShipmentDocumentService documents)
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool RequiresIssuedTatc => documents.Settings.ReleaseLetterRequiresIssuedTatc;

    /// <summary>TATC de las unidades indicadas consultado en el sistema de TATC (sin unidades = todas las del registro).</summary>
    public async Task<ReleaseLetterTatcDto> CheckTatcAsync(
        string blNumber,
        IReadOnlyCollection<string>? containers,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var result = await tatcProvider.GetByBlNumberAsync(blNumber, cancellationToken);
        if (result.IsFailure)
            return new ReleaseLetterTatcDto(false, null, result.Error.Code, now, []);

        var records = result.Value?.Containers ?? [];
        var numbers = containers is { Count: > 0 }
            ? containers
            : records.Select(c => c.ContainerNumber).ToList();

        var lines = numbers
            .Select(number =>
            {
                var record = records.FirstOrDefault(c => string.Equals(c.ContainerNumber, number, StringComparison.OrdinalIgnoreCase));
                return record is null
                    ? new ReleaseLetterTatcContainerDto(number.ToUpperInvariant(), TatcStatuses.NotRegistered, null, null, [])
                    : new ReleaseLetterTatcContainerDto(
                        record.ContainerNumber,
                        TatcStatusMapper.MapContainer(record.Status),
                        record.Status,
                        record.TatcNumber,
                        record.PendingReasons);
            })
            .ToList();

        var status = lines.Count == 0 ? TatcStatuses.NotRegistered : TatcStatusMapper.Aggregate(lines.Select(l => l.Status).ToList());
        return new ReleaseLetterTatcDto(true, status, null, now, lines);
    }

    public static void RecordSubmission(ReleaseLetterRequest letter, ReleaseLetterTatcDto tatc)
    {
        letter.TatcAvailableAtSubmission = tatc.Available;
        letter.TatcStatusAtSubmission = tatc.Status;
        letter.TatcErrorAtSubmission = tatc.ErrorCode;
        letter.TatcCheckedAtSubmission = tatc.CheckedAt;
        letter.TatcSnapshotAtSubmission = JsonSerializer.Serialize(tatc.Containers, JsonOptions);
    }

    /// <summary>Resumen del TATC para la línea de tiempo de la solicitud.</summary>
    public static string Describe(ReleaseLetterTatcDto tatc) =>
        tatc.Available
            ? $"TATC {tatc.Status}: {string.Join(", ", tatc.Containers.Select(c => $"{c.ContainerNumber} {c.Status}"))}."
            : $"El sistema de TATC no respondió ({tatc.ErrorCode}).";

    /// <summary>
    /// Aprobación de Customer Service: consulta y registra el TATC, aplica la regla de TATC configurada, genera la carta
    /// asociada a las unidades seleccionadas, la publica en el repositorio (M6-09) y la envía al correo registrado de la
    /// organización solicitante. La transición de la solicitud la hace el flujo estándar.
    /// </summary>
    public async Task<Result<ShipmentDocument>> IssueOnApprovalAsync(
        ServiceRequest request,
        ServiceActor approver,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var letter = await dbContext.ReleaseLetterRequests.FirstOrDefaultAsync(l => l.ServiceRequestId == request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"The release letter data of {request.RequestNumber} is missing.");

        var containers = ServiceRequestViews.Containers(request);
        var tatc = await CheckTatcAsync(request.BlNumber, containers, now, cancellationToken);
        if (RequiresIssuedTatc)
        {
            if (!tatc.Available)
                return Result<ShipmentDocument>.Failure(DomainErrors.ReleaseLetter.TatcUnavailable);

            var pending = tatc.Containers.Where(c => c.Status != TatcStatuses.Issued).Select(c => $"{c.ContainerNumber} ({c.Status})").ToList();
            if (pending.Count > 0)
                return Result<ShipmentDocument>.Failure(DomainErrors.ReleaseLetter.TatcNotIssued(string.Join(", ", pending)));
        }

        var bl = await dbContext.BillsOfLading.AsNoTracking().FirstAsync(b => b.Id == request.BillOfLadingId, cancellationToken);
        var organization = await dbContext.Clients.AsNoTracking().FirstAsync(c => c.Id == request.OrganizationId, cancellationToken);
        var counter = await dbContext.CounterRecords.AsNoTracking().FirstOrDefaultAsync(c => c.BillOfLadingId == bl.Id, cancellationToken);
        var values = ReadValues(request);

        var data = await documents.LoadDataAsync(bl, cancellationToken);
        var header = documents.NewHeader(ShipmentDocumentTypes.ReleaseLetter, bl, now);
        var model = ShipmentDocumentTemplates.ReleaseLetter(data, header, new ReleaseLetterData(
            request.RequestNumber,
            organization.Name,
            letter.LegalEntityType,
            Value(values, ReleaseLetterFields.ConsigneeName) ?? "-",
            Value(values, ReleaseLetterFields.ConsigneeTaxId) ?? "-",
            Value(values, ReleaseLetterFields.ConsigneeAddress),
            Value(values, ReleaseLetterFields.LegalRepresentativeName),
            Value(values, ReleaseLetterFields.LegalRepresentativeId),
            Value(values, ReleaseLetterFields.CarrierName) ?? "-",
            Value(values, ReleaseLetterFields.CarrierTaxId),
            letter.CarrierOrganizationId is not null,
            Value(values, ReleaseLetterFields.DriverName),
            Value(values, ReleaseLetterFields.DriverId),
            Value(values, ReleaseLetterFields.TruckPlate),
            Value(values, ReleaseLetterFields.Observations),
            containers,
            tatc.Available,
            tatc.Containers.Select(c => new ReleaseLetterTatcLine(c.ContainerNumber, c.Status, c.TatcNumber)).ToList(),
            tatc.CheckedAt,
            CounterStatus(counter),
            approver.Name,
            now));

        var actor = new DocumentActor(approver.UserId, approver.Name, null, request.OnBehalfOfClientId, request.AccessGrantId);
        var issued = await documents.IssueAsync(
            new DocumentIssue(
                ShipmentDocumentTypes.ReleaseLetter,
                bl,
                model,
                ShipmentDocumentOrigins.Request,
                actor,
                organization.Id,
                containers,
                GenerationKey: DocumentServiceRequests.GenerationKey(request.Id),
                RecipientEmails: [organization.Email]),
            cancellationToken);
        if (issued.IsFailure)
            return issued;

        var sent = await documents.SendAsync(issued.Value, [organization.Email], DocumentChannels.Email, DocumentActor.System, now, cancellationToken);
        if (sent.IsFailure)
            return Result<ShipmentDocument>.Failure(sent.Error);

        letter.TatcAvailableAtApproval = tatc.Available;
        letter.TatcStatusAtApproval = tatc.Status;
        letter.TatcErrorAtApproval = tatc.ErrorCode;
        letter.TatcCheckedAtApproval = tatc.CheckedAt;
        letter.TatcSnapshotAtApproval = JsonSerializer.Serialize(tatc.Containers, JsonOptions);
        letter.DocumentId = issued.Value.Id;

        ServiceRequestWorkflow.AddEvent(dbContext, request, request.Status, request.Status, ServiceActor.System,
            $"Al aprobar: {Describe(tatc)}", now);

        return issued;
    }

    /// <summary>Vista de la carta (cliente o interna).</summary>
    public async Task<ReleaseLetterRequestDto> DetailAsync(
        ServiceRequest request,
        bool clientView,
        bool internalView,
        string? organizationName,
        CancellationToken cancellationToken)
    {
        var letter = await dbContext.ReleaseLetterRequests.AsNoTracking().FirstAsync(l => l.ServiceRequestId == request.Id, cancellationToken);
        var detail = await ServiceRequestViews.DetailAsync(dbContext, request, clientView, cancellationToken);

        var document = letter.DocumentId is { } documentId
            ? await dbContext.ShipmentDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken)
            : null;

        ReleaseLetterTatcDto? now = null;
        ReleaseLetterCounterDto? counter = null;
        if (internalView)
        {
            now = await CheckTatcAsync(request.BlNumber, ServiceRequestViews.Containers(request), DateTime.UtcNow, cancellationToken);
            var record = await dbContext.CounterRecords.AsNoTracking().FirstOrDefaultAsync(c => c.BillOfLadingId == request.BillOfLadingId, cancellationToken);
            counter = record is null
                ? null
                : new ReleaseLetterCounterDto(record.ExchangeDate, record.HblReceived, record.HblReceivedAt, record.Deconsolidated, record.DeconsolidatedAt);
        }

        return new ReleaseLetterRequestDto(
            detail,
            letter.LegalEntityType,
            letter.CarrierOrganizationId,
            new ReleaseLetterTatcDto(
                letter.TatcAvailableAtSubmission, letter.TatcStatusAtSubmission, letter.TatcErrorAtSubmission,
                letter.TatcCheckedAtSubmission, ReadTatc(letter.TatcSnapshotAtSubmission)),
            letter.TatcCheckedAtApproval is { } approvedAt
                ? new ReleaseLetterTatcDto(
                    letter.TatcAvailableAtApproval ?? false, letter.TatcStatusAtApproval, letter.TatcErrorAtApproval,
                    approvedAt, ReadTatc(letter.TatcSnapshotAtApproval))
                : null,
            now,
            counter,
            RequiresIssuedTatc,
            document is null ? null : ShipmentDocumentService.ToDto(document, organizationName));
    }

    private static IReadOnlyList<ReleaseLetterTatcContainerDto> ReadTatc(string? json) =>
        string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<ReleaseLetterTatcContainerDto>>(json, JsonOptions) ?? [];

    private static JsonElement ReadValues(ServiceRequest request)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(request.InputValuesJson) ? "{}" : request.InputValuesJson);
        return document.RootElement.Clone();
    }

    private static string? Value(JsonElement values, string key) =>
        values.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim()
            : null;

    private static string? CounterStatus(CounterRecord? record)
    {
        if (record is null)
            return null;

        static string Date(DateOnly? date) => date?.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) ?? "-";

        var hbl = record.HblReceived ? $"HBL recibido el {Date(record.HblReceivedAt)}" : "HBL no recibido";
        var deconsolidated = record.Deconsolidated ? $"desconsolidado el {Date(record.DeconsolidatedAt)}" : "sin desconsolidar";
        return $"{hbl}; {deconsolidated}";
    }
}
