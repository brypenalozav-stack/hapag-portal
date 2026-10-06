namespace HapagPortal.Application.AccountPayments;

using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Cargos pendientes de un cliente con condición de crédito leída de Nexus (M5-07, M8-02), en su vista de
/// pago propia, separada del carro. El IPO no figura (M4-03). Cuando exista el estado de cuenta de M7-03
/// (Fase 2), esta vista se integra allí.
/// </summary>
public sealed record AccountPayablesDto(
    PaymentOrganizationDto Organization,
    CommercialConditionsDto Conditions,
    IReadOnlyList<PayableItemDto> Items,
    IReadOnlyList<CurrencyTotalDto> Totals,
    DateTime EvaluatedAt);

public sealed record GetAccountPayablesQuery : IQuery<AccountPayablesDto>;

public sealed record AccountPaymentItemRequest(string ItemType, Guid SourceId, string? BillingTaxId);

/// <summary>
/// Paga en una sola transacción los cargos y facturas elegidos de la vista de crédito (M5-07). La
/// confirmación libera la carga con la misma lógica que cualquier pago (NF-03).
/// </summary>
public sealed record PayFromAccountCommand(
    IReadOnlyList<AccountPaymentItemRequest> Items,
    string PaymentCurrency,
    string PaymentMethodCode,
    string? IdempotencyKey) : ICommand<CheckoutResultDto>;

public sealed class PayFromAccountCommandValidator : AbstractValidator<PayFromAccountCommand>
{
    public PayFromAccountCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleFor(x => x.Items.Count).LessThanOrEqualTo(200).WithName("Items");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ItemType)
                .Must(t => PayableItemTypes.All.Contains(t))
                .WithMessage("ItemType must be LocalCharge, Freight, Demurrage, WarehouseChange or Invoice.");
            item.RuleFor(i => i.SourceId).NotEmpty();
            item.RuleFor(i => i.BillingTaxId).MaximumLength(20);
        });
        RuleFor(x => x.PaymentCurrency).NotEmpty().Matches("^[A-Za-z]{3}$").WithMessage("PaymentCurrency must be an ISO 4217 code.");
        RuleFor(x => x.PaymentMethodCode).NotEmpty().MaximumLength(40);
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage(DomainErrors.PaymentFlow.IdempotencyKeyRequired.Message)
            .MaximumLength(100);
    }
}

internal static class AccountEligibility
{
    public static Result Check(PayerContext payer)
    {
        if (!payer.Conditions.Available)
            return Result.Failure(DomainErrors.ChargeRules.ConditionsUnavailable);

        return payer.Conditions.HasCredit
            ? Result.Success()
            : Result.Failure(DomainErrors.AccountPayment.NotCreditCustomer);
    }
}

public sealed class GetAccountPayablesQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    PayableItemResolver resolver)
    : IQueryHandler<GetAccountPayablesQuery, AccountPayablesDto>
{
    public async Task<Result<AccountPayablesDto>> Handle(GetAccountPayablesQuery request, CancellationToken cancellationToken)
    {
        var payer = await resolver.LoadPayerAsync(requireOperate: false, cancellationToken);
        if (payer.IsFailure)
            return Result<AccountPayablesDto>.Failure(payer.Error);

        var eligible = AccountEligibility.Check(payer.Value);
        if (eligible.IsFailure)
            return Result<AccountPayablesDto>.Failure(eligible.Error);

        var organization = payer.Value.Organization;
        var blIds = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), payer.Value.Scope)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        // Candidatos baratos de leer; cada uno se valida con las mismas reglas que el carro.
        var candidates = new List<(string Type, Guid Id)>();

        candidates.AddRange((await dbContext.LocalCharges.AsNoTracking()
                .Where(c => blIds.Contains(c.BillOfLadingId) && c.Status == ChargeStatus.Pending)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken))
            .Select(id => (PayableItemTypes.LocalCharge, id)));

        candidates.AddRange((await dbContext.BillsOfLading.AsNoTracking()
                .Where(b => blIds.Contains(b.Id) && b.FreightAmount > 0m && b.FreightPaidAt == null)
                .Select(b => b.Id)
                .ToListAsync(cancellationToken))
            .Select(id => (PayableItemTypes.Freight, id)));

        candidates.AddRange((await dbContext.DemurrageCharges.AsNoTracking()
                .Where(d => blIds.Contains(d.BillOfLadingId) && d.Status != DemurrageChargeStatus.Paid && d.InvoiceNumber == null && !d.IsExempt)
                .Select(d => d.Id)
                .ToListAsync(cancellationToken))
            .Select(id => (PayableItemTypes.Demurrage, id)));

        candidates.AddRange((await dbContext.WarehouseChanges.AsNoTracking()
                .Where(w => blIds.Contains(w.BillOfLadingId) && w.Status == WarehouseChangeStatus.PendingPayment && !w.IsFree
                    && w.RequestedByClientId == organization.Id)
                .Select(w => w.Id)
                .ToListAsync(cancellationToken))
            .Select(id => (PayableItemTypes.WarehouseChange, id)));

        candidates.AddRange((await dbContext.CustomerInvoices.AsNoTracking()
                .Where(i => i.OrganizationId == organization.Id && i.Status == InvoiceStatus.Pending && i.IsPayable)
                .Select(i => i.Id)
                .ToListAsync(cancellationToken))
            .Select(id => (PayableItemTypes.Invoice, id)));

        var items = new List<PayableItemDto>();
        foreach (var (type, id) in candidates)
        {
            var resolved = await resolver.ResolveAsync(payer.Value, type, id, null, cancellationToken);
            if (resolved.IsFailure)
            {
                // NF-11: sin Nexus no se presentan montos como definitivos.
                if (resolved.Error.Code == DomainErrors.ChargeRules.ConditionsUnavailable.Code)
                    return Result<AccountPayablesDto>.Failure(resolved.Error);
                continue;
            }

            items.Add(resolved.Value.ToDto());
        }

        var totals = items
            .GroupBy(i => i.Currency)
            .Select(g => new CurrencyTotalDto(g.Key, g.Sum(i => i.Amount), g.Sum(i => i.TaxAmount), g.Sum(i => i.TotalAmount)))
            .OrderBy(t => t.Currency, StringComparer.Ordinal)
            .ToList();

        return Result<AccountPayablesDto>.Success(new AccountPayablesDto(
            new PaymentOrganizationDto(organization.Id, organization.Name, TaxIdNormalizer.Normalize(organization.TaxId)),
            payer.Value.Conditions,
            items.OrderBy(i => i.BlNumber).ThenBy(i => i.ConceptCode).ToList(),
            totals,
            DateTime.UtcNow));
    }
}

