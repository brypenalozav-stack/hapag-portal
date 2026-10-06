namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.WebService;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Administración interna de los clientes del canal Web Service (M3-17): alta con su usuario técnico y primera clave,
/// alcances, límite por minuto, firmante de la carta de responsabilidad, rotación y revocación de claves sin desplegar
/// (NF-09) y bitácora de sus solicitudes (NF-14). Las claves se muestran una sola vez y se guardan como hash.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/admin/api-clients")]
public sealed class AdminApiClientsController : ApiController
{
    [HttpGet]
    [HasPermission(ApiClientPermissions.Manage)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? organizationId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetApiClientsQuery(organizationId, status), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(ApiClientPermissions.Manage)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetApiClientQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [HasPermission(ApiClientPermissions.Manage)]
    public async Task<IActionResult> Create([FromBody] ApiClientRequestBody request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CreateApiClientCommand(
            request.OrganizationId ?? Guid.Empty,
            request.Name ?? string.Empty,
            request.Scopes ?? [],
            request.RateLimitPerMinute ?? 60,
            request.Signatory,
            request.TechnicalContactEmail,
            request.Notes), cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(ApiClientPermissions.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ApiClientRequestBody request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateApiClientCommand(
            id,
            request.Name ?? string.Empty,
            request.Scopes ?? [],
            request.RateLimitPerMinute ?? 60,
            request.Signatory,
            request.TechnicalContactEmail,
            request.Notes), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Emite una clave nueva (se muestra una sola vez); las vigentes sirven hasta el fin del período de gracia.</summary>
    [HttpPost("{id:guid}/keys/rotate")]
    [HasPermission(ApiClientPermissions.Manage)]
    public async Task<IActionResult> RotateKey(Guid id, [FromBody] RotateApiClientKeyBody? request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RotateApiClientKeyCommand(id, request?.GraceMinutes ?? 0), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/keys/{keyId:guid}/revoke")]
    [HasPermission(ApiClientPermissions.Manage)]
    public async Task<IActionResult> RevokeKey(Guid id, Guid keyId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RevokeApiClientKeyCommand(id, keyId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/revoke")]
    [HasPermission(ApiClientPermissions.Manage)]
    public async Task<IActionResult> Revoke(Guid id, [FromBody] RevokeApiClientBody request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RevokeApiClientCommand(id, request.Reason ?? string.Empty), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}/requests")]
    [HasPermission(ApiClientPermissions.Manage)]
    public async Task<IActionResult> GetRequests(
        Guid id,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Sender.Send(new GetApiClientRequestsQuery(id, page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record ApiClientRequestBody(
    Guid? OrganizationId,
    string? Name,
    IReadOnlyList<string>? Scopes,
    int? RateLimitPerMinute,
    ApiClientSignatoryDto? Signatory,
    string? TechnicalContactEmail,
    string? Notes);

public sealed record RotateApiClientKeyBody(int? GraceMinutes);

public sealed record RevokeApiClientBody(string? Reason);
