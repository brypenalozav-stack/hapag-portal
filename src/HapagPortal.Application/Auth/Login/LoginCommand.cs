namespace HapagPortal.Application.Auth.Login;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record LoginCommand(
    string Email,
    string Password) : ICommand<AuthResponseDto>;
