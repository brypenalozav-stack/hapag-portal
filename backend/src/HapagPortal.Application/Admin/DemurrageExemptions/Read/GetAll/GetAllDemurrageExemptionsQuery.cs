namespace HapagPortal.Application.Admin.DemurrageExemptions.Read.GetAll;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetAllDemurrageExemptionsQuery() : IQuery<List<DemurrageExemptionResponseDto>>;
