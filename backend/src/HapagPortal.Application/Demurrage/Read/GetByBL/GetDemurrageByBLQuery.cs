namespace HapagPortal.Application.Demurrage.Read.GetByBL;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetDemurrageByBLQuery(string BLNumber) : IQuery<List<DemurrageChargeDto>>;
