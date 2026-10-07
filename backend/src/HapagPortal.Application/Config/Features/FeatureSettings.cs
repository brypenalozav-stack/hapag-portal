namespace HapagPortal.Application.Config.Features;

/// <summary>
/// Nombres de los flags de funcionalidades (sección <c>Features</c>). Cada uno agrupa las fichas de Fase 2 que se
/// apagan juntas; el frontend usa los mismos nombres (<c>GET /api/v1/config/features</c>).
/// </summary>
public static class FeatureNames
{
    /// <summary>M1-06 Listas de distribución.</summary>
    public const string ContactLists = "ContactLists";

    /// <summary>M1-09 Transportistas pre-creados (la carta de liberación los lee directo de la base).</summary>
    public const string Carriers = "Carriers";

    /// <summary>M1-21 Empresa matriz y vinculaciones con filiales.</summary>
    public const string ParentCompany = "ParentCompany";

    /// <summary>M1-25 Bandeja de notificaciones (solo la interfaz: el publicador y los correos siguen activos).</summary>
    public const string NotificationsInbox = "NotificationsInbox";

    /// <summary>M1-26 Comunicados.</summary>
    public const string Announcements = "Announcements";

    /// <summary>M1-27 Modo guía.</summary>
    public const string GuideMode = "GuideMode";

    /// <summary>M2-03, M2-04 y M3-07 a M3-15: servicios on demand (la carta de liberación usa su bandeja).</summary>
    public const string OnDemandServices = "OnDemandServices";

    /// <summary>Página de listado <c>/service-orders</c>; las ODS del detalle del embarque son Fase 1.</summary>
    public const string ServiceOrdersPage = "ServiceOrdersPage";

    /// <summary>M3-06 Historial del cambio de almacén.</summary>
    public const string WarehouseHistory = "WarehouseHistory";

    /// <summary>M3-11 Refacturación IAO.</summary>
    public const string Reinvoicing = "Reinvoicing";

    /// <summary>M3-17 Canal Web Service (<c>/api/ws/v1</c>) y sus clientes.</summary>
    public const string ApiClients = "ApiClients";

    /// <summary>M3-19 Pago anticipado de Gate Out y cruce de anticipos.</summary>
    public const string GateOutAdvance = "GateOutAdvance";

    /// <summary>M5-06 Boleta de depósito adjunta.</summary>
    public const string DepositProofs = "DepositProofs";

    /// <summary>M5-10 Forma de pago por ítem con crédito.</summary>
    public const string CreditImputation = "CreditImputation";

    /// <summary>M6-02 Certificado de flete.</summary>
    public const string FreightCertificate = "FreightCertificate";

    /// <summary>M6-08 Carta de liberación y desconsolidado (activa por decisión del usuario).</summary>
    public const string ReleaseLetter = "ReleaseLetter";

    /// <summary>M7-03 Estado de cuenta en línea.</summary>
    public const string AccountStatement = "AccountStatement";

    /// <summary>M8-05 Inicio del área de administración.</summary>
    public const string AdminHome = "AdminHome";

    /// <summary>M8-08 Vista como cliente.</summary>
    public const string Impersonation = "Impersonation";

    /// <summary>M8-09 Counter (fuente de la carta de liberación; activo).</summary>
    public const string Counter = "Counter";

    /// <summary>M9-01 Reportería de transacciones y excepciones.</summary>
    public const string TransactionReports = "TransactionReports";

    /// <summary>M10-04 Documentos entregados por el asistente.</summary>
    public const string AssistantDelivery = "AssistantDelivery";

    /// <summary>M2-10 Plazos documentales (<c>/admin/deadlines</c>), fuera del documento base.</summary>
    public const string DocumentaryDeadlines = "DocumentaryDeadlines";

    /// <summary>Valor por defecto de cada flag: Fase 2 apagada, salvo la carta de liberación y Counter.</summary>
    public static readonly IReadOnlyDictionary<string, bool> Defaults = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
    {
        [ContactLists] = false,
        [Carriers] = false,
        [ParentCompany] = false,
        [NotificationsInbox] = false,
        [Announcements] = false,
        [GuideMode] = false,
        [OnDemandServices] = false,
        [ServiceOrdersPage] = false,
        [WarehouseHistory] = false,
        [Reinvoicing] = false,
        [ApiClients] = false,
        [GateOutAdvance] = false,
        [DepositProofs] = false,
        [CreditImputation] = false,
        [FreightCertificate] = false,
        [ReleaseLetter] = true,
        [AccountStatement] = false,
        [AdminHome] = false,
        [Impersonation] = false,
        [Counter] = true,
        [TransactionReports] = false,
        [AssistantDelivery] = false,
        [DocumentaryDeadlines] = false,
    };

    public static IReadOnlyList<string> All { get; } = Defaults.Keys.ToList();
}

/// <summary>
/// Flags de funcionalidades (sección <c>Features</c> de la configuración). Las funciones de Fase 2 quedan apagadas por
/// defecto y se reactivan por configuración, sin desplegar código: <c>Features__OnDemandServices=true</c>. Un flag
/// desconocido se considera apagado.
/// </summary>
public sealed class FeatureSettings
{
    public const string SectionName = "Features";

    private readonly Dictionary<string, bool> _flags;

    public FeatureSettings()
        : this(null)
    {
    }

    /// <param name="overrides">Valores configurados; los flags no indicados toman su valor por defecto.</param>
    public FeatureSettings(IEnumerable<KeyValuePair<string, bool>>? overrides)
    {
        _flags = new Dictionary<string, bool>(FeatureNames.Defaults, StringComparer.OrdinalIgnoreCase);
        if (overrides is null)
            return;
        foreach (var (name, enabled) in overrides)
        {
            if (FeatureNames.Defaults.ContainsKey(name))
                _flags[name] = enabled;
        }
    }

    /// <summary>Todos los flags encendidos (pruebas y entornos de demostración).</summary>
    public static FeatureSettings AllEnabled() => new(FeatureNames.All.Select(n => new KeyValuePair<string, bool>(n, true)));

    public bool IsEnabled(string name) => _flags.TryGetValue(name, out var enabled) && enabled;

    /// <summary>Estado de cada flag, con el nombre canónico.</summary>
    public IReadOnlyDictionary<string, bool> Snapshot() =>
        FeatureNames.All.ToDictionary(n => n, n => _flags[n], StringComparer.Ordinal);
}
