namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Configuration.Secrets;
using HapagPortal.Application.Configuration.Settings;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Configuración por ámbito. La autorización (Global vs Client) se aplica en los handlers
/// según el permiso y el ClientId del usuario. Los secretos nunca se devuelven en claro.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
public sealed class ConfigurationController : ApiController
{
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(
        [FromQuery] string scope, [FromQuery] Guid? clientId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetSettingsQuery(scope, clientId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpsertSetting(
        [FromBody] UpsertSettingCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("secrets")]
    public async Task<IActionResult> GetSecrets(
        [FromQuery] string scope, [FromQuery] Guid? clientId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetSecretsQuery(scope, clientId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("secrets")]
    public async Task<IActionResult> UpsertSecret(
        [FromBody] UpsertSecretCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
