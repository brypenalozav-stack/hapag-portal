namespace HapagPortal.Application.Documents.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Demurrage.Common;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Verificación previa al certificado de libre deuda (M6-07): el embarque no debe registrar cargos locales
/// pendientes, líneas de demurrage sin pagar, facturas pendientes (M7-01), flete Collect sin pagar ni
/// demoras anticipadas exigidas por la regla interna sin pagar (M3-16). Con crédito vigente en Nexus
/// (M8-02, M4-03) una factura aún no vencida no bloquea; sin crédito, o si Nexus no responde, toda factura
/// pendiente bloquea. Las demoras anticipadas se informan en su propio motivo y no como cargo pendiente.
/// </summary>
public sealed class NoDebtEvaluator(
    IApplicationDbContext dbContext,
    IChargeRulesService chargeRulesService,
    DemurrageStatusBuilder demurrageStatusBuilder)
{
    public static bool IsApplicable(BillOfLading billOfLading) =>
        billOfLading.Country == CountryCodes.Bolivia && DemurrageStateEvaluator.IsImport(billOfLading);

    public async Task<IReadOnlyList<NoDebtBlockerDto>> EvaluateAsync(
        BillOfLading billOfLading,
        Client account,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var blockers = new List<NoDebtBlockerDto>();
        var today = BusinessCalendar.LocalDate(billOfLading.Country, now);

        var charges = await dbContext.LocalCharges.AsNoTracking()
            .Where(c => c.BillOfLadingId == billOfLading.Id && c.Status == ChargeStatus.Pending)
            .ToListAsync(cancellationToken);
        var pendingCharges = charges.Where(c => c.ChargeType != ChargeConceptCodes.AdvanceDemurrageBo).ToList();
        if (pendingCharges.Count > 0)
        {
            blockers.Add(Blocker(
                NoDebtBlockers.PendingCharges,
                pendingCharges.Select(c => c.ChargeType),
                pendingCharges.Select(c => (c.Currency, c.TotalAmount))));
        }

        var lines = await dbContext.DemurrageCharges.AsNoTracking()
            .Where(d => d.BillOfLadingId == billOfLading.Id && !d.IsExempt && d.Status != DemurrageChargeStatus.Paid)
            .ToListAsync(cancellationToken);
        if (lines.Count > 0)
        {
            blockers.Add(Blocker(
                NoDebtBlockers.PendingDemurrage,
                lines.Select(l => l.InvoiceNumber is null ? l.ContainerNumber : $"{l.ContainerNumber} ({l.InvoiceNumber})"),
                lines.Select(l => (l.Currency, l.TotalAmount))));
        }

        var conditions = await chargeRulesService.GetConditionsAsync(account, cancellationToken);
        var hasCredit = conditions.Available && conditions.HasCredit;

        var invoices = (await dbContext.CustomerInvoices.AsNoTracking()
                .Where(i => i.BillOfLadingId == billOfLading.Id
                    && i.DocumentType != InvoiceDocumentTypes.CreditNote
                    && (i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Overdue))
                .ToListAsync(cancellationToken))
            .Where(i => !hasCredit || i.Status == InvoiceStatus.Overdue || (i.DueDate is { } due && due < today))
            .ToList();
        if (invoices.Count > 0)
        {
            blockers.Add(Blocker(
                NoDebtBlockers.PendingInvoices,
                invoices.Select(i => i.SiiNumber ?? i.SourceNumber),
                invoices.Select(i => (i.Currency, i.TotalAmount))));
        }

        if (string.Equals(billOfLading.FreightTerms, "Collect", StringComparison.OrdinalIgnoreCase) && billOfLading.FreightPaidAt is null)
        {
            blockers.Add(Blocker(
                NoDebtBlockers.PendingFreight,
                [PaymentConcepts.Freight],
                [(billOfLading.FreightCurrency, billOfLading.FreightAmount)]));
        }

        var rule = await demurrageStatusBuilder.FindAdvanceRuleAsync(billOfLading, today, cancellationToken);
        if (rule is not null)
        {
            var advance = charges.FirstOrDefault(c => c.ChargeType == ChargeConceptCodes.AdvanceDemurrageBo)
                ?? await dbContext.LocalCharges.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.BillOfLadingId == billOfLading.Id && c.ChargeType == ChargeConceptCodes.AdvanceDemurrageBo, cancellationToken);

            if (advance is null || advance.Status != ChargeStatus.Paid)
            {
                blockers.Add(Blocker(
                    NoDebtBlockers.AdvanceDemurrage,
                    [advance is null ? AdvanceDemurrageStatus.NotRequested : AdvanceDemurrageStatus.Pending],
                    advance is null ? [] : [(advance.Currency, advance.TotalAmount)]));
            }
        }

        return blockers;
    }

    /// <summary>Texto del error con cada motivo y sus referencias, para presentarlo al cliente.</summary>
    public static string Describe(IReadOnlyList<NoDebtBlockerDto> blockers) =>
        string.Join("; ", blockers.Select(b => $"{b.Code} ({string.Join(", ", b.References)})"));

    private static NoDebtBlockerDto Blocker(
        string code,
        IEnumerable<string> references,
        IEnumerable<(string Currency, decimal Amount)> amounts) =>
        new(
            code,
            references.Distinct(StringComparer.Ordinal).ToList(),
            amounts
                .GroupBy(a => a.Currency, StringComparer.OrdinalIgnoreCase)
                .Select(g => new NoDebtAmountDto(g.Key, g.Sum(a => a.Amount)))
                .OrderBy(a => a.Currency, StringComparer.Ordinal)
                .ToList());
}
