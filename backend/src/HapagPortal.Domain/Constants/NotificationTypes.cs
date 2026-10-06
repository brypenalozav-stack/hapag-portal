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
