namespace HapagPortal.Application.WarehouseChanges.Read.GetById;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetWarehouseChangeByIdQuery(Guid Id) : IQuery<WarehouseChangeResponseDto>;
