namespace HapagPortal.Domain.Constants;

/// <summary>
/// Estado de una solicitud de servicio on demand (M2-03, M2-04). Flujo:
/// Draft → Submitted → (PendingApproval → Approved | Rejected) → PendingPayment → Paid → InProgress →
/// Completed; Cancelled desde cualquier estado previo al pago. Sin cobro (exento o sin tarifa) se pasa
/// directamente a InProgress o Completed.
/// </summary>
public static class ServiceRequestStatus
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string PendingApproval = "PendingApproval";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string PendingPayment = "PendingPayment";
    public const string Paid = "Paid";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All =
        [Draft, Submitted, PendingApproval, Approved, Rejected, PendingPayment, Paid, InProgress, Completed, Cancelled];

    /// <summary>Estados finales: la solicitud ya no cambia.</summary>
    public static readonly string[] Terminal = [Rejected, Completed, Cancelled];

    /// <summary>Estados que el cliente todavía puede anular (antes del pago).</summary>
    public static readonly string[] Cancellable = [Draft, Submitted, PendingApproval, Approved, PendingPayment];

    /// <summary>Estados que liberan el servicio para una nueva solicitud del mismo BL.</summary>
    public static readonly string[] Released = [Rejected, Cancelled];
}

/// <summary>Equipo interno que revisa (aprobación) o presta (después del pago) el servicio.</summary>
public static class ServiceTeams
{
    public const string None = "None";

    /// <summary>Equipo de equipos/depósitos (Drop Off, M3-09).</summary>
    public const string Ed = "ED";
    public const string CustomerService = "CustomerService";

    public static readonly string[] All = [None, Ed, CustomerService];
    public static readonly string[] Internal = [Ed, CustomerService];
}

/// <summary>Cómo se determina el cobro del servicio.</summary>
public static class ServicePricingModes
{
    /// <summary>Sin cobro.</summary>
    public const string None = "None";

    /// <summary>Tarifa vigente del mantenedor (M8-01), con tramos cuando corresponde: genera un cargo.</summary>
    public const string Tariff = "Tariff";

    /// <summary>
    /// Cargos registrados en el sistema de origen para el concepto (como Gate Out en M3-01), con las reglas
    /// de exención de Nexus (M4-01, M4-02): no se genera un cargo nuevo, se vinculan los existentes.
    /// </summary>
    public const string SourceCharge = "SourceCharge";

    public static readonly string[] All = [None, Tariff, SourceCharge];
}

/// <summary>Cantidad cobrada: una vez por solicitud o por cada contenedor seleccionado.</summary>
public static class ServiceQuantityModes
{
    public const string PerRequest = "PerRequest";
    public const string PerContainer = "PerContainer";

    public static readonly string[] All = [PerRequest, PerContainer];
}

/// <summary>Hito desde el que se mide el tiempo transcurrido para las tarifas por tramos (NF-22).</summary>
public static class ServiceMilestones
{
    public const string None = "None";
    public const string VesselDeparture = "VesselDeparture";
    public const string VesselArrival = "VesselArrival";

    /// <summary>
    /// Plazo aduanero del BL (<c>DeadlineInstance</c> de la regla <c>DeadlineRuleCode</c>); si el BL no tiene
    /// esa instancia, el zarpe (ETD) más <c>MilestoneOffsetHours</c>.
    /// </summary>
    public const string CustomsDeadline = "CustomsDeadline";

    public static readonly string[] All = [None, VesselDeparture, VesselArrival, CustomsDeadline];
}

/// <summary>Uso del hito: sin distinción, dentro y fuera de plazo con tarifas distintas, o solo fuera de plazo.</summary>
public static class ServiceTimingRules
{
    public const string None = "None";
    public const string InTimeAndLate = "InTimeAndLate";
    public const string LateOnly = "LateOnly";

    public static readonly string[] All = [None, InTimeAndLate, LateOnly];
}

