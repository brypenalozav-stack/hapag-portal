namespace HapagPortal.Application.BillsOfLading.Read.GetAll;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetAllBLsQuery(
    string? Country,
    Guid? ClientId) : IQuery<List<BillOfLadingResponseDto>>;
