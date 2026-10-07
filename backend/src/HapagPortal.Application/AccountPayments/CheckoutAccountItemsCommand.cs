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
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Ítem elegido en la vista de crédito con su forma de pago (M5-10): <c>PayNow</c> (predeterminada) o <c>Credit</c>.</summary>
public sealed record AccountCheckoutItemRequest(string ItemType, Guid SourceId, string? BillingTaxId, string? Mode);

/// <summary>Imputación a la línea de crédito registrada (M5-10): número <c>CRI-</c>, moneda del cargo y sus ítems.</summary>
public sealed record CreditImputationDto(
    Guid Id,
    string Number,
    string Status,
    string Country,
    string Currency,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    DateTime ImputedAt,
    IReadOnlyList<PaymentItemDto> Items);

/// <summary>
/// Resultado del cierre con forma de pago por ítem (M5-10): el pago inmediato (si hubo ítems a pagar ahora) y una
/// imputación a crédito por moneda, con el total dividido entre ambos.
/// </summary>
public sealed record AccountCheckoutResultDto(
    CheckoutResultDto? Payment,
    IReadOnlyList<CreditImputationDto> CreditImputations,
    IReadOnlyList<CurrencyTotalDto> PayNowTotals,
    IReadOnlyList<CurrencyTotalDto> CreditTotals,
    bool Replayed);

/// <summary>
/// Cierre de la vista de pago de un cliente con crédito vigente (M5-07, M5-10, desde el estado de cuenta M7-03):
/// cada ítem se paga ahora o se imputa a la línea de crédito cuando su concepto es elegible
/// (<see cref="CreditImputationEligibility"/>). Lo pagado ahora es un pago normal (una transacción, liberación
/// por la cola); lo imputado se registra sin cobro y libera la carga igual (NF-03). Los clientes sin crédito no
/// usan esta vista (<c>AccountPayment.NotCreditCustomer</c>). La cabecera <c>Idempotency-Key</c> identifica la
/// solicitud completa (NF-01).
/// </summary>
public sealed record CheckoutAccountItemsCommand(
    IReadOnlyList<AccountCheckoutItemRequest> Items,
    string? PaymentCurrency,
    string? PaymentMethodCode,
    string? IdempotencyKey) : ICommand<AccountCheckoutResultDto>;

public sealed class CheckoutAccountItemsCommandValidator : AbstractValidator<CheckoutAccountItemsCommand>
{
    public CheckoutAccountItemsCommandValidator()
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
            item.RuleFor(i => i.Mode)
                .Must(m => m is null || AccountPaymentModes.All.Contains(m))
                .WithMessage("Mode must be PayNow or Credit.");
        });
        RuleFor(x => x.PaymentCurrency).Matches("^[A-Za-z]{3}$").When(x => !string.IsNullOrWhiteSpace(x.PaymentCurrency))
            .WithMessage("PaymentCurrency must be an ISO 4217 code.");
        RuleFor(x => x.PaymentMethodCode).MaximumLength(40);
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage(DomainErrors.PaymentFlow.IdempotencyKeyRequired.Message)
            .MaximumLength(80);
    }
}

