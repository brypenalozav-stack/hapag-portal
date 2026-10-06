namespace HapagPortal.Application.Payments.Create;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class CreatePaymentCommandHandler(
    IApplicationDbContext dbContext,
    IPaymentGatewayService paymentGatewayService,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : ICommandHandler<CreatePaymentCommand, PaymentResponseDto>
{
    private const int TaxPercentageDivisor = 100;
    private const int CurrencyDecimalPlaces = 2;

    /// <summary>
    /// Maps frontend SCREAMING_SNAKE_CASE payment types to backend PascalCase.
    /// </summary>
    private static readonly Dictionary<string, string> PaymentTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FREIGHT"] = "Freight",
        ["LOCAL_CHARGES"] = "LocalCharges",
        ["DEMURRAGE"] = "Demurrage",
        ["COMBINED"] = "Combined",
        ["Freight"] = "Freight",
        ["LocalCharges"] = "LocalCharges",
        ["Demurrage"] = "Demurrage",
        ["Combined"] = "Combined",
    };

    /// <summary>
    /// Maps frontend SCREAMING_SNAKE_CASE payment methods to backend PascalCase.
    /// </summary>
    private static readonly Dictionary<string, string> PaymentMethodMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BANK_TRANSFER"] = PaymentMethods.BankTransfer,
        ["CREDIT_CARD"] = PaymentMethods.CreditCard,
        ["CREDIT_LINE"] = "CreditLine",
        ["QR_PAYMENT"] = PaymentMethods.Khipu,
        ["CreditCard"] = PaymentMethods.CreditCard,
        ["DebitCard"] = PaymentMethods.DebitCard,
        ["BankTransfer"] = PaymentMethods.BankTransfer,
        ["WebPay"] = PaymentMethods.WebPay,
        ["Cash"] = PaymentMethods.Cash,
        ["Check"] = PaymentMethods.Check,
        ["Khipu"] = PaymentMethods.Khipu,
        ["Deposit"] = PaymentMethods.Deposit,
    };

    public async Task<Result<PaymentResponseDto>> Handle(
        CreatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var clientId = currentUserService.ClientId;

        if (clientId is null)
            return Result<PaymentResponseDto>.Failure(
                new Error("Error.Unauthorized", "User is not associated with a client."));

        // Se filtra por accesos EN LA CONSULTA: nunca se carga un BL ajeno (BUG IDOR). El pago es
        // de la propia organización: la visibilidad total del administrador (M8-06) no habilita pagar.
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (scope.IsAdmin)
            scope = scope with { IsAdmin = false };

        var bl = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading, scope)
            .Include(b => b.Client)
            .Include(b => b.LocalCharges)
            .FirstOrDefaultAsync(b => b.Id == request.BlId, cancellationToken);

        var permissions = bl is null
            ? ShipmentPermissionSet.None
            : await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken);

        if (bl is null || !permissions.Can(ShipmentActionCodes.ViewShipment))
            return Result<PaymentResponseDto>.Failure(
                DomainErrors.BillOfLading.NotFound(request.BlId));

        // M8-07: durante una ventana de bloqueo no se inician pagos.
        var open = await PaymentBlocks.EnsureOpenAsync(dbContext, request.Country, DateTime.UtcNow, cancellationToken);
        if (open.IsFailure)
            return Result<PaymentResponseDto>.Failure(open.Error);

        var paymentType = PaymentTypeMap.GetValueOrDefault(request.Type, request.Type);

        // Matriz de M1-11 por concepto y perfil del usuario (M1-02): sin ambos, no se paga.
        if (!RequiredActions(paymentType).All(permissions.CanExecute))
            return Result<PaymentResponseDto>.Failure(Error.Forbidden);

        // NF-14 / M1-03: si el pago lo habilita un acceso otorgado (mandato), queda identificado el
        // mandante además de la organización y el usuario mandatario que lo ejecuta.
        var grant = RequiredActions(paymentType)
            .Select(permissions.GrantFor)
            .FirstOrDefault(g => g is not null);
        var paymentMethod = PaymentMethodMap.GetValueOrDefault(request.Method, request.Method);

        // Determine currency from BL or country
        var currency = bl.FreightCurrency;
        if (string.IsNullOrEmpty(currency))
            currency = request.Country == "BO" ? "BOB" : "CLP";

        // Calculate amount: if no details provided, auto-generate from BL charges
        decimal subtotal;
        decimal? precomputedTax = null;
        var details = request.Details ?? [];

        if (details.Count == 0)
        {
            if (paymentType == "LocalCharges")
            {
                // Los cargos locales ya traen su impuesto desglosado. El subtotal es la
                // BASE (lc.Amount) y el impuesto es la suma por cargo gravable, evitando
                // el doble IVA de sumar TotalAmount y volver a aplicar la tasa país (BUG-8).
                var charges = bl.LocalCharges ?? [];
                subtotal = charges.Sum(lc => lc.Amount);
                precomputedTax = charges.Where(lc => lc.IsTaxable).Sum(lc => lc.TaxAmount);
            }
            else
            {
                subtotal = bl.FreightAmount;
            }

            details =
            [
                new PaymentDetailRequest(
                    paymentType,
                    $"Payment for {paymentType} - BL {bl.BLNumber}",
                    subtotal,
                    currency)
            ];
        }
        else
        {
            subtotal = details.Sum(d => d.Amount);
        }

        decimal taxAmount;
        if (precomputedTax.HasValue)
        {
            taxAmount = Math.Round(precomputedTax.Value, CurrencyDecimalPlaces);
        }
        else
        {
            var taxConfig = await dbContext.TaxConfigurations
                .FirstOrDefaultAsync(t => t.Country == request.Country && t.IsActive, cancellationToken);

            var taxRate = taxConfig?.TaxRate ?? 0m;
            taxAmount = Math.Round(subtotal * taxRate / TaxPercentageDivisor, CurrencyDecimalPlaces);
        }

        var totalAmount = subtotal + taxAmount;

        var paymentNumber = $"{DocumentPrefixes.Payment}{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";

        var payment = new Payment
        {
            BillOfLadingId = request.BlId,
            ClientId = clientId.Value,
            PaymentNumber = paymentNumber,
            PaymentType = paymentType,
            PaymentMethod = paymentMethod,
            Amount = subtotal,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            Country = request.Country,
            PaymentDate = DateTime.UtcNow,
            OnBehalfOfClientId = grant?.GrantorOrganizationId,
            AccessGrantId = grant?.GrantId,
            CreatedByUserId = currentUserService.UserId
        };

        dbContext.Payments.Add(payment);
        PaymentLifecycle.Created(dbContext, payment, PaymentActor.From(currentUserService), DateTime.UtcNow);

        foreach (var detail in details)
        {
            var paymentDetail = new PaymentDetail
            {
                PaymentId = payment.Id,
                ConceptType = detail.ConceptType,
                Description = detail.Description,
                Amount = detail.Amount,
                Currency = detail.Currency
            };

            dbContext.PaymentDetails.Add(paymentDetail);
        }

        if (IsElectronicPayment(paymentMethod))
        {
            var gatewayRequest = new InitiatePaymentRequest(
                paymentMethod,
                totalAmount,
                payment.Currency,
                $"Payment {paymentNumber} for BL {bl.BLNumber}",
                $"/payments/{payment.Id}/confirmation");

            var gatewayResult = await paymentGatewayService.InitiatePaymentAsync(
                gatewayRequest, cancellationToken);

            if (!gatewayResult.Success)
            {
                await PaymentLifecycle.FailAsync(
                    dbContext, payment, PaymentActor.System, PaymentFailureReasons.ProviderUnavailable, DateTime.UtcNow, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Result<PaymentResponseDto>.Failure(
                    new Error("Payment.GatewayError", gatewayResult.ErrorMessage ?? "Payment gateway error."));
            }

            payment.ExternalReference = gatewayResult.ExternalReference;
            PaymentLifecycle.Transition(dbContext, payment, PaymentStatus.Processing, PaymentActor.System, null, DateTime.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<PaymentResponseDto>.Success(
            new PaymentResponseDto(
                payment.Id,
                payment.PaymentNumber,
                payment.PaymentType,
                payment.PaymentMethod,
                payment.Amount,
                payment.TaxAmount,
                payment.TotalAmount,
                payment.Currency,
                payment.Status,
                bl.BLNumber,
                payment.BillOfLadingId,
                payment.ClientId,
                bl.Client?.Name,
                payment.Country,
                payment.DepositProofUrl,
                payment.CreatedAt,
                payment.ConfirmedAt,
                null));
    }

    private static string[] RequiredActions(string paymentType) => paymentType switch
    {
        "Freight" => [ShipmentActionCodes.PayFreight],
        "LocalCharges" => [ShipmentActionCodes.PayMandatoryLocalCharges],
        "Demurrage" => [ShipmentActionCodes.PayImportDemurrage],
        "Combined" => [ShipmentActionCodes.PayFreight, ShipmentActionCodes.PayMandatoryLocalCharges],
        _ => [ShipmentActionCodes.PayMandatoryLocalCharges]
    };

    private static bool IsElectronicPayment(string paymentMethod) =>
        paymentMethod is PaymentMethods.CreditCard or PaymentMethods.DebitCard
            or PaymentMethods.BankTransfer or PaymentMethods.WebPay;
}
