namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.Organizations.ContactLists;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Listas de distribución de contactos por tipo de reporte (M1-06): el cliente ve y actualiza los correos registrados
/// (AN, copias, facturas, free time, etc.); los cambios se propagan al registro de contactos (P0060) y quedan auditados.
/// Actualizar requiere un perfil que opere; la matriz de M1-11 excluye a los transportistas.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/organizations/me/contact-lists")]
[RequiresFeature(FeatureNames.ContactLists)]
public sealed class ContactListsController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetContactListsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("{reportType}")]
    public async Task<IActionResult> Update(string reportType, [FromBody] ContactListUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateContactListCommand(reportType, request.Emails ?? []), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] string? reportType, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetContactListHistoryQuery(reportType), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

public sealed record ContactListUpdateRequest(IReadOnlyList<string>? Emails);
