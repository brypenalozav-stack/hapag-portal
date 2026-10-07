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

    /// <summary>Getnet Chile Web Checkout: pasarela del botón Santander (clave de pago <c>Santander</c>).</summary>
    public const string Getnet = "Getnet";

    /// <summary>Bci Pagos (ex Pago Fácil): pasarela del botón BCI (clave de pago <c>Bci</c>).</summary>
    public const string BciPagos = "BciPagos";

    public const string DbNet = "DbNet";
    public const string Signature = "Signature";
    public const string Storage = "Storage";
    public const string Tracking = "Tracking";
    public const string Tatc = "Tatc";

    /// <summary>Registro de contactos y listas de distribución de reportes (P0060, M1-06, Ola I).</summary>
    public const string Contacts = "Contacts";

    /// <summary>
    /// Pasarelas de pago: clave del <c>IPaymentProvider</c> (la del medio de pago, <c>PaymentProviderKeys</c>) y sección
    /// de configuración <c>Integrations:&lt;Sistema&gt;</c> con su <c>Mode</c>.
    /// </summary>
    public static readonly IReadOnlyList<(string ProviderKey, string System)> PaymentGateways =
    [
        ("Khipu", Khipu),
        ("BancoChile", BancoChile),
        ("Santander", Getnet),
        ("Bci", BciPagos),
    ];

    public static readonly IReadOnlyList<string> All =
        [Nexus, Fis, Khipu, BancoChile, Getnet, BciPagos, DbNet, Signature, Storage, Tracking, Tatc, Contacts];

    /// <summary>Sistemas con cliente Real. Signature y Storage solo tienen adaptador Dummy: <c>Mode=Real</c> detiene el arranque.</summary>
    public static readonly IReadOnlyList<string> WithRealAdapter =
        [Nexus, Fis, Khipu, BancoChile, Getnet, BciPagos, DbNet, Tracking, Tatc, Contacts];

    /// <summary>Sistemas Real que no llaman a una API (formulario firmado): no exigen <c>BaseUrl</c>.</summary>
    public static readonly IReadOnlyList<string> WithoutBaseUrl = [BancoChile];
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
