namespace HapagPortal.Application.Payments.Simulator;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.Payments.Lifecycle;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Resultado elegido en la página del simulador de pago (modo de prueba) para el pago con la referencia del portal
/// <paramref name="ExternalReference"/>: <c>approved</c>, <c>rejected</c>, <c>pending</c> o <c>cancelled</c>
/// (<see cref="PaymentSimulatorOutcomes"/>). Devuelve el estado del pago, como <see cref="GetPaymentStatusQuery"/>.
/// </summary>
public sealed record SimulatePaymentCommand(string ExternalReference, string Outcome) : ICommand<PaymentStatusDto>;

public sealed class SimulatePaymentCommandValidator : AbstractValidator<SimulatePaymentCommand>
{
    public SimulatePaymentCommandValidator()
    {
        RuleFor(x => x.ExternalReference).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Outcome)
            .Must(o => o is not null && PaymentSimulatorOutcomes.All.Contains(o.Trim().ToLowerInvariant()))
            .WithMessage("Outcome must be approved, rejected, pending or cancelled.");
    }
}

/// <summary>
/// Simulador de pago: existe solo si la pasarela del pago está en modo de prueba (<see cref="ISimulatedPaymentProvider"/>)
/// y el pago es de la organización del usuario; si no, <c>Payment.NotFound</c> (404, sin revelar nada). Guarda el
/// resultado en <see cref="IPaymentSimulatorStore"/> y lo aplica en el acto por el mismo camino que la verificación al
/// volver de la pasarela y la notificación (<see cref="PaymentStatusSync"/>): la pasarela simulada informa el estado y
/// se compara referencia, monto y moneda. Idempotente: un pago confirmado no cambia ni se confirma dos veces.
/// </summary>
public sealed class SimulatePaymentCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService,
    IPaymentProviderResolver providerResolver,
    IPaymentSimulatorStore simulatorStore)
    : ICommandHandler<SimulatePaymentCommand, PaymentStatusDto>
{
    private static readonly Error NotFound = new("Payment.NotFound", "The payment was not found.");

    public async Task<Result<PaymentStatusDto>> Handle(SimulatePaymentCommand request, CancellationToken cancellationToken)
    {
        var reference = request.ExternalReference.Trim();
        var paymentId = await dbContext.Payments.AsNoTracking()
            .Where(p => p.ExternalReference == reference)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (paymentId is null)
            return Result<PaymentStatusDto>.Failure(NotFound);

        var loaded = await PaymentAccess.LoadAsync(
            dbContext, accessEvaluator, currentUserService, paymentId.Value, ownerOnly: true, tracking: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<PaymentStatusDto>.Failure(NotFound);

        var payment = loaded.Value;
        if (string.IsNullOrWhiteSpace(payment.ProviderKey) ||
            providerResolver.Resolve(payment.ProviderKey) is not ISimulatedPaymentProvider provider)
        {
            return Result<PaymentStatusDto>.Failure(NotFound);
        }

        simulatorStore.Record(reference, request.Outcome);

        var actor = new PaymentActor($"{payment.ProviderKey.ToUpperInvariant()}_SIMULATOR", currentUserService.UserId);
        await PaymentStatusSync.SyncAsync(dbContext, payment, provider, actor, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<PaymentStatusDto>.Success(await PaymentAccess.StatusAsync(dbContext, payment, cancellationToken));
    }
}
