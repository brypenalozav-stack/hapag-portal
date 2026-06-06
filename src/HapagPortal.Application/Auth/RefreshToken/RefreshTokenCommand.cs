namespace HapagPortal.Application.Auth.RefreshToken;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record RefreshTokenCommand(
    string Token,
    string RefreshToken) : ICommand<AuthResponseDto>;
