namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.ThirdPartyAccess.Audit;
using HapagPortal.Application.ThirdPartyAccess.Defaults;
using HapagPortal.Application.ThirdPartyAccess.Grants;
using HapagPortal.Application.ThirdPartyAccess.OpenAccess;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Vista única de accesos y permisos de la organización (M1-24): otorgamiento individual y masivo,
/// revocación, vigencia y permisos editables, terceros por defecto, acceso abierto, acceso anticipado
/// por booking, mandatos y auditoría (M1-03, M1-12 a M1-23). Consultar requiere una organización
/// aprobada; cambiar, el permiso de perfil <c>org.access.manage</c>. Las reglas de M1-11 se aplican
/// en Application con el evaluador de accesos.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/access")]
public sealed class AccessManagementController : ApiController
{
    [HttpGet("grants")]
    public async Task<IActionResult> GetGrants([FromQuery] GetAccessGrantsQuery query, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("grants/{id:guid}")]
    public async Task<IActionResult> GetGrant(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAccessGrantQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Otorgamiento en un solo flujo: destinatario, vigencia y permisos sobre uno o más BL/bookings.</summary>
    [HttpPost("grants")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> Grant([FromBody] GrantAccessCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Otorgamiento masivo: mismo contrato; la respuesta confirma cuántos registros se actualizaron.</summary>
    [HttpPost("grants/bulk")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> GrantBulk([FromBody] GrantAccessCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("grants/early-booking")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> GrantEarlyBooking(
        [FromBody] GrantEarlyBookingAccessCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("grants/{id:guid}")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> UpdateGrant(
        Guid id,
        [FromBody] UpdateAccessGrantCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { Id = id }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("grants/{id:guid}/revoke")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> RevokeGrant(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RevokeGrantRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RevokeAccessGrantsCommand([id], request?.Reason), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("grants/revoke")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> RevokeGrants(
        [FromBody] RevokeAccessGrantsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("grants/{id:guid}/accept-terms")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> AcceptMandateTerms(
        Guid id,
        [FromBody] AcceptMandateTermsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { Id = id }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("mandate-terms")]
    public async Task<IActionResult> GetMandateTerms(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMandateTermsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("grantees")]
    public async Task<IActionResult> SearchGrantees(
        [FromQuery] SearchGranteeOrganizationsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("grantable-actions")]
    public async Task<IActionResult> GetGrantableActions(
        [FromQuery] GetGrantableActionsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("defaults")]
    public async Task<IActionResult> GetDefaults(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetDefaultGranteesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("defaults")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> CreateDefault(
        [FromBody] CreateDefaultGranteeCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(GetDefaults), result.Value) : HandleFailure(result);
    }

    [HttpPut("defaults/{id:guid}")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> UpdateDefault(
        Guid id,
        [FromBody] UpdateDefaultGranteeCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { Id = id }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("defaults/{id:guid}")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> RemoveDefault(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RemoveDefaultGranteeCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpGet("open-access")]
    public async Task<IActionResult> GetOpenAccess(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetOpenAccessSettingQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("open-access")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> UpdateOpenAccess(
        [FromBody] UpdateOpenAccessSettingCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAudit([FromQuery] GetAccessAuditQuery query, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

/// <summary>Motivo opcional de una revocación individual.</summary>
public sealed record RevokeGrantRequest(string? Reason);
