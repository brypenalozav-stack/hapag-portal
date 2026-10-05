namespace HapagPortal.Infrastructure.Integrations;

/// <summary>
/// Nombres de los sistemas externos. Cada uno es la sección <c>Integrations:&lt;Sistema&gt;</c> de la
/// configuración y, en los proveedores de pago, la clave del <c>IPaymentProvider</c> registrado.
/// </summary>
public static class IntegrationSystems
{
    public const string Nexus = "Nexus";
    public const string Fis = "Fis";
    public const string Khipu = "Khipu";
    public const string BancoChile = "BancoChile";
    public const string Santander = "Santander";
    public const string Bci = "Bci";
    public const string DbNet = "DbNet";
    public const string Signature = "Signature";
    public const string Storage = "Storage";
    public const string Tracking = "Tracking";

    public static readonly IReadOnlyList<string> PaymentProviders = [Khipu, BancoChile, Santander, Bci];

    public static readonly IReadOnlyList<string> All =
        [Nexus, Fis, Khipu, BancoChile, Santander, Bci, DbNet, Signature, Storage, Tracking];

    /// <summary>
    /// Sistemas con cliente Real (Fase 6c). El resto (Santander, Bci, Signature, Storage) solo tiene
    /// adaptador Dummy y <c>Mode=Real</c> detiene el arranque.
    /// </summary>
    public static readonly IReadOnlyList<string> WithRealAdapter = [Nexus, Fis, Khipu, BancoChile, DbNet, Tracking];
}

/// <summary>Valores admitidos en <c>Integrations:&lt;Sistema&gt;:Mode</c>.</summary>
public static class IntegrationModes
{
    public const string Dummy = "Dummy";
    public const string Real = "Real";
}