public sealed class PayFromAccountCommandHandler(
    IApplicationDbContext dbContext,
    PayableItemResolver resolver,
    PaymentCheckoutService checkout)
    : ICommandHandler<PayFromAccountCommand, CheckoutResultDto>
{
    public async Task<Result<CheckoutResultDto>> Handle(PayFromAccountCommand request, CancellationToken cancellationToken)
    {
        var currency = request.PaymentCurrency.Trim().ToUpperInvariant();
        var methodCode = request.PaymentMethodCode.Trim().ToUpperInvariant();
        var requested = request.Items
            .GroupBy(i => (i.ItemType, i.SourceId))
            .Select(g => g.First())
            .ToList();

        // NF-01: la misma solicitud devuelve el mismo resultado (antes de validar: sus ítems ya están en el pago).
        var fingerprint = Fingerprint(currency, methodCode, requested);
        var replay = await checkout.ReplayAsync(request.IdempotencyKey!, fingerprint, cancellationToken);
        if (replay is not null)
            return replay;

        var payer = await resolver.LoadPayerAsync(requireOperate: true, cancellationToken);
        if (payer.IsFailure)
            return Result<CheckoutResultDto>.Failure(payer.Error);

        var eligible = AccountEligibility.Check(payer.Value);
        if (eligible.IsFailure)
            return Result<CheckoutResultDto>.Failure(eligible.Error);

        var lines = new List<CheckoutLine>();
        var ownTaxId = TaxIdNormalizer.Normalize(payer.Value.Organization.TaxId);

        foreach (var entry in requested)
        {
            var resolved = await resolver.ResolveAsync(payer.Value, entry.ItemType, entry.SourceId, null, cancellationToken);
            if (resolved.IsFailure)
                return Result<CheckoutResultDto>.Failure(resolved.Error);

            var item = resolved.Value;
            var taxId = entry.BillingTaxId is null
                ? item.BillingOptions.FirstOrDefault(o => o.TaxId == ownTaxId)?.TaxId ?? item.BillingOptions[0].TaxId
                : TaxIdNormalizer.Normalize(entry.BillingTaxId);
            var billing = item.BillingOptions.FirstOrDefault(o => o.TaxId == taxId);
            if (billing is null)
                return Result<CheckoutResultDto>.Failure(DomainErrors.Cart.InvalidItem(item.Label, DomainErrors.Cart.BillingTaxIdNotAllowed));

            if (!item.AllowedCurrencies.Contains(currency))
                return Result<CheckoutResultDto>.Failure(DomainErrors.Cart.InvalidItem(item.Label, DomainErrors.Cart.CurrencyNotAllowed(currency, item.AllowedCurrencies)));

            lines.Add(new CheckoutLine(item, billing.TaxId, billing.Name, null));
        }

        var countries = lines.Select(l => l.Item.Country).Distinct().ToList();
        if (countries.Count != 1)
            return Result<CheckoutResultDto>.Failure(DomainErrors.AccountPayment.MixedCountries);

        var country = countries[0];

        var open = await PaymentBlocks.EnsureOpenAsync(dbContext, country, DateTime.UtcNow, cancellationToken);
        if (open.IsFailure)
            return Result<CheckoutResultDto>.Failure(open.Error);

        var method = await checkout.MethodAsync(country, methodCode, currency, cancellationToken);
        if (method.IsFailure)
            return Result<CheckoutResultDto>.Failure(method.Error);

        return await checkout.CreateAsync(
            payer.Value, PaymentOrigins.Account, country, currency, method.Value, lines, request.IdempotencyKey!, fingerprint, cancellationToken);
    }

    private static string Fingerprint(string currency, string method, IEnumerable<AccountPaymentItemRequest> items)
    {
        var keys = string.Join(';', items.Select(i => $"{i.ItemType}:{i.SourceId:N}").Order(StringComparer.Ordinal));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(keys)))[..32];
        return $"{PaymentOrigins.Account}|{currency}|{method}|{hash}";
    }
}
