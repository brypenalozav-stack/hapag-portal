namespace HapagPortal.Application.Auth.ForgotPassword;

using HapagPortal.Application.Common.Messaging;

public sealed record ForgotPasswordCommand(string Email) : ICommand;
