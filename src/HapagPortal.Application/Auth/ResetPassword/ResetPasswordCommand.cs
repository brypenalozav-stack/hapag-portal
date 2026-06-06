namespace HapagPortal.Application.Auth.ResetPassword;

using HapagPortal.Application.Common.Messaging;

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword) : ICommand;
