namespace HapagPortal.Application.ChargeRules.Common;

using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Implementación de <see cref="IChargeRulesService"/>. Nexus es la fuente de las condiciones (M8-02):
/// los mantenedores locales heredados <c>CreditClient</c> y <c>DemurrageExemption</c> no se consultan.
/// Si Nexus no responde, el resultado lo indica (<c>RulesAvailable = false</c>) y ningún cargo queda
/// habilitado para el carro, para no cobrar a un exento ni ocultar el IPO sin confirmar el crédito.
/// Las fechas de vigencia se resuelven en la fecha local del país (NF-22).
/// </summary>
public sealed class ChargeRulesService(
    IApplicationDbContext dbContext,
    IExemptionReader exemptionReader,
    ICreditConditionReader creditConditionReader,
    IExchangeRateService exchangeRateService,
    IResponsibilityLetterStatus responsibilityLetterStatus) : IChargeRulesService
{
    private const string ConsigneeRole = "Consignee";
    private const int MaxParentDepth = 3;

    private readonly Dictionary<Guid, CommercialConditionsDto> _conditions = [];
    private readonly Dictionary<(string TaxId, DateOnly At), Result<IReadOnlyList<ExemptionInfo>>> _exemptions = [];

    public async Task<CommercialConditionsDto> GetConditionsAsync(Client organization, CancellationToken cancellationToken = default)
    {
        if (_conditions.TryGetValue(organization.Id, out var cached))
            return cached;

        var taxId = TaxIdNormalizer.Normalize(organization.TaxId);
        var today = BusinessCalendar.LocalDate(organization.Country, DateTime.UtcNow);
        var read = await creditConditionReader.GetConditionsAsync(taxId, organization.MatchCode, cancellationToken);

        CommercialConditionsDto conditions;
        if (read.IsFailure)
        {
            conditions = new CommercialConditionsDto(
                false, RuleSources.Nexus, taxId, organization.MatchCode, false, null, [], null, null,
                false, false, false, read.Error.Code);
        }
        else
        {
            var credit = read.Value?.Credit is { } condition && TariffCalculator.IsInForce(condition.ValidFrom, condition.ValidTo, today)
                ? condition
                : null;
            var isFreightForwarder = read.Value?.IsFreightForwarder == true;

            conditions = new CommercialConditionsDto(
                true,
                RuleSources.Nexus,
                taxId,
                read.Value?.MatchCode ?? organization.MatchCode,
                credit is not null,
                credit?.CreditDays,
                credit?.Concepts ?? [],
                credit?.ValidFrom,
                credit?.ValidTo,
                isFreightForwarder,
                ResponsibilityLetterRequired: isFreightForwarder,
                IpoExcluded: credit is not null,
                ErrorCode: null,
                credit?.CreditLimit,
                credit?.CreditLimit is null ? null : credit.CreditLimitCurrency?.Trim().ToUpperInvariant());
        }

        _conditions[organization.Id] = conditions;
        return conditions;
    }

    public Task<Result<IReadOnlyList<ExemptionInfo>>> GetExemptionsAsync(
        Client organization,
        DateOnly at,
        CancellationToken cancellationToken = default) =>
        ReadExemptionsAsync(organization.TaxId, organization.MatchCode, at, cancellationToken);

    public async Task<ChargeRulesEvaluation> EvaluateAsync(
        BillOfLading billOfLading,
        Client payer,
        ShipmentPermissionSet permissions,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var country = billOfLading.Country;
        var today = BusinessCalendar.LocalDate(country, now);
        var localCurrency = CountryCodes.GetCurrency(country);

        var catalog = await dbContext.ChargeConcepts.AsNoTracking()
            .ToDictionaryAsync(c => c.Code, cancellationToken);

        var charges = (await dbContext.LocalCharges
                .Where(c => c.BillOfLadingId == billOfLading.Id)
                .ToListAsync(cancellationToken))
            .Where(c => CategoryOf(catalog, c.ChargeType) != ChargeCategories.Demurrage)
            .OrderBy(c => catalog.GetValueOrDefault(c.ChargeType)?.DisplayOrder ?? int.MaxValue)
            .ThenBy(c => c.ChargeType, StringComparer.Ordinal)
            .ToList();

        var conditions = await GetConditionsAsync(payer, cancellationToken);

        var needsExemptions = charges.Any(c => c.Status == ChargeStatus.Pending && IsExemptible(catalog, c.ChargeType));
        var figures = needsExemptions
            ? await LoadFiguresAsync(billOfLading, today, cancellationToken)
            : [];

        var applied = await dbContext.AppliedExemptions.AsNoTracking()
            .Where(a => a.BillOfLadingId == billOfLading.Id)
            .ToListAsync(cancellationToken);

        var rulesAvailable = conditions.Available && figures.All(f => f.Dto.Available);
        var pending = new List<PendingExemption>();
        var results = new List<RuledChargeDto>();

        foreach (var charge in charges)
        {
            // M4-03: el IPO no se presenta ni se agrega al carro de un cliente con crédito.
            if (charge.ChargeType == ChargeConceptCodes.Ipo && conditions.IpoExcluded)
                continue;

            var outcome = ChargeOutcomes.Payable;
            var payableBase = charge.Amount;
            var payableTax = charge.IsTaxable ? charge.TaxAmount : 0m;
            ExemptionTraceDto? trace = null;

            if (charge.Status == ChargeStatus.Paid)
            {
                outcome = ChargeOutcomes.Paid;
                payableBase = 0m;
                payableTax = 0m;
            }
            else if (charge.Status == ChargeStatus.CreditImputed)
            {
                // M5-10: imputado a la línea de crédito; no se paga ahora (queda en el estado de cuenta).
                outcome = ChargeOutcomes.CreditImputed;
                payableBase = 0m;
                payableTax = 0m;
            }
            else if (charge.Status == ChargeStatus.Exempt)
            {
                outcome = ChargeOutcomes.Exempt;
                payableBase = 0m;
                payableTax = 0m;
                trace = applied.Where(a => a.LocalChargeId == charge.Id).OrderByDescending(a => a.AppliedAt).Select(ToTrace).FirstOrDefault();
            }
            else if (IsExemptible(catalog, charge.ChargeType))
            {
                var match = FindExemption(figures, charge.ChargeType, today);
                if (match is not null)
                {
                    var exemptBase = await ExemptBaseAsync(match.Value.Info, charge, today, cancellationToken);
                    if (exemptBase is null)
                    {
                        rulesAvailable = false;
                    }
                    else
                    {
                        payableBase = charge.Amount - exemptBase.Value;
                        payableTax = charge.IsTaxable && payableBase > 0
                            ? MoneyRounding.Round(payableBase * charge.TaxRate / 100m, charge.Currency)
                            : 0m;
                        outcome = payableBase == 0m ? ChargeOutcomes.Exempt : ChargeOutcomes.PartiallyExempt;

                        var previous = applied.Where(a => a.LocalChargeId == charge.Id).OrderByDescending(a => a.AppliedAt).FirstOrDefault();
                        trace = new ExemptionTraceDto(
                            match.Value.Figure.Party,
                            match.Value.Figure.TaxId,
                            match.Value.Figure.MatchCode,
                            match.Value.Info.Concept,
                            match.Value.Info.Amount,
                            match.Value.Info.Currency,
                            match.Value.Info.ValidFrom,
                            match.Value.Info.ValidTo,
                            RuleSources.Nexus,
                            exemptBase.Value,
                            previous?.AppliedAt);

                        if (previous is null)
                            pending.Add(new PendingExemption(charge.Id, trace, payableBase == 0m));
                    }
                }
            }

            var payableTotal = payableBase + payableTax;
            var (action, blockedReason) = payableTotal > 0m
                ? ActionFor(permissions, rulesAvailable)
                : (ChargeActions.None, null);

            LocalCurrencyAmountDto? local = null;
            if (payableTotal > 0m && !string.Equals(charge.Currency, localCurrency, StringComparison.OrdinalIgnoreCase))
            {
                var quote = await exchangeRateService.GetQuoteAsync(charge.Currency, localCurrency, today, cancellationToken);
                if (quote.IsSuccess)
                {
                    local = new LocalCurrencyAmountDto(
                        localCurrency,
                        MoneyRounding.Round(payableTotal * quote.Value.Rate, localCurrency),
                        quote.Value.Rate,
                        quote.Value.EffectiveDate,
                        quote.Value.Source);
                }
            }

            results.Add(new RuledChargeDto(
                charge.Id,
                charge.ChargeType,
                catalog.GetValueOrDefault(charge.ChargeType)?.Name ?? charge.ChargeType,
                charge.Description,
                CategoryOf(catalog, charge.ChargeType),
                charge.Amount,
                charge.TaxAmount,
                charge.TotalAmount,
                charge.Currency,
                charge.Status,
                outcome,
                payableBase,
                payableTax,
                payableTotal,
                action,
                blockedReason,
                trace,
                local));
        }

        // Una condición recién descubierta como no disponible deshabilita también los cargos ya evaluados.
        if (!rulesAvailable)
        {
            results = results
                .Select(r => r.PayableTotal > 0m
                    ? r with { Action = ChargeActions.None, ActionBlockedReason = ChargeActionBlockReasons.RulesUnavailable }
                    : r)
                .ToList();
        }

        var totals = results
            .Where(r => r.PayableTotal > 0m)
            .GroupBy(r => r.Currency)
            .Select(g => new CurrencyTotalDto(g.Key, g.Sum(r => r.PayableAmount), g.Sum(r => r.PayableTaxAmount), g.Sum(r => r.PayableTotal)))
            .OrderBy(t => t.Currency, StringComparer.Ordinal)
            .ToList();

        var applicable = results.Where(r => r.Outcome is not (ChargeOutcomes.Paid or ChargeOutcomes.CreditImputed)).ToList();

        var requirements = new List<ProcessRequirementDto>();
        if (conditions.ResponsibilityLetterRequired)
        {
            var status = await responsibilityLetterStatus.GetStatusAsync(billOfLading.Id, payer.Id, cancellationToken);
            requirements.Add(new ProcessRequirementDto(
                ProcessRequirements.ResponsibilityLetter,
                status,
                BlocksProcess: status != ProcessRequirementStatus.Fulfilled,
                RuleSources.Nexus));
        }

        var dto = new ShipmentChargesDto(
            billOfLading.Id,
            billOfLading.BLNumber,
            country,
            billOfLading.ShipmentType,
            BusinessCalendar.TimeZoneId(country),
            new PartyDto(payer.Id, payer.Name, TaxIdNormalizer.Normalize(payer.TaxId), payer.MatchCode),
            conditions,
            figures.Select(f => f.Dto).ToList(),
            results,
            totals,
            AllApplicableExempt: applicable.Count > 0 && applicable.All(r => r.Outcome == ChargeOutcomes.Exempt),
            RequiresPayment: results.Any(r => r.PayableTotal > 0m),
            RulesAvailable: rulesAvailable,
            requirements,
            CanProceed: requirements.All(r => !r.BlocksProcess),
            EvaluatedAt: now);

        return new ChargeRulesEvaluation(dto, rulesAvailable ? pending : [], charges);
    }

    private static string CategoryOf(IReadOnlyDictionary<string, ChargeConcept> catalog, string code) =>
        catalog.GetValueOrDefault(code)?.Category ?? ChargeCategories.LocalCharge;

    private static bool IsExemptible(IReadOnlyDictionary<string, ChargeConcept> catalog, string code) =>
        catalog.GetValueOrDefault(code)?.NexusExemptible ?? ChargeConceptCodes.NexusExemptible.Contains(code);

    private static (string Action, string? Reason) ActionFor(ShipmentPermissionSet permissions, bool rulesAvailable)
    {
        if (!rulesAvailable)
            return (ChargeActions.None, ChargeActionBlockReasons.RulesUnavailable);

        if (permissions.RequiresAssociationForPayment)
            return (ChargeActions.None, ChargeActionBlockReasons.AssociationRequired);

        if (!permissions.CanExecute(ShipmentActionCodes.PayMandatoryLocalCharges))
            return (ChargeActions.None, ChargeActionBlockReasons.NoPermission);

        return (ChargeActions.AddToCart, null);
    }

    private static ExemptionTraceDto ToTrace(AppliedExemption a) => new(
        a.ExemptParty, a.PartyTaxId, a.PartyMatchCode, a.ConceptCode, a.ConditionAmount, a.ConditionCurrency,
        a.ConditionValidFrom, a.ConditionValidTo, a.Source, a.ExemptAmount, a.AppliedAt);

    /// <summary>
    /// Exención del concepto vigente para alguna figura; el consignatario del Master se revisa primero.
    /// Se prefiere una exención total a una parcial.
    /// </summary>
    private static (ExemptionFigureDto Figure, ExemptionInfo Info)? FindExemption(
        IReadOnlyList<(ExemptionFigureDto Dto, IReadOnlyList<ExemptionInfo> Items)> figures,
        string concept,
        DateOnly today)
    {
        var candidates = figures
            .SelectMany(f => f.Items
                .Where(i => string.Equals(i.Concept, concept, StringComparison.OrdinalIgnoreCase)
                    && TariffCalculator.IsInForce(i.ValidFrom, i.ValidTo, today))
                .Select(i => (Figure: f.Dto, Info: i)))
            .ToList();

        if (candidates.Count == 0)
            return null;

        return candidates.FirstOrDefault(c => c.Info.Amount is null) is { Figure: not null } full
            ? full
            : candidates.OrderByDescending(c => c.Info.Amount).First();
    }

    /// <summary>Base exenta en la moneda del cargo; nula si no se pudo convertir el monto de la condición.</summary>
    private async Task<decimal?> ExemptBaseAsync(ExemptionInfo info, LocalCharge charge, DateOnly today, CancellationToken cancellationToken)
    {
        if (info.Amount is null)
            return charge.Amount;

        var amount = info.Amount.Value;
        if (!string.IsNullOrWhiteSpace(info.Currency) && !string.Equals(info.Currency, charge.Currency, StringComparison.OrdinalIgnoreCase))
        {
            var quote = await exchangeRateService.GetQuoteAsync(info.Currency, charge.Currency, today, cancellationToken);
            if (quote.IsFailure)
                return null;

            amount = MoneyRounding.Round(amount * quote.Value.Rate, charge.Currency);
        }

        return Math.Min(Math.Max(amount, 0m), charge.Amount);
    }

    /// <summary>
    /// Figuras de M4-01: consignatario del BL Master (raíz de la cadena Hijo/Nieto) y cliente final (el
    /// consignatario del BL hijo o, en un BL Master, el titular del embarque).
    /// </summary>
    private async Task<IReadOnlyList<(ExemptionFigureDto Dto, IReadOnlyList<ExemptionInfo> Items)>> LoadFiguresAsync(
        BillOfLading billOfLading,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var root = billOfLading;
        for (var depth = 0; depth < MaxParentDepth && root.ParentBLId is not null; depth++)
        {
            var parentId = root.ParentBLId.Value;
            var parent = await dbContext.BillsOfLading.AsNoTracking().FirstOrDefaultAsync(b => b.Id == parentId, cancellationToken);
            if (parent is null)
                break;
            root = parent;
        }

        var ids = new[] { billOfLading.Id, root.Id };
        var consignees = await dbContext.BLParties.AsNoTracking()
            .Where(p => ids.Contains(p.BillOfLadingId) && p.Role == ConsigneeRole && p.TaxId != null)
            .ToListAsync(cancellationToken);

        var master = await PartyOrOwnerAsync(consignees.FirstOrDefault(p => p.BillOfLadingId == root.Id), root.ClientId, cancellationToken);
        var finalClient = root.Id == billOfLading.Id
            ? await PartyOrOwnerAsync(null, billOfLading.ClientId, cancellationToken)
            : await PartyOrOwnerAsync(consignees.FirstOrDefault(p => p.BillOfLadingId == billOfLading.Id), billOfLading.ClientId, cancellationToken);

        var figures = new List<(ExemptionFigureDto, IReadOnlyList<ExemptionInfo>)>();

        foreach (var (party, figure) in new[] { (ExemptionParties.MasterConsignee, master), (ExemptionParties.FinalClient, finalClient) })
        {
            if (figure is null)
                continue;

            var read = await ReadExemptionsAsync(figure.TaxId, figure.MatchCode, today, cancellationToken);
            IReadOnlyList<ExemptionInfo> items = read.IsSuccess ? read.Value : [];

            figures.Add((
                new ExemptionFigureDto(
                    party,
                    figure.Name,
                    figure.TaxId,
                    figure.MatchCode,
                    read.IsSuccess,
                    items.Select(i => new ExemptionConditionDto(i.Concept, i.Amount, i.Currency, i.ValidFrom, i.ValidTo)).ToList()),
                items));
        }

        return figures;
    }

    private async Task<PartyDto?> PartyOrOwnerAsync(BLParty? party, Guid ownerId, CancellationToken cancellationToken)
    {
        if (party?.TaxId is not null)
        {
            var organization = await dbContext.Clients.AsNoTracking()
                .FirstOrDefaultAsync(c => c.TaxId == party.TaxId, cancellationToken);
            return new PartyDto(organization?.Id, party.Name, TaxIdNormalizer.Normalize(party.TaxId), organization?.MatchCode);
        }

        var owner = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == ownerId, cancellationToken);
        return owner is null
            ? null
            : new PartyDto(owner.Id, owner.Name, TaxIdNormalizer.Normalize(owner.TaxId), owner.MatchCode);
    }

    private async Task<Result<IReadOnlyList<ExemptionInfo>>> ReadExemptionsAsync(
        string taxId,
        string? matchCode,
        DateOnly at,
        CancellationToken cancellationToken)
    {
        var normalized = TaxIdNormalizer.Normalize(taxId);
        if (_exemptions.TryGetValue((normalized, at), out var cached))
            return cached;

        var read = await exemptionReader.GetExemptionsAsync(normalized, matchCode, at, cancellationToken);
        _exemptions[(normalized, at)] = read;
        return read;
    }
}
