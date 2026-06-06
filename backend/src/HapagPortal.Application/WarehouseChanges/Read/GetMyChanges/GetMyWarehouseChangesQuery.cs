namespace HapagPortal.Application.WarehouseChanges.Read.GetMyChanges;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetMyWarehouseChangesQuery() : IQuery<List<WarehouseChangeResponseDto>>;
