namespace HapagPortal.Application.ServiceRequests.Common;

using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Resultado de las condiciones de una definición sobre un BL.</summary>
public sealed record ServiceAvailability(bool Available, IReadOnlyList<string> Reasons);

/// <summary>Datos para calcular el cobro de un servicio sobre un BL en un instante (UTC, NF-22).</summary>
public sealed record ServiceQuoteInput(
    ServiceDefinition Definition,
    BillOfLading BillOfLading,
    Client Organization,
    ShipmentPermissionSet Permissions,
    IReadOnlyList<string> Containers,
    IReadOnlyDictionary<string, decimal> Numbers,
    DateTime Now,
    Guid? ExcludeRequestId = null);

/// <summary>Cobro calculado, con los cargos de origen vinculados y las exenciones de Nexus por registrar.</summary>
public sealed record ServiceQuoteComputation(
    ServiceQuoteDto Quote,
    IReadOnlyList<LocalCharge> SourceCharges,
    IReadOnlyList<PendingExemption> Exemptions);

/// <summary>
/// Motor estándar de los servicios on demand (M2-03, M2-04): decide si una definición aplica a un BL
/// (país, operación, estado, ventana del embarque, contenedores, hito vencido, solicitud vigente, cargo de
/// origen) y calcula su cobro reutilizando las piezas existentes:
/// <list type="bullet">
/// <item>tarifa vigente del mantenedor (<see cref="ITariffResolver"/>, M8-01) con tramos por tiempo desde un
/// hito del embarque medido en UTC con el calendario del país (NF-22), por un dato ingresado o por unidades;</item>
/// <item>plazo aduanero del BL o de su manifiesto (<c>DeadlineInstance</c>) cuando existe, para distinguir
/// dentro y fuera de plazo (M3-13, M3-14);</item>
/// <item>exenciones de Nexus por <see cref="IChargeRulesService"/>: las de la organización y del titular para el
/// concepto de la definición (XOM, M3-10) y, para cargos del sistema de origen, la misma evaluación de M3-01
/// y M4-01/M4-02 (Gate In, M3-15);</item>
/// <item>impuesto del país (<c>TaxConfiguration</c>) como en el certificado de transbordo (Ola E).</item>
/// </list>
/// </summary>
public sealed class ServiceCatalogEvaluator(
    IApplicationDbContext dbContext,
    ITariffResolver tariffResolver,
    IChargeRulesService chargeRulesService)
{
    public const string MilestoneSourceCustomsDeadline = "CUSTOMS_DEADLINE";
    public const string MilestoneSourceEtd = "ETD";
    public const string MilestoneSourceEta = "ETA";

    /// <summary>País, operación y permiso de la matriz para ver el servicio (sin ellos no se lista).</summary>
    public static bool Applies(ServiceDefinition definition, BillOfLading billOfLading, ShipmentPermissionSet permissions) =>
        definition.IsActive
        && Csv(definition.Countries).Contains(billOfLading.Country)
        && Csv(definition.Operations).Contains(ServiceOperations.Of(billOfLading.ShipmentType))
        && permissions.Can(definition.ActionCode);

    public static IReadOnlyList<string> Csv(string? csv) =>
        (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => v.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

    /// <summary>Condiciones dinámicas de la definición sobre el BL en el instante indicado.</summary>
    public async Task<ServiceAvailability> EvaluateAsync(
        ServiceDefinition definition,
        BillOfLading billOfLading,
        int containerCount,
        DateTime now,
        Guid? excludeRequestId,
        CancellationToken cancellationToken)
    {
        var reasons = new List<string>();

        var statuses = Csv(definition.RequiredBlStatuses);
        if (statuses.Count > 0 && !statuses.Contains(billOfLading.Status.ToUpperInvariant()))
            reasons.Add(ServiceUnavailableReasons.BlStatus);

        switch (definition.AvailabilityWindow)
        {
            case ServiceAvailabilityWindows.BeforeDeparture when billOfLading.ETD is { } etd && now >= etd:
                reasons.Add(ServiceUnavailableReasons.AlreadyDeparted);
                break;
            case ServiceAvailabilityWindows.AfterDeparture when billOfLading.ETD is not { } departure || now < departure:
                reasons.Add(ServiceUnavailableReasons.NotDeparted);
                break;
            case ServiceAvailabilityWindows.AfterArrival when billOfLading.ETA is not { } arrival || now < arrival:
                reasons.Add(ServiceUnavailableReasons.NotArrived);
                break;
        }

        if (definition.RequiresContainers && containerCount == 0)
            reasons.Add(ServiceUnavailableReasons.NoContainers);

        if (definition.TimingRule == ServiceTimingRules.LateOnly)
        {
            var milestone = await ResolveMilestoneAsync(definition, billOfLading, cancellationToken);
            if (milestone is null)
                reasons.Add(ServiceUnavailableReasons.MilestoneUnavailable);
            else if (now <= milestone.Value.At)
                reasons.Add(ServiceUnavailableReasons.NotOverdue);
        }

        if (!definition.AllowMultiplePerBl && await HasActiveRequestAsync(definition.Id, billOfLading.Id, excludeRequestId, cancellationToken))
            reasons.Add(ServiceUnavailableReasons.AlreadyRequested);

        if (definition.PricingMode == ServicePricingModes.SourceCharge
            && (await PendingSourceChargesAsync(definition, billOfLading, excludeRequestId, cancellationToken)).Count == 0)
            reasons.Add(ServiceUnavailableReasons.NoSourceCharge);

        return new ServiceAvailability(reasons.Count == 0, reasons);
    }

    /// <summary>Cobro del servicio en el instante de <see cref="ServiceQuoteInput.Now"/> (tramo vigente, NF-22).</summary>
    public async Task<Result<ServiceQuoteComputation>> QuoteAsync(ServiceQuoteInput input, CancellationToken cancellationToken)
    {
        var definition = input.Definition;
        var bl = input.BillOfLading;
        var country = bl.Country;
        var today = BusinessCalendar.LocalDate(country, input.Now);
        var timeZone = BusinessCalendar.TimeZoneId(country);

        return definition.PricingMode switch
        {
            ServicePricingModes.Tariff => await TariffQuoteAsync(input, today, timeZone, cancellationToken),
            ServicePricingModes.SourceCharge => await SourceChargeQuoteAsync(input, timeZone, cancellationToken),
            _ => Result<ServiceQuoteComputation>.Success(new ServiceQuoteComputation(
                Empty(definition, timeZone, input.Now) with { Quantity = 0 }, [], []))
        };
    }

    /// <summary>
    /// Hito desde el que se mide el tiempo: el plazo aduanero del BL o de su manifiesto (regla
    /// <c>DeadlineRuleCode</c>) si existe; si no, el zarpe o el arribo estimados más el desfase configurado.
    /// </summary>
    public async Task<(DateTime At, string Source)?> ResolveMilestoneAsync(
        ServiceDefinition definition,
        BillOfLading billOfLading,
        CancellationToken cancellationToken)
    {
        switch (definition.Milestone)
        {
            case ServiceMilestones.VesselDeparture:
                return billOfLading.ETD is { } etd ? (Utc(etd).AddHours(definition.MilestoneOffsetHours), MilestoneSourceEtd) : null;

            case ServiceMilestones.VesselArrival:
                return billOfLading.ETA is { } eta ? (Utc(eta).AddHours(definition.MilestoneOffsetHours), MilestoneSourceEta) : null;

            case ServiceMilestones.CustomsDeadline:
            {
                var deadline = await CustomsDeadlineAsync(definition.DeadlineRuleCode, billOfLading, cancellationToken);
                if (deadline is not null)
                    return (Utc(deadline.Value), MilestoneSourceCustomsDeadline);

                return billOfLading.ETD is { } departure
                    ? (Utc(departure).AddHours(definition.MilestoneOffsetHours), MilestoneSourceEtd)
                    : null;
            }

            default:
                return null;
        }
    }

    /// <summary>Cargos pendientes del concepto en el BL que ninguna otra solicitud vigente cobra.</summary>
    public async Task<IReadOnlyList<LocalCharge>> PendingSourceChargesAsync(
        ServiceDefinition definition,
        BillOfLading billOfLading,
        Guid? excludeRequestId,
        CancellationToken cancellationToken)
    {
        var charges = await dbContext.LocalCharges.AsNoTracking()
            .Where(c => c.BillOfLadingId == billOfLading.Id
                && c.ChargeType == definition.ChargeConceptCode
                && c.Status == ChargeStatus.Pending)
            .ToListAsync(cancellationToken);

        if (charges.Count == 0)
            return [];

        var linked = await LinkedChargeIdsAsync(charges.Select(c => c.Id).ToList(), excludeRequestId, cancellationToken);
        return charges.Where(c => !linked.Contains(c.Id)).ToList();
    }

    private async Task<HashSet<Guid>> LinkedChargeIdsAsync(List<Guid> chargeIds, Guid? excludeRequestId, CancellationToken cancellationToken)
    {
        var links = await dbContext.ServiceRequestCharges.AsNoTracking()
            .Where(l => chargeIds.Contains(l.LocalChargeId) && l.ServiceRequestId != excludeRequestId)
            .ToListAsync(cancellationToken);

        if (links.Count == 0)
            return [];

        var requestIds = links.Select(l => l.ServiceRequestId).Distinct().ToList();
        var active = await dbContext.ServiceRequests.AsNoTracking()
            .Where(r => requestIds.Contains(r.Id) && !ServiceRequestStatus.Released.Contains(r.Status))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        return links.Where(l => active.Contains(l.ServiceRequestId)).Select(l => l.LocalChargeId).ToHashSet();
    }

    private async Task<bool> HasActiveRequestAsync(Guid definitionId, Guid blId, Guid? excludeRequestId, CancellationToken cancellationToken) =>
        await dbContext.ServiceRequests.AsNoTracking()
            .AnyAsync(r => r.DefinitionId == definitionId
                && r.BillOfLadingId == blId
                && r.Id != excludeRequestId
                && r.Status != ServiceRequestStatus.Draft
                && !ServiceRequestStatus.Released.Contains(r.Status), cancellationToken);

    private async Task<DateTime?> CustomsDeadlineAsync(string? ruleCode, BillOfLading billOfLading, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ruleCode))
            return null;

        var ruleId = await dbContext.DeadlineRules.AsNoTracking()
            .Where(r => r.Code == ruleCode && r.IsActive)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (ruleId is null)
            return null;

        var own = await dbContext.DeadlineInstances.AsNoTracking()
            .Where(d => d.RuleId == ruleId && d.BillOfLadingId == billOfLading.Id)
            .OrderByDescending(d => d.DueAt)
            .Select(d => (DateTime?)d.DueAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (own is not null)
            return own;

        // Plazo a nivel de manifiesto: el del manifiesto en que se transmitió el BL.
        var manifestIds = await dbContext.CustomsTransmissions.AsNoTracking()
            .Where(t => t.BillOfLadingId == billOfLading.Id)
            .Select(t => t.ManifestId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (manifestIds.Count == 0)
            return null;

        return await dbContext.DeadlineInstances.AsNoTracking()
            .Where(d => d.RuleId == ruleId && d.BillOfLadingId == null && d.ManifestId != null && manifestIds.Contains(d.ManifestId.Value))
            .OrderByDescending(d => d.DueAt)
            .Select(d => (DateTime?)d.DueAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Result<ServiceQuoteComputation>> TariffQuoteAsync(
        ServiceQuoteInput input,
        DateOnly today,
        string timeZone,
        CancellationToken cancellationToken)
    {
        var definition = input.Definition;
        var bl = input.BillOfLading;
        var concept = definition.ChargeConceptCode!;

        // Hito y momento de la solicitud respecto de él (M3-13 dentro/fuera de plazo, M3-14 solo fuera de plazo).
        (DateTime At, string Source)? milestone = definition.Milestone == ServiceMilestones.None
            ? null
            : await ResolveMilestoneAsync(definition, bl, cancellationToken);

        var timing = ServiceTimings.NotApplicable;
        if (definition.TimingRule != ServiceTimingRules.None)
        {
            if (milestone is null)
                return Fail(DomainErrors.ServiceRequest.NotAvailable(ServiceUnavailableReasons.MilestoneUnavailable));

            timing = input.Now > milestone.Value.At ? ServiceTimings.Late : ServiceTimings.InTime;
            if (definition.TimingRule == ServiceTimingRules.LateOnly && timing == ServiceTimings.InTime)
                return Fail(DomainErrors.ServiceRequest.NotAvailable(ServiceUnavailableReasons.NotOverdue));
        }

        var tariffCode = timing == ServiceTimings.Late && !string.IsNullOrWhiteSpace(definition.LateTariffCode)
            ? definition.LateTariffCode
            : definition.TariffCode;

        // Unidades cobradas: una por solicitud o una por contenedor (sin los del embarcador si la regla lo excluye).
        var containers = await dbContext.BLContainers.AsNoTracking()
            .Where(c => c.BillOfLadingId == bl.Id)
            .ToListAsync(cancellationToken);
        var selected = containers
            .Where(c => input.Containers.Contains(c.ContainerNumber.ToUpperInvariant()))
            .OrderBy(c => c.ContainerNumber, StringComparer.Ordinal)
            .ToList();

        var excluded = new List<string>();
        List<BLContainer?> units;
        if (definition.QuantityMode == ServiceQuantityModes.PerContainer)
        {
            if (selected.Count == 0)
                return Fail(DomainErrors.ServiceRequest.ContainersRequired);

            if (definition.ExcludeShipperOwnedContainers)
                excluded.AddRange(selected.Where(c => c.IsShipperOwned).Select(c => c.ContainerNumber));

            units = selected.Where(c => !excluded.Contains(c.ContainerNumber)).Cast<BLContainer?>().ToList();
        }
        else
        {
            units = [null];
        }

        // Excepción de Nexus para el concepto (XOM): la organización solicitante o el titular del embarque.
        var exemption = await ExemptionAsync(definition, input.Organization, bl, today, cancellationToken);
        if (exemption.IsFailure)
            return Fail(exemption.Error);

        if (exemption.Value is not null || units.Count == 0)
        {
            var reference = exemption.Value ?? $"SOC:{string.Join(",", excluded)}";
            return Result<ServiceQuoteComputation>.Success(new ServiceQuoteComputation(
                Empty(definition, timeZone, input.Now) with
                {
                    Quantity = 0,
                    Timing = timing,
                    MilestoneAt = milestone?.At,
                    MilestoneSource = milestone?.Source,
                    IsExempt = true,
                    ExemptionReference = reference,
                    ExcludedContainers = excluded
                },
                [],
                []));
        }

        var holidays = await tariffResolver.GetHolidaysAsync(bl.Country, cancellationToken);
        var lines = new List<ServiceQuoteLineDto>();
        ResolvedTariff? first = null;
        int? measured = null;

        foreach (var unit in units)
        {
            var tariff = (await tariffResolver.GetInForceAsync(
                    new TariffLookup(bl.Country, concept, today, unit?.ContainerType, tariffCode),
                    cancellationToken))
                .FirstOrDefault();
            if (tariff is null)
                return Fail(DomainErrors.Tariff.NotInForce(concept, bl.Country));
            if (first is not null && tariff.Currency != first.Currency)
                return Fail(DomainErrors.ServiceDefinition.Invalid("The tariffs of the containers must share the currency."));
            first ??= tariff;

            var measure = Measure(definition, tariff, input, milestone?.At, units.Count, holidays);
            if (measure is null)
                return Fail(DomainErrors.ServiceRequest.MeasureRequired);
            measured = measure;

            var computation = TariffCalculator.Compute(tariff.Amount, tariff.TierMode, tariff.Tiers, measure.Value);
            if (!computation.Covered)
                return Fail(DomainErrors.Tariff.NotCovered);

            lines.Add(new ServiceQuoteLineDto(
                unit?.ContainerNumber,
                unit?.ContainerType,
                MoneyRounding.Round(computation.Amount, tariff.Currency),
                tariff.Code,
                computation.Lines));
        }

        var currency = first!.Currency;
        var amount = lines.Sum(l => l.Amount);
        var taxRate = definition.Taxable ? await TaxRateAsync(bl.Country, cancellationToken) : 0m;
        var tax = MoneyRounding.Round(amount * taxRate / 100m, currency);

        var quote = new ServiceQuoteDto(
            definition.PricingMode,
            concept,
            RequiresPayment: amount + tax > 0m,
            amount,
            tax,
            amount + tax,
            currency,
            taxRate,
            lines.Count,
            first.TierUnit,
            first.TierUnit == TariffTierUnits.None && definition.MeasureFieldKey is null ? null : measured,
            timing,
            milestone?.At,
            milestone?.Source,
            first.TariffId,
            first.Code,
            first.Source,
            IsExempt: false,
            ExemptionReference: null,
            excluded,
            lines,
            [],
            timeZone,
            input.Now);

        return Result<ServiceQuoteComputation>.Success(new ServiceQuoteComputation(quote, [], []));
    }

    /// <summary>
    /// Cargos del sistema de origen (M3-15 con la lectura de M3-01): se evalúan con las reglas de Nexus
    /// (M4-01, M4-02); si todos quedan exentos, el servicio no genera cobro y la condición queda reflejada.
    /// </summary>
    private async Task<Result<ServiceQuoteComputation>> SourceChargeQuoteAsync(
        ServiceQuoteInput input,
        string timeZone,
        CancellationToken cancellationToken)
    {
        var definition = input.Definition;
        var pending = await PendingSourceChargesAsync(definition, input.BillOfLading, input.ExcludeRequestId, cancellationToken);
        if (pending.Count == 0)
            return Fail(DomainErrors.ServiceRequest.NoSourceCharge);

        var evaluation = await chargeRulesService.EvaluateAsync(input.BillOfLading, input.Organization, input.Permissions, cancellationToken);
        if (!evaluation.Result.RulesAvailable)
            return Fail(DomainErrors.ChargeRules.ConditionsUnavailable);

        var ids = pending.Select(c => c.Id).ToHashSet();
        var ruled = evaluation.Result.Charges.Where(c => ids.Contains(c.ChargeId)).ToList();
        if (ruled.Count == 0)
            return Fail(DomainErrors.ServiceRequest.NoSourceCharge);

        var currency = ruled[0].Currency;
        if (ruled.Any(r => r.Currency != currency))
            return Fail(DomainErrors.ServiceDefinition.Invalid("The source charges of the service must share the currency."));

        var amount = ruled.Sum(r => r.PayableAmount);
        var tax = ruled.Sum(r => r.PayableTaxAmount);
        var exempt = ruled.All(r => r.Outcome == ChargeOutcomes.Exempt);
        var trace = ruled.Select(r => r.Exemption).FirstOrDefault(e => e is not null);
        var exemptions = evaluation.Exemptions.Where(e => ids.Contains(e.ChargeId)).ToList();
        var charges = evaluation.Charges.Where(c => ids.Contains(c.Id)).ToList();

        var quote = new ServiceQuoteDto(
            definition.PricingMode,
            definition.ChargeConceptCode,
            RequiresPayment: amount + tax > 0m,
            amount,
            tax,
            amount + tax,
            currency,
            ruled[0].TotalAmount > 0m && ruled[0].Amount > 0m ? Math.Round(ruled[0].TaxAmount * 100m / ruled[0].Amount, 2) : 0m,
            ruled.Count,
            null,
            null,
            ServiceTimings.NotApplicable,
            null,
            null,
            null,
            null,
            RuleSources.Nexus,
            exempt,
            trace is null ? null : $"{trace.Source}:{trace.Concept}:{trace.TaxId}",
            [],
            [],
            ruled.Select(r => new ServiceSourceChargeDto(
                r.ChargeId, r.ConceptCode, r.Description, r.Amount, r.TaxAmount, r.TotalAmount, r.Currency, r.Outcome, r.PayableTotal)).ToList(),
            timeZone,
            input.Now);

        return Result<ServiceQuoteComputation>.Success(new ServiceQuoteComputation(quote, charges, exemptions));
    }

    private async Task<Result<string?>> ExemptionAsync(
        ServiceDefinition definition,
        Client organization,
        BillOfLading billOfLading,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(definition.ExemptionConcept))
            return Result<string?>.Success(null);

        var parties = new List<Client> { organization };
        if (billOfLading.ClientId != organization.Id)
        {
            var owner = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == billOfLading.ClientId, cancellationToken);
            if (owner is not null)
                parties.Add(owner);
        }

        foreach (var party in parties)
        {
            var read = await chargeRulesService.GetExemptionsAsync(party, today, cancellationToken);
            if (read.IsFailure)
                return Result<string?>.Failure(DomainErrors.ChargeRules.ConditionsUnavailable);

            var match = read.Value.FirstOrDefault(e =>
                string.Equals(e.Concept, definition.ExemptionConcept, StringComparison.OrdinalIgnoreCase)
                && e.Amount is null
                && TariffCalculator.IsInForce(e.ValidFrom, e.ValidTo, today));

            if (match is not null)
                return Result<string?>.Success($"{RuleSources.Nexus}:{match.Concept}:{TaxIdNormalizer.Normalize(party.TaxId)}:{match.ValidFrom:yyyy-MM-dd}");
        }

        return Result<string?>.Success(null);
    }

    /// <summary>
    /// Medida de los tramos: el dato numérico ingresado, el tiempo transcurrido desde el hito en la unidad de
    /// la tarifa (UTC y calendario del país, NF-22) o la cantidad de unidades. Nulo si falta el dato.
    /// </summary>
    private static int? Measure(
        ServiceDefinition definition,
        ResolvedTariff tariff,
        ServiceQuoteInput input,
        DateTime? milestoneAt,
        int unitCount,
        IReadOnlySet<DateOnly> holidays)
    {
        if (!string.IsNullOrWhiteSpace(definition.MeasureFieldKey))
            return input.Numbers.TryGetValue(definition.MeasureFieldKey, out var value) ? (int)decimal.Floor(value) : null;

        if (TariffTierUnits.TimeBased.Contains(tariff.TierUnit))
        {
            return milestoneAt is null
                ? null
                : BusinessCalendar.Elapsed(tariff.TierUnit, input.BillOfLading.Country, milestoneAt.Value, input.Now, holidays);
        }

        return tariff.TierUnit == TariffTierUnits.Units ? unitCount : 1;
    }

    private async Task<decimal> TaxRateAsync(string country, CancellationToken cancellationToken) =>
        await dbContext.TaxConfigurations.AsNoTracking()
            .Where(t => t.Country == country && t.IsActive)
            .Select(t => (decimal?)t.TaxRate)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;

    private static ServiceQuoteDto Empty(ServiceDefinition definition, string timeZone, DateTime now) => new(
        definition.PricingMode, definition.ChargeConceptCode, false, 0m, 0m, 0m, null, 0m, 1, null, null,
        ServiceTimings.NotApplicable, null, null, null, null, null, false, null, [], [], [], timeZone, now);

    private static Result<ServiceQuoteComputation> Fail(Error error) => Result<ServiceQuoteComputation>.Failure(error);

    private static DateTime Utc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