/// <summary>Resultado de comparar la solicitud con el hito.</summary>
public static class ServiceTimings
{
    public const string NotApplicable = "NotApplicable";
    public const string InTime = "InTime";
    public const string Late = "Late";
}

/// <summary>Ventana del embarque en que el servicio se puede solicitar (M2-04: exportación tras el zarpe).</summary>
public static class ServiceAvailabilityWindows
{
    public const string Always = "Always";
    public const string BeforeDeparture = "BeforeDeparture";
    public const string AfterDeparture = "AfterDeparture";
    public const string AfterArrival = "AfterArrival";

    public static readonly string[] All = [Always, BeforeDeparture, AfterDeparture, AfterArrival];
}

/// <summary>Referencia con que el cliente identifica el embarque del servicio.</summary>
public static class ServiceReferenceTypes
{
    public const string Bl = "BL";
    public const string Booking = "Booking";

    public static readonly string[] All = [Bl, Booking];
}

/// <summary>Operación del embarque (como en <c>GET /shipments?operation=</c>).</summary>
public static class ServiceOperations
{
    public const string Import = "IMPORT";
    public const string Export = "EXPORT";

    public static readonly string[] All = [Import, Export];

    public static string Of(string shipmentType) =>
        string.Equals(shipmentType, "Export", StringComparison.OrdinalIgnoreCase) ? Export : Import;
}

/// <summary>Tipos de campo del formulario de un servicio.</summary>
public static class ServiceInputFieldTypes
{
    public const string Text = "text";
    public const string TextArea = "textarea";
    public const string Date = "date";
    public const string Number = "number";
    public const string Select = "select";
    public const string File = "file";

    /// <summary>Selección múltiple de contenedores del BL.</summary>
    public const string Containers = "containers";

    public static readonly string[] All = [Text, TextArea, Date, Number, Select, File, Containers];
}

/// <summary>Motivo por el que un servicio no se puede solicitar sobre un BL.</summary>
public static class ServiceUnavailableReasons
{
    public const string BlStatus = "BL_STATUS";
    public const string NotDeparted = "NOT_DEPARTED";
    public const string AlreadyDeparted = "ALREADY_DEPARTED";
    public const string NotArrived = "NOT_ARRIVED";
    public const string NoContainers = "NO_CONTAINERS";
    public const string NotOverdue = "NOT_OVERDUE";
    public const string MilestoneUnavailable = "MILESTONE_UNAVAILABLE";
    public const string AlreadyRequested = "ALREADY_REQUESTED";
    public const string NoPermission = "NO_PERMISSION";
    public const string NoSourceCharge = "NO_SOURCE_CHARGE";
    public const string TariffNotInForce = "TARIFF_NOT_IN_FORCE";
}

/// <summary>Quién ejecutó un evento de la solicitud.</summary>
public static class ServiceRequestActorKinds
{
    public const string Client = "Client";
    public const string Internal = "Internal";
    public const string System = "System";
}

/// <summary>Campo de adjunto reservado para el documento que entrega el equipo interno al completar.</summary>
public static class ServiceRequestAttachmentFields
{
    public const string Output = "_output";
}

/// <summary>Permisos internos de la bandeja de solicitudes (ED, Customer Service).</summary>
public static class ServiceRequestPermissions
{
    public const string Process = "service-requests.process";
}

/// <summary>Códigos de las definiciones de servicio sembradas (Fase 2, Ola G).</summary>
public static class ServiceDefinitionCodes
{
    public const string SealManagement = "SEAL_MANAGEMENT";
    public const string LateArrival = "LATE_ARRIVAL";
    public const string EarlyArrival = "EARLY_ARRIVAL";
    public const string DropOff = "DROP_OFF_SCL";
    public const string ContainerAdministrationXom = "XOM";
    public const string BlCorrection = "BL_CORRECTION";
    public const string BlHouseTransmission = "BL_HOUSE_TRANSMISSION";
    public const string MatrixLate = "MATRIX_LATE";
    public const string GateInReturn = "GATE_IN_RETURN";
    public const string Opening = "OPENING";
    public const string Valuation = "VALUATION";

