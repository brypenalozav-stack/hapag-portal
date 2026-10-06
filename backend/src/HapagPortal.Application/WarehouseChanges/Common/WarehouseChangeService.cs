namespace HapagPortal.Application.WarehouseChanges.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.InternalChargeRules;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Tarifa de cambio de almacén ofrecida (por ejemplo KTE o KTF), con su origen.</summary>
public sealed record WarehouseChangeTariffDto(
    string? Code,
    string? Description,
    decimal Amount,
    string Currency,
    string Source,
    DateOnly ValidFrom,
    DateOnly? ValidTo);

/// <summary>Derecho a cambio gratuito (M3-04): origen (NEXUS o PORTAL), referencia y usos por BL.</summary>
public sealed record FreeEntitlementDto(
    bool IsFree,
    string? Source,
    string? Reference,
    string? Reason,
    int? MaxUsesPerBl,
    int UsedOnBl);

/// <summary>Cotización del cambio de almacén para un BL: gratuito o tarifas vigentes (M3-04, M8-01).</summary>
public sealed record WarehouseChangeQuoteDto(
    string BlNumber,
    string Country,
    string? ContainerNumber,
    FreeEntitlementDto Entitlement,
    IReadOnlyList<WarehouseChangeTariffDto> Tariffs,
    string? DefaultTariffCode,
    bool CanRequest,
    string? BlockedReason);

/// <summary>Solicitud de cambio de almacén creada (individual o desde una solicitud masiva).</summary>
public sealed record WarehouseChangeDetailDto(
    Guid Id,
    Guid BillOfLadingId,
    string BlNumber,
    string? ContainerNumber,
    string FromWarehouse,
    string ToWarehouse,
    decimal Amount,
    string Currency,
    string Status,
    string Country,
    bool IsFree,
    bool RequiresPayment,
    string? TariffCode,
    string? TariffSource,
    string? EntitlementSource,
    string? EntitlementReference,
    Guid? BatchId,
    DateTime CreatedAt,
    DateTime? CompletedAt);

/// <summary>Datos de una solicitud: organización solicitante y BL ya autorizados por el llamador.</summary>
public sealed record WarehouseChangeInput(
    BillOfLading BillOfLading,
    Client Requester,
    Guid? RequestedByUserId,
    string? ContainerNumber,
    string? FromWarehouse,
    string ToWarehouse,
    string? TariffCode,
    Guid? BatchId);

