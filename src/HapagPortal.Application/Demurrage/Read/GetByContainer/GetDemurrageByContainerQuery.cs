namespace HapagPortal.Application.Demurrage.Read.GetByContainer;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetDemurrageByContainerQuery(string ContainerNumber) : IQuery<List<DemurrageChargeDto>>;
