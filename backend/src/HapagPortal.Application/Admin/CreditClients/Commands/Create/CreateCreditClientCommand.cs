namespace HapagPortal.Application.Admin.CreditClients.Commands.Create;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record CreateCreditClientCommand(
    Guid ClientId,
    string Country,
    decimal CreditLimit,
    DateTime? ExpiresAt) : ICommand<CreditClientResponseDto>;