public sealed class CheckoutAccountItemsCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    PayableItemResolver resolver,
    PaymentCheckoutService checkout)
    : ICommandHandler<CheckoutAccountItemsCommand, AccountCheckoutResultDto>
{
    /// <summary>Sufijo de la clave de cada imputación (una por moneda) respecto de la clave de la solicitud.</summary>
    public const string CreditKeySuffix = "#CRI-";

    public async Task<Result<AccountCheckoutResultDto>> Handle(CheckoutAccountItemsCommand request, CancellationToken cancellationToken)
    {
        var key = request.IdempotencyKey!.Trim();
        var currency = request.PaymentCurrency?.Trim().ToUpperInvariant();
        var methodCode = request.PaymentMethodCode?.Trim().ToUpperInvariant();
        var requested = request.Items
            .GroupBy(i => (i.ItemType, i.SourceId))
            .Select(g => g.First())
            .ToList();
        var fingerprint = Fingerprint(currency, methodCode, requested);

        // NF-01: la misma solicitud devuelve el mismo resultado sin volver a validar ni cobrar.
        var replay = await ReplayAsync(key, fingerprint, cancellationToken);
        if (replay is not null)
            return replay;

        var payer = await resolver.LoadPayerAsync(requireOperate: true, cancellationToken);
        if (payer.IsFailure)
            return Result<AccountCheckoutResultDto>.Failure(payer.Error);

        var eligible = AccountEligibility.Check(payer.Value);
        if (eligible.IsFailure)
            return Result<AccountCheckoutResultDto>.Failure(eligible.Error);

        var credit = await CreditImputationEligibility.LoadAsync(dbContext, cancellationToken);
        var ownTaxId = TaxIdNormalizer.Normalize(payer.Value.Organization.TaxId);
        var payNow = new List<CheckoutLine>();
        var imputed = new List<CheckoutLine>();

        foreach (var entry in requested)
        {
            var resolved = await resolver.ResolveAsync(payer.Value, entry.ItemType, entry.SourceId, null, cancellationToken);
            if (resolved.IsFailure)
                return Result<AccountCheckoutResultDto>.Failure(resolved.Error);

            var item = resolved.Value;
            var taxId = entry.BillingTaxId is null
                ? item.BillingOptions.FirstOrDefault(o => o.TaxId == ownTaxId)?.TaxId ?? item.BillingOptions[0].TaxId
                : TaxIdNormalizer.Normalize(entry.BillingTaxId);
            var billing = item.BillingOptions.FirstOrDefault(o => o.TaxId == taxId);
            if (billing is null)
                return Result<AccountCheckoutResultDto>.Failure(DomainErrors.Cart.InvalidItem(item.Label, DomainErrors.Cart.BillingTaxIdNotAllowed));

            var line = new CheckoutLine(item, billing.TaxId, billing.Name, null);
            if (entry.Mode == AccountPaymentModes.Credit)
            {
                if (!credit.IsEligible(payer.Value.Conditions, item.ItemType, item.Country, item.ConceptCode))
                    return Result<AccountCheckoutResultDto>.Failure(DomainErrors.Cart.InvalidItem(item.Label, DomainErrors.AccountPayment.CreditNotEligible));

                imputed.Add(line);
                continue;
            }

            if (currency is null || methodCode is null)
                return Result<AccountCheckoutResultDto>.Failure(DomainErrors.AccountPayment.PaymentDataRequired);
            if (!item.AllowedCurrencies.Contains(currency))
                return Result<AccountCheckoutResultDto>.Failure(DomainErrors.Cart.InvalidItem(item.Label, DomainErrors.Cart.CurrencyNotAllowed(currency, item.AllowedCurrencies)));

            payNow.Add(line);
        }

        var countries = payNow.Concat(imputed).Select(l => l.Item.Country).Distinct().ToList();
        if (countries.Count != 1)
            return Result<AccountCheckoutResultDto>.Failure(DomainErrors.AccountPayment.MixedCountries);

        var country = countries[0];
        PaymentMethodConfig? method = null;
        if (payNow.Count > 0)
        {
            // M8-07: el bloqueo por horario afecta solo a lo que se paga ahora.
            var open = await PaymentBlocks.EnsureOpenAsync(dbContext, country, DateTime.UtcNow, cancellationToken);
            if (open.IsFailure)
                return Result<AccountCheckoutResultDto>.Failure(open.Error);

            var available = await checkout.MethodAsync(country, methodCode!, currency!, cancellationToken);
            if (available.IsFailure)
                return Result<AccountCheckoutResultDto>.Failure(available.Error);
            method = available.Value;
        }

        // Primero el pago: si la plataforma no responde no se imputa nada y la solicitud se reintenta con otra clave.
        CheckoutResultDto? paid = null;
        if (payNow.Count > 0)
        {
            var created = await checkout.CreateAsync(
                payer.Value, PaymentOrigins.Account, country, currency!, method!, payNow, key, fingerprint, cancellationToken);
            if (created.IsFailure)
                return Result<AccountCheckoutResultDto>.Failure(created.Error);
            paid = created.Value;
        }

        var imputations = new List<Payment>();
        foreach (var group in imputed.GroupBy(l => l.Item.Currency).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            imputations.Add(await checkout.CreateCreditImputationAsync(
                payer.Value, country, group.Key, group.ToList(), $"{key}{CreditKeySuffix}{group.Key}", fingerprint, cancellationToken));
        }

        return Result<AccountCheckoutResultDto>.Success(await ResultAsync(paid, imputations, replayed: false, cancellationToken));
    }

    private async Task<Result<AccountCheckoutResultDto>?> ReplayAsync(string key, string fingerprint, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
            return Result<AccountCheckoutResultDto>.Failure(Error.Unauthorized);

        var creditPrefix = key + CreditKeySuffix;
        var existing = await dbContext.Payments.AsNoTracking()
            .Where(p => p.CreatedByUserId == userId.Value && p.IdempotencyKey != null
                && (p.IdempotencyKey == key || p.IdempotencyKey.StartsWith(creditPrefix)))
            .ToListAsync(cancellationToken);
        if (existing.Count == 0)
            return null;

        if (existing.Any(p => !string.Equals(p.RequestFingerprint, fingerprint, StringComparison.Ordinal)))
            return Result<AccountCheckoutResultDto>.Failure(DomainErrors.PaymentFlow.IdempotencyConflict);

        var paid = existing.FirstOrDefault(p => p.IdempotencyKey == key);
        if (paid is { Status: PaymentStatus.Failed, FailureReason: PaymentFailureReasons.ProviderUnavailable })
            return Result<AccountCheckoutResultDto>.Failure(DomainErrors.PaymentFlow.ProviderUnavailable);

        var paidDto = paid is null ? null : await checkout.ResultAsync(paid, replayed: true, cancellationToken);
        var imputations = existing.Where(p => p.Origin == PaymentOrigins.CreditLine).ToList();
        return Result<AccountCheckoutResultDto>.Success(await ResultAsync(paidDto, imputations, replayed: true, cancellationToken));
    }

    private async Task<AccountCheckoutResultDto> ResultAsync(
        CheckoutResultDto? paid,
        IReadOnlyList<Payment> imputations,
        bool replayed,
        CancellationToken cancellationToken)
    {
        var ids = imputations.Select(p => p.Id).ToList();
        var details = (await dbContext.PaymentDetails.AsNoTracking()
                .Where(d => ids.Contains(d.PaymentId))
                .ToListAsync(cancellationToken))
            .ToLookup(d => d.PaymentId);

        var credit = imputations.Select(p => new CreditImputationDto(
                p.Id, p.PaymentNumber, p.Status, p.Country, p.Currency, p.Amount, p.TaxAmount, p.TotalAmount,
                p.ConfirmedAt ?? p.PaymentDate, details[p.Id].Select(PaymentViews.Item).ToList()))
            .ToList();

        var payNowTotals = paid is null
            ? []
            : new List<CurrencyTotalDto> { new(paid.Payment.Currency, paid.Payment.Amount, paid.Payment.TaxAmount, paid.Payment.TotalAmount) };
        var creditTotals = credit
            .Select(c => new CurrencyTotalDto(c.Currency, c.Amount, c.TaxAmount, c.TotalAmount))
            .ToList();

        return new AccountCheckoutResultDto(paid, credit, payNowTotals, creditTotals, replayed);
    }

    private static string Fingerprint(string? currency, string? method, IEnumerable<AccountCheckoutItemRequest> items)
    {
        var keys = string.Join(';', items
            .Select(i => $"{i.ItemType}:{i.SourceId:N}:{(i.Mode == AccountPaymentModes.Credit ? "C" : "P")}")
            .Order(StringComparer.Ordinal));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(keys)))[..32];
        return $"{PaymentOrigins.Account}|{currency}|{method}|{hash}";
    }
}
