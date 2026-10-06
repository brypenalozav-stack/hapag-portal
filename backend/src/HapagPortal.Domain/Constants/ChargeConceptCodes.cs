namespace HapagPortal.Domain.Constants;

/// <summary>
/// Códigos estables del catálogo de conceptos de cobro (Fase 1, Ola C). Los códigos de exención y de
/// tarifa coinciden con CT-NEXUS (<c>GATE_IN</c>, <c>EDS</c>, <c>GATE_OUT</c>, <c>WAREHOUSE_CHANGE</c>).
/// Reemplaza a la antigua clase <c>ChargeTypes</c>, que no tenía consumidores. El catálogo administrable
/// se siembra en <see cref="Entities.ChargeConcept"/>.
/// </summary>
public static class ChargeConceptCodes
{
    // Servicios de Fase 1 (capítulo 3: CL-IMP-02/03/04, CL-EXP-02, BO-IMP-03/05/06/16, BO-EXP-03)
    public const string GateIn = "GATE_IN";
    public const string Eds = "EDS";
    public const string GateOut = "GATE_OUT";
    public const string Ipo = "IPO";
    public const string Mhd = "MHD";
    public const string Demurrage = "DEMURRAGE";
    public const string WarehouseChange = "WAREHOUSE_CHANGE";
    public const string AdvanceDemurrageBo = "ADVANCE_DEMURRAGE_BO";

    // Servicio con tarifa por tramos de tiempo desde el plazo (M3-08, M8-01): ejemplo de tramos horarios.
    public const string LateArrival = "LATE_ARRIVAL";

    // Servicio pagado que emite el certificado de transbordo al confirmarse el pago (M6-01, Ola E).
    public const string TransshipmentCertificate = "TRANSSHIPMENT_CERT";

    // Recargos de origen ya presentes en los datos (semilla e importación).
    public const string Thc = "THC";
    public const string ThcReefer = "THC_RF";
    public const string BlFee = "BL_FEE";
    public const string Isps = "ISPS";
    public const string TransitFee = "TRANSIT_FEE";

    /// <summary>Conceptos cuya exención administra Nexus (M4-01, M3-01; GATE_OUT previsto en CT-NEXUS).</summary>
    public static readonly string[] NexusExemptible = [GateIn, Eds, GateOut];
}

/// <summary>
/// Pestaña en la que se presenta el concepto: recargos locales, sobreestadía (demurrage, MHD y demoras
/// anticipadas, M3-02) o servicio solicitado desde el portal (cambio de almacén, Late Arrival).
/// </summary>
public static class ChargeCategories
{
    public const string LocalCharge = "LocalCharge";
    public const string Demurrage = "Demurrage";
    public const string Service = "Service";

    public static readonly string[] All = [LocalCharge, Demurrage, Service];
}

/// <summary>
/// Origen del valor aplicado: el mantenedor del portal (M8-01) o Nexus (<c>ITariffProvider</c>,
/// <c>IExemptionReader</c>, <c>ICreditConditionReader</c>).
/// </summary>
public static class RuleSources
{
    public const string Portal = "PORTAL";
    public const string Nexus = "NEXUS";
}

/// <summary>Estado de un recargo local. <see cref="Exempt"/> lo deja Ola C al aplicar una exención (M4-02).</summary>
public static class ChargeStatus
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Exempt = "Exempt";
}
