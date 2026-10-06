namespace HapagPortal.WebApi.Controllers.V1;

using System.Text;
using Asp.Versioning;
using HapagPortal.Application.DangerousGoods;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Buscador de clasificación de mercancías peligrosas (M10-06), disponible para todos los perfiles (M1-11), y
/// carga interna de la base de referencia desde un CSV (mantenedores).
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/dangerous-goods")]
public sealed class DangerousGoodsController : ApiController
{
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new SearchDangerousGoodsQuery(q ?? string.Empty), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Importa un CSV (UTF-8) con la lista de mercancías peligrosas; ver <see cref="ImportDangerousGoodsCommand"/>.</summary>
    [HttpPost("import")]
    [HasPermission(MaintainerPermissions.Manage)]
    [RequestSizeLimit(ImportDangerousGoodsCommandValidator.MaxContentLength + 1024 * 1024)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var content = await reader.ReadToEndAsync(cancellationToken);

        var result = await Sender.Send(new ImportDangerousGoodsCommand(content), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
