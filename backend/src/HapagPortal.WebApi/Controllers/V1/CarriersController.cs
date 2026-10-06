namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Organizations.Carriers;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Pre-creación de perfiles de transportistas (M1-09): el cliente crea el perfil de un transportista sin cuenta y le
/// asigna BL y bookings, que el transportista ve desde su primer ingreso. Cambiar requiere <c>org.access.manage</c>.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/organizations/me/carriers")]
public sealed class CarriersController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCarrierPreRegistrationsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> PreCreate([FromBody] PreCreateCarrierCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? StatusCode(result.Value.Created ? StatusCodes.Status201Created : StatusCodes.Status200OK, result.Value)
            : HandleFailure(result);
    }

    [HttpPost("{id:guid}/assignments")]
    [HasPermission(AccessPermissions.ManageThirdPartyAccess)]
    public async Task<IActionResult> Assign(Guid id, [FromBody] CarrierAssignmentRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new AssignCarrierReferencesCommand(id, request.BlNumbers, request.BookingNumbers), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record CarrierAssignmentRequest(IReadOnlyList<string>? BlNumbers, IReadOnlyList<string>? BookingNumbers);
