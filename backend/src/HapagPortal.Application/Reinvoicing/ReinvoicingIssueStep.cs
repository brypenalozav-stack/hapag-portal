namespace HapagPortal.Application.Reinvoicing;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.PostProcessing;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Paso de la cola recuperable (NF-03) que emite la nueva factura de las refacturaciones IAO pagadas en el pago
/// (M3-11): solo las aceptadas por la nueva razón social; las pendientes de aceptación quedan en curso y la
/// aceptación vuelve a encolar el paso. Idempotente: una refacturación con factura emitida no se vuelve a emitir y
/// la fuente de facturación (CT-DBNET) deduplica por la referencia externa (número de la solicitud).
/// </summary>
public sealed class ReinvoicingIssueStep(
    IApplicationDbContext dbContext,
    ReinvoicingService service,
    ServiceRequestWorkflow workflow) : IPaymentPostStep
{
    public string JobType => PaymentOutboxJobTypes.Reinvoicing;

    public async Task ExecuteAsync(Payment payment, IReadOnlyList<PaymentDetail> details, DateTime now, CancellationToken cancellationToken)
    {
        var requests = await dbContext.ServiceRequests
            .Where(r => r.PaymentId == payment.Id
                && r.DefinitionCode == ServiceDefinitionCodes.IaoReinvoicing
                && r.Status == ServiceRequestStatus.InProgress)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            var reissue = await dbContext.InvoiceReissues.FirstOrDefaultAsync(r => r.ServiceRequestId == request.Id, cancellationToken);
            if (reissue is null || reissue.NewInvoiceId is not null || reissue.AcceptanceStatus != ReinvoicingAcceptanceStatus.Accepted)
                continue;

            var invoice = await service.IssueAsync(request, reissue, now, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var name = await dbContext.ServiceDefinitions.AsNoTracking()
                .Where(d => d.Id == request.DefinitionId)
                .Select(d => d.NameEs)
                .FirstOrDefaultAsync(cancellationToken) ?? request.DefinitionCode;
            await workflow.NotifyAsync(request, name, cancellationToken);
            await service.NotifyAsync(
                request,
                NotificationTypes.InvoiceReissued,
                $"Factura {invoice.SiiNumber} emitida ({request.RequestNumber})",
                $"Se emitió la factura {invoice.SiiNumber} a {invoice.LegalName} ({invoice.TaxId}). Reemplaza a la factura " +
                $"{reissue.OriginalSiiNumber ?? reissue.OriginalSourceNumber} y está disponible en Facturas.",
                cancellationToken);
        }
    }
}
