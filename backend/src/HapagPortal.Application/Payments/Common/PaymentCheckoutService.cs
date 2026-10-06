namespace HapagPortal.Application.Payments.Common;

using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Línea a pagar: ítem validado, RUT de facturación elegido (M5-09) y, si viene del carro, su ítem.</summary>
public sealed record CheckoutLine(
    ResolvedPayableItem Item,
    string BillingTaxId,
    string BillingName,
    CartItem? CartItem);

/// <summary>
/// Crea un pago con un detalle por ítem (M5-01) y lo inicia en la plataforma del medio elegido (M5-03):
/// <list type="bullet">
/// <item>NF-01: la clave de idempotencia del usuario identifica la solicitud; repetirla devuelve el mismo
/// resultado (también si fue un fallo) y nunca crea un segundo cobro.</item>
/// <item>NF-12: el pago se guarda <c>Pending</c> antes de llamar a la plataforma; si no responde, queda
/// <c>Failed</c> con <c>PROVIDER_UNAVAILABLE</c>, sin cobro y con los ítems devueltos al carro.</item>
/// <item>M5-05: la conversión a la moneda de pago usa el tipo de Nexus del día y se registra por pago.</item>
/// <item>NF-08: solo se guardan las referencias y el resultado; el instrumento se captura en la plataforma.</item>
/// </list>
/// </summary>
public sealed class PaymentCheckoutService(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IExchangeRateService exchangeRateService,
    IPaymentProviderResolver paymentProviderResolver)
{
    public const string NextActionRedirect = "Redirect";
    public const string NextActionIssueSlip = "IssueSlip";
    public const string NextActionNone = "None";

    /// <summary>Resultado ya registrado para la clave del usuario, o nulo si la clave es nueva.</summary>
    public async Task<Result<CheckoutResultDto>?> ReplayAsync(string idempotencyKey, string fingerprint, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
            return Result<CheckoutResultDto>.Failure(Error.Unauthorized);

        var key = idempotencyKey.Trim();
        var existing = await dbContext.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.CreatedByUserId == userId.Value && p.IdempotencyKey == key, cancellationToken);

        if (existing is null)
            return null;

        if (!string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            return Result<CheckoutResultDto>.Failure(DomainErrors.PaymentFlow.IdempotencyConflict);

        if (existing.Status == PaymentStatus.Failed && existing.FailureReason == PaymentFailureReasons.ProviderUnavailable)
            return Result<CheckoutResultDto>.Failure(DomainErrors.PaymentFlow.ProviderUnavailable);

        return Result<CheckoutResultDto>.Success(await ResultAsync(existing, replayed: true, cancellationToken));
    }

    /// <summary>Medio habilitado en el país que acepta la moneda (M5-03).</summary>
    public async Task<Result<PaymentMethodConfig>> MethodAsync(
        string country,
        string methodCode,
        string currency,
        CancellationToken cancellationToken)
    {
        var code = methodCode.Trim().ToUpperInvariant();
        var method = await dbContext.PaymentMethodConfigs.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Country == country && m.Code == code, cancellationToken);

        if (method is null || !PaymentMethodCatalog.Accepts(method, currency)
            || (method.Kind == PaymentMethodKinds.Online && string.IsNullOrWhiteSpace(method.ProviderKey)))
        {
            return Result<PaymentMethodConfig>.Failure(DomainErrors.PaymentMethodConfig.NotAvailable(code, currency));
        }

        return Result<PaymentMethodConfig>.Success(method);
    }

    public async Task<Result<CheckoutResultDto>> CreateAsync(
        PayerContext payer,
        string origin,
        string country,
        string currency,
        PaymentMethodConfig method,
        IReadOnlyList<CheckoutLine> lines,
        string idempotencyKey,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = BusinessCalendar.LocalDate(country, now);
        var actor = PaymentActor.From(currentUserService);
        var organization = payer.Organization;

        var paymentNumber = PaymentLifecycle.NewNumber(DocumentPrefixes.Payment, now);
        var payment = new Payment
        {
            PaymentNumber = paymentNumber,
            PaymentType = origin,
            PaymentMethod = method.Code,
            Currency = currency,
            Status = PaymentStatus.Pending,
            Country = country,
            PaymentDate = now,
            ClientId = organization.Id,
            Origin = origin,
            IdempotencyKey = idempotencyKey.Trim(),
            RequestFingerprint = fingerprint,
            CreatedByUserId = currentUserService.UserId,
            PaymentMethodCode = method.Code,
            ProviderKey = method.ProviderKey,
            ExternalReference = paymentNumber,
            PayerTaxId = TaxIdNormalizer.Normalize(organization.TaxId),
            PayerName = organization.Name,
            SlipNumber = method.Kind == PaymentMethodKinds.Deposit ? PaymentLifecycle.NewNumber(DocumentPrefixes.DepositSlip, now) : null
        };

        var details = new List<PaymentDetail>();
        var rates = new HashSet<decimal>();

        foreach (var line in lines)
        {
            var item = line.Item;
            var quote = await exchangeRateService.GetQuoteAsync(item.Currency, currency, today, cancellationToken);
            if (quote.IsFailure)
                return Result<CheckoutResultDto>.Failure(quote.Error);

            var converted = item.Currency != currency;
            var amount = MoneyRounding.Round(item.Amount * quote.Value.Rate, currency);
            var tax = MoneyRounding.Round(item.TaxAmount * quote.Value.Rate, currency);

            details.Add(new PaymentDetail
            {
                PaymentId = payment.Id,
                ConceptType = item.ConceptCode,
                Description = item.Description,
                Amount = amount,
                TaxAmount = tax,
                Currency = currency,
                ItemType = item.ItemType,
                SourceId = item.SourceId,
                BillOfLadingId = item.BillOfLading?.Id,
                BlNumber = item.BillOfLading?.BLNumber,
                BookingNumber = item.BillOfLading?.BookingNumber,
                BillingTaxId = line.BillingTaxId,
                BillingName = line.BillingName,
                OriginalAmount = item.TotalAmount,
                OriginalCurrency = item.Currency,
                ExchangeRate = converted ? quote.Value.Rate : null,
                OnBehalfOfClientId = item.OnBehalfOfClientId,
                AccessGrantId = item.AccessGrantId
            });

            if (converted)
            {
                rates.Add(quote.Value.Rate);
                exchangeRateService.Record(ExchangeRateTransactionTypes.Payment, payment.Id, quote.Value, item.TotalAmount, amount + tax);
            }

            if (line.CartItem is not null)
                line.CartItem.LockFor(payment.Id);
        }

        payment.Amount = details.Sum(d => d.Amount);
        payment.TaxAmount = details.Sum(d => d.TaxAmount);
        payment.TotalAmount = payment.Amount + payment.TaxAmount;
        payment.ExchangeRate = rates.Count == 1 ? rates.Single() : null;

        var blIds = details.Select(d => d.BillOfLadingId).Distinct().ToList();
        payment.BillOfLadingId = blIds.Count == 1 ? blIds[0] : null;

        // NF-14: un pago íntegramente bajo el mandato de un mismo mandante lo identifica a nivel de pago.
        var mandators = details.Select(d => d.OnBehalfOfClientId).Distinct().ToList();
        if (mandators.Count == 1 && mandators[0] is not null)
        {
            payment.OnBehalfOfClientId = mandators[0];
            var grants = details.Select(d => d.AccessGrantId).Distinct().ToList();
            payment.AccessGrantId = grants.Count == 1 ? grants[0] : null;
        }

        dbContext.Payments.Add(payment);
        foreach (var detail in details)
            dbContext.PaymentDetails.Add(detail);
        PaymentLifecycle.Created(dbContext, payment, actor, now);

        try
        {
            // NF-12: el pago existe con estado claro antes de hablar con la plataforma.
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otro cierre (con otra clave) bloqueó o cambió los mismos ítems entre la lectura y el guardado:
            // el token de concurrencia del ítem lo detecta, este pago no se crea y no se llama a la plataforma.
            return Result<CheckoutResultDto>.Failure(DomainErrors.Cart.Conflict);
        }
        catch (DbUpdateException)
        {
            // Dos solicitudes simultáneas con la misma clave: gana la primera (índice único, NF-01).
            var replay = await ReplayAsync(idempotencyKey, fingerprint, cancellationToken);
            if (replay is not null)
                return replay;

            throw;
        }

        if (method.Kind == PaymentMethodKinds.Deposit)
            return Result<CheckoutResultDto>.Success(await ResultAsync(payment, replayed: false, cancellationToken));

        var initiation = await InitiateAsync(method, payment, cancellationToken);
        if (initiation.IsFailure)
        {
            await PaymentLifecycle.FailAsync(
                dbContext, payment, PaymentActor.System, PaymentFailureReasons.ProviderUnavailable, DateTime.UtcNow, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<CheckoutResultDto>.Failure(DomainErrors.PaymentFlow.ProviderUnavailable);
        }

        payment.ProviderReference = initiation.Value.ProviderReference;
        payment.RedirectUrl = initiation.Value.RedirectUrl;
        PaymentLifecycle.Transition(
            dbContext, payment, PaymentStatus.Processing, PaymentActor.System, $"Initiated in {method.ProviderKey}", DateTime.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CheckoutResultDto>.Success(await ResultAsync(payment, replayed: false, cancellationToken));
    }

    private async Task<Result<PaymentInitiation>> InitiateAsync(
        PaymentMethodConfig method,
        Payment payment,
        CancellationToken cancellationToken)
    {
        var provider = paymentProviderResolver.Resolve(method.ProviderKey!);
        if (provider is null)
            return Result<PaymentInitiation>.Failure(DomainErrors.Integration.NotConfigured(method.ProviderKey!));

        try
        {
            return await provider.InitiateAsync(
                new PaymentInitiationRequest(
                    payment.ExternalReference!,
                    payment.TotalAmount,
                    payment.Currency,
                    $"Hapag-Lloyd {payment.PaymentNumber}",
                    $"/payments/{payment.Id}/result",
                    $"/api/v1/payments/webhook/{method.ProviderKey!.ToLowerInvariant()}",
                    payment.PayerTaxId),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Result<PaymentInitiation>.Failure(DomainErrors.Integration.Unavailable(method.ProviderKey!));
        }
    }

    public async Task<CheckoutResultDto> ResultAsync(Payment payment, bool replayed, CancellationToken cancellationToken)
    {
        var details = await dbContext.PaymentDetails.AsNoTracking()
            .Where(d => d.PaymentId == payment.Id)
            .ToListAsync(cancellationToken);

        var nextAction = payment.Status switch
        {
            PaymentStatus.Processing when payment.RedirectUrl is not null => NextActionRedirect,
            PaymentStatus.Pending when payment.SlipNumber is not null => NextActionIssueSlip,
            _ => NextActionNone
        };

        return new CheckoutResultDto(
            PaymentViews.Summary(payment, details),
            nextAction,
            nextAction == NextActionRedirect ? payment.RedirectUrl : null,
            replayed);
    }
}

/// <summary>Proyecciones de un pago a sus DTO.</summary>
public static class PaymentViews
{
    public static PaymentItemDto Item(PaymentDetail d) => new(
        d.Id, d.ItemType, d.SourceId, d.ConceptType, d.Description, d.BlNumber, d.BookingNumber, d.Amount, d.TaxAmount,
        d.Amount + d.TaxAmount, d.Currency, d.OriginalAmount, d.OriginalCurrency, d.ExchangeRate, d.BillingTaxId,
        d.BillingName, d.ReleasedAt);

    public static PaymentSummaryDto Summary(Payment p, IEnumerable<PaymentDetail> details) => new(
        p.Id, p.PaymentNumber, p.Status, p.StatusChangedAt, p.FailureReason, p.Origin, p.Country, p.Currency, p.Amount,
        p.TaxAmount, p.TotalAmount, p.PaymentMethod, p.PaymentMethodCode, p.ExternalReference, p.ProviderReference,
        p.ReceiptNumber, p.SlipNumber, p.SlipIssuedAt, p.CreatedAt == default ? p.PaymentDate : p.CreatedAt, p.ConfirmedAt,
        details.Select(Item).ToList());
}
