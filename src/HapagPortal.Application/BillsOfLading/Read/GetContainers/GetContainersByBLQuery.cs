namespace HapagPortal.Application.BillsOfLading.Read.GetContainers;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetContainersByBLQuery(string BLNumber) : IQuery<List<BLContainerDto>>;
