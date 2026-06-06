namespace HapagPortal.Application.LocalCharges.Read.GetByContainer;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetLocalChargesByContainerQuery(string ContainerNumber) : IQuery<List<LocalChargeDto>>;
