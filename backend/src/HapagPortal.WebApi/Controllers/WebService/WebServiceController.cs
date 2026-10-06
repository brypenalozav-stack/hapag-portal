namespace HapagPortal.WebApi.Controllers.WebService;

using Asp.Versioning;
using HapagPortal.Application.Documents.ResponsibilityLetter;
using HapagPortal.Application.WarehouseChanges.Bulk;
using HapagPortal.Application.WarehouseChanges.Requests;
using HapagPortal.Application.WebService;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Canal de requerimientos vía Web Service (M3-17), desarrollado y administrado por Hapag-Lloyd: los sistemas de clientes
/// de alto volumen envían y consultan, máquina a máquina, cartas de responsabilidad y cambios de almacén (individuales y
/// masivos) de su organización. Autenticación por clave (<c>X-Api-Key</c>, NF-09), alcances por cliente, límite por
/// minuto, idempotencia (<c>Idempotency-Key</c>) y bitácora (NF-14). Cada operación ejecuta el mismo comando del portal con
/// el usuario técnico del cliente: mismas reglas de acceso (M1-11, NF-05), estados y trazabilidad. Sin notificaciones
/// salientes (webhooks): el cliente consulta el estado con <c>GET /requests/{id}</c>.
/// </summary>
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = ApiKeyDefaults.Scheme)]
[Route("api/ws/v{version:apiVersion}")]
public sealed class WebServiceController : ApiController
{
    /// <summary>Cliente autenticado: organización, alcances, límite y versión vigente de los términos de la carta.</summary>
    [HttpGet("me")]
    [WebServiceOperation(ApiClientOperations.ClientInfo)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetWsClientInfoQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("responsibility-letters/terms")]
    [WebServiceOperation(ApiClientOperations.ResponsibilityLetterTerms, Scope = ApiClientScopes.ResponsibilityLetter)]
    public async Task<IActionResult> ResponsibilityLetterTerms(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetResponsibilityLetterTermsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Carta de responsabilidad (M6-06) emitida con el firmante configurado de la organización.</summary>
    [HttpPost("responsibility-letters")]
    [WebServiceOperation(ApiClientOperations.ResponsibilityLetter, Scope = ApiClientScopes.ResponsibilityLetter, Creates = true)]
    public async Task<IActionResult> SubmitResponsibilityLetter([FromBody] WsResponsibilityLetterRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new SubmitWsResponsibilityLetterCommand(
            request.BlNumber ?? string.Empty,
            request.AcceptTerms,
            request.TermsVersion ?? string.Empty,
            request.CargoDescription,
            request.Observations,
            request.ContactPhone), cancellationToken);
        if (result.IsFailure)
            return HandleFailure(result);

        var document = result.Value;
        HttpContext.Items[WebServiceTarget.Item] = new WebServiceTarget(
            ApiClientTargetTypes.ShipmentDocument, document.Id, document.DocumentNumber, document.BlNumber);
        return StatusCode(StatusCodes.Status201Created, document);
    }

    /// <summary>Cambio de almacén individual (M3-04).</summary>
    [HttpPost("warehouse-changes")]
    [WebServiceOperation(ApiClientOperations.WarehouseChange, Scope = ApiClientScopes.WarehouseChange, Creates = true)]
    public async Task<IActionResult> SubmitWarehouseChange([FromBody] WsWarehouseChangeRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RequestWarehouseChangeCommand(
            request.BlNumber ?? string.Empty,
            request.ContainerNumber,
            request.FromWarehouse,
            request.ToWarehouse ?? string.Empty,
            request.TariffCode), cancellationToken);
        if (result.IsFailure)
            return HandleFailure(result);

        var change = result.Value;
        HttpContext.Items[WebServiceTarget.Item] = new WebServiceTarget(
            ApiClientTargetTypes.WarehouseChange, change.Id, $"{change.FromWarehouse} -> {change.ToWarehouse}", change.BlNumber);
        return StatusCode(StatusCodes.Status201Created, change);
    }

    /// <summary>Cambio de almacén masivo (M3-05), procesado en segundo plano (NF-19); consultar el avance por la solicitud.</summary>
    [HttpPost("warehouse-changes/bulk")]
    [WebServiceOperation(ApiClientOperations.WarehouseChangeBatch, Scope = ApiClientScopes.WarehouseChange, Creates = true)]
    public async Task<IActionResult> SubmitWarehouseChangeBatch([FromBody] WsWarehouseChangeBatchRequest request, CancellationToken cancellationToken)
    {
        var items = (request.Items ?? [])
            .Select(i => new WarehouseChangeBatchItemRequest(i.BlNumber ?? string.Empty, i.ContainerNumber, i.FromWarehouse, i.ToWarehouse ?? string.Empty, i.TariffCode))
            .ToList();

        var result = await Sender.Send(new SubmitWarehouseChangeBatchCommand(items), cancellationToken);
        if (result.IsFailure)
            return HandleFailure(result);

        var batch = result.Value;
        HttpContext.Items[WebServiceTarget.Item] = new WebServiceTarget(
            ApiClientTargetTypes.WarehouseChangeBatch, batch.Id, $"{batch.TotalItems} lines", null);
        return StatusCode(StatusCodes.Status202Accepted, batch);
    }

    /// <summary>Solicitudes enviadas por el cliente con el estado actual de lo que crearon.</summary>
    [HttpGet("requests")]
    [WebServiceOperation(ApiClientOperations.ListRequests)]
    public async Task<IActionResult> GetRequests(
        [FromQuery] string? operation,
        [FromQuery] string? outcome,
        [FromQuery] string? blNumber,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Sender.Send(new GetWsRequestsQuery(operation, outcome, blNumber, page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("requests/{id:guid}")]
    [WebServiceOperation(ApiClientOperations.GetRequest)]
    public async Task<IActionResult> GetRequest(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetWsRequestQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record WsResponsibilityLetterRequest(
    string? BlNumber,
    bool AcceptTerms,
    string? TermsVersion,
    string? CargoDescription,
    string? Observations,
    string? ContactPhone);

public sealed record WsWarehouseChangeRequest(
    string? BlNumber,
    string? ContainerNumber,
    string? FromWarehouse,
    string? ToWarehouse,
    string? TariffCode);

public sealed record WsWarehouseChangeBatchRequest(IReadOnlyList<WsWarehouseChangeRequest>? Items);
