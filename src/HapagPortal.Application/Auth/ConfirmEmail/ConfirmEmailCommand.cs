namespace HapagPortal.Application.Auth.ConfirmEmail;

using HapagPortal.Application.Common.Messaging;

public sealed record ConfirmEmailCommand(
    string Email,
    string Token) : ICommand;
