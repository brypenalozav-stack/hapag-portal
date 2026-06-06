namespace HapagPortal.Application.BillsOfLading.Read.GetMyBLs;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetMyBLsQuery() : IQuery<List<BillOfLadingResponseDto>>;
