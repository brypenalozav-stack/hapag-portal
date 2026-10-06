namespace HapagPortal.Application.ShoppingCart;

using FluentValidation;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Carro unificado del usuario en su organización, agrupado por país y moneda de pago (M5-01, M5-08).</summary>
public sealed record GetCartQuery : IQuery<CartDto>;

/// <summary>
/// Validación previa de un ítem: monto a pagar, monedas habilitadas (M5-04) y RUT de facturación
/// posibles (M5-09), para elegirlos antes de agregarlo. <c>Reference</c> identifica una factura por su
/// número de origen o folio (pago de la factura de demurrage, M3-18).
/// </summary>
public sealed record GetCartItemOptionsQuery(string ItemType, Guid? SourceId, string? Reference) : IQuery<PayableItemDto>;

/// <summary>
/// Agrega un ítem validado al carro. El RUT de facturación se elige al agregar (M5-09); la moneda de pago,
/// si no se indica, es la que corresponde al cargo (M5-08).
/// </summary>
public sealed record AddCartItemCommand(
    string ItemType,
    Guid? SourceId,
    string? Reference,
    string BillingTaxId,
    string? PaymentCurrency) : ICommand<CartDto>;

public sealed record RemoveCartItemCommand(Guid ItemId) : ICommand<CartDto>;

/// <summary>Convierte un ítem a otra moneda habilitada con el tipo de cambio de Nexus (M5-08, M5-05).</summary>
public sealed record ChangeCartItemCurrencyCommand(Guid ItemId, string PaymentCurrency) : ICommand<CartDto>;

/// <summary>Vacía el carro (o un sub-carro); los ítems de un pago en curso se conservan.</summary>
public sealed record ClearCartCommand(string? Country, string? PaymentCurrency) : ICommand<CartDto>;

public sealed class GetCartItemOptionsQueryValidator : AbstractValidator<GetCartItemOptionsQuery>
{
    public GetCartItemOptionsQueryValidator()
    {
        CartRules.ItemReference(this, x => x.ItemType, x => x.SourceId, x => x.Reference);
    }
}

public sealed class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemCommandValidator()
    {
        CartRules.ItemReference(this, x => x.ItemType, x => x.SourceId, x => x.Reference);
        RuleFor(x => x.BillingTaxId).NotEmpty().MaximumLength(20);
        RuleFor(x => x.PaymentCurrency).Matches("^[A-Za-z]{3}$").When(x => x.PaymentCurrency is not null)
            .WithMessage("PaymentCurrency must be an ISO 4217 code.");
    }
}

public sealed class ChangeCartItemCurrencyCommandValidator : AbstractValidator<ChangeCartItemCurrencyCommand>
{
    public ChangeCartItemCurrencyCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.PaymentCurrency).NotEmpty().Matches("^[A-Za-z]{3}$").WithMessage("PaymentCurrency must be an ISO 4217 code.");
    }
}

internal static class CartRules
{
    public static void ItemReference<T>(
        AbstractValidator<T> validator,
        Func<T, string> itemType,
        Func<T, Guid?> sourceId,
        Func<T, string?> reference)
    {
        validator.RuleFor(x => itemType(x))
            .Must(t => PayableItemTypes.All.Contains(t))
            .WithName("ItemType")
            .WithMessage("ItemType must be LocalCharge, Freight, Demurrage, WarehouseChange or Invoice.");
        validator.RuleFor(x => x)
            .Must(x => sourceId(x) is { } id && id != Guid.Empty
                || (itemType(x) == PayableItemTypes.Invoice && !string.IsNullOrWhiteSpace(reference(x))))
            .WithName("SourceId")
            .WithMessage("SourceId is required (invoices may be referenced by number instead).");
        validator.RuleFor(x => reference(x)).MaximumLength(50).WithName("Reference");
    }

