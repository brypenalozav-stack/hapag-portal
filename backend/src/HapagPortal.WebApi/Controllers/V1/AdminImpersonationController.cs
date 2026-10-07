namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.Impersonation;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// «Vista como cliente» desde el área de administración (M8-08): solo usuarios internos con <c>impersonation.use</c>
/// eligen la organización y el usuario cliente; la sesión es de solo consulta por defecto, vence sola, no se anida, no
/// toca las credenciales del cliente y cada solicitud queda auditada con la identidad del actor.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[HasPermission(AdministrationPermissions.UseImpersonation)]
[Route("api/v{version:apiVersion}/admin/impersonation")]
[RequiresFeature(FeatureNames.Impersonation)]
public sealed class AdminImpersonationController : ApiController
{
    /// <summary>Usuarios de la organización, indicando cuáles pueden verse como cliente.</summary>
    [HttpGet("targets")]
    public async Task<IActionResult> Targets([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetImpersonationTargetsQuery(organizationId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Inicia la sesión: devuelve el token del cliente marcado como impersonación (sin refresh token).</summary>
    [HttpPost("sessions")]
    public async Task<IActionResult> Start([FromBody] StartImpersonationCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : HandleFailure(result);
    }

    /// <summary>Registro de sesiones: actor, cliente, organización, inicio, término y duración.</summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> Sessions([FromQuery] GetImpersonationSessionsQuery query, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Solicitudes hechas durante la sesión, auditadas con la identidad del actor.</summary>
    [HttpGet("sessions/{id:guid}/requests")]
    public async Task<IActionResult> Requests(
        Guid id,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Sender.Send(new GetImpersonationRequestsQuery(id, page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("sessions/{id:guid}/end")]
    public async Task<IActionResult> End(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new EndImpersonationSessionCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
