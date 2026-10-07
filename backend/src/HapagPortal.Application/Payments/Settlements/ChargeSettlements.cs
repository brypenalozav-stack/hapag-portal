namespace HapagPortal.Application.Payments.Settlements;

using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Pago anticipado o imputación a crédito de un cargo (M7-03, M3-19, M5-10), con su cruce con la factura
/// (<c>matchedInvoice*</c>) y el recibo del anticipo en el repositorio (<c>receiptDocument*</c>).
/// </summary>
public sealed record ChargeSettlementDto(
    Guid Id,
    string Kind,
    string Status,
    Guid PaymentId,
    string PaymentNumber,
    string? ReceiptNumber,
    Guid PayerOrganizationId,
    string? PayerTaxId,
    string? PayerName,
    Guid? OnBehalfOfOrganizationId,
    string? BillingTaxId,
    string? BillingName,
    Guid? BlId,
    string? BlNumber,
    string? BookingNumber,
    string Country,
    string ItemType,
    Guid SourceId,
    string ConceptCode,
    string? Description,
    decimal Amount,
    string Currency,
    decimal PaidAmount,
    string PaidCurrency,
    DateTime SettledAt,
    Guid? ReceiptDocumentId,
    string? ReceiptDocumentNumber,
    Guid? MatchedInvoiceId,
    string? MatchedInvoiceNumber,
    DateTime? MatchedAt,
    string? MatchedBy,
    string? MatchNote);

/// <summary>
/// Cobertura de una factura por un pago anterior a su emisión (M7-03, M3-19): pago, comprobante y recibo del
/// anticipo en el repositorio documental. La factura queda pagada y no vuelve a cobrarse.
/// </summary>
public sealed record InvoiceCoverageDto(
    string Kind,
    Guid PaymentId,
    string PaymentNumber,
    string? ReceiptNumber,
    Guid? ReceiptDocumentId,
    string? ReceiptDocumentNumber,
    decimal Amount,
    string Currency,
    DateTime SettledAt,
    DateTime? MatchedAt);

/// <summary>
/// Registro y cruce de los cargos cubiertos antes de su factura (M7-03, M3-19, M5-10, NF-04):
/// <list type="bullet">
/// <item>La liberación de un pago registra un anticipo por cada recargo, línea de demurrage, flete o cambio de
/// almacén liberado; la de una imputación a crédito, una imputación. Idempotente por ítem del pago.</item>
/// <item>El cruce vincula los registros abiertos con la factura que los incluye: mismo BL, concepto, moneda y
/// RUT facturado, y la suma de los montos igual al total de la factura. El anticipo deja la factura pagada
/// (cubierta, sin saldo); la imputación la deja como deuda a crédito y deja de figurar como no facturada.</item>
/// </list>
/// No guarda: lo hace el llamador.
/// </summary>
public static class ChargeSettlements
{
    /// <summary>Tipos de ítem cuyo pago antes de la factura es un anticipo (las facturas ya están emitidas).</summary>
    public static readonly string[] PrepayableItemTypes =
        [PayableItemTypes.LocalCharge, PayableItemTypes.Demurrage, PayableItemTypes.Freight, PayableItemTypes.WarehouseChange];

    private static readonly string[] MatchableDocumentTypes =
        [InvoiceDocumentTypes.Invoice, InvoiceDocumentTypes.ExemptInvoice, InvoiceDocumentTypes.DebitNote];

