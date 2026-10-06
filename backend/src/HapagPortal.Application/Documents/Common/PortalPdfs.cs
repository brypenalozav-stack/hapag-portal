namespace HapagPortal.Application.Documents.Common;

using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// PDF de documentos que no se archivan en el repositorio del embarque porque se generan desde su registro
/// en cada descarga: comprobante o boleta de un pago (M7-02) y orden de servicio. Reemplazan los PDF de
/// marcador anteriores a la Ola E.
/// </summary>
public static class PortalPdfs
{
    public static async Task<byte[]> PaymentReceiptAsync(
        IApplicationDbContext dbContext,
        IPdfDocumentRenderer renderer,
        DocumentSettings settings,
        Payment payment,
        string number,
        CancellationToken cancellationToken)
    {
        var details = await dbContext.PaymentDetails.AsNoTracking()
            .Where(d => d.PaymentId == payment.Id)
            .ToListAsync(cancellationToken);
        var payer = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == payment.ClientId, cancellationToken);

        return renderer.Render(ShipmentDocumentTemplates.PaymentReceipt(
            payment,
            details,
            settings.IssuerFor(payment.Country),
            number,
            payment.PayerName ?? payer?.Name ?? "-",
            payment.PayerTaxId ?? (payer is null ? null : TaxIdNormalizer.Normalize(payer.TaxId))));
    }

    public static async Task<byte[]> ServiceOrderAsync(
        IApplicationDbContext dbContext,
        IPdfDocumentRenderer renderer,
        DocumentSettings settings,
        ServiceOrder order,
        CancellationToken cancellationToken)
    {
        var bl = await dbContext.BillsOfLading.AsNoTracking().FirstOrDefaultAsync(b => b.Id == order.BillOfLadingId, cancellationToken);
        var client = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == order.ClientId, cancellationToken);

        return renderer.Render(ShipmentDocumentTemplates.ServiceOrder(
            order,
            bl,
            settings.IssuerFor(order.Country),
            client?.Name ?? "-",
            client is null ? null : TaxIdNormalizer.Normalize(client.TaxId)));
    }
}
