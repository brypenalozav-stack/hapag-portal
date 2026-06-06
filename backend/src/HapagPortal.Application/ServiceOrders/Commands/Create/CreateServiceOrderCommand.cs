namespace HapagPortal.Application.ServiceOrders.Commands.Create;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record CreateServiceOrderCommand(
    string OrderType,
    string? Description,
    Guid BillOfLadingId,
    string Country) : ICommand<ServiceOrderResponseDto>;
