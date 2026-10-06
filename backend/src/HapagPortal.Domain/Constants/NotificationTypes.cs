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
}
