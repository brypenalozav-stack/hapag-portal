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

        // Los documentos imprimen nombres, no códigos: conceptos del catálogo de cargos y medio del mantenedor (M5-03).
        var codes = details.Select(d => d.ConceptType).Distinct().ToList();
        var conceptNames = (await dbContext.ChargeConcepts.AsNoTracking()
                .Where(c => codes.Contains(c.Code))
                .Select(c => new { c.Code, c.Name })
                .ToListAsync(cancellationToken))
            .GroupBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);
        var methodCode = payment.PaymentMethodCode ?? payment.PaymentMethod;
        var methodName = await dbContext.PaymentMethodConfigs.AsNoTracking()
            .Where(m => m.Country == payment.Country && m.Code == methodCode)
            .Select(m => m.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return renderer.Render(ShipmentDocumentTemplates.PaymentReceipt(
            payment,
            details,
            settings.IssuerFor(payment.Country),
            number,
            payment.PayerName ?? payer?.Name ?? "-",
            payment.PayerTaxId ?? (payer is null ? null : TaxIdNormalizer.Normalize(payer.TaxId)),
            conceptNames,
            methodName,
            settings.DepositInstructionsFor(payment.Country)));
    }

    /// <summary>
    /// Comprobante de depósito de los datos de demostración, que no tienen archivo (M5-06): resume los datos del
    /// abono informados para que la bandeja de Finanzas se pueda probar de punta a punta.
    /// </summary>
    public static PdfDocumentModel DepositProofSample(Payment payment, DepositProof proof, string issuer) => new(
        "Comprobante de depósito (muestra)",
        "Documento de demostración: el cliente no adjuntó un archivo real",
        issuer,
        payment.Country == Domain.Constants.CountryCodes.Bolivia
            ? "Agente de Hapag-Lloyd AG - Operación Bolivia"
            : "Agente de Hapag-Lloyd AG - Operación Chile",
        payment.SlipNumber ?? payment.PaymentNumber,
        proof.UploadedAt,
        Domain.Charges.BusinessCalendar.TimeZoneId(payment.Country),
        [
            new("Pago", payment.PaymentNumber),
            new("Boleta", payment.SlipNumber),
            new("Moneda", payment.Currency)
        ],
        [
            new PdfSection("Abono informado",
            [
                new("Banco", proof.BankName),
                new("N° de operación", proof.BankReference),
                new("Fecha del depósito", proof.DepositDate?.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture)),
                new("Monto", proof.DepositAmount is null ? null : $"{ShipmentDocumentTemplates.Money(proof.DepositAmount.Value)} {payment.Currency}"),
                new("Observaciones", proof.Notes)
            ])
        ],
        null,
        null,
        "Muestra generada por el Portal de Clientes de Hapag-Lloyd.");

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
