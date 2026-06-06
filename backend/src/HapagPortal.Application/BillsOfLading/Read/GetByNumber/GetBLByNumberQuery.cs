namespace HapagPortal.Application.BillsOfLading.Read.GetByNumber;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetBLByNumberQuery(string BLNumber) : IQuery<BillOfLadingResponseDto>;
