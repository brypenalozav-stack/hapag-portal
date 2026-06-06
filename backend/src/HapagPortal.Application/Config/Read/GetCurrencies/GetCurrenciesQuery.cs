namespace HapagPortal.Application.Config.Read.GetCurrencies;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetCurrenciesQuery() : IQuery<List<CurrencyResponseDto>>;