    /// <summary>Carro del usuario en su organización; se crea al agregar el primer ítem.</summary>
    public static async Task<Cart?> FindAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken) =>
        await dbContext.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.OrganizationId == organizationId, cancellationToken);
}

/// <summary>Arma la vista del carro: sub-carros por país y moneda con subtotal, medios y bloqueo vigente.</summary>
public sealed class CartViewBuilder(IApplicationDbContext dbContext, PayableItemResolver resolver)
{
    public async Task<CartDto> BuildAsync(Cart? cart, Guid organizationId, CancellationToken cancellationToken)
    {
        if (cart is null)
            return new CartDto(null, organizationId, 0, [], null);

        var items = await dbContext.CartItems.AsNoTracking()
            .Where(i => i.CartId == cart.Id)
            .OrderBy(i => i.AddedAt)
            .ToListAsync(cancellationToken);

        var catalog = await resolver.CatalogAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var groups = new List<CartGroupDto>();

        foreach (var group in items.GroupBy(i => (i.Country, i.PaymentCurrency)).OrderBy(g => g.Key.Country).ThenBy(g => g.Key.PaymentCurrency))
        {
            var views = new List<CartItemDto>();
            foreach (var item in group)
            {
                var allowed = await resolver.AllowedCurrenciesAsync(item.Country, item.ConceptCode, item.Currency, cancellationToken);
                views.Add(ToDto(item, catalog, allowed));
            }

            var methods = await PaymentMethodCatalog.AvailableAsync(dbContext, group.Key.Country, group.Key.PaymentCurrency, cancellationToken);
            var block = await PaymentBlocks.StatusAsync(dbContext, group.Key.Country, now, cancellationToken);
            var open = group.Where(i => i.LockedByPaymentId is null).ToList();

            groups.Add(new CartGroupDto(
                group.Key.Country,
                group.Key.PaymentCurrency,
                open.Sum(i => i.PaymentAmount),
                group.Count(),
                group.Count() - open.Count,
                views,
                methods.Select(PaymentMethodCatalog.ToDto).ToList(),
                block));
        }

        DateTime? updatedAt = items.Count == 0 ? cart.ModifiedAt ?? cart.CreatedAt : items.Max(i => i.AddedAt);
        return new CartDto(cart.Id, organizationId, items.Count, groups, updatedAt);
    }

    private static CartItemDto ToDto(CartItem i, IReadOnlyDictionary<string, ChargeConcept> catalog, IReadOnlyList<string> allowed) => new(
        i.Id, i.ItemType, i.SourceId, i.BillOfLadingId, i.BlNumber, i.BookingNumber, i.Country, i.ConceptCode,
        catalog.GetValueOrDefault(i.ConceptCode)?.Name ?? ConceptName(i.ConceptCode), i.Description, i.Amount, i.TaxAmount,
        i.TotalAmount, i.Currency, i.PaymentCurrency, i.PaymentAmount,
        i.ExchangeRate is { } rate && i.RateEffectiveDate is { } date
            ? new ExchangeRateUsedDto(i.Currency, i.PaymentCurrency, rate, date, i.RateSource ?? string.Empty)
            : null,
        allowed, i.BillingTaxId, i.BillingName, i.OnBehalfOfClientId, i.LockedByPaymentId, i.AddedAt);

    private static string ConceptName(string code) => code switch
    {
        PaymentConcepts.Freight => "Flete",
        PaymentConcepts.Invoice => "Factura",
        _ => code
    };
}

public sealed class GetCartQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    PayableItemResolver resolver,
    CartViewBuilder viewBuilder)
    : IQueryHandler<GetCartQuery, CartDto>
{
    public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var payer = await resolver.LoadPayerAsync(requireOperate: false, cancellationToken);
        if (payer.IsFailure)
            return Result<CartDto>.Failure(payer.Error);

        var organizationId = payer.Value.Organization.Id;
        var cart = await CartRules.FindAsync(dbContext, currentUserService.UserId!.Value, organizationId, cancellationToken);
        return Result<CartDto>.Success(await viewBuilder.BuildAsync(cart, organizationId, cancellationToken));
    }
}

