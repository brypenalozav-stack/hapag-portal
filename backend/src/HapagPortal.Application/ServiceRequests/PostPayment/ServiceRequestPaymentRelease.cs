namespace HapagPortal.Application.ServiceRequests.PostPayment;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.PostProcessing;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Avance de las solicitudes de servicio por la liberación del pago (NF-03): cuando todos los cargos de una
/// solicitud pendiente de pago quedan pagados (o exentos), pasa a pagada y luego a en curso con el equipo que
/// presta el servicio (Late Arrival/Early, sellos, correcciones) o a completada. Idempotente: solo avanza
/// solicitudes en <c>PendingPayment</c>, por lo que un reintento de la liberación no repite hitos.
/// </summary>
public static class ServiceRequestPaymentRelease
{
    /// <summary>Devuelve las solicitudes que avanzaron (el llamador guarda los cambios).</summary>
    public static async Task<IReadOnlyList<ServiceRequest>> AdvanceAsync(
        IApplicationDbContext dbContext,
        IReadOnlyCollection<Guid> paidChargeIds,
        Payment payment,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (paidChargeIds.Count == 0)
            return [];

        var requestIds = await dbContext.ServiceRequestCharges.AsNoTracking()
            .Where(l => paidChargeIds.Contains(l.LocalChargeId))
            .Select(l => l.ServiceRequestId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (requestIds.Count == 0)
            return [];

        var requests = await dbContext.ServiceRequests
            .Where(r => requestIds.Contains(r.Id) && r.Status == ServiceRequestStatus.PendingPayment)
            .ToListAsync(cancellationToken);

        var advanced = new List<ServiceRequest>();
        foreach (var request in requests)
        {
            var linked = await dbContext.ServiceRequestCharges.AsNoTracking()
                .Where(l => l.ServiceRequestId == request.Id)
                .Select(l => l.LocalChargeId)
                .ToListAsync(cancellationToken);

            // Las entidades ya seguidas conservan el estado Pagado que acaba de asignar la liberación.
            var charges = await dbContext.LocalCharges
                .Where(c => linked.Contains(c.Id))
                .ToListAsync(cancellationToken);
            // Imputado a crédito (M5-10) libera igual que pagado.
            if (charges.Count < linked.Count
                || charges.Any(c => c.Status is not (ChargeStatus.Paid or ChargeStatus.Exempt or ChargeStatus.CreditImputed)))
                continue;

            var definition = await dbContext.ServiceDefinitions.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == request.DefinitionId, cancellationToken)
                ?? throw new InvalidOperationException($"The service definition '{request.DefinitionId}' no longer exists.");

            var actor = new ServiceActor($"PAYMENT {payment.PaymentNumber}", null, ServiceRequestActorKinds.System);
            var paid = ServiceRequestWorkflow.Transition(dbContext, request, ServiceRequestStatus.Paid, actor,
                payment.ReceiptNumber is null ? null : $"Comprobante {payment.ReceiptNumber}.", now);
            if (paid.IsFailure)
                throw new InvalidOperationException(paid.Error.Message);

            request.PaymentId = payment.Id;

            // M3-11: la refacturación queda en curso hasta emitir la nueva factura (paso Reinvoicing), que exige
            // además la aceptación del cobro por la nueva razón social.
            var fulfilled = request.DefinitionCode == ServiceDefinitionCodes.IaoReinvoicing
                ? ServiceRequestWorkflow.Transition(dbContext, request, ServiceRequestStatus.InProgress, ServiceActor.System,
                    "Pendiente de la emisión de la nueva factura (requiere la aceptación de la nueva razón social).", now)
                : ServiceRequestWorkflow.Fulfill(dbContext, request, definition, ServiceActor.System, null, now);
            if (fulfilled.IsFailure)
                throw new InvalidOperationException(fulfilled.Error.Message);

            advanced.Add(request);
        }

        return advanced;
    }
}

/// <summary>
/// Aviso a los administradores de la organización del estado alcanzado por las solicitudes pagadas en el pago
/// (en curso o completada). Paso de la cola recuperable (NF-03) encolado por la liberación; la clave de
/// deduplicación evita repetir el aviso en un reintento.
/// </summary>
public sealed class NotifyServiceRequestsStep(
    IApplicationDbContext dbContext,
    ServiceRequestWorkflow workflow) : IPaymentPostStep
{
    public string JobType => PaymentOutboxJobTypes.ServiceRequests;

    public async Task ExecuteAsync(Payment payment, IReadOnlyList<PaymentDetail> details, DateTime now, CancellationToken cancellationToken)
    {
        var chargeIds = details
            .Where(d => d.ItemType == PayableItemTypes.LocalCharge && d.SourceId is not null)
            .Select(d => d.SourceId!.Value)
            .ToList();

        var requestIds = await dbContext.ServiceRequestCharges.AsNoTracking()
            .Where(l => chargeIds.Contains(l.LocalChargeId))
            .Select(l => l.ServiceRequestId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var requests = await dbContext.ServiceRequests.AsNoTracking()
            .Where(r => requestIds.Contains(r.Id) && r.PaymentId == payment.Id)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            var name = await dbContext.ServiceDefinitions.AsNoTracking()
                .Where(d => d.Id == request.DefinitionId)
                .Select(d => d.NameEs)
                .FirstOrDefaultAsync(cancellationToken) ?? request.DefinitionCode;

            await workflow.NotifyAsync(request, name, cancellationToken);
        }
    }
}
