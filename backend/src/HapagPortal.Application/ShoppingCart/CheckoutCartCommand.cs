namespace HapagPortal.Application.ShoppingCart;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Cierre independiente de un sub-carro (M5-08): un pago con un detalle por ítem (M5-01) en la moneda y
/// con el medio elegidos. Cada ítem se vuelve a validar (permisos, montos, RUT de facturación y moneda)
/// antes de cobrar. Requiere clave de idempotencia (NF-01) y no opera durante un bloqueo de pagos (M8-07).
/// </summary>
public sealed record CheckoutCartCommand(
    string Country,
    string PaymentCurrency,
    string PaymentMethodCode,
    string? IdempotencyKey) : ICommand<CheckoutResultDto>;

public sealed class CheckoutCartCommandValidator : AbstractValidator<CheckoutCartCommand>
{
    public CheckoutCartCommandValidator()
    {
        RuleFor(x => x.Country)
            .Must(c => CountryCodes.ValidCountries.Contains(c?.Trim().ToUpperInvariant()))
            .WithMessage("Country must be CL or BO.");
        RuleFor(x => x.PaymentCurrency).NotEmpty().Matches("^[A-Za-z]{3}$").WithMessage("PaymentCurrency must be an ISO 4217 code.");
        RuleFor(x => x.PaymentMethodCode).NotEmpty().MaximumLength(40);
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage(DomainErrors.PaymentFlow.IdempotencyKeyRequired.Message)
            .MaximumLength(100);
    }
}

public sealed class CheckoutCartCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    PayableItemResolver resolver,
    PaymentCheckoutService checkout)
    : ICommandHandler<CheckoutCartCommand, CheckoutResultDto>
{
    public async Task<Result<CheckoutResultDto>> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        var country = request.Country.Trim().ToUpperInvariant();
        var currency = request.PaymentCurrency.Trim().ToUpperInvariant();
        var methodCode = request.PaymentMethodCode.Trim().ToUpperInvariant();
        var fingerprint = $"{PaymentOrigins.Cart}|{country}|{currency}|{methodCode}";

        // NF-01: la misma solicitud devuelve el mismo resultado, sin un segundo cobro.
        var replay = await checkout.ReplayAsync(request.IdempotencyKey!, fingerprint, cancellationToken);
        if (replay is not null)
            return replay;

        var payer = await resolver.LoadPayerAsync(requireOperate: true, cancellationToken);
        if (payer.IsFailure)
            return Result<CheckoutResultDto>.Failure(payer.Error);

        var eligible = CartEligibility.Check(payer.Value);
        if (eligible.IsFailure)
            return Result<CheckoutResultDto>.Failure(eligible.Error);

        var open = await PaymentBlocks.EnsureOpenAsync(dbContext, country, DateTime.UtcNow, cancellationToken);
        if (open.IsFailure)
            return Result<CheckoutResultDto>.Failure(open.Error);

        var method = await checkout.MethodAsync(country, methodCode, currency, cancellationToken);
        if (method.IsFailure)
            return Result<CheckoutResultDto>.Failure(method.Error);

        var cart = await CartRules.FindAsync(dbContext, currentUserService.UserId!.Value, payer.Value.Organization.Id, cancellationToken);
        var items = cart is null
            ? []
            : await dbContext.CartItems
                .Where(i => i.CartId == cart.Id && i.Country == country && i.PaymentCurrency == currency && i.LockedByPaymentId == null)
                .OrderBy(i => i.AddedAt)
                .ToListAsync(cancellationToken);

        if (items.Count == 0)
            return Result<CheckoutResultDto>.Failure(DomainErrors.Cart.Empty);

        var lines = new List<CheckoutLine>();
        foreach (var item in items)
        {
            var label = $"{item.BlNumber ?? item.Description} {item.ConceptCode}".Trim();
            var resolved = await resolver.ResolveAsync(payer.Value, item.ItemType, item.SourceId, null, cancellationToken);
            if (resolved.IsFailure)
                return Result<CheckoutResultDto>.Failure(DomainErrors.Cart.InvalidItem(label, resolved.Error));

            // M5-09: la combinación RUT × concepto se valida de nuevo antes de pagar (un acceso pudo vencer).
            if (resolved.Value.BillingOptions.All(o => o.TaxId != item.BillingTaxId))
                return Result<CheckoutResultDto>.Failure(DomainErrors.Cart.InvalidItem(label, DomainErrors.Cart.BillingTaxIdNotAllowed));

            if (!resolved.Value.AllowedCurrencies.Contains(currency))
            {
                return Result<CheckoutResultDto>.Failure(DomainErrors.Cart.InvalidItem(
                    label, DomainErrors.Cart.CurrencyNotAllowed(currency, resolved.Value.AllowedCurrencies)));
            }

            lines.Add(new CheckoutLine(resolved.Value, item.BillingTaxId, item.BillingName, item));
        }

        return await checkout.CreateAsync(
            payer.Value, PaymentOrigins.Cart, country, currency, method.Value, lines, request.IdempotencyKey!, fingerprint, cancellationToken);
    }
}
