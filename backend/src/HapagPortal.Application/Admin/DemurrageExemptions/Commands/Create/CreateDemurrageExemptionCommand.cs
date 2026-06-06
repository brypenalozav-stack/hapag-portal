namespace HapagPortal.Application.Admin.DemurrageExemptions.Commands.Create;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record CreateDemurrageExemptionCommand(
    string ClientName,
    string TaxId,
    string Country,
    string? Reason) : ICommand<DemurrageExemptionResponseDto>;