    public static async Task<ChargeSettlement?> RecordAsync(
        IApplicationDbContext dbContext,
        Payment payment,
        PaymentDetail detail,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (detail.ItemType is null || detail.SourceId is null || !PrepayableItemTypes.Contains(detail.ItemType))
            return null;

        var existing = await dbContext.ChargeSettlements.FirstOrDefaultAsync(s => s.PaymentDetailId == detail.Id, cancellationToken);
        if (existing is not null)
            return existing;

        var settlement = new ChargeSettlement
        {
            Kind = payment.Origin == PaymentOrigins.CreditLine ? SettlementKinds.CreditImputation : SettlementKinds.Advance,
            Status = SettlementStatus.Open,
            PaymentId = payment.Id,
            PaymentDetailId = detail.Id,
            PaymentNumber = payment.PaymentNumber,
            ReceiptNumber = payment.ReceiptNumber,
            PayerOrganizationId = payment.ClientId,
            PayerTaxId = payment.PayerTaxId,
            PayerName = payment.PayerName,
            OnBehalfOfOrganizationId = detail.OnBehalfOfClientId ?? payment.OnBehalfOfClientId,
            BillingTaxId = detail.BillingTaxId,
            BillingName = detail.BillingName,
            BillOfLadingId = detail.BillOfLadingId ?? (detail.ItemType == PayableItemTypes.Freight ? detail.SourceId : null),
            BlNumber = detail.BlNumber,
            BookingNumber = detail.BookingNumber,
            Country = payment.Country,
            ItemType = detail.ItemType,
            SourceId = detail.SourceId.Value,
            ConceptCode = detail.ConceptType,
            Description = detail.Description,
            Amount = detail.OriginalAmount ?? detail.Amount + detail.TaxAmount,
            Currency = detail.OriginalCurrency ?? detail.Currency,
            PaidAmount = detail.Amount + detail.TaxAmount,
            PaidCurrency = detail.Currency,
            SettledAt = payment.ConfirmedAt ?? now,
            CreatedAt = now
        };

        dbContext.ChargeSettlements.Add(settlement);
        return settlement;
    }

