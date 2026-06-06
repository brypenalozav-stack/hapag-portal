namespace HapagPortal.Application.LocalCharges.Read.GetByBL;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetLocalChargesByBLQuery(string BLNumber) : IQuery<List<LocalChargeDto>>;