/// <summary>
/// Cambio de almacén con autogestión del cambio gratuito (M3-04) y tarifa vigente del mantenedor
/// (M8-01, KTE/KTF). El derecho a cambio gratuito sale de Nexus (exención WAREHOUSE_CHANGE de la
/// organización, prevista en CT-NEXUS) o de una regla interna <c>FreeWarehouseChange</c> con límite de
/// usos por BL; en ese caso la solicitud se completa sin cobro y sin Customer Service. Sin derecho,
/// queda pendiente de pago con la tarifa vigente (por defecto, el primer código vigente: KTE).
/// </summary>
public sealed class WarehouseChangeService(
    IApplicationDbContext dbContext,
    IChargeRulesService chargeRulesService,
    ITariffResolver tariffResolver,
    IExchangeRateService exchangeRateService,
    IShipmentSource shipmentSource)
{
    public const string NotAvailableWarehouse = "N/D";

    public async Task<Result<WarehouseChangeQuoteDto>> QuoteAsync(
        BillOfLading billOfLading,
        Client requester,
        string? containerNumber,
        CancellationToken cancellationToken)
    {
        var container = await FindContainerAsync(billOfLading, containerNumber, cancellationToken);
        if (container.IsFailure)
            return Result<WarehouseChangeQuoteDto>.Failure(container.Error);

        var today = BusinessCalendar.LocalDate(billOfLading.Country, DateTime.UtcNow);
        var entitlement = await EvaluateEntitlementAsync(billOfLading, requester, today, cancellationToken);
        if (entitlement.IsFailure)
            return Result<WarehouseChangeQuoteDto>.Failure(entitlement.Error);

        var tariffs = await tariffResolver.GetInForceAsync(
            new TariffLookup(billOfLading.Country, ChargeConceptCodes.WarehouseChange, today, container.Value?.ContainerType),
            cancellationToken);

        var canRequest = entitlement.Value.IsFree || tariffs.Count > 0;

        return Result<WarehouseChangeQuoteDto>.Success(new WarehouseChangeQuoteDto(
            billOfLading.BLNumber,
            billOfLading.Country,
            container.Value?.ContainerNumber,
            entitlement.Value,
            tariffs.Select(t => new WarehouseChangeTariffDto(t.Code, t.Description, t.Amount, t.Currency, t.Source, t.ValidFrom, t.ValidTo)).ToList(),
            tariffs.FirstOrDefault()?.Code,
            canRequest,
            canRequest ? null : DomainErrors.Tariff.NotInForce(ChargeConceptCodes.WarehouseChange, billOfLading.Country).Code));
    }

    /// <summary>Crea la solicitud (sin guardar). El llamador ya validó acceso y permisos sobre el BL.</summary>
    public async Task<Result<WarehouseChange>> CreateAsync(WarehouseChangeInput input, CancellationToken cancellationToken)
    {
        var bl = input.BillOfLading;
        var container = await FindContainerAsync(bl, input.ContainerNumber, cancellationToken);
        if (container.IsFailure)
            return Result<WarehouseChange>.Failure(container.Error);

        var from = string.IsNullOrWhiteSpace(input.FromWarehouse)
            ? await CurrentDepotAsync(bl, cancellationToken)
            : input.FromWarehouse.Trim();
        var to = input.ToWarehouse.Trim();

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return Result<WarehouseChange>.Failure(DomainErrors.WarehouseChange.SameWarehouse);

        var now = DateTime.UtcNow;
        var today = BusinessCalendar.LocalDate(bl.Country, now);
        var entitlement = await EvaluateEntitlementAsync(bl, input.Requester, today, cancellationToken);
        if (entitlement.IsFailure)
            return Result<WarehouseChange>.Failure(entitlement.Error);

        var change = new WarehouseChange
        {
            BillOfLadingId = bl.Id,
            ContainerNumber = container.Value?.ContainerNumber,
            FromWarehouse = from,
            ToWarehouse = to,
            Country = bl.Country,
            Currency = CountryCodes.GetCurrency(bl.Country),
            Status = WarehouseChangeStatus.PendingPayment,
            RequestedByClientId = input.Requester.Id,
            RequestedByUserId = input.RequestedByUserId,
            BatchId = input.BatchId
        };

        if (entitlement.Value.IsFree)
        {
            change.Amount = 0m;
            change.IsFree = true;
            change.EntitlementSource = entitlement.Value.Source;
            change.EntitlementReference = entitlement.Value.Reference;
            change.Status = WarehouseChangeStatus.Completed;
            change.CompletedAt = now;
        }
        else
        {
            var tariff = (await tariffResolver.GetInForceAsync(
                    new TariffLookup(bl.Country, ChargeConceptCodes.WarehouseChange, today, container.Value?.ContainerType, input.TariffCode?.Trim().ToUpperInvariant()),
                    cancellationToken))
                .FirstOrDefault();

            if (tariff is null)
                return Result<WarehouseChange>.Failure(DomainErrors.Tariff.NotInForce(ChargeConceptCodes.WarehouseChange, bl.Country));

            change.Amount = MoneyRounding.Round(TariffCalculator.Compute(tariff.Amount, tariff.TierMode, tariff.Tiers, 1).Amount, tariff.Currency);
            change.Currency = tariff.Currency;
            change.TariffCode = tariff.Code;
            change.TariffSource = tariff.Source;

            var localCurrency = CountryCodes.GetCurrency(bl.Country);
            if (change.Currency != localCurrency)
            {
                var rate = await exchangeRateService.GetQuoteAsync(change.Currency, localCurrency, today, cancellationToken);
                if (rate.IsFailure)
                    return Result<WarehouseChange>.Failure(rate.Error);

                exchangeRateService.Record(
                    ExchangeRateTransactionTypes.WarehouseChange, change.Id, rate.Value, change.Amount,
                    MoneyRounding.Round(change.Amount * rate.Value.Rate, localCurrency));
            }
        }

        dbContext.WarehouseChanges.Add(change);
        return Result<WarehouseChange>.Success(change);
    }

    /// <summary>
    /// Derecho a cambio gratuito: primero la exención de Nexus (concepto WAREHOUSE_CHANGE), luego la regla
    /// interna vigente de la cuenta si todavía tiene usos disponibles en el BL.
    /// </summary>
    public async Task<Result<FreeEntitlementDto>> EvaluateEntitlementAsync(
        BillOfLading billOfLading,
        Client requester,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var used = await dbContext.WarehouseChanges.AsNoTracking()
            .CountAsync(w => w.BillOfLadingId == billOfLading.Id && w.IsFree && w.Status != WarehouseChangeStatus.Cancelled, cancellationToken);

        var exemptions = await chargeRulesService.GetExemptionsAsync(requester, today, cancellationToken);
        var nexus = exemptions.IsSuccess
            ? exemptions.Value.FirstOrDefault(e =>
                string.Equals(e.Concept, ChargeConceptCodes.WarehouseChange, StringComparison.OrdinalIgnoreCase)
                && TariffCalculator.IsInForce(e.ValidFrom, e.ValidTo, today)
                && e.Amount is null)
            : null;

        if (nexus is not null)
        {
            return Result<FreeEntitlementDto>.Success(new FreeEntitlementDto(
                true, RuleSources.Nexus, $"NEXUS:{ChargeConceptCodes.WarehouseChange}:{nexus.ValidFrom:yyyy-MM-dd}", null, null, used));
        }

        var rule = await InternalChargeRuleMatcher.FindAsync(
            dbContext, InternalChargeRuleTypes.FreeWarehouseChange, billOfLading.Country, requester, today, cancellationToken);

        if (rule is not null && (rule.MaxUsesPerBl is null || used < rule.MaxUsesPerBl))
        {
            return Result<FreeEntitlementDto>.Success(new FreeEntitlementDto(
                true, RuleSources.Portal, $"RULE:{rule.Id}", rule.Reason, rule.MaxUsesPerBl, used));
        }

        // Sin regla del portal que lo conceda, una falla de Nexus no puede confirmarse como "sin derecho".
        if (exemptions.IsFailure)
            return Result<FreeEntitlementDto>.Failure(DomainErrors.ChargeRules.ConditionsUnavailable);

        return Result<FreeEntitlementDto>.Success(new FreeEntitlementDto(
            false, null, null, rule?.Reason, rule?.MaxUsesPerBl, used));
    }

    public static WarehouseChangeDetailDto ToDetail(WarehouseChange change, string blNumber) => new(
        change.Id,
        change.BillOfLadingId,
        blNumber,
        change.ContainerNumber,
        change.FromWarehouse,
        change.ToWarehouse,
        change.Amount,
        change.Currency,
        change.Status,
        change.Country,
        change.IsFree,
        RequiresPayment: !change.IsFree && change.Status == WarehouseChangeStatus.PendingPayment,
        change.TariffCode,
        change.TariffSource,
        change.EntitlementSource,
        change.EntitlementReference,
        change.BatchId,
        change.CreatedAt,
        change.CompletedAt);

    private async Task<Result<BLContainer?>> FindContainerAsync(BillOfLading billOfLading, string? containerNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(containerNumber))
            return Result<BLContainer?>.Success(null);

        var number = containerNumber.Trim().ToUpperInvariant();
        var container = await dbContext.BLContainers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.BillOfLadingId == billOfLading.Id && c.ContainerNumber.ToUpper() == number, cancellationToken);

        return container is null
            ? Result<BLContainer?>.Failure(DomainErrors.WarehouseChange.ContainerNotFound(number))
            : Result<BLContainer?>.Success(container);
    }

    /// <summary>Almacén actual: depósito de importación informado por FIS (CT-FIS) o "N/D".</summary>
    private async Task<string> CurrentDepotAsync(BillOfLading billOfLading, CancellationToken cancellationToken)
    {
        var source = await shipmentSource.GetByBlNumberAsync(billOfLading.BLNumber, cancellationToken);
        return source.IsSuccess && !string.IsNullOrWhiteSpace(source.Value?.DepotImport)
            ? source.Value!.DepotImport!
            : NotAvailableWarehouse;
    }
}