    /// <summary>
    /// Cruza los registros abiertos de los BL de las facturas indicadas con ellas. Devuelve las facturas
    /// vinculadas. Un registro solo se cruza una vez; una factura con registros ya vinculados no se vuelve a usar.
    /// </summary>
    public static async Task<IReadOnlyList<CustomerInvoice>> MatchAsync(
        IApplicationDbContext dbContext,
        IReadOnlyCollection<CustomerInvoice> invoices,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var candidates = invoices.Where(IsMatchable).ToList();
        if (candidates.Count == 0)
            return [];

        var blIds = candidates.Select(i => i.BillOfLadingId!.Value).Distinct().ToList();
        var open = await dbContext.ChargeSettlements
            .Where(s => s.Status == SettlementStatus.Open && s.BillOfLadingId != null && blIds.Contains(s.BillOfLadingId.Value))
            .ToListAsync(cancellationToken);
        if (open.Count == 0)
            return [];

        var invoiceIds = candidates.Select(i => i.Id).ToList();
        var used = (await dbContext.ChargeSettlements.AsNoTracking()
                .Where(s => s.MatchedInvoiceId != null && invoiceIds.Contains(s.MatchedInvoiceId.Value))
                .Select(s => s.MatchedInvoiceId!.Value)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var matched = new List<CustomerInvoice>();
        foreach (var invoice in candidates.Where(i => !used.Contains(i.Id)).OrderBy(i => i.IssueDate).ThenBy(i => i.SourceNumber, StringComparer.Ordinal))
        {
            var group = open
                .Where(s => s.Status == SettlementStatus.Open
                    && s.BillOfLadingId == invoice.BillOfLadingId
                    && s.ConceptCode == invoice.ConceptCode
                    && s.Currency == invoice.Currency
                    && (s.BillingTaxId is null || TaxIdNormalizer.AreEqual(s.BillingTaxId, invoice.TaxId)))
                .OrderBy(s => s.SettledAt)
                .ToList();
            if (group.Count == 0)
                continue;

            // Uno solo con el total exacto, o todos los del concepto sumando el total.
            var exact = group.FirstOrDefault(s => s.Amount == invoice.TotalAmount);
            List<ChargeSettlement>? selection = exact is not null
                ? [exact]
                : group.Sum(s => s.Amount) == invoice.TotalAmount ? group : null;
            if (selection is null)
                continue;

            Apply(selection, invoice, actor, null, now);
            matched.Add(invoice);
        }

        return matched;
    }

    /// <summary>
    /// Vincula los registros con la factura. Un anticipo la deja pagada por ese pago (no vuelve a cobrarse); una
    /// imputación a crédito la deja como deuda a crédito.
    /// </summary>
    public static void Apply(IReadOnlyList<ChargeSettlement> settlements, CustomerInvoice invoice, string actor, string? note, DateTime now)
    {
        foreach (var settlement in settlements)
        {
            settlement.Status = SettlementStatus.Matched;
            settlement.MatchedInvoiceId = invoice.Id;
            settlement.MatchedAt = now;
            settlement.MatchedBy = actor;
            settlement.MatchNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }

        var advance = settlements.FirstOrDefault(s => s.Kind == SettlementKinds.Advance);
        if (advance is null)
            return;

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt ??= advance.SettledAt;
        invoice.PaymentId ??= advance.PaymentId;
        invoice.IsPayable = false;
    }

    /// <summary>Factura que puede cubrir anticipos: con BL y concepto, no nota de crédito, anulada ni reemplazada.</summary>
    public static bool IsMatchable(CustomerInvoice invoice) =>
        invoice.BillOfLadingId is not null
        && invoice.ConceptCode is not null
        && MatchableDocumentTypes.Contains(invoice.DocumentType)
        && invoice.Status is not (InvoiceStatus.Cancelled or InvoiceStatus.Superseded);

    /// <summary>Cobertura por anticipo de cada factura indicada (M7-01, M7-03).</summary>
    public static async Task<IReadOnlyDictionary<Guid, InvoiceCoverageDto>> CoverageAsync(
        IApplicationDbContext dbContext,
        IReadOnlyCollection<Guid> invoiceIds,
        CancellationToken cancellationToken)
    {
        if (invoiceIds.Count == 0)
            return new Dictionary<Guid, InvoiceCoverageDto>();

        var settlements = await dbContext.ChargeSettlements.AsNoTracking()
            .Where(s => s.Kind == SettlementKinds.Advance && s.MatchedInvoiceId != null && invoiceIds.Contains(s.MatchedInvoiceId.Value))
            .ToListAsync(cancellationToken);
        var documents = await DocumentNumbersAsync(dbContext, settlements, cancellationToken);

        return settlements
            .GroupBy(s => s.MatchedInvoiceId!.Value)
            .ToDictionary(g => g.Key, g =>
            {
                var first = g.OrderBy(s => s.SettledAt).First();
                var document = g.Select(s => s.ReceiptDocumentId).FirstOrDefault(d => d is not null);
                return new InvoiceCoverageDto(
                    first.Kind, first.PaymentId, first.PaymentNumber, first.ReceiptNumber, document,
                    document is { } id ? documents.GetValueOrDefault(id) : null, g.Sum(s => s.Amount), first.Currency,
                    first.SettledAt, first.MatchedAt);
            });
    }

    public static async Task<IReadOnlyList<ChargeSettlementDto>> ToDtosAsync(
        IApplicationDbContext dbContext,
        IReadOnlyList<ChargeSettlement> settlements,
        CancellationToken cancellationToken)
    {
        var documents = await DocumentNumbersAsync(dbContext, settlements, cancellationToken);
        var invoiceIds = settlements.Where(s => s.MatchedInvoiceId is not null).Select(s => s.MatchedInvoiceId!.Value).Distinct().ToList();
        var invoices = await dbContext.CustomerInvoices.AsNoTracking()
            .Where(i => invoiceIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.SiiNumber ?? i.SourceNumber, cancellationToken);

        return settlements.Select(s => new ChargeSettlementDto(
            s.Id, s.Kind, s.Status, s.PaymentId, s.PaymentNumber, s.ReceiptNumber, s.PayerOrganizationId, s.PayerTaxId, s.PayerName,
            s.OnBehalfOfOrganizationId, s.BillingTaxId, s.BillingName, s.BillOfLadingId, s.BlNumber, s.BookingNumber, s.Country,
            s.ItemType, s.SourceId, s.ConceptCode, s.Description, s.Amount, s.Currency, s.PaidAmount, s.PaidCurrency, s.SettledAt,
            s.ReceiptDocumentId, s.ReceiptDocumentId is { } documentId ? documents.GetValueOrDefault(documentId) : null,
            s.MatchedInvoiceId, s.MatchedInvoiceId is { } invoiceId ? invoices.GetValueOrDefault(invoiceId) : null,
            s.MatchedAt, s.MatchedBy, s.MatchNote)).ToList();
    }

    private static async Task<Dictionary<Guid, string>> DocumentNumbersAsync(
        IApplicationDbContext dbContext,
        IEnumerable<ChargeSettlement> settlements,
        CancellationToken cancellationToken)
    {
        var ids = settlements.Where(s => s.ReceiptDocumentId is not null).Select(s => s.ReceiptDocumentId!.Value).Distinct().ToList();
        return ids.Count == 0
            ? []
            : await dbContext.ShipmentDocuments.AsNoTracking()
                .Where(d => ids.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.DocumentNumber, cancellationToken);
    }
}
