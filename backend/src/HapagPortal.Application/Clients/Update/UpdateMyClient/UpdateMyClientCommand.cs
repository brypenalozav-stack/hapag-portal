namespace HapagPortal.Application.Clients.Update.UpdateMyClient;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record UpdateMyClientCommand(
    string? Phone,
    string? Address,
    string? City) : ICommand<ClientResponseDto>;
