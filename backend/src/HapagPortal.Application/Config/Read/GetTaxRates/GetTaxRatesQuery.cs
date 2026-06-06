namespace HapagPortal.Application.Config.Read.GetTaxRates;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetTaxRatesQuery(string? Country) : IQuery<List<TaxRateResponseDto>>;
