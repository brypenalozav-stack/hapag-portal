namespace HapagPortal.Application.Documents.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Solicitudes de los documentos de Bolivia que se gestionan sobre el modelo de servicios on demand (Ola G) con un flujo
/// propio (Ola J): certificado de flete (M6-02) y carta de liberación y desconsolidado (M6-08). Así comparten número,
/// estados, línea de tiempo con fecha y actor (NF-14), avisos a la organización y la bandeja interna de Customer
/// Service con el resto de las solicitudes. No guarda: lo hace el llamador.
/// </summary>
public static class DocumentServiceRequests
{
    /// <summary>Definición activa del flujo que aplica al BL y cuya acción de M1-11 el usuario puede ver.</summary>
    public static async Task<Result<ServiceDefinition>> DefinitionAsync(
        IApplicationDbContext dbContext,
        string code,
        ServiceShipmentContext context,
        CancellationToken cancellationToken)
    {
        var definition = await dbContext.ServiceDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == code && d.IsActive, cancellationToken);

        return definition is null || !ServiceCatalogEvaluator.Applies(definition, context.BillOfLading, context.Permissions)
            ? Result<ServiceDefinition>.Failure(DomainErrors.ServiceDefinition.NotFoundByCode(code))
            : Result<ServiceDefinition>.Success(definition);
    }

    /// <summary>Crea la solicitud (borrador) con los datos validados y la envía; el llamador decide el estado siguiente.</summary>
    public static Result<ServiceRequest> CreateSubmitted(
        IApplicationDbContext dbContext,
        ServiceDefinition definition,
        ServiceShipmentContext context,
        ICurrentUserService currentUserService,
        ServiceActor actor,
        string normalizedJson,
        IReadOnlyList<string> containers,
        DateTime now)
    {
        var bl = context.BillOfLading;
        var grant = context.Permissions.GrantFor(definition.ActionCode);
        var request = new ServiceRequest
        {
            RequestNumber = PaymentLifecycle.NewNumber(DocumentPrefixes.ServiceRequest, now),
            DefinitionId = definition.Id,
            DefinitionCode = definition.Code,
            OrganizationId = context.Organization.Id,
            RequestedByUserId = currentUserService.UserId,
            RequestedByEmail = currentUserService.Email,
            OnBehalfOfClientId = grant?.GrantorOrganizationId,
            AccessGrantId = grant?.GrantId,
            BillOfLadingId = bl.Id,
            BlNumber = bl.BLNumber,
            BookingNumber = bl.BookingNumber,
            Country = bl.Country,
            Operation = ServiceOperations.Of(bl.ShipmentType),
            InputValuesJson = normalizedJson,
            ContainerNumbers = containers.Count == 0 ? null : string.Join(',', containers),
            Status = ServiceRequestStatus.Draft,
            StatusChangedAt = now,
            CreatedAt = now
        };

        dbContext.ServiceRequests.Add(request);
        ServiceRequestWorkflow.AddEvent(dbContext, request, null, ServiceRequestStatus.Draft, actor, null, now);

        var submitted = ServiceRequestWorkflow.Transition(dbContext, request, ServiceRequestStatus.Submitted, actor, null, now);
        return submitted.IsFailure ? Result<ServiceRequest>.Failure(submitted.Error) : Result<ServiceRequest>.Success(request);
    }

    /// <summary>Clave de emisión del documento de una solicitud: una solicitud, un documento.</summary>
    public static string GenerationKey(Guid serviceRequestId) => $"service-request:{serviceRequestId}";

    /// <summary>Solo importación de Bolivia (BO-IMP-08, BO-IMP-11).</summary>
    public static bool IsBoliviaImport(BillOfLading bl) =>
        bl.Country == CountryCodes.Bolivia && ServiceOperations.Of(bl.ShipmentType) == ServiceOperations.Import;

    /// <summary>Solicitudes de la organización (como solicitante o mandante) de una definición sobre el BL, más recientes primero.</summary>
    public static async Task<IReadOnlyList<ServiceRequestSummaryDto>> OwnRequestsAsync(
        IApplicationDbContext dbContext,
        string code,
        Guid billOfLadingId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var requests = await dbContext.ServiceRequests.AsNoTracking()
            .Where(r => r.DefinitionCode == code
                && r.BillOfLadingId == billOfLadingId
                && r.Status != ServiceRequestStatus.Draft
                && (r.OrganizationId == organizationId || r.OnBehalfOfClientId == organizationId))
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.RequestNumber)
            .ToListAsync(cancellationToken);

        return await ServiceRequestViews.SummariesAsync(dbContext, requests, cancellationToken);
    }

    /// <summary>Documentos del tipo sobre el BL que el usuario puede ver (M1-11), más recientes primero.</summary>
    public static async Task<IReadOnlyList<ShipmentDocumentDto>> VisibleDocumentsAsync(
        IApplicationDbContext dbContext,
        ShipmentServiceContextView view,
        string documentType,
        CancellationToken cancellationToken)
    {
        if (!ShipmentDocumentService.CanView(view.Permissions, documentType))
            return [];

        var documents = await dbContext.ShipmentDocuments.AsNoTracking()
            .Where(d => d.BillOfLadingId == view.BillOfLadingId && d.DocumentType == documentType)
            .OrderByDescending(d => d.IssuedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(d => ShipmentDocumentService.ToDto(
            d, d.IssuedForOrganizationId == view.OrganizationId ? view.OrganizationName : null)).ToList();
    }
}

/// <summary>Lo que necesitan las vistas de los documentos de una solicitud: BL, permisos y organización del usuario.</summary>
public sealed record ShipmentServiceContextView(
    Guid BillOfLadingId,
    ShipmentPermissionSet Permissions,
    Guid OrganizationId,
    string OrganizationName);
