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

    /// <summary>
    /// Representación de una factura cuando el emisor documental (CT-DBNET) está simulado: imprime los datos
    /// del documento sincronizado en el portal, marcado como no válido tributariamente, en lugar de un PDF vacío.
    /// </summary>
    public static PdfDocumentModel SimulatedInvoice(
        CustomerInvoice invoice,
        string issuer,
        string? conceptName,
        DateTime generatedAt)
    {
        var (statusLabel, statusTone) = invoice.Status switch
        {
            Domain.Constants.InvoiceStatus.Paid => ("Pagada", PdfTones.Success),
            Domain.Constants.InvoiceStatus.Overdue => ("Vencida", PdfTones.Danger),
            Domain.Constants.InvoiceStatus.Cancelled => ("Anulada", PdfTones.Neutral),
            Domain.Constants.InvoiceStatus.Superseded => ("Reemplazada", PdfTones.Neutral),
            _ => ("Pendiente de pago", PdfTones.Warning)
        };
        var title = invoice.DocumentType switch
        {
            Domain.Constants.InvoiceDocumentTypes.ExemptInvoice => "Factura exenta electrónica",
            Domain.Constants.InvoiceDocumentTypes.CreditNote => "Nota de crédito electrónica",
            Domain.Constants.InvoiceDocumentTypes.DebitNote => "Nota de débito electrónica",
            _ => "Factura electrónica"
        };
        var taxLabel = invoice.Country == Domain.Constants.CountryCodes.Bolivia ? "Impuesto" : "IVA";
        var number = invoice.SiiNumber ?? invoice.SourceNumber;
        var concept = conceptName ?? invoice.ConceptCode ?? "Servicios";
        static string Date(DateOnly? date) => date?.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "-";

        return new PdfDocumentModel(
            title,
            "Representación simulada: el emisor de documentos tributarios no está conectado",
            issuer,
            invoice.Country == Domain.Constants.CountryCodes.Bolivia
                ? "Agente de Hapag-Lloyd AG - Operación Bolivia"
                : "Agente de Hapag-Lloyd AG - Operación Chile",
            number,
            generatedAt,
            Domain.Charges.BusinessCalendar.TimeZoneId(invoice.Country),
            [
                new("Folio", number),
                new("Fecha de emisión", Date(invoice.IssueDate)),
                new("Vencimiento", Date(invoice.DueDate)),
                new("BL", invoice.BlNumber),
                new("Booking", invoice.BookingNumber),
                new("Moneda", invoice.Currency)
            ],
            [
                new PdfSection("Receptor",
                [
                    new("Razón social", invoice.LegalName),
                    new(invoice.Country == Domain.Constants.CountryCodes.Bolivia ? "NIT" : "RUT", invoice.TaxId)
                ]),
                new PdfSection("Detalle",
                    Table: new PdfTable(
                        ["Concepto", "Neto", taxLabel, "Total"],
                        [[
                            invoice.BlNumber is null ? concept : $"{concept}\nBL {invoice.BlNumber}",
                            ShipmentDocumentTemplates.Money(invoice.NetAmount, invoice.Currency),
                            ShipmentDocumentTemplates.Money(invoice.TaxAmount, invoice.Currency),
                            ShipmentDocumentTemplates.Money(invoice.TotalAmount, invoice.Currency)
                        ]],
                        NumericColumns: [1, 2, 3],
                        ColumnWidths: [4, 1.4, 1.2, 1.4]),
                    Totals:
                    [
                        new("Neto", ShipmentDocumentTemplates.Amount(invoice.NetAmount, invoice.Currency)),
                        new(taxLabel, ShipmentDocumentTemplates.Amount(invoice.TaxAmount, invoice.Currency)),
                        new("Total", ShipmentDocumentTemplates.Amount(invoice.TotalAmount, invoice.Currency))
                    ],
                    Note: "Este documento es una representación generada por el portal en modo de prueba. No tiene validez tributaria: el documento oficial lo emite el sistema de facturación electrónica.",
                    NoteTone: PdfTones.Warning)
            ],
            null,
            null,
            "Documento generado por el Portal de Clientes de Hapag-Lloyd en modo de prueba.",
            StatusLabel: statusLabel,
            StatusTone: statusTone,
            Highlight: new PdfHighlight(
                invoice.Status == Domain.Constants.InvoiceStatus.Paid ? "Total pagado" : "Total",
                ShipmentDocumentTemplates.Amount(invoice.TotalAmount, invoice.Currency),
                invoice.DueDate is null || invoice.Status == Domain.Constants.InvoiceStatus.Paid ? null : $"Vence el {Date(invoice.DueDate)}",
                statusTone));
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
