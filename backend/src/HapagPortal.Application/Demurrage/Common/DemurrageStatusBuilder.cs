namespace HapagPortal.Application.Demurrage.Common;

using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.InternalChargeRules;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Estado del demurrage de un BL (M3-18), en orden de precedencia: facturado con deuda (pagar la
/// factura; la calculadora queda bloqueada), calculado y no pagado (agregar al carro), aún no calculado
/// (calcular) y sin demurrage (mensaje de que no registra deuda). La misma evaluación debe usarse en
/// Estado de Cuenta (M7-03) para que ambos módulos sean consistentes.
/// </summary>
public static class DemurrageStateEvaluator
{
    public const string NoDebtMessage = "NO_DEBT";

    public static (string State, string Action, bool CalculatorEnabled) Evaluate(
        BillOfLading billOfLading,
        IReadOnlyCollection<DemurrageCharge> lines,
        DateTime nowUtc)
    {
        var active = lines.Where(l => !l.IsExempt).ToList();
        var hasInvoice = lines.Any(l => l.InvoiceNumber is not null);

        if (active.Any(l => l.InvoiceNumber is not null && l.Status != DemurrageChargeStatus.Paid))
            return (DemurrageStates.InvoicedWithDebt, ChargeActions.Pay, false);

        if (active.Any(l => l.InvoiceNumber is null && l.Status != DemurrageChargeStatus.Paid))
            return (DemurrageStates.CalculatedUnpaid, ChargeActions.AddToCart, !hasInvoice);

        if (lines.Count == 0 && IsImport(billOfLading) && HasArrived(billOfLading, nowUtc))
            return (DemurrageStates.NotCalculated, ChargeActions.Calculate, true);

        return (DemurrageStates.NoDemurrage, ChargeActions.None, false);
    }

    public static bool IsImport(BillOfLading billOfLading) =>
        string.Equals(billOfLading.ShipmentType, "Import", StringComparison.OrdinalIgnoreCase);

    public static bool HasArrived(BillOfLading billOfLading, DateTime nowUtc) =>
        billOfLading.ETA is not null && billOfLading.ETA.Value <= nowUtc;
}

/// <summary>
/// Arma el estado de demurrage del BL con MHD (M3-02), demoras anticipadas de Bolivia (M3-16) y los
/// datos de la calculadora. Las demoras anticipadas se exigen a la cuenta titular del BL según las
/// reglas internas (<see cref="InternalChargeRule"/>); su monto sale del mantenedor de tarifas.
/// </summary>
public sealed class DemurrageStatusBuilder(
    IApplicationDbContext dbContext,
    ITariffResolver tariffResolver,
    IExchangeRateService exchangeRateService,
    IShipmentSource shipmentSource)
{
    public const string FreeDaysFromSource = "FIS";
    public const string FreeDaysFromCharges = "PORTAL";
    public const string FreeDaysFromTariff = "TARIFF";

    public async Task<DemurrageStatusDto> BuildAsync(
        BillOfLading billOfLading,
        ShipmentPermissionSet permissions,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var country = billOfLading.Country;
        var today = BusinessCalendar.LocalDate(country, now);

        var lines = await dbContext.DemurrageCharges.AsNoTracking()
            .Where(d => d.BillOfLadingId == billOfLading.Id)
            .OrderBy(d => d.ContainerNumber)
            .ToListAsync(cancellationToken);

        var containers = await dbContext.BLContainers.AsNoTracking()
            .Where(c => c.BillOfLadingId == billOfLading.Id)
            .OrderBy(c => c.ContainerNumber)
            .ToListAsync(cancellationToken);

        var catalog = await dbContext.ChargeConcepts.AsNoTracking().ToDictionaryAsync(c => c.Code, cancellationToken);
        var demurrageConcepts = (await dbContext.LocalCharges.AsNoTracking()
                .Where(c => c.BillOfLadingId == billOfLading.Id)
                .ToListAsync(cancellationToken))
            .Where(c => catalog.GetValueOrDefault(c.ChargeType)?.Category == ChargeCategories.Demurrage)
            .OrderBy(c => catalog[c.ChargeType].DisplayOrder)
            .ToList();

        var (state, action, calculatorEnabled) = DemurrageStateEvaluator.Evaluate(billOfLading, lines, now);
        var (canAct, blockedReason) = ActionAllowed(permissions);

        var advance = await EvaluateAdvanceAsync(billOfLading, containers, demurrageConcepts, today, canAct, blockedReason, cancellationToken);
        var (concepts, mhd) = await BuildConceptsAsync(demurrageConcepts, catalog, advance, today, canAct, blockedReason, cancellationToken);

        var invoices = lines
            .Where(l => !l.IsExempt && l.InvoiceNumber is not null && l.Status != DemurrageChargeStatus.Paid)
            .GroupBy(l => (l.InvoiceNumber!, l.Currency))
            .Select(g => new DemurrageInvoiceDto(
                g.Key.Item1,
                g.Sum(l => l.TotalAmount),
                g.Key.Currency,
                g.Max(l => l.InvoiceDueDate),
                g.Max(l => l.InvoicedAt),
                g.Select(l => l.Id).ToList()))
            .ToList();

        var requirements = new List<ProcessRequirementDto>();
        if (advance.Required)
        {
            requirements.Add(new ProcessRequirementDto(
                ProcessRequirements.AdvanceDemurrage,
                advance.Status == AdvanceDemurrageStatus.Paid ? ProcessRequirementStatus.Fulfilled
                    : advance.Status == AdvanceDemurrageStatus.Pending ? ProcessRequirementStatus.Pending
                    : ProcessRequirementStatus.Missing,
                advance.CldBlocked,
                RuleSources.Portal));
        }

        return new DemurrageStatusDto(
            billOfLading.Id,
            billOfLading.BLNumber,
            country,
            BusinessCalendar.TimeZoneId(country),
            state,
            action,
            ActionAllowed: action != ChargeActions.None && canAct,
            ActionBlockedReason: action != ChargeActions.None && !canAct ? blockedReason : null,
            calculatorEnabled,
            MessageCode: state == DemurrageStates.NoDemurrage ? DemurrageStateEvaluator.NoDebtMessage : null,
            invoices,
            lines.Select(ToLine).ToList(),
            calculatorEnabled ? await CalculationInputsAsync(billOfLading, containers, lines, today, cancellationToken) : null,
            concepts,
            mhd,
            advance,
            requirements,
            now);
    }

    /// <summary>Días libres: los de FIS (CT-FIS), si no los del último cálculo; sin ellos, los tramos en cero de la tarifa.</summary>
    public async Task<(int? FreeDays, string Source)> ResolveFreeDaysAsync(
        BillOfLading billOfLading,
        IReadOnlyCollection<DemurrageCharge> lines,
        CancellationToken cancellationToken)
    {
        var source = await shipmentSource.GetByBlNumberAsync(billOfLading.BLNumber, cancellationToken);
        if (source.IsSuccess && source.Value?.FreeDays is { } freeDays)
            return (freeDays, FreeDaysFromSource);

        var previous = lines.Where(l => l.FreeDays > 0).Select(l => (int?)l.FreeDays).FirstOrDefault();
        return previous is not null ? (previous, FreeDaysFromCharges) : (null, FreeDaysFromTariff);
    }

    public static (bool Allowed, string? Reason) ActionAllowed(ShipmentPermissionSet permissions)
    {
        if (permissions.RequiresAssociationForPayment)
            return (false, ChargeActionBlockReasons.AssociationRequired);

        return permissions.CanExecute(ShipmentActionCodes.PayImportDemurrage)
            ? (true, null)
            : (false, ChargeActionBlockReasons.NoPermission);
    }

    /// <summary>Monto de demoras anticipadas según el mantenedor: tarifa por contenedor (o una unidad sin contenedores).</summary>
    public async Task<(decimal Amount, string Currency)?> QuoteAdvanceAsync(
        BillOfLading billOfLading,
        IReadOnlyList<BLContainer> containers,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string?> types = containers.Count == 0
            ? [null]
            : containers.Select(c => (string?)c.ContainerType).ToList();
        decimal total = 0m;
        string? currency = null;

        foreach (var type in types)
        {
            var tariff = (await tariffResolver.GetInForceAsync(
                    new TariffLookup(billOfLading.Country, ChargeConceptCodes.AdvanceDemurrageBo, today, type),
                    cancellationToken))
                .FirstOrDefault(t => currency is null || t.Currency == currency);

            if (tariff is null)
                return null;

            currency = tariff.Currency;
            total += TariffCalculator.Compute(tariff.Amount, tariff.TierMode, tariff.Tiers, 1).Amount;
        }

        return (MoneyRounding.Round(total, currency!), currency!);
    }

    private async Task<AdvanceDemurrageDto> EvaluateAdvanceAsync(
        BillOfLading billOfLading,
        IReadOnlyList<BLContainer> containers,
        IReadOnlyList<LocalCharge> concepts,
        DateOnly today,
        bool canAct,
        string? blockedReason,
        CancellationToken cancellationToken)
    {
        var rule = await FindAdvanceRuleAsync(billOfLading, today, cancellationToken);
        if (rule is null)
        {
            return new AdvanceDemurrageDto(
                false, AdvanceDemurrageStatus.NotRequired, null, null, null, null, null, false, null, ChargeActions.None, null);
        }

        var charge = concepts.FirstOrDefault(c => c.ChargeType == ChargeConceptCodes.AdvanceDemurrageBo);
        var status = charge is null
            ? AdvanceDemurrageStatus.NotRequested
            : charge.Status == ChargeStatus.Paid ? AdvanceDemurrageStatus.Paid : AdvanceDemurrageStatus.Pending;

        decimal? amount = charge?.TotalAmount;
        var currency = charge?.Currency;
        if (charge is null)
        {
            var quote = await QuoteAdvanceAsync(billOfLading, containers, today, cancellationToken);
            amount = quote?.Amount;
            currency = quote?.Currency;
        }

        var action = status == AdvanceDemurrageStatus.Paid ? ChargeActions.None : ChargeActions.AddToCart;

        return new AdvanceDemurrageDto(
            true,
            status,
            rule.Id,
            rule.Reason,
            amount,
            currency,
            charge?.Id,
            CldBlocked: status != AdvanceDemurrageStatus.Paid,
            CldBlockReason: status != AdvanceDemurrageStatus.Paid ? ProcessRequirements.AdvanceDemurrage : null,
            action,
            action != ChargeActions.None && !canAct ? blockedReason : null);
    }

    public async Task<InternalChargeRule?> FindAdvanceRuleAsync(BillOfLading billOfLading, DateOnly today, CancellationToken cancellationToken)
    {
        if (billOfLading.Country != CountryCodes.Bolivia || !DemurrageStateEvaluator.IsImport(billOfLading))
            return null;

        var account = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == billOfLading.ClientId, cancellationToken);
        return account is null
            ? null
            : await InternalChargeRuleMatcher.FindAsync(
                dbContext, InternalChargeRuleTypes.AdvanceDemurrageRequired, CountryCodes.Bolivia, account, today, cancellationToken);
    }

    /// <summary>
    /// Conceptos MHD y de demoras anticipadas. Lo pagado por adelantado se descuenta del MHD pendiente en
    /// orden, convertido a la moneda del MHD con el tipo de cambio de Nexus si difiere (M3-16, M5-05).
    /// </summary>
    private async Task<(IReadOnlyList<DemurrageConceptChargeDto> Concepts, MhdSummaryDto? Mhd)> BuildConceptsAsync(
        IReadOnlyList<LocalCharge> charges,
        IReadOnlyDictionary<string, ChargeConcept> catalog,
        AdvanceDemurrageDto advance,
        DateOnly today,
        bool canAct,
        string? blockedReason,
        CancellationToken cancellationToken)
    {
        var pendingMhd = charges.Where(c => c.ChargeType == ChargeConceptCodes.Mhd && c.Status != ChargeStatus.Paid).ToList();
        var mhdCurrency = pendingMhd.FirstOrDefault()?.Currency;

        var remaining = 0m;
        if (advance.Status == AdvanceDemurrageStatus.Paid && advance.Amount is > 0m && mhdCurrency is not null)
        {
            var quote = await exchangeRateService.GetQuoteAsync(advance.Currency!, mhdCurrency, today, cancellationToken);
            if (quote.IsSuccess)
                remaining = MoneyRounding.Round(advance.Amount.Value * quote.Value.Rate, mhdCurrency);
        }

        var items = new List<DemurrageConceptChargeDto>();
        var deducted = 0m;

        foreach (var charge in charges)
        {
            var deduction = 0m;
            if (charge.ChargeType == ChargeConceptCodes.Mhd && charge.Status != ChargeStatus.Paid && charge.Currency == mhdCurrency && remaining > 0m)
            {
                deduction = Math.Min(remaining, charge.TotalAmount);
                remaining -= deduction;
                deducted += deduction;
            }

            var payable = charge.Status == ChargeStatus.Paid ? 0m : charge.TotalAmount - deduction;
            var action = payable > 0m ? ChargeActions.AddToCart : ChargeActions.None;

            items.Add(new DemurrageConceptChargeDto(
                charge.Id,
                charge.ChargeType,
                catalog.GetValueOrDefault(charge.ChargeType)?.Name ?? charge.ChargeType,
                charge.Description,
                charge.Amount,
                charge.TaxAmount,
                charge.TotalAmount,
                charge.Currency,
                charge.Status,
                deduction,
                payable,
                action,
                action != ChargeActions.None && !canAct ? blockedReason : null));
        }

        MhdSummaryDto? mhd = null;
        if (mhdCurrency is not null)
        {
            var total = pendingMhd.Where(c => c.Currency == mhdCurrency).Sum(c => c.TotalAmount);
            mhd = new MhdSummaryDto(mhdCurrency, total, deducted, total - deducted);
        }

        return (items, mhd);
    }

    private async Task<CalculationInputsDto> CalculationInputsAsync(
        BillOfLading billOfLading,
        IReadOnlyList<BLContainer> containers,
        IReadOnlyCollection<DemurrageCharge> lines,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var (freeDays, source) = await ResolveFreeDaysAsync(billOfLading, lines, cancellationToken);
        var tariffs = await tariffResolver.GetInForceAsync(
            new TariffLookup(billOfLading.Country, ChargeConceptCodes.Demurrage, today), cancellationToken);

        return new CalculationInputsDto(
            billOfLading.ETA,
            freeDays,
            source,
            containers.Select(c => new CalculationContainerDto(c.ContainerNumber, c.ContainerType, c.Status)).ToList(),
            tariffs.Count > 0,
            today);
    }

    private static DemurrageLineDto ToLine(DemurrageCharge d) => new(
        d.Id, d.ContainerNumber, d.FreeDays, d.DemurrageDays, d.DailyRate, d.TotalAmount, d.Currency,
        d.StartDate, d.EndDate, d.Status, d.IsExempt, d.ExemptReason, d.InvoiceNumber, d.InvoicedAt, d.InvoiceDueDate);
}
