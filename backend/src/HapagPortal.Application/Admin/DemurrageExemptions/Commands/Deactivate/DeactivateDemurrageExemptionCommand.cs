namespace HapagPortal.Application.Admin.DemurrageExemptions.Commands.Deactivate;

using HapagPortal.Application.Common.Messaging;

public sealed record DeactivateDemurrageExemptionCommand(Guid Id) : ICommand;
