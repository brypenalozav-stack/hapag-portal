namespace HapagPortal.Domain.Constants;

public static class NotificationTypes
{
    public const string DeadlineAtRisk = "DeadlineAtRisk";
    public const string DeadlineOverdue = "DeadlineOverdue";
    public const string TransmissionRejected = "TransmissionRejected";
    public const string TransmissionError = "TransmissionError";
    public const string JoinRequestReceived = "JoinRequestReceived";
    public const string JoinRequestApproved = "JoinRequestApproved";
    public const string JoinRequestRejected = "JoinRequestRejected";
    public const string OrganizationApproved = "OrganizationApproved";
    public const string OrganizationRejected = "OrganizationRejected";

    // Accesos a terceros (M1-12, M1-14, M1-22)
    public const string AccessGranted = "AccessGranted";
    public const string AccessUpdated = "AccessUpdated";
    public const string AccessRevoked = "AccessRevoked";
    public const string AccessExpired = "AccessExpired";
    public const string AccessRevokedByCascade = "AccessRevokedByCascade";

    // Pagos (M5-01, NF-03)
    public const string PaymentConfirmed = "PaymentConfirmed";

    // Documentos del embarque (M6-01, M6-03, M6-04)
    public const string DocumentIssued = "DocumentIssued";

    // Finanzas (Fase 2 Ola H): comprobante de depósito (M5-06), imputación a crédito (M5-10) y refacturación IAO (M3-11).
    public const string DepositProofSubmitted = "DepositProofSubmitted";
    public const string DepositProofRejected = "DepositProofRejected";
    public const string CreditImputationRegistered = "CreditImputationRegistered";
    public const string ReinvoicingAccepted = "ReinvoicingAccepted";
    public const string ReinvoicingDeclined = "ReinvoicingDeclined";
    public const string InvoiceReissued = "InvoiceReissued";

    // Solicitudes de servicios on demand (M2-03, M2-04, M3-07 a M3-15), a los administradores de la organización.
    public const string ServiceRequestPendingApproval = "ServiceRequestPendingApproval";
    public const string ServiceRequestApproved = "ServiceRequestApproved";
    public const string ServiceRequestRejected = "ServiceRequestRejected";
    public const string ServiceRequestPendingPayment = "ServiceRequestPendingPayment";
    public const string ServiceRequestPaid = "ServiceRequestPaid";
    public const string ServiceRequestInProgress = "ServiceRequestInProgress";
    public const string ServiceRequestCompleted = "ServiceRequestCompleted";
    public const string ServiceRequestCancelled = "ServiceRequestCancelled";

    // Fase 2 Ola I: comunicados (M1-26), empresa matriz (M1-21) y transportistas pre-creados (M1-09).
    public const string AnnouncementPublished = "AnnouncementPublished";
    public const string ParentLinkRequested = "ParentLinkRequested";
    public const string ParentLinkApproved = "ParentLinkApproved";
    public const string ParentLinkRejected = "ParentLinkRejected";
    public const string ParentVisibilityChanged = "ParentVisibilityChanged";
    public const string CarrierActivated = "CarrierActivated";

    /// <summary>
    /// Catálogo de la bandeja (M1-25): módulo de cada tipo y su política de correo. <c>EmailAvailable</c> = el tipo
    /// puede enviarse también por correo (los dirigidos a un rol interno son solo de la bandeja); <c>EmailDefault</c>
    /// = valor cuando el usuario no definió preferencia; <c>EmailMandatory</c> = el correo no puede desactivarse
    /// (avisos que el destinatario necesita aunque todavía no pueda ingresar al portal).
    /// </summary>
    public static readonly IReadOnlyList<NotificationTypeInfo> Catalog =
    [
        new(DeadlineAtRisk, NotificationModules.Deadlines, false, false, false),
        new(DeadlineOverdue, NotificationModules.Deadlines, false, false, false),
        new(TransmissionRejected, NotificationModules.Customs, false, false, false),
        new(TransmissionError, NotificationModules.Customs, false, false, false),
        new(JoinRequestReceived, NotificationModules.Organization, true, true, false),
        new(JoinRequestApproved, NotificationModules.Organization, true, true, true),
        new(JoinRequestRejected, NotificationModules.Organization, true, true, true),
        new(OrganizationApproved, NotificationModules.Organization, true, true, true),
        new(OrganizationRejected, NotificationModules.Organization, true, true, true),
        new(AccessGranted, NotificationModules.Access, true, true, false),
        new(AccessUpdated, NotificationModules.Access, true, true, false),
        new(AccessRevoked, NotificationModules.Access, true, true, false),
        new(AccessExpired, NotificationModules.Access, true, true, false),
        new(AccessRevokedByCascade, NotificationModules.Access, true, true, false),
        new(PaymentConfirmed, NotificationModules.Payments, true, true, false),
        new(DocumentIssued, NotificationModules.Documents, true, true, false),
        new(DepositProofSubmitted, NotificationModules.Finance, false, false, false),
        new(DepositProofRejected, NotificationModules.Finance, true, true, false),
        new(CreditImputationRegistered, NotificationModules.Finance, true, true, false),
        new(ReinvoicingAccepted, NotificationModules.Finance, true, true, false),
        new(ReinvoicingDeclined, NotificationModules.Finance, true, true, false),
        new(InvoiceReissued, NotificationModules.Finance, true, true, false),
        new(ServiceRequestPendingApproval, NotificationModules.Services, true, true, false),
        new(ServiceRequestApproved, NotificationModules.Services, true, true, false),
        new(ServiceRequestRejected, NotificationModules.Services, true, true, false),
        new(ServiceRequestPendingPayment, NotificationModules.Services, true, true, false),
        new(ServiceRequestPaid, NotificationModules.Services, true, true, false),
        new(ServiceRequestInProgress, NotificationModules.Services, true, true, false),
        new(ServiceRequestCompleted, NotificationModules.Services, true, true, false),
        new(ServiceRequestCancelled, NotificationModules.Services, true, true, false),
        new(AnnouncementPublished, NotificationModules.Announcements, true, false, false),
        new(ParentLinkRequested, NotificationModules.Administration, false, false, false),
        new(ParentLinkApproved, NotificationModules.Organization, true, true, false),
        new(ParentLinkRejected, NotificationModules.Organization, true, true, false),
        new(ParentVisibilityChanged, NotificationModules.Organization, true, true, false),
        new(CarrierActivated, NotificationModules.Access, true, true, false),
    ];

