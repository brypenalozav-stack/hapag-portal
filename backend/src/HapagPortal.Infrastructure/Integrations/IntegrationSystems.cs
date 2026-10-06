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
    public const string Tatc = "Tatc";

    /// <summary>Registro de contactos y listas de distribución de reportes (P0060, M1-06, Ola I).</summary>
    public const string Contacts = "Contacts";

    public static readonly IReadOnlyList<string> PaymentProviders = [Khipu, BancoChile, Santander, Bci];

    public static readonly IReadOnlyList<string> All =
        [Nexus, Fis, Khipu, BancoChile, Santander, Bci, DbNet, Signature, Storage, Tracking, Tatc, Contacts];

    /// <summary>
    /// Sistemas con cliente Real (Fase 6c; TATC en la Ola F; contactos en la Ola I). El resto (Santander, Bci,
    /// Signature, Storage) solo tiene adaptador Dummy y <c>Mode=Real</c> detiene el arranque.
    /// </summary>
    public static readonly IReadOnlyList<string> WithRealAdapter = [Nexus, Fis, Khipu, BancoChile, DbNet, Tracking, Tatc, Contacts];
}

/// <summary>
/// Valores admitidos en <c>Integrations:&lt;Sistema&gt;:Mode</c>. <see cref="Local"/> solo aplica a Storage:
/// archivos en disco (<c>Integrations:Storage:LocalPath</c>) para que los documentos persistan.
/// </summary>
public static class IntegrationModes
{
    public const string Dummy = "Dummy";
    public const string Real = "Real";
    public const string Local = "Local";
}
