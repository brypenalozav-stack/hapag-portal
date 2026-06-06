namespace HapagPortal.Application.Admin.CreditClients.Commands.Update;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record UpdateCreditClientCommand(
    Guid Id,
    decimal? CreditLimit,
    string? CreditStatus,
    DateTime? ExpiresAt) : ICommand<CreditClientResponseDto>;
