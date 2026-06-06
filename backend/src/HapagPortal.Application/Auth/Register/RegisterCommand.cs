namespace HapagPortal.Application.Auth.Register;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record RegisterCommand(
    string Name,
    string TaxId,
    string Country,
    string Email,
    string Password,
    string ClientType,
    string? Phone,
    string? AgentCode) : ICommand<ClientResponseDto>;
