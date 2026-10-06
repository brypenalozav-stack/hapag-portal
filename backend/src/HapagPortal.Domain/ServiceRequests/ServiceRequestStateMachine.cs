using HapagPortal.Domain.Constants;

namespace HapagPortal.Domain.ServiceRequests;

/// <summary>
/// Transiciones válidas de una solicitud de servicio on demand. Sin aprobación ni cobro se pasa de enviada a
/// en curso o completada; con cobro, la liberación del pago lleva de pendiente de pago a pagada y luego a en
/// curso (si un equipo presta el servicio) o completada. Rechazada, completada y anulada son finales.
/// </summary>
public static class ServiceRequestStateMachine
{
    private static readonly Dictionary<string, string[]> Transitions = new(StringComparer.Ordinal)
    {
        [ServiceRequestStatus.Draft] = [ServiceRequestStatus.Submitted, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.Submitted] =
        [
            ServiceRequestStatus.PendingApproval, ServiceRequestStatus.PendingPayment, ServiceRequestStatus.InProgress,
            ServiceRequestStatus.Completed, ServiceRequestStatus.Cancelled
        ],
        [ServiceRequestStatus.PendingApproval] =
            [ServiceRequestStatus.Approved, ServiceRequestStatus.Rejected, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.Approved] =
        [
            ServiceRequestStatus.PendingPayment, ServiceRequestStatus.InProgress, ServiceRequestStatus.Completed,
            ServiceRequestStatus.Cancelled
        ],
        [ServiceRequestStatus.PendingPayment] = [ServiceRequestStatus.Paid, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.Paid] = [ServiceRequestStatus.InProgress, ServiceRequestStatus.Completed],
        [ServiceRequestStatus.InProgress] = [ServiceRequestStatus.Completed],
    };

    public static bool CanTransition(string from, string to) =>
        Transitions.TryGetValue(from, out var targets) && targets.Contains(to);

    public static bool IsTerminal(string status) => ServiceRequestStatus.Terminal.Contains(status);
}
