using HapagPortal.Domain.Constants;

namespace HapagPortal.Domain.Payments;

/// <summary>
/// Monedas en que se puede pagar un cargo (M5-04, M5-08):
/// <list type="number">
/// <item>Las habilitadas en el mantenedor para el concepto y el país; sin configuración, la moneda del
/// cargo y la moneda local del país.</item>
/// <item>Reglas de Finanzas para la operación de Chile: cualquier recargo puede pagarse en CLP; los cargos
/// en EUR se pagan en CLP; los cargos en BOB no se pagan en BOB.</item>
/// </list>
/// El cargo se asigna por defecto al carro de su moneda si está habilitada; si no, al de la moneda local.
/// </summary>
public static class PaymentCurrencyPolicy
{
    private const string Clp = "CLP";
    private const string Eur = "EUR";
    private const string Bob = "BOB";

    /// <param name="configured">Monedas habilitadas en el mantenedor; nulo si el par no está configurado.</param>
    public static IReadOnlyList<string> Allowed(string country, string chargeCurrency, IReadOnlyCollection<string>? configured)
    {
        var charge = chargeCurrency.Trim().ToUpperInvariant();
        var local = CountryCodes.GetCurrency(country);

        var allowed = configured is null
            ? new List<string> { charge, local }
            : configured.Select(c => c.Trim().ToUpperInvariant()).ToList();

        if (country == CountryCodes.Chile)
        {
            if (!allowed.Contains(Clp))
                allowed.Add(Clp);

            if (charge == Eur)
                allowed.RemoveAll(c => c != Clp);

            if (charge == Bob)
                allowed.RemoveAll(c => c == Bob);
        }

        return allowed
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c == charge ? 0 : c == local ? 1 : 2)
            .ThenBy(c => c, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Moneda de pago por defecto: la del cargo si está habilitada; si no, la local; si no, la primera.</summary>
    public static string? Default(string country, string chargeCurrency, IReadOnlyList<string> allowed)
    {
        var charge = chargeCurrency.Trim().ToUpperInvariant();
        var local = CountryCodes.GetCurrency(country);

        if (allowed.Contains(charge))
            return charge;

        return allowed.Contains(local) ? local : allowed.FirstOrDefault();
    }
}
