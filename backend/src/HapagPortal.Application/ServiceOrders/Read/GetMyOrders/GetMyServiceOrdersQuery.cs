namespace HapagPortal.Application.ServiceOrders.Read.GetMyOrders;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetMyServiceOrdersQuery() : IQuery<List<ServiceOrderResponseDto>>;