    /// <summary>Entrada del catálogo; un tipo desconocido se trata como del módulo General, con correo permitido.</summary>
    public static NotificationTypeInfo InfoOf(string type) =>
        Catalog.FirstOrDefault(t => t.Type == type) ?? new NotificationTypeInfo(type, NotificationModules.General, true, true, false);

    /// <summary>Tipo de notificación del estado al que llegó una solicitud (nulo si no se notifica).</summary>
    public static string? ForServiceRequestStatus(string status) => status switch
    {
        ServiceRequestStatus.PendingApproval => ServiceRequestPendingApproval,
        ServiceRequestStatus.Approved => ServiceRequestApproved,
        ServiceRequestStatus.Rejected => ServiceRequestRejected,
        ServiceRequestStatus.PendingPayment => ServiceRequestPendingPayment,
        ServiceRequestStatus.Paid => ServiceRequestPaid,
        ServiceRequestStatus.InProgress => ServiceRequestInProgress,
        ServiceRequestStatus.Completed => ServiceRequestCompleted,
        ServiceRequestStatus.Cancelled => ServiceRequestCancelled,
        _ => null
    };
}

/// <summary>Tipo de notificación con su módulo y su política de correo (M1-25).</summary>
public sealed record NotificationTypeInfo(
    string Type,
    string Module,
    bool EmailAvailable,
    bool EmailDefault,
    bool EmailMandatory);

/// <summary>Módulo de origen de una notificación, para filtrar la bandeja (M1-25).</summary>
public static class NotificationModules
{
    public const string Organization = "Organization";
    public const string Access = "Access";
    public const string Payments = "Payments";
    public const string Documents = "Documents";
    public const string Services = "Services";
    public const string Finance = "Finance";
    public const string Customs = "Customs";
    public const string Deadlines = "Deadlines";
    public const string Announcements = "Announcements";
    public const string Administration = "Administration";
    public const string General = "General";

    public static readonly string[] All =
        [Organization, Access, Payments, Documents, Services, Finance, Customs, Deadlines, Announcements, Administration, General];
}

/// <summary>Gestión o elemento al que corresponde una notificación (M1-25: identifica el embarque o la gestión).</summary>
public static class NotificationEntityTypes
{
    public const string Shipment = "Shipment";
    public const string Organization = "Organization";
    public const string JoinRequest = "JoinRequest";
    public const string AccessGrant = "AccessGrant";
    public const string Payment = "Payment";
    public const string ShipmentDocument = "ShipmentDocument";
    public const string ServiceRequest = "ServiceRequest";
    public const string DepositProof = "DepositProof";
    public const string CustomsTransmission = "CustomsTransmission";
    public const string Deadline = "Deadline";
    public const string Announcement = "Announcement";
    public const string ParentLink = "ParentLink";
    public const string CarrierPreRegistration = "CarrierPreRegistration";
}

/// <summary>
/// Acción que puede tomarse desde la notificación (M1-25). El destino es <c>ActionTargetId</c>; la interfaz la
/// ejecuta con el endpoint del módulo (p. ej. aprobar la solicitud de vinculación) y el servidor vuelve a autorizarla.
/// </summary>
public static class NotificationActionTypes
{
    public const string ApproveJoinRequest = "ApproveJoinRequest";
    public const string ReviewOrganization = "ReviewOrganization";
    public const string ReviewParentLink = "ReviewParentLink";
    public const string VerifyDepositProof = "VerifyDepositProof";
    public const string UploadDepositProof = "UploadDepositProof";
    public const string OpenPayment = "OpenPayment";
    public const string OpenServiceRequest = "OpenServiceRequest";
    public const string OpenShipment = "OpenShipment";
    public const string OpenDocument = "OpenDocument";
    public const string OpenAccessGrants = "OpenAccessGrants";
    public const string OpenAnnouncement = "OpenAnnouncement";
    public const string OpenInvoice = "OpenInvoice";

    /// <summary>Acciones que resuelven una gestión pendiente: dejan de estar disponibles al resolverse.</summary>
    public static readonly string[] Resolvable = [ApproveJoinRequest, ReviewOrganization, ReviewParentLink, VerifyDepositProof, UploadDepositProof];
}
