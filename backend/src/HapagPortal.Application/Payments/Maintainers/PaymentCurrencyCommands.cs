namespace HapagPortal.Application.Payments.Maintainers;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Payments;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed record PaymentCurrencyRuleDto(Guid Id, string Currency, bool IsEnabled, DateTime CreatedAt, DateTime? ModifiedAt);

/// <summary>
/// Monedas de pago de un concepto en un país (M5-04). <c>Configured</c> falso: rige la moneda del cargo y
/// la local. <c>ChileFinanceRules</c> recuerda que en Chile se agregan las reglas de M5-08 al validar.
/// </summary>
public sealed record PaymentCurrencyConceptDto(
    string Country,
    string ConceptCode,
    string ConceptName,
    bool Configured,
    IReadOnlyList<string> EnabledCurrencies,
    IReadOnlyList<PaymentCurrencyRuleDto> Rules,
    bool ChileFinanceRules);

/// <summary>Instantánea de una moneda por concepto para el registro de cambios (NF-15).</summary>
public sealed record PaymentCurrencySnapshot(string Country, string ConceptCode, string Currency, bool IsEnabled)
{
    public static PaymentCurrencySnapshot From(PaymentCurrencyRule rule) =>
        new(rule.Country, rule.ConceptCode, rule.Currency, rule.IsEnabled);
}

public sealed record GetPaymentCurrenciesQuery(string? Country) : IQuery<IReadOnlyList<PaymentCurrencyConceptDto>>;

/// <summary>
/// Fija las monedas habilitadas de un concepto en un país: habilita, deshabilita o agrega cada moneda y
/// registra cada cambio con usuario, fecha y valor anterior (NF-15). Lista vacía = ninguna habilitada.
/// </summary>
public sealed record SetPaymentCurrenciesCommand(string Country, string ConceptCode, IReadOnlyList<string> Currencies)
    : ICommand<PaymentCurrencyConceptDto>;

/// <summary>Quita la configuración del concepto: vuelve a regir la moneda del cargo y la local.</summary>
public sealed record ResetPaymentCurrenciesCommand(string Country, string ConceptCode) : ICommand;

public sealed record GetPaymentCurrencyHistoryQuery(string Country, string ConceptCode)
    : IQuery<IReadOnlyList<PaymentMaintainerChangeDto<PaymentCurrencySnapshot>>>;

public sealed class SetPaymentCurrenciesCommandValidator : AbstractValidator<SetPaymentCurrenciesCommand>
{
    public SetPaymentCurrenciesCommandValidator()
    {
        RuleFor(x => x.Country)
            .Must(c => CountryCodes.ValidCountries.Contains(c?.Trim().ToUpperInvariant()))
            .WithMessage("Country must be CL or BO.");
        RuleFor(x => x.ConceptCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Currencies).NotNull();
        RuleForEach(x => x.Currencies).Matches("^[A-Za-z]{3}$").WithMessage("Each currency must be an ISO 4217 code.");
    }
}

internal static class PaymentCurrencyConcepts
{
    /// <summary>Conceptos administrables: el catálogo de cargos más flete y facturas.</summary>
    public static async Task<IReadOnlyDictionary<string, string>> AllAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var concepts = await dbContext.ChargeConcepts.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PaymentConcepts.Freight] = "Flete",
        };

        foreach (var concept in concepts)
            result[concept.Code] = concept.Name;

        result[PaymentConcepts.Invoice] = "Factura";
        return result;
    }

    public static async Task<PaymentCurrencyConceptDto> ViewAsync(
        IApplicationDbContext dbContext,
        string country,
        string concept,
        string name,
        CancellationToken cancellationToken)
    {
        var rules = await dbContext.PaymentCurrencyRules.AsNoTracking()
            .Where(r => r.Country == country && r.ConceptCode == concept)
            .OrderBy(r => r.Currency)
            .ToListAsync(cancellationToken);

        return ToDto(country, concept, name, rules);
    }

    public static PaymentCurrencyConceptDto ToDto(string country, string concept, string name, IReadOnlyList<PaymentCurrencyRule> rules) => new(
        country,
        concept,
        name,
        rules.Count > 0,
        rules.Where(r => r.IsEnabled).Select(r => r.Currency).OrderBy(c => c, StringComparer.Ordinal).ToList(),
        rules.Select(r => new PaymentCurrencyRuleDto(r.Id, r.Currency, r.IsEnabled, r.CreatedAt, r.ModifiedAt)).ToList(),
        country == CountryCodes.Chile);
}

