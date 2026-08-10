namespace HapagPortal.Application.Auth.ForgotPassword;

using HapagPortal.Application.Common.Messaging;

public sealed record ForgotPasswordCommand(string Email) : ICommand<ForgotPasswordResponse>;

/// <summary>
/// ResetToken solo se rellena en entornos no productivos, para poder probar el flujo
/// sin un servidor de correo. En producción siempre es null (BUG-5).
/// </summary>
public sealed record ForgotPasswordResponse(string? ResetToken);
