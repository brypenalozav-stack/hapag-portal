namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.Auth.ConfirmEmail;
using HapagPortal.Application.Auth.ForgotPassword;
using HapagPortal.Application.Auth.Login;
using HapagPortal.Application.Auth.Logout;
using HapagPortal.Application.Auth.RefreshToken;
using HapagPortal.Application.Auth.Register;
using HapagPortal.Application.Auth.RequestMembership;
using HapagPortal.Application.Auth.ResetPassword;
using HapagPortal.Application.Organizations.Carriers;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

[ApiVersion("1.0")]
public sealed class AuthController : ApiController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Register), result.Value)
            : HandleFailure(result);
    }

    /// <summary>Solicitud de un usuario nuevo para vincularse a una organización ya registrada (M1-08).</summary>
    [HttpPost("register/join")]
    public async Task<IActionResult> RequestMembership(
        [FromBody] RequestOrganizationMembershipCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Accepted(result.Value)
            : HandleFailure(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok()
            : HandleFailure(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok()
            : HandleFailure(result);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok()
            : HandleFailure(result);
    }

    /// <summary>
    /// Reenvía el código de invitación de un transportista pre-creado que intenta registrarse (M1-09). Siempre 202, exista
    /// o no la cuenta.
    /// </summary>
    [HttpPost("register/pre-created/resend-invitation")]
    public async Task<IActionResult> ResendCarrierInvitation(
        [FromBody] ResendCarrierInvitationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleFailure(result);
    }

    /// <summary>Cierre de sesión en el servidor (M1-10): revoca el refresh token. Idempotente.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LogoutCommand? command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command ?? new LogoutCommand(), cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }
}
