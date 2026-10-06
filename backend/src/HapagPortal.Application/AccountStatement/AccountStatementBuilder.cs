namespace HapagPortal.Application.AccountStatement;

using System.Globalization;
using HapagPortal.Application.AccountPayments;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Invoices;
using HapagPortal.Application.Payments.Settlements;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Payments;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Filtros y orden del estado de cuenta (M7-03).</summary>
public sealed record StatementFilter(
    Guid? OrganizationId,
    string? BlNumber,
    string? BookingNumber,
    DateOnly? From,
    DateOnly? To,
    string? Status,
    string? Currency,
    string? DocumentType,
    string? Sort,
    bool Descending);

/// <summary>
/// Arma el estado de cuenta de una organización (M7-03) con las piezas existentes, sin acceso directo a sistemas
/// de origen (NF-05: todo filtrado por los accesos del usuario):
/// <list type="bullet">
/// <item>Organizaciones y facturas visibles según M7-01 (<see cref="InvoiceAccess"/>): la propia y las que le
/// otorgaron acceso a sus facturas; la información no se mezcla entre ellas.</item>
/// <item>Facturado: facturas pendientes o vencidas (no notas de crédito, anuladas ni reemplazadas) y las cubiertas
/// por un anticipo (saldo cero, con el pago que las cubrió).</item>
/// <item>No facturado: recargos pendientes con las reglas de Nexus (exenciones M4-01/M4-02, IPO excluido con
/// crédito M4-03) y su monto real, cargos de servicios on demand, recargos de demurrage, líneas de demurrage
/// calculadas y no facturadas (coherente con los estados de M3-18) y flete pendiente, según la acción de la
/// matriz M1-11 de cada BL.</item>
/// <item>Imputado a crédito (M5-10) hasta que llega su factura, y anticipos con su cruce (M3-19, NF-04).</item>
/// <item>Antigüedad por tramos y aviso de vencimiento próximo configurables (<c>ConfigurationSetting</c>).</item>
/// </list>
/// </summary>
public sealed class AccountStatementBuilder(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    IChargeRulesService chargeRulesService,
    IExchangeRateService exchangeRateService,
    ICurrentUserService currentUserService)
{
    public const string CurrentBucket = "CURRENT";
    public const string ReasonLimitNotInformed = "LIMIT_NOT_INFORMED";
    public const string ReasonExchangeRateUnavailable = "EXCHANGE_RATE_UNAVAILABLE";
    public const string ChannelCart = "Cart";
    public const string ChannelAccount = "Account";

    private Dictionary<string, ChargeConcept> _catalog = [];

    private static readonly string[] OpenInvoiceTypes =
        [InvoiceDocumentTypes.Invoice, InvoiceDocumentTypes.ExemptInvoice, InvoiceDocumentTypes.DebitNote];

    public async Task<Result<AccountStatementDto>> BuildAsync(StatementFilter filter, CancellationToken cancellationToken)
    {
        var loaded = await InvoiceView.OrganizationAsync(dbContext, accessEvaluator, filter.OrganizationId, cancellationToken);
        if (loaded.IsFailure)
            return Result<AccountStatementDto>.Failure(loaded.Error);

        var (invoiceScope, organizationDto) = loaded.Value;
        var scope = invoiceScope.Scope;
        var organization = await dbContext.Clients.AsNoTracking().FirstAsync(c => c.Id == organizationDto.Id, cancellationToken);
        var country = organization.Country ?? CountryCodes.Chile;
        var now = DateTime.UtcNow;
        var today = BusinessCalendar.LocalDate(country, now);
        var taxId = TaxIdNormalizer.Normalize(organization.TaxId);

        var (boundaries, dueSoonDays) = await SettingsAsync(cancellationToken);
        var conditions = await chargeRulesService.GetConditionsAsync(organization, cancellationToken);

        // Quien paga es la organización del usuario (M5-01 / M5-07): su condición decide carro o vista de crédito.
        var own = scope.OrganizationId is { } ownId && !scope.IsAdmin && scope.OrganizationType != OrganizationTypes.Internal
            ? ownId == organization.Id ? organization : await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == ownId, cancellationToken)
            : null;
        var payerConditions = own is null ? null
            : own.Id == organization.Id ? conditions
            : await chargeRulesService.GetConditionsAsync(own, cancellationToken);
        var channel = payerConditions?.HasCredit == true ? ChannelAccount : ChannelCart;
        var canPay = own is not null && scope.CanOperate && payerConditions?.Available == true;
        var eligibility = await CreditImputationEligibility.LoadAsync(dbContext, cancellationToken);
        _catalog = await dbContext.ChargeConcepts.AsNoTracking().ToDictionaryAsync(c => c.Code, cancellationToken);

        var keys = await CartKeysAsync(scope, cancellationToken);
        var lines = new List<StatementLineDto>();

        // ── Facturado ────────────────────────────────────────────
        var allInvoices = await InvoiceAccess.Filter(dbContext.CustomerInvoices.AsNoTracking(), invoiceScope, organization.Id)
            .ToListAsync(cancellationToken);
        DateTime? lastUpdated = allInvoices.Count == 0 ? null : allInvoices.Max(i => i.SyncedAt);
        var coverage = await ChargeSettlements.CoverageAsync(
            dbContext, allInvoices.Where(i => i.Status == InvoiceStatus.Paid).Select(i => i.Id).ToList(), cancellationToken);
        var invoiceInFlight = await InFlightAsync(PayableItemTypes.Invoice, allInvoices.Select(i => i.Id).ToList(), cancellationToken);

        foreach (var invoice in allInvoices.Where(i => OpenInvoiceTypes.Contains(i.DocumentType)))
        {
            var covered = coverage.GetValueOrDefault(invoice.Id);
            if (invoice.Status != InvoiceStatus.Pending && covered is null)
                continue;

            var view = InvoiceView.ToDto(invoice, today, keys.Contains((PayableItemTypes.Invoice, invoice.Id)), covered);
            var overdue = view.Status == InvoiceStatus.Overdue;
            var balance = covered is null ? invoice.TotalAmount : 0m;
            var daysOverdue = overdue ? today.DayNumber - invoice.DueDate!.Value.DayNumber : (int?)null;
            var dueSoon = covered is null && !overdue && invoice.DueDate is { } due
                && due.DayNumber - today.DayNumber >= 0 && due.DayNumber - today.DayNumber <= dueSoonDays;
            var inPayment = invoiceInFlight.Contains(invoice.Id);

            lines.Add(new StatementLineDto(
                $"INV:{invoice.Id:N}",
                StatementLineKinds.Invoiced,
                invoice.DocumentType,
                covered is not null ? StatementStatuses.Covered : overdue ? StatementStatuses.Overdue : StatementStatuses.Pending,
                dueSoon,
                daysOverdue,
                covered is null ? Bucket(daysOverdue, boundaries) : null,
                PayableItemTypes.Invoice,
                invoice.Id,
                invoice.SiiNumber ?? invoice.SourceNumber,
                invoice.SiiNumber,
                invoice.SourceNumber,
                invoice.ConceptCode ?? PaymentConcepts.Invoice,
                ConceptName(invoice.ConceptCode ?? PaymentConcepts.Invoice),
                invoice.SiiNumber is null ? invoice.SourceNumber : $"{invoice.SourceNumber} (folio {invoice.SiiNumber})",
                invoice.BillOfLadingId,
                invoice.BlNumber,
                invoice.BookingNumber,
                invoice.LegalName,
                TaxIdNormalizer.Normalize(invoice.TaxId),
                invoice.IssueDate,
                invoice.IssueDate,
                invoice.DueDate,
                invoice.NetAmount,
                invoice.TaxAmount,
                invoice.TotalAmount,
                balance,
                invoice.Currency,
                null,
                null,
                covered,
                Payable: canPay && view.IsPayable && !inPayment,
                view.InCart,
                inPayment,
                CanImputeToCredit: false));
        }

        // ── No facturado ─────────────────────────────────────────
        var bls = await CandidateBlsAsync(invoiceScope, organization.Id, cancellationToken);
        var conditionsAvailable = conditions.Available;

        if (bls.Count > 0)
        {
            var blIds = bls.Select(b => b.Id).ToList();
            var charges = await dbContext.LocalCharges.AsNoTracking()
                .Where(c => blIds.Contains(c.BillOfLadingId) && c.Status == ChargeStatus.Pending)
                .ToListAsync(cancellationToken);
            var demurrage = await dbContext.DemurrageCharges.AsNoTracking()
                .Where(d => blIds.Contains(d.BillOfLadingId) && d.Status != DemurrageChargeStatus.Paid && d.InvoiceNumber == null && !d.IsExempt)
                .ToListAsync(cancellationToken);
            var freightPaid = (await dbContext.Payments.AsNoTracking()
                    .Where(p => p.BillOfLadingId != null && blIds.Contains(p.BillOfLadingId.Value)
                        && p.PaymentType == PayableItemTypes.Freight && p.Status == PaymentStatus.Confirmed)
                    .Select(p => p.BillOfLadingId!.Value)
                    .ToListAsync(cancellationToken))
                .ToHashSet();

            var chargeIds = charges.Select(c => c.Id).ToList();
            var serviceLinks = await dbContext.ServiceRequestCharges.AsNoTracking()
                .Where(l => chargeIds.Contains(l.LocalChargeId))
                .ToListAsync(cancellationToken);
            var requestIds = serviceLinks.Select(l => l.ServiceRequestId).Distinct().ToList();
            var requestNumbers = await dbContext.ServiceRequests.AsNoTracking()
                .Where(r => requestIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.RequestNumber, cancellationToken);
            var serviceOf = serviceLinks
                .GroupBy(l => l.LocalChargeId)
                .ToDictionary(g => g.Key, g => requestNumbers.GetValueOrDefault(g.First().ServiceRequestId));

            var chargeInFlight = await InFlightAsync(PayableItemTypes.LocalCharge, chargeIds, cancellationToken);
            var demurrageInFlight = await InFlightAsync(PayableItemTypes.Demurrage, demurrage.Select(d => d.Id).ToList(), cancellationToken);
            var freightInFlight = await InFlightAsync(PayableItemTypes.Freight, blIds, cancellationToken);

            foreach (var bl in bls.OrderBy(b => b.BLNumber, StringComparer.Ordinal))
            {
                var blCharges = charges.Where(c => c.BillOfLadingId == bl.Id).ToList();
                var blDemurrage = demurrage.Where(d => d.BillOfLadingId == bl.Id).ToList();
                var freightPending = bl.FreightAmount > 0m && bl.FreightPaidAt is null && !freightPaid.Contains(bl.Id);
                if (blCharges.Count == 0 && blDemurrage.Count == 0 && !freightPending)
                    continue;

                var permissions = await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken);
                var holder = new StatementHolder(bl, organization, taxId);

                if (blCharges.Count > 0
                    && (permissions.Can(ShipmentActionCodes.PayMandatoryLocalCharges) || permissions.Can(ShipmentActionCodes.PayOnDemandLocalCharges)))
                {
                    var ruled = new Dictionary<Guid, RuledChargeDto>();
                    if (blCharges.Any(c => CategoryOf(c.ChargeType) != ChargeCategories.Demurrage))
                    {
                        var evaluation = await chargeRulesService.EvaluateAsync(bl, organization, permissions, cancellationToken);
                        conditionsAvailable &= evaluation.Result.RulesAvailable;
                        foreach (var r in evaluation.Result.Charges)
                            ruled[r.ChargeId] = r;
                    }

                    foreach (var charge in blCharges)
                    {
                        var isDemurrage = CategoryOf(charge.ChargeType) == ChargeCategories.Demurrage;
                        decimal amount = charge.Amount, tax = charge.IsTaxable ? charge.TaxAmount : 0m;
                        if (!isDemurrage)
                        {
                            // M4-03: el IPO de un cliente con crédito no se presenta (no está en la evaluación).
                            if (!ruled.TryGetValue(charge.Id, out var rule))
                                continue;
                            if (rule.Outcome == ChargeOutcomes.Exempt || rule.PayableTotal <= 0m)
                                continue;

                            // Monto real del cargo tras las exenciones de Nexus; nunca cero por la condición de crédito.
                            amount = rule.PayableAmount;
                            tax = rule.PayableTaxAmount;
                        }
                        else if (!permissions.Can(ShipmentActionCodes.PayImportDemurrage))
                        {
                            continue;
                        }

                        var service = serviceOf.GetValueOrDefault(charge.Id);
                        var documentType = isDemurrage ? StatementDocumentTypes.Demurrage
                            : service is not null ? StatementDocumentTypes.ServiceCharge
                            : StatementDocumentTypes.LocalCharge;
                        var inPayment = chargeInFlight.Contains(charge.Id);
                        var imputable = channel == ChannelAccount && payerConditions is not null
                            && eligibility.IsEligible(payerConditions, PayableItemTypes.LocalCharge, bl.Country, charge.ChargeType);

                        lines.Add(Uninvoiced(
                            holder, $"CHG:{charge.Id:N}", documentType, PayableItemTypes.LocalCharge, charge.Id, charge.ChargeType,
                            charge.Description, BusinessCalendar.LocalDate(bl.Country, charge.CreatedAt == default ? now : charge.CreatedAt),
                            amount, tax, charge.Currency, service, keys, inPayment, canPay, imputable && !inPayment));
                    }
                }

                if (blDemurrage.Count > 0 && permissions.Can(ShipmentActionCodes.PayImportDemurrage))
                {
                    foreach (var line in blDemurrage)
                    {
                        lines.Add(Uninvoiced(
                            holder, $"DEM:{line.Id:N}", StatementDocumentTypes.Demurrage, PayableItemTypes.Demurrage, line.Id,
                            ChargeConceptCodes.Demurrage, $"Demurrage {line.ContainerNumber} ({line.DemurrageDays} d)",
                            BusinessCalendar.LocalDate(bl.Country, line.CreatedAt == default ? now : line.CreatedAt),
                            line.TotalAmount, 0m, line.Currency, null, keys, demurrageInFlight.Contains(line.Id), canPay, false));
                    }
                }

                if (freightPending && permissions.Can(ShipmentActionCodes.PayFreight))
                {
                    lines.Add(Uninvoiced(
                        holder, $"FRT:{bl.Id:N}", StatementDocumentTypes.Freight, PayableItemTypes.Freight, bl.Id,
                        PaymentConcepts.Freight, $"Flete {bl.PortOfLoading} - {bl.PortOfDischarge}",
                        BusinessCalendar.LocalDate(bl.Country, bl.CreatedAt == default ? now : bl.CreatedAt),
                        bl.FreightAmount, 0m, bl.FreightCurrency, null, keys, freightInFlight.Contains(bl.Id), canPay, false));
                }
            }
        }

        // ── Imputado a crédito y anticipos ──────────────────────
        var settlements = await SettlementsAsync(invoiceScope, organization.Id, taxId, cancellationToken);
        foreach (var credit in settlements.Where(s => s.Kind == SettlementKinds.CreditImputation && s.Status == SettlementStatus.Open))
        {
            var date = BusinessCalendar.LocalDate(credit.Country, credit.SettledAt);
            lines.Add(new StatementLineDto(
                $"CRI:{credit.Id:N}", StatementLineKinds.CreditImputed, DocumentTypeOf(credit.ItemType), StatementStatuses.CreditImputed,
                false, null, null, credit.ItemType, credit.SourceId, credit.PaymentNumber, null, null, credit.ConceptCode,
                ConceptName(credit.ConceptCode), credit.Description, credit.BillOfLadingId, credit.BlNumber, credit.BookingNumber,
                credit.BillingName ?? organization.Name, credit.BillingTaxId ?? taxId, date, null, null, credit.Amount, 0m,
                credit.Amount, credit.Amount, credit.Currency, null, credit.PaymentNumber, null, false, false, false, false));
        }

        var advances = settlements.Where(s => s.Kind == SettlementKinds.Advance).ToList();
        var summary = Summary(lines, advances);
        var aging = Aging(lines, boundaries);
        var creditDto = conditions.HasCredit ? await CreditAsync(conditions, lines, today, cancellationToken) : null;

        var filtered = Sort(lines.Where(l => Matches(l, filter)), filter).ToList();
        var filteredAdvances = advances
            .Where(a => MatchesSettlement(a, filter))
            .OrderByDescending(a => a.SettledAt)
            .ToList();

        return Result<AccountStatementDto>.Success(new AccountStatementDto(
            organizationDto,
            country,
            BusinessCalendar.TimeZoneId(country),
            today,
            now,
            lastUpdated,
            conditionsAvailable,
            conditions.HasCredit,
            creditDto,
            dueSoonDays,
            summary,
            aging,
            filtered,
            await ChargeSettlements.ToDtosAsync(dbContext, filteredAdvances, cancellationToken),
            new StatementActionsDto(channel, canPay, canPay && channel == ChannelAccount)));
    }

    private sealed record StatementHolder(BillOfLading Bl, Client Organization, string TaxId);

    private StatementLineDto Uninvoiced(
        StatementHolder holder,
        string key,
        string documentType,
        string itemType,
        Guid sourceId,
        string concept,
        string? description,
        DateOnly date,
        decimal amount,
        decimal tax,
        string currency,
        string? serviceRequest,
        HashSet<(string, Guid)> keys,
        bool inPayment,
        bool canPay,
        bool imputable) =>
        new(
            key, StatementLineKinds.Uninvoiced, documentType, StatementStatuses.Uninvoiced, false, null, null, itemType, sourceId,
            serviceRequest ?? concept, null, null, concept, ConceptName(concept), description, holder.Bl.Id, holder.Bl.BLNumber,
            holder.Bl.BookingNumber, holder.Organization.Name, holder.TaxId, date, null, null, amount, tax, amount + tax, amount + tax,
            currency, serviceRequest, null, null, canPay && !inPayment, keys.Contains((itemType, sourceId)), inPayment, imputable);

    private string CategoryOf(string code) => _catalog.GetValueOrDefault(code)?.Category ?? ChargeCategories.LocalCharge;

    private string ConceptName(string code) => code switch
    {
        PaymentConcepts.Freight => "Flete",
        PaymentConcepts.Invoice => "Factura",
        _ => _catalog.GetValueOrDefault(code)?.Name ?? code
    };

    private static string DocumentTypeOf(string itemType) => itemType switch
    {
        PayableItemTypes.Demurrage => StatementDocumentTypes.Demurrage,
        PayableItemTypes.Freight => StatementDocumentTypes.Freight,
        _ => StatementDocumentTypes.LocalCharge
    };

    /// <summary>Tramo por días vencidos: CURRENT si no vence; luego 1-30, 31-60, 61-90, 91+ con los límites configurados.</summary>
    public static string Bucket(int? daysOverdue, IReadOnlyList<int> boundaries)
    {
        if (daysOverdue is not { } days || days <= 0)
            return CurrentBucket;

        var from = 1;
        foreach (var to in boundaries)
        {
            if (days <= to)
                return $"{from}-{to}";
            from = to + 1;
        }

        return $"{from}+";
    }

    public static IReadOnlyList<StatementAgingBucketDto> Buckets(IReadOnlyList<int> boundaries)
    {
        var buckets = new List<StatementAgingBucketDto> { new(CurrentBucket, null, 0) };
        var from = 1;
        foreach (var to in boundaries)
        {
            buckets.Add(new StatementAgingBucketDto($"{from}-{to}", from, to));
            from = to + 1;
        }

        buckets.Add(new StatementAgingBucketDto($"{from}+", from, null));
        return buckets;
    }

    /// <summary>Límites de los tramos (ascendentes, positivos) y días de aviso; valores por omisión si no hay configuración válida.</summary>
    public async Task<(IReadOnlyList<int> Boundaries, int DueSoonDays)> SettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.ConfigurationSettings.AsNoTracking()
            .Where(s => s.Scope == ConfigurationScopes.Global
                && (s.Key == StatementSettingKeys.AgingBuckets || s.Key == StatementSettingKeys.DueSoonDays))
            .ToListAsync(cancellationToken);

        var boundaries = ParseBoundaries(settings.FirstOrDefault(s => s.Key == StatementSettingKeys.AgingBuckets)?.Value)
            ?? ParseBoundaries(StatementSettingKeys.DefaultAgingBuckets)!;
        var dueSoon = int.TryParse(settings.FirstOrDefault(s => s.Key == StatementSettingKeys.DueSoonDays)?.Value, NumberStyles.Integer,
            CultureInfo.InvariantCulture, out var days) && days is >= 0 and <= 90
            ? days
            : StatementSettingKeys.DefaultDueSoonDays;

        return (boundaries, dueSoon);
    }

    public static IReadOnlyList<int>? ParseBoundaries(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var numbers = new List<int>();
        foreach (var part in parts)
        {
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n <= 0)
                return null;
            numbers.Add(n);
        }

        return numbers.Count is > 0 and <= 10 && numbers.Zip(numbers.Skip(1)).All(p => p.First < p.Second) ? numbers : null;
    }

    private static IReadOnlyList<StatementSummaryDto> Summary(IReadOnlyList<StatementLineDto> lines, IReadOnlyList<ChargeSettlement> advances)
    {
        var currencies = lines.Select(l => l.Currency)
            .Concat(advances.Where(a => a.Status == SettlementStatus.Open).Select(a => a.Currency))
            .Distinct()
            .Order(StringComparer.Ordinal);

        return currencies.Select(currency =>
        {
            var of = lines.Where(l => l.Currency == currency).ToList();
            var invoiced = of.Where(l => l.Kind == StatementLineKinds.Invoiced && l.Status != StatementStatuses.Covered).ToList();
            var overdue = invoiced.Where(l => l.Status == StatementStatuses.Overdue).Sum(l => l.Balance);
            var dueSoon = invoiced.Where(l => l.DueSoon).Sum(l => l.Balance);
            var invoicedBalance = invoiced.Sum(l => l.Balance);
            var uninvoiced = of.Where(l => l.Kind == StatementLineKinds.Uninvoiced).Sum(l => l.Balance);
            var imputed = of.Where(l => l.Kind == StatementLineKinds.CreditImputed).Sum(l => l.Balance);
            var unapplied = advances.Where(a => a.Status == SettlementStatus.Open && a.Currency == currency).Sum(a => a.Amount);

            return new StatementSummaryDto(
                currency,
                invoicedBalance + uninvoiced + imputed,
                invoicedBalance,
                overdue,
                dueSoon,
                invoicedBalance - overdue,
                uninvoiced,
                imputed,
                unapplied);
        }).ToList();
    }

    private static StatementAgingDto Aging(IReadOnlyList<StatementLineDto> lines, IReadOnlyList<int> boundaries)
    {
        var buckets = Buckets(boundaries);
        var open = lines.Where(l => l.Kind == StatementLineKinds.Invoiced && l.Status != StatementStatuses.Covered).ToList();

        var rows = open
            .GroupBy(l => l.Currency)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g =>
            {
                var amounts = buckets.Select(b => g.Where(l => l.AgingBucket == b.Code).Sum(l => l.Balance)).ToList();
                return new StatementAgingRowDto(g.Key, amounts, amounts.Sum());
            })
            .ToList();

        return new StatementAgingDto(buckets, rows);
    }

    private async Task<StatementCreditDto> CreditAsync(
        CommercialConditionsDto conditions,
        IReadOnlyList<StatementLineDto> lines,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (conditions.CreditLimit is not { } limit || string.IsNullOrWhiteSpace(conditions.CreditLimitCurrency))
        {
            return new StatementCreditDto(conditions.CreditDays, conditions.CreditConcepts, conditions.CreditValidFrom, conditions.CreditValidTo,
                null, null, null, null, ReasonLimitNotInformed);
        }

        var limitCurrency = conditions.CreditLimitCurrency;
        var used = 0m;
        var consumed = lines
            .Where(l => (l.Kind == StatementLineKinds.Invoiced && l.Status != StatementStatuses.Covered) || l.Kind == StatementLineKinds.CreditImputed)
            .GroupBy(l => l.Currency);

        foreach (var group in consumed)
        {
            var quote = await exchangeRateService.GetQuoteAsync(group.Key, limitCurrency, today, cancellationToken);
            if (quote.IsFailure)
            {
                return new StatementCreditDto(conditions.CreditDays, conditions.CreditConcepts, conditions.CreditValidFrom, conditions.CreditValidTo,
                    limit, limitCurrency, null, null, ReasonExchangeRateUnavailable);
            }

            used += MoneyRounding.Round(group.Sum(l => l.Balance) * quote.Value.Rate, limitCurrency);
        }

        return new StatementCreditDto(conditions.CreditDays, conditions.CreditConcepts, conditions.CreditValidFrom, conditions.CreditValidTo,
            limit, limitCurrency, used, limit - used, null);
    }

    private static bool Matches(StatementLineDto line, StatementFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.BlNumber)
            && !string.Equals(line.BlNumber, filter.BlNumber.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrWhiteSpace(filter.BookingNumber)
            && !string.Equals(line.BookingNumber, filter.BookingNumber.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;
        if (filter.From is { } from && line.ReferenceDate < from)
            return false;
        if (filter.To is { } to && line.ReferenceDate > to)
            return false;
        if (!string.IsNullOrWhiteSpace(filter.Currency) && !string.Equals(line.Currency, filter.Currency.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrWhiteSpace(filter.DocumentType) && line.DocumentType != filter.DocumentType)
            return false;

        return filter.Status switch
        {
            null or "" => true,
            StatementStatuses.DueSoon => line.DueSoon,
            _ => line.Status == filter.Status
        };
    }

    private static bool MatchesSettlement(ChargeSettlement settlement, StatementFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.BlNumber)
            && !string.Equals(settlement.BlNumber, filter.BlNumber.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrWhiteSpace(filter.BookingNumber)
            && !string.Equals(settlement.BookingNumber, filter.BookingNumber.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrWhiteSpace(filter.Currency) && !string.Equals(settlement.Currency, filter.Currency.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        var date = BusinessCalendar.LocalDate(settlement.Country, settlement.SettledAt);
        return (filter.From is null || date >= filter.From) && (filter.To is null || date <= filter.To);
    }

    /// <summary>
    /// Orden por omisión: vencidas primero (más antigua primero), luego por vencimiento, las cubiertas, lo no
    /// facturado y lo imputado a crédito; o por fecha, monto o BL.
    /// </summary>
    private static IEnumerable<StatementLineDto> Sort(IEnumerable<StatementLineDto> lines, StatementFilter filter)
    {
        var sort = string.IsNullOrWhiteSpace(filter.Sort) ? StatementSortFields.DueDate : filter.Sort;
        if (sort == StatementSortFields.DueDate)
        {
            var ordered = lines
                .OrderBy(l => Rank(l))
                .ThenBy(l => l.DueDate ?? DateOnly.MaxValue)
                .ThenBy(l => l.BlNumber, StringComparer.Ordinal)
                .ThenBy(l => l.Number, StringComparer.Ordinal);
            return filter.Descending ? ordered.Reverse() : ordered;
        }

        Func<StatementLineDto, object?> key = sort switch
        {
            StatementSortFields.IssueDate => l => l.ReferenceDate,
            StatementSortFields.Amount => l => l.Balance,
            _ => l => l.BlNumber ?? string.Empty
        };

        return filter.Descending
            ? lines.OrderByDescending(key).ThenBy(l => l.Number, StringComparer.Ordinal)
            : lines.OrderBy(key).ThenBy(l => l.Number, StringComparer.Ordinal);
    }

    private static int Rank(StatementLineDto line) => line.Status switch
    {
        StatementStatuses.Overdue => 0,
        StatementStatuses.Pending => 1,
        StatementStatuses.Covered => 2,
        StatementStatuses.Uninvoiced => 3,
        _ => 4
    };

    /// <summary>
    /// BL de la organización con posibles cargos no facturados: los propios accesibles (no los vistos solo por un
    /// acceso otorgado, que pertenecen al estado de cuenta del mandante); de una organización mandante, los BL
    /// en que su acceso habilita ver facturas (M7-01); el administrador interno, los BL de la organización.
    /// </summary>
    private async Task<IReadOnlyList<BillOfLading>> CandidateBlsAsync(InvoiceScope invoiceScope, Guid organizationId, CancellationToken cancellationToken)
    {
        var scope = invoiceScope.Scope;
        if (scope.IsAdmin)
        {
            return await dbContext.BillsOfLading.AsNoTracking()
                .Where(b => b.ClientId == organizationId)
                .ToListAsync(cancellationToken);
        }

        if (organizationId != scope.OrganizationId)
        {
            if (!invoiceScope.BlFilter.TryGetValue(organizationId, out var visible) || visible is null || visible.Count == 0)
                return [];

            var ids = visible.ToList();
            return await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope)
                .Where(b => ids.Contains(b.Id))
                .ToListAsync(cancellationToken);
        }

        var accessible = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope).ToListAsync(cancellationToken);
        if (accessible.Count == 0)
            return [];

        var sources = await accessEvaluator.GetAccessSourcesAsync(scope, accessible, cancellationToken);
        return accessible
            .Where(b => sources.GetValueOrDefault(b.Id) != ShipmentAccessSources.Grant)
            .ToList();
    }

    /// <summary>Anticipos e imputaciones de la organización (pagadora o facturada), segregados como las facturas.</summary>
    private async Task<IReadOnlyList<ChargeSettlement>> SettlementsAsync(
        InvoiceScope invoiceScope,
        Guid organizationId,
        string taxId,
        CancellationToken cancellationToken)
    {
        var candidates = await dbContext.ChargeSettlements.AsNoTracking()
            .Where(s => s.PayerOrganizationId == organizationId || s.OnBehalfOfOrganizationId == organizationId || s.BillingTaxId != null)
            .ToListAsync(cancellationToken);

        var own = candidates
            .Where(s => s.PayerOrganizationId == organizationId
                || s.OnBehalfOfOrganizationId == organizationId
                || TaxIdNormalizer.AreEqual(s.BillingTaxId, taxId))
            .ToList();

        if (invoiceScope.Scope.IsAdmin || organizationId == invoiceScope.Scope.OrganizationId)
            return own;

        // Mandante: solo los BL en que su acceso habilita ver facturas.
        return invoiceScope.BlFilter.TryGetValue(organizationId, out var visible) && visible is not null
            ? own.Where(s => s.BillOfLadingId is { } blId && visible.Contains(blId)).ToList()
            : [];
    }

    private async Task<HashSet<(string, Guid)>> CartKeysAsync(AccessScope scope, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId || scope.OrganizationId is not { } organizationId)
            return [];

        var cartIds = dbContext.Carts.Where(c => c.UserId == userId && c.OrganizationId == organizationId).Select(c => c.Id);
        var items = await dbContext.CartItems.AsNoTracking()
            .Where(i => cartIds.Contains(i.CartId))
            .Select(i => new { i.ItemType, i.SourceId })
            .ToListAsync(cancellationToken);

        return items.Select(i => (i.ItemType, i.SourceId)).ToHashSet();
    }

    /// <summary>Fuentes incluidas en un pago en curso (NF-01): no se pueden volver a pagar ahora.</summary>
    private async Task<HashSet<Guid>> InFlightAsync(string itemType, IReadOnlyList<Guid> sourceIds, CancellationToken cancellationToken)
    {
        if (sourceIds.Count == 0)
            return [];

        var details = await dbContext.PaymentDetails.AsNoTracking()
            .Where(d => d.ItemType == itemType && d.SourceId != null && sourceIds.Contains(d.SourceId.Value))
            .Select(d => new { d.PaymentId, SourceId = d.SourceId!.Value })
            .ToListAsync(cancellationToken);
        if (details.Count == 0)
            return [];

        var paymentIds = details.Select(d => d.PaymentId).Distinct().ToList();
        var inFlight = (await dbContext.Payments.AsNoTracking()
                .Where(p => paymentIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Status })
                .ToListAsync(cancellationToken))
            .Where(p => PaymentStateMachine.IsInFlight(p.Status))
            .Select(p => p.Id)
            .ToHashSet();

        return details.Where(d => inFlight.Contains(d.PaymentId)).Select(d => d.SourceId).ToHashSet();
    }
}