public sealed class GetCartItemOptionsQueryHandler(PayableItemResolver resolver)
    : IQueryHandler<GetCartItemOptionsQuery, PayableItemDto>
{
    public async Task<Result<PayableItemDto>> Handle(GetCartItemOptionsQuery request, CancellationToken cancellationToken)
    {
        var payer = await resolver.LoadPayerAsync(requireOperate: false, cancellationToken);
        if (payer.IsFailure)
            return Result<PayableItemDto>.Failure(payer.Error);

        var eligible = CartEligibility.Check(payer.Value);
        if (eligible.IsFailure)
            return Result<PayableItemDto>.Failure(eligible.Error);

        var item = await resolver.ResolveAsync(payer.Value, request.ItemType, request.SourceId, request.Reference, cancellationToken);
        return item.IsFailure
            ? Result<PayableItemDto>.Failure(item.Error)
            : Result<PayableItemDto>.Success(item.Value.ToDto());
    }
}

/// <summary>
/// M5-01 / M5-07: el carro es para clientes sin condición de crédito; sin respuesta de Nexus no se puede
/// saber, así que no se agrega nada (no se supone la condición).
/// </summary>
internal static class CartEligibility
{
    public static Result Check(PayerContext payer)
    {
        if (!payer.Conditions.Available)
            return Result.Failure(DomainErrors.ChargeRules.ConditionsUnavailable);

        return payer.Conditions.HasCredit
            ? Result.Failure(DomainErrors.Cart.CreditCustomer)
            : Result.Success();
    }
}

public sealed class AddCartItemCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    PayableItemResolver resolver,
    IExchangeRateService exchangeRateService,
    CartViewBuilder viewBuilder)
    : ICommandHandler<AddCartItemCommand, CartDto>
{
    public async Task<Result<CartDto>> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        var payer = await resolver.LoadPayerAsync(requireOperate: true, cancellationToken);
        if (payer.IsFailure)
            return Result<CartDto>.Failure(payer.Error);

        var eligible = CartEligibility.Check(payer.Value);
        if (eligible.IsFailure)
            return Result<CartDto>.Failure(eligible.Error);

        var resolved = await resolver.ResolveAsync(payer.Value, request.ItemType, request.SourceId, request.Reference, cancellationToken);
        if (resolved.IsFailure)
            return Result<CartDto>.Failure(resolved.Error);

        var item = resolved.Value;

        // M5-09: solo un RUT habilitado para el usuario sobre este ítem.
        var billingTaxId = TaxIdNormalizer.Normalize(request.BillingTaxId);
        var billing = item.BillingOptions.FirstOrDefault(o => o.TaxId == billingTaxId);
        if (billing is null)
            return Result<CartDto>.Failure(DomainErrors.Cart.BillingTaxIdNotAllowed);

        // M5-08: la moneda de pago se valida antes de incorporar el cargo.
        var currency = request.PaymentCurrency?.Trim().ToUpperInvariant() ?? item.DefaultPaymentCurrency;
        if (!item.AllowedCurrencies.Contains(currency))
            return Result<CartDto>.Failure(DomainErrors.Cart.CurrencyNotAllowed(currency, item.AllowedCurrencies));

        var userId = currentUserService.UserId!.Value;
        var organizationId = payer.Value.Organization.Id;
        var cart = await CartRules.FindAsync(dbContext, userId, organizationId, cancellationToken);
        if (cart is null)
        {
            cart = new Cart { UserId = userId, OrganizationId = organizationId };
            dbContext.Carts.Add(cart);
        }
        else if (await dbContext.CartItems.AnyAsync(
            i => i.CartId == cart.Id && i.ItemType == item.ItemType && i.SourceId == item.SourceId, cancellationToken))
        {
            return Result<CartDto>.Failure(DomainErrors.Cart.Duplicate);
        }

        var now = DateTime.UtcNow;
        var cartItem = new CartItem
        {
            CartId = cart.Id,
            ItemType = item.ItemType,
            SourceId = item.SourceId,
            BillOfLadingId = item.BillOfLading?.Id,
            BlNumber = item.BillOfLading?.BLNumber,
            BookingNumber = item.BillOfLading?.BookingNumber,
            Country = item.Country,
            ConceptCode = item.ConceptCode,
            Description = item.Description,
            Amount = item.Amount,
            TaxAmount = item.TaxAmount,
            TotalAmount = item.TotalAmount,
            Currency = item.Currency,
            PaymentCurrency = currency,
            BillingTaxId = billing.TaxId,
            BillingName = billing.Name,
            BillingOrganizationId = billing.OrganizationId,
            OnBehalfOfClientId = item.OnBehalfOfClientId,
            AccessGrantId = item.AccessGrantId,
            AddedByUserId = userId,
            AddedAt = now
        };

        var conversion = await CartConversion.ApplyAsync(exchangeRateService, cartItem, currency, now, cancellationToken);
        if (conversion.IsFailure)
            return Result<CartDto>.Failure(conversion.Error);

        dbContext.CartItems.Add(cartItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CartDto>.Success(await viewBuilder.BuildAsync(cart, organizationId, cancellationToken));
    }
}