    /// <summary>
    /// Refacturación IAO con pérdida de IVA (M3-11, Ola H). Se solicita desde una factura (<c>/reinvoicing</c>),
    /// no desde el listado de servicios del BL.
    /// </summary>
    public const string IaoReinvoicing = "IAO_REINVOICING";

    /// <summary>
    /// Certificado de flete, importación de Bolivia (M6-02, BO-IMP-08, Ola J). Se solicita desde los documentos del
    /// embarque (<c>/documents/{bl}/freight-certificate</c>); primera entrega sin pago ni carro.
    /// </summary>
    public const string FreightCertificate = "FREIGHT_CERTIFICATE";

    /// <summary>
    /// Carta de liberación y desconsolidado, importación de Bolivia (M6-08, BO-IMP-11, Ola J). Se solicita desde los
    /// documentos del embarque (<c>/documents/{bl}/release-letter</c>) y la aprueba Customer Service en la bandeja.
    /// </summary>
    public const string ReleaseLetter = "RELEASE_LETTER";

    /// <summary>Definiciones que se solicitan por un flujo propio y no por <c>/service-requests</c>.</summary>
    public static readonly string[] DedicatedFlow = [IaoReinvoicing, FreightCertificate, ReleaseLetter];
}

/// <summary>
/// Modo de cobro del certificado de flete (M6-02, <c>Documents:FreightCertificateMode</c>). La primera entrega de
/// Fase 2 es sin pago ni carro (<see cref="Free"/>); <see cref="Paid"/> queda reservado para cuando se defina y valide
/// el flujo de cobro en BOB (M5-01, M5-04) y no está disponible: el arranque falla si se configura.
/// </summary>
public static class FreightCertificateModes
{
    public const string Free = "Free";
    public const string Paid = "Paid";

    public static readonly string[] All = [Free, Paid];
}

/// <summary>Claves del formulario del certificado de flete (M6-02).</summary>
public static class FreightCertificateFields
{
    public const string ConsigneeName = "consigneeName";
    public const string ConsigneeTaxId = "consigneeTaxId";
    public const string Purpose = "purpose";
    public const string Recipient = "recipient";
    public const string Notes = "notes";
}

/// <summary>Finalidad declarada del certificado de flete.</summary>
public static class FreightCertificatePurposes
{
    public const string Customs = "CUSTOMS";
    public const string Insurance = "INSURANCE";
    public const string Bank = "BANK";
    public const string Other = "OTHER";

    public static readonly string[] All = [Customs, Insurance, Bank, Other];
}

/// <summary>
/// Tipo de sociedad del consignatario en la carta de liberación y desconsolidado (M6-08): define los datos exigidos.
/// Empresa: razón social, NIT y representante legal con su documento; persona natural: nombre y documento de
/// identidad (CI). Definición provisoria hasta su validación con el área legal.
/// </summary>
public static class LegalEntityTypes
{
    public const string Company = "COMPANY";
    public const string NaturalPerson = "NATURAL_PERSON";

    public static readonly string[] All = [Company, NaturalPerson];
}

/// <summary>Claves del formulario de la carta de liberación y desconsolidado (M6-08).</summary>
public static class ReleaseLetterFields
{
    public const string Containers = "containers";
    public const string LegalEntityType = "legalEntityType";
    public const string ConsigneeName = "consigneeName";
    public const string ConsigneeTaxId = "consigneeTaxId";
    public const string ConsigneeAddress = "consigneeAddress";
    public const string LegalRepresentativeName = "legalRepresentativeName";
    public const string LegalRepresentativeId = "legalRepresentativeId";
    public const string CarrierName = "carrierName";
    public const string CarrierTaxId = "carrierTaxId";
    public const string DriverName = "driverName";
    public const string DriverId = "driverId";
    public const string TruckPlate = "truckPlate";
    public const string Observations = "observations";
}
