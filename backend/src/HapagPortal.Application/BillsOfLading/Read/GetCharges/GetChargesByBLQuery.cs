namespace HapagPortal.Application.BillsOfLading.Read.GetCharges;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetChargesByBLQuery(string BLNumber) : IQuery<BLChargesResponseDto>;
