namespace HapagPortal.Application.WarehouseChanges.Commands.Create;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record CreateWarehouseChangeCommand(
    string FromWarehouse,
    string ToWarehouse,
    Guid BillOfLadingId,
    string Country,
    decimal Amount,
    string? Currency = null) : ICommand<WarehouseChangeResponseDto>;
