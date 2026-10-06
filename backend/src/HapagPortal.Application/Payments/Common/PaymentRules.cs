namespace HapagPortal.Application.Payments.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Payments;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Ventanas de bloqueo de pagos (M8-07) evaluadas en cada solicitud, en el huso del país (NF-22): no
/// hace falta activarlas ni desactivarlas a mano. Bloquean iniciar y completar pagos; las consultas y la
/// confirmación de pagos ya cobrados por la plataforma siguen operando.
/// </summary>
public static class PaymentBlocks
{
    public static async Task<PaymentBlockWindow?> FindActiveAsync(
        IApplicationDbContext dbContext,
        string country,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        // Margen de un día: el filtro exacto se hace en la hora local del país.
        var from = BusinessCalendar.LocalDate(country, nowUtc).AddDays(-1);

        var windows = await dbContext.PaymentBlockWindows.AsNoTracking()
            .Where(w => w.IsActive && (w.Country == null || w.Country == country) && w.EndDate >= from)
            .ToListAsync(cancellationToken);

        return windows
            .Where(w => PaymentBlockSchedule.IsActive(w, country, nowUtc))
            .OrderByDescending(w => PaymentBlockSchedule.End(w))
            .FirstOrDefault();
    }

    /// <summary>Falla con el mensaje configurado para el cliente si hay una ventana activa.</summary>
    public static async Task<Result> EnsureOpenAsync(
        IApplicationDbContext dbContext,
        string country,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var window = await FindActiveAsync(dbContext, country, nowUtc, cancellationToken);
        return window is null
            ? Result.Success()
            : Result.Failure(DomainErrors.PaymentFlow.Blocked(window.ClientMessage));
    }

    public static async Task<PaymentBlockStatusDto> StatusAsync(
        IApplicationDbContext dbContext,
        string country,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var window = await FindActiveAsync(dbContext, country, nowUtc, cancellationToken);
        return new PaymentBlockStatusDto(
            country,
            BusinessCalendar.TimeZoneId(country),
            window is not null,
            window?.Id,
            window?.ClientMessage,
            window?.EndDate,
            window?.EndTime,
            nowUtc);
    }
}

/// <summary>Monedas de pago por concepto y país según el mantenedor (M5-04) y las reglas de M5-08.</summary>
public static class PaymentCurrencies
{
    public static async Task<IReadOnlyList<string>> AllowedAsync(
        IApplicationDbContext dbContext,
        string country,
        string conceptCode,
        string chargeCurrency,
        CancellationToken cancellationToken)
    {
        var rules = await dbContext.PaymentCurrencyRules.AsNoTracking()
            .Where(r => r.Country == country && r.ConceptCode == conceptCode)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<string>? configured = rules.Count == 0
            ? null
            : rules.Where(r => r.IsEnabled).Select(r => r.Currency).ToList();

        return PaymentCurrencyPolicy.Allowed(country, chargeCurrency, configured);
    }
}

/// <summary>Medios de pago habilitados por país y moneda (M5-03).</summary>
public static class PaymentMethodCatalog
{
    public static IReadOnlyList<string> Currencies(PaymentMethodConfig method) =>
        ParseCurrencies(method.Currencies);

    public static IReadOnlyList<string> ParseCurrencies(string? csv) =>
        (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(c => c.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

    public static string FormatCurrencies(IEnumerable<string> currencies) =>
        string.Join(',', currencies.Select(c => c.Trim().ToUpperInvariant()).Where(c => c.Length > 0).Distinct(StringComparer.Ordinal));

    public static bool Accepts(PaymentMethodConfig method, string currency) =>
        method.IsEnabled && Currencies(method).Contains(currency, StringComparer.OrdinalIgnoreCase);

    public static async Task<IReadOnlyList<PaymentMethodConfig>> AvailableAsync(
        IApplicationDbContext dbContext,
        string country,
        string? currency,
        CancellationToken cancellationToken)
    {
        var methods = await dbContext.PaymentMethodConfigs.AsNoTracking()
            .Where(m => m.Country == country && m.IsEnabled)
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.Code)
            .ToListAsync(cancellationToken);

        return currency is null ? methods : methods.Where(m => Accepts(m, currency)).ToList();
    }

    public static PaymentMethodDto ToDto(PaymentMethodConfig m) => new(
        m.Id, m.Code, m.Name, m.Description, m.Country, m.Kind, m.ProviderKey, Currencies(m), m.IsEnabled,
        m.DisplayOrder, m.CreatedAt, m.ModifiedAt);
}