public sealed class GetPaymentCurrenciesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetPaymentCurrenciesQuery, IReadOnlyList<PaymentCurrencyConceptDto>>
{
    public async Task<Result<IReadOnlyList<PaymentCurrencyConceptDto>>> Handle(GetPaymentCurrenciesQuery request, CancellationToken cancellationToken)
    {
        var concepts = await PaymentCurrencyConcepts.AllAsync(dbContext, cancellationToken);
        var countries = request.Country is null
            ? CountryCodes.ValidCountries
            : [request.Country.Trim().ToUpperInvariant()];

        var rules = await dbContext.PaymentCurrencyRules.AsNoTracking()
            .Where(r => countries.Contains(r.Country))
            .ToListAsync(cancellationToken);

        IReadOnlyList<PaymentCurrencyConceptDto> rows = countries
            .SelectMany(country => concepts.Select(concept => PaymentCurrencyConcepts.ToDto(
                country,
                concept.Key,
                concept.Value,
                rules.Where(r => r.Country == country && r.ConceptCode == concept.Key).OrderBy(r => r.Currency).ToList())))
            .ToList();

        return Result<IReadOnlyList<PaymentCurrencyConceptDto>>.Success(rows);
    }
}

public sealed class SetPaymentCurrenciesCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<SetPaymentCurrenciesCommand, PaymentCurrencyConceptDto>
{
    public async Task<Result<PaymentCurrencyConceptDto>> Handle(SetPaymentCurrenciesCommand request, CancellationToken cancellationToken)
    {
        var country = request.Country.Trim().ToUpperInvariant();
        var concept = request.ConceptCode.Trim().ToUpperInvariant();

        var concepts = await PaymentCurrencyConcepts.AllAsync(dbContext, cancellationToken);
        if (!concepts.TryGetValue(concept, out var name))
            return Result<PaymentCurrencyConceptDto>.Failure(DomainErrors.ChargeConcept.NotFound(concept));

        var wanted = request.Currencies.Select(c => c.Trim().ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToHashSet();
        var rules = await dbContext.PaymentCurrencyRules
            .Where(r => r.Country == country && r.ConceptCode == concept)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var rule in rules)
        {
            var enabled = wanted.Contains(rule.Currency);
            if (rule.IsEnabled == enabled)
                continue;

            var previous = PaymentCurrencySnapshot.From(rule);
            rule.IsEnabled = enabled;
            MaintainerChangeLogger.Log(
                dbContext, currentUserService, MaintainerNames.PaymentCurrency, rule.Id, MaintainerActions.Updated,
                previous, PaymentCurrencySnapshot.From(rule), now);
        }

        foreach (var currency in wanted.Where(c => rules.All(r => r.Currency != c)).Order(StringComparer.Ordinal))
        {
            var rule = new PaymentCurrencyRule { Country = country, ConceptCode = concept, Currency = currency, IsEnabled = true };
            dbContext.PaymentCurrencyRules.Add(rule);
            rules.Add(rule);
            MaintainerChangeLogger.Log(
                dbContext, currentUserService, MaintainerNames.PaymentCurrency, rule.Id, MaintainerActions.Created,
                null, PaymentCurrencySnapshot.From(rule), now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<PaymentCurrencyConceptDto>.Success(
            PaymentCurrencyConcepts.ToDto(country, concept, name, rules.OrderBy(r => r.Currency).ToList()));
    }
}

public sealed class ResetPaymentCurrenciesCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<ResetPaymentCurrenciesCommand>
{
    public async Task<Result> Handle(ResetPaymentCurrenciesCommand request, CancellationToken cancellationToken)
    {
        var country = request.Country.Trim().ToUpperInvariant();
        var concept = request.ConceptCode.Trim().ToUpperInvariant();
        var rules = await dbContext.PaymentCurrencyRules
            .Where(r => r.Country == country && r.ConceptCode == concept)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var rule in rules)
        {
            MaintainerChangeLogger.Log(
                dbContext, currentUserService, MaintainerNames.PaymentCurrency, rule.Id, MaintainerActions.Deactivated,
                PaymentCurrencySnapshot.From(rule), null, now);
            dbContext.PaymentCurrencyRules.Remove(rule);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class GetPaymentCurrencyHistoryQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetPaymentCurrencyHistoryQuery, IReadOnlyList<PaymentMaintainerChangeDto<PaymentCurrencySnapshot>>>
{
    public async Task<Result<IReadOnlyList<PaymentMaintainerChangeDto<PaymentCurrencySnapshot>>>> Handle(
        GetPaymentCurrencyHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var country = request.Country.Trim().ToUpperInvariant();
        var concept = request.ConceptCode.Trim().ToUpperInvariant();

        // Incluye las filas quitadas (borrado lógico) para reconstruir lo vigente en cualquier fecha.
        var ids = await dbContext.PaymentCurrencyRules.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.Country == country && r.ConceptCode == concept)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var changes = await dbContext.MaintainerChangeLogs.AsNoTracking()
            .Where(c => c.Maintainer == MaintainerNames.PaymentCurrency && ids.Contains(c.EntityId))
            .OrderByDescending(c => c.ChangedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PaymentMaintainerChangeDto<PaymentCurrencySnapshot>> items = changes
            .Select(c => new PaymentMaintainerChangeDto<PaymentCurrencySnapshot>(
                c.Id, c.EntityId, c.Action, c.ChangedAt, c.ChangedBy, c.ChangedByUserId,
                MaintainerChangeLogger.Read<PaymentCurrencySnapshot>(c.PreviousValue),
                MaintainerChangeLogger.Read<PaymentCurrencySnapshot>(c.NewValue)))
            .ToList();

        return Result<IReadOnlyList<PaymentMaintainerChangeDto<PaymentCurrencySnapshot>>>.Success(items);
    }
}

/// <summary>
/// Monedas en que se puede pagar un concepto (para el cliente): el mantenedor con las reglas de M5-08
/// para la moneda del cargo indicada.
/// </summary>
public sealed record GetEffectivePaymentCurrenciesQuery(string Country, string ConceptCode, string ChargeCurrency)
    : IQuery<EffectivePaymentCurrenciesDto>;

public sealed record EffectivePaymentCurrenciesDto(
    string Country,
    string ConceptCode,
    string ChargeCurrency,
    IReadOnlyList<string> AllowedCurrencies,
    string? DefaultCurrency);

public sealed class GetEffectivePaymentCurrenciesQueryValidator : AbstractValidator<GetEffectivePaymentCurrenciesQuery>
{
    public GetEffectivePaymentCurrenciesQueryValidator()
    {
        RuleFor(x => x.Country)
            .Must(c => CountryCodes.ValidCountries.Contains(c?.Trim().ToUpperInvariant()))
            .WithMessage("Country must be CL or BO.");
        RuleFor(x => x.ConceptCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ChargeCurrency).NotEmpty().Matches("^[A-Za-z]{3}$");
    }
}

public sealed class GetEffectivePaymentCurrenciesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetEffectivePaymentCurrenciesQuery, EffectivePaymentCurrenciesDto>
{
    public async Task<Result<EffectivePaymentCurrenciesDto>> Handle(GetEffectivePaymentCurrenciesQuery request, CancellationToken cancellationToken)
    {
        var country = request.Country.Trim().ToUpperInvariant();
        var concept = request.ConceptCode.Trim().ToUpperInvariant();
        var currency = request.ChargeCurrency.Trim().ToUpperInvariant();

        var allowed = await PaymentCurrencies.AllowedAsync(dbContext, country, concept, currency, cancellationToken);
        return Result<EffectivePaymentCurrenciesDto>.Success(new EffectivePaymentCurrenciesDto(
            country, concept, currency, allowed, PaymentCurrencyPolicy.Default(country, currency, allowed)));
    }
}
