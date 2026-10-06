using System.Globalization;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;

namespace HapagPortal.Infrastructure.Integrations.Nexus;

/// <summary>
/// Cliente Real de Nexus (CT-NEXUS). Implementa los cuatro puertos de Nexus sobre la misma tubería
/// HTTP (logging NF-27 + resiliencia). Sin datos (404): lista vacía en exenciones y tarifas,
/// <c>Success(null)</c> en condiciones y tipo de cambio.
/// </summary>
public sealed class HttpNexusClient(HttpClient httpClient, ISecretResolver secretResolver)
    : IExemptionReader, ICreditConditionReader, IExchangeRateProvider, ITariffProvider
{
    private static readonly IntegrationEndpoint Endpoint = new(
        IntegrationSystems.Nexus, SecretTypes.NexusApiKey, "X-Api-Key", IntegrationHttp.CamelCaseJson);

    public async Task<Result<IReadOnlyList<ExemptionInfo>>> GetExemptionsAsync(
        string taxId,
        string? matchCode,
        DateOnly at,
        CancellationToken cancellationToken = default)
    {
        var uri = IntegrationHttp.WithQuery("exemptions", ("taxId", taxId), ("matchCode", matchCode), ("at", FormatDate(at)));
        var result = await SendAsync<ExemptionListDto>("getExemptions", uri, cancellationToken);

        if (result.IsFailure)
            return Result<IReadOnlyList<ExemptionInfo>>.Failure(result.Error);

        IReadOnlyList<ExemptionInfo> items = result.Value?.Items
            .Select(e => new ExemptionInfo(e.Concept, e.Amount, e.Currency, e.ValidFrom, e.ValidTo))
            .ToList() ?? [];

        return Result<IReadOnlyList<ExemptionInfo>>.Success(items);
    }

    public async Task<Result<CustomerConditions?>> GetConditionsAsync(
        string taxId,
        string? matchCode,
        CancellationToken cancellationToken = default)
    {
        var uri = IntegrationHttp.WithQuery($"customers/{IntegrationHttp.Segment(taxId)}/conditions", ("matchCode", matchCode));
        var result = await SendAsync<CustomerConditionsDto>("getCustomerConditions", uri, cancellationToken);

        if (result.IsFailure)
            return Result<CustomerConditions?>.Failure(result.Error);

        var conditions = result.Value is { } dto
            ? new CustomerConditions(
                dto.TaxId,
                dto.MatchCode,
                dto.IsFreightForwarder,
                dto.Credit is { } credit
                    ? new CreditCondition(credit.Concepts, credit.CreditDays, credit.ValidFrom, credit.ValidTo, credit.CreditLimit, credit.CreditLimitCurrency)
                    : null)
            : null;

        return Result<CustomerConditions?>.Success(conditions);
    }

    public async Task<Result<ExchangeRateQuote?>> GetRateAsync(
        string fromCurrency,
        string toCurrency,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var uri = IntegrationHttp.WithQuery("exchange-rates", ("from", fromCurrency), ("to", toCurrency), ("date", FormatDate(date)));
        var result = await SendAsync<ExchangeRateDto>("getExchangeRate", uri, cancellationToken);

        if (result.IsFailure)
            return Result<ExchangeRateQuote?>.Failure(result.Error);

        var quote = result.Value is { } dto
            ? new ExchangeRateQuote(dto.FromCurrency, dto.ToCurrency, dto.Rate, dto.EffectiveDate, dto.Source, dto.Approved)
            : null;

        return Result<ExchangeRateQuote?>.Success(quote);
    }

    public async Task<Result<IReadOnlyList<TariffItem>>> GetTariffsAsync(
        string countryCode,
        string concept,
        DateOnly at,
        CancellationToken cancellationToken = default)
    {
        var uri = IntegrationHttp.WithQuery("tariffs", ("country", countryCode), ("concept", concept), ("at", FormatDate(at)));
        var result = await SendAsync<TariffListDto>("getTariffs", uri, cancellationToken);

        if (result.IsFailure)
            return Result<IReadOnlyList<TariffItem>>.Failure(result.Error);

        IReadOnlyList<TariffItem> items = result.Value?.Items
            .Select(t => new TariffItem(t.Concept, t.ContainerType, t.Amount, t.Currency, t.ValidFrom, t.ValidTo))
            .ToList() ?? [];

        return Result<IReadOnlyList<TariffItem>>.Success(items);
    }

    private Task<Result<T?>> SendAsync<T>(string operation, string uri, CancellationToken cancellationToken)
        where T : class =>
        IntegrationHttp.SendAsync<T>(httpClient, secretResolver, Endpoint, operation, HttpMethod.Get, uri, cancellationToken);

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private sealed record ExemptionListDto(string TaxId, DateOnly At, IReadOnlyList<ExemptionDto> Items, string? MatchCode = null);

    private sealed record ExemptionDto(
        string Concept,
        DateOnly ValidFrom,
        decimal? Amount = null,
        string? Currency = null,
        DateOnly? ValidTo = null);

    private sealed record CustomerConditionsDto(string TaxId, bool IsFreightForwarder, CreditConditionDto? Credit, string? MatchCode = null);

    private sealed record CreditConditionDto(
        IReadOnlyList<string> Concepts,
        int CreditDays,
        DateOnly ValidFrom,
        DateOnly? ValidTo = null,
        decimal? CreditLimit = null,
        string? CreditLimitCurrency = null);

    private sealed record ExchangeRateDto(
        string FromCurrency,
        string ToCurrency,
        decimal Rate,
        DateOnly EffectiveDate,
        string Source,
        bool Approved);

    private sealed record TariffListDto(string Country, string Concept, DateOnly At, IReadOnlyList<TariffItemDto> Items);

    private sealed record TariffItemDto(
        string Concept,
        decimal Amount,
        string Currency,
        DateOnly ValidFrom,
        string? Code = null,
        string? ContainerType = null,
        DateOnly? ValidTo = null);
}