/// <summary>Monto del ítem en la moneda de pago con el tipo de Nexus del día en el país (M5-05).</summary>
internal static class CartConversion
{
    public static async Task<Result> ApplyAsync(
        IExchangeRateService exchangeRateService,
        CartItem item,
        string paymentCurrency,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var quote = await exchangeRateService.GetQuoteAsync(
            item.Currency, paymentCurrency, BusinessCalendar.LocalDate(item.Country, now), cancellationToken);
        if (quote.IsFailure)
            return Result.Failure(quote.Error);

        var converted = item.Currency != paymentCurrency;
        item.PaymentCurrency = paymentCurrency;
        item.PaymentAmount = MoneyRounding.Round(item.Amount * quote.Value.Rate, paymentCurrency)
            + MoneyRounding.Round(item.TaxAmount * quote.Value.Rate, paymentCurrency);
        item.ExchangeRate = converted ? quote.Value.Rate : null;
        item.RateEffectiveDate = converted ? quote.Value.EffectiveDate : null;
        item.RateSource = converted ? quote.Value.Source : null;
        item.Touch();
        return Result.Success();
    }
}

/// <summary>
/// Guardado de cambios sobre ítems del carro: si un cierre los bloqueó (o los cambió) entre la lectura y el
/// guardado, el token de concurrencia del ítem lo detecta y se responde un conflicto en lugar de un error 500.
/// </summary>
internal static class CartConcurrency
{
    public static async Task<Result> SaveAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(DomainErrors.Cart.Conflict);
        }
    }
}

public sealed class RemoveCartItemCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    PayableItemResolver resolver,
    CartViewBuilder viewBuilder)
    : ICommandHandler<RemoveCartItemCommand, CartDto>
{
    public async Task<Result<CartDto>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var payer = await resolver.LoadPayerAsync(requireOperate: false, cancellationToken);
        if (payer.IsFailure)
            return Result<CartDto>.Failure(payer.Error);

        var organizationId = payer.Value.Organization.Id;
        var cart = await CartRules.FindAsync(dbContext, currentUserService.UserId!.Value, organizationId, cancellationToken);
        var item = cart is null
            ? null
            : await dbContext.CartItems.FirstOrDefaultAsync(i => i.Id == request.ItemId && i.CartId == cart.Id, cancellationToken);

        if (item is null)
            return Result<CartDto>.Failure(DomainErrors.Cart.ItemNotFound(request.ItemId));
        if (item.LockedByPaymentId is not null)
            return Result<CartDto>.Failure(DomainErrors.Cart.ItemLocked);

        dbContext.CartItems.Remove(item);
        var saved = await CartConcurrency.SaveAsync(dbContext, cancellationToken);
        if (saved.IsFailure)
            return Result<CartDto>.Failure(saved.Error);

        return Result<CartDto>.Success(await viewBuilder.BuildAsync(cart, organizationId, cancellationToken));
    }
}

public sealed class ChangeCartItemCurrencyCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    PayableItemResolver resolver,
    IExchangeRateService exchangeRateService,
    CartViewBuilder viewBuilder)
    : ICommandHandler<ChangeCartItemCurrencyCommand, CartDto>
{
    public async Task<Result<CartDto>> Handle(ChangeCartItemCurrencyCommand request, CancellationToken cancellationToken)
    {
        var payer = await resolver.LoadPayerAsync(requireOperate: true, cancellationToken);
        if (payer.IsFailure)
            return Result<CartDto>.Failure(payer.Error);

        var organizationId = payer.Value.Organization.Id;
        var cart = await CartRules.FindAsync(dbContext, currentUserService.UserId!.Value, organizationId, cancellationToken);
        var item = cart is null
            ? null
            : await dbContext.CartItems.FirstOrDefaultAsync(i => i.Id == request.ItemId && i.CartId == cart.Id, cancellationToken);

        if (item is null)
            return Result<CartDto>.Failure(DomainErrors.Cart.ItemNotFound(request.ItemId));
        if (item.LockedByPaymentId is not null)
            return Result<CartDto>.Failure(DomainErrors.Cart.ItemLocked);

        var currency = request.PaymentCurrency.Trim().ToUpperInvariant();
        var allowed = await resolver.AllowedCurrenciesAsync(item.Country, item.ConceptCode, item.Currency, cancellationToken);
        if (!allowed.Contains(currency))
            return Result<CartDto>.Failure(DomainErrors.Cart.CurrencyNotAllowed(currency, allowed));

        var conversion = await CartConversion.ApplyAsync(exchangeRateService, item, currency, DateTime.UtcNow, cancellationToken);
        if (conversion.IsFailure)
            return Result<CartDto>.Failure(conversion.Error);

        var saved = await CartConcurrency.SaveAsync(dbContext, cancellationToken);
        if (saved.IsFailure)
            return Result<CartDto>.Failure(saved.Error);

        return Result<CartDto>.Success(await viewBuilder.BuildAsync(cart, organizationId, cancellationToken));
    }
}

public sealed class ClearCartCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    PayableItemResolver resolver,
    CartViewBuilder viewBuilder)
    : ICommandHandler<ClearCartCommand, CartDto>
{
    public async Task<Result<CartDto>> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var payer = await resolver.LoadPayerAsync(requireOperate: false, cancellationToken);
        if (payer.IsFailure)
            return Result<CartDto>.Failure(payer.Error);

        var organizationId = payer.Value.Organization.Id;
        var cart = await CartRules.FindAsync(dbContext, currentUserService.UserId!.Value, organizationId, cancellationToken);
        if (cart is null)
            return Result<CartDto>.Success(await viewBuilder.BuildAsync(null, organizationId, cancellationToken));

        var country = request.Country?.Trim().ToUpperInvariant();
        var currency = request.PaymentCurrency?.Trim().ToUpperInvariant();
        var items = await dbContext.CartItems
            .Where(i => i.CartId == cart.Id && i.LockedByPaymentId == null)
            .Where(i => country == null || i.Country == country)
            .Where(i => currency == null || i.PaymentCurrency == currency)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
            dbContext.CartItems.Remove(item);

        var saved = await CartConcurrency.SaveAsync(dbContext, cancellationToken);
        if (saved.IsFailure)
            return Result<CartDto>.Failure(saved.Error);

        return Result<CartDto>.Success(await viewBuilder.BuildAsync(cart, organizationId, cancellationToken));
    }
}
