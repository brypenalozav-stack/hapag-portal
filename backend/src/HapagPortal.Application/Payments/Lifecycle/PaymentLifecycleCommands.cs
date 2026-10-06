namespace HapagPortal.Application.Payments.Lifecycle;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.Payments.PostProcessing;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Payments;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Estado actual del pago con su historial de transiciones (NF-02): el cliente sabe con certeza si pagó (NF-12).</summary>
public sealed record GetPaymentStatusQuery(Guid PaymentId) : IQuery<PaymentStatusDto>;

/// <summary>
/// Emite la boleta de depósito (M5-02): desde aquí el cliente ya no puede anularla; Finanzas verifica el
/// abono y confirma o anula. Es parte del pago, por lo que no opera durante un bloqueo (M8-07).
/// </summary>
public sealed record IssueDepositSlipCommand(Guid PaymentId) : ICommand<PaymentStatusDto>;

/// <summary>Anulación por Finanzas de un pago en curso o una boleta emitida, con motivo (M5-02).</summary>
public sealed record FinanceCancelPaymentCommand(Guid PaymentId, string Reason) : ICommand<PaymentStatusDto>;

/// <summary>Operaciones posteriores al pago para los equipos internos (NF-03). Por defecto, las detenidas y las con reintentos.</summary>
public sealed record GetPaymentOperationsQuery(string? Status) : IQuery<IReadOnlyList<PaymentOperationDto>>;

/// <summary>Reintenta de inmediato una operación pendiente o detenida (NF-03).</summary>
public sealed record RetryPaymentOperationCommand(Guid Id) : ICommand<PaymentOperationDto>;

/// <summary>Procesa la cola de operaciones posteriores al pago (proceso en segundo plano).</summary>
public sealed record ProcessPaymentOutboxCommand(int BatchSize) : ICommand<int>;

/// <summary>Transacciones de un período con su estado de conciliación (NF-04). Fechas locales del país.</summary>
public sealed record GetPaymentReconciliationQuery(
    DateOnly? From,
    DateOnly? To,
    string? Country,
    string? Status) : IQuery<IReadOnlyList<PaymentReconciliationDto>>;

public sealed class FinanceCancelPaymentCommandValidator : AbstractValidator<FinanceCancelPaymentCommand>
{
    public FinanceCancelPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class GetPaymentReconciliationQueryValidator : AbstractValidator<GetPaymentReconciliationQuery>
{
    public GetPaymentReconciliationQueryValidator()
    {
        RuleFor(x => x.Country)
            .Must(c => c is null || CountryCodes.ValidCountries.Contains(c))
            .WithMessage("Country must be CL or BO.");
        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithName("To")
            .WithMessage("To must be on or after From.");
    }
}

/// <summary>
/// Visibilidad de un pago (NF-05): la organización que pagó, el mandante en cuyo nombre se pagó
/// (NF-14) o un usuario interno con permiso de Finanzas.
/// </summary>
public static class PaymentAccess
{
    public static async Task<Result<Payment>> LoadAsync(
        IApplicationDbContext dbContext,
        IShipmentAccessEvaluator accessEvaluator,
        ICurrentUserService currentUserService,
        Guid paymentId,
        bool ownerOnly,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var query = tracking ? dbContext.Payments : dbContext.Payments.AsNoTracking();
        var payment = await query.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
        if (payment is null)
            return Result<Payment>.Failure(DomainErrors.Payment.NotFound(paymentId));

        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        var organizationId = scope.OrganizationId;
        var visible = payment.ClientId == organizationId
            || (!ownerOnly && payment.OnBehalfOfClientId is not null && payment.OnBehalfOfClientId == organizationId)
            || (!ownerOnly && (scope.IsAdmin || currentUserService.HasPermission(PaymentPermissions.Finance)));

        return visible
            ? Result<Payment>.Success(payment)
            : Result<Payment>.Failure(DomainErrors.Payment.NotFound(paymentId));
    }

    public static async Task<PaymentStatusDto> StatusAsync(
        IApplicationDbContext dbContext,
        Payment payment,
        CancellationToken cancellationToken)
    {
        var details = await dbContext.PaymentDetails.AsNoTracking()
            .Where(d => d.PaymentId == payment.Id)
            .ToListAsync(cancellationToken);

        var history = await dbContext.PaymentStatusChanges.AsNoTracking()
            .Where(h => h.PaymentId == payment.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync(cancellationToken);

        var releasePending = await dbContext.PaymentOutboxMessages.AsNoTracking()
            .AnyAsync(m => m.PaymentId == payment.Id && m.JobType == PaymentOutboxJobTypes.Release && m.Status != PaymentOutboxStatus.Succeeded, cancellationToken);

        var denial = ReceiptCancellationPolicy.ClientDenialReason(payment.Status);

        return new PaymentStatusDto(
            PaymentViews.Summary(payment, details),
            history.Select(h => new PaymentStatusChangeDto(h.FromStatus, h.ToStatus, h.ChangedAt, h.ChangedBy, h.Reason)).ToList(),
            CanCancel: denial is null,
            CancelDeniedReason: denial,
            CanIssueSlip: payment.Status == PaymentStatus.Pending && payment.SlipNumber is not null,
            ReleasePending: releasePending);
    }
}

public sealed class GetPaymentStatusQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetPaymentStatusQuery, PaymentStatusDto>
{
    public async Task<Result<PaymentStatusDto>> Handle(GetPaymentStatusQuery request, CancellationToken cancellationToken)
    {
        var payment = await PaymentAccess.LoadAsync(
            dbContext, accessEvaluator, currentUserService, request.PaymentId, ownerOnly: false, tracking: false, cancellationToken);

        return payment.IsFailure
            ? Result<PaymentStatusDto>.Failure(payment.Error)
            : Result<PaymentStatusDto>.Success(await PaymentAccess.StatusAsync(dbContext, payment.Value, cancellationToken));
    }
}

public sealed class IssueDepositSlipCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService)
    : ICommandHandler<IssueDepositSlipCommand, PaymentStatusDto>
{
    public async Task<Result<PaymentStatusDto>> Handle(IssueDepositSlipCommand request, CancellationToken cancellationToken)
    {
        var loaded = await PaymentAccess.LoadAsync(
            dbContext, accessEvaluator, currentUserService, request.PaymentId, ownerOnly: true, tracking: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<PaymentStatusDto>.Failure(loaded.Error);

        var payment = loaded.Value;
        if (payment.SlipNumber is null)
            return Result<PaymentStatusDto>.Failure(DomainErrors.PaymentFlow.NotDeposit);

        if (payment.Status == PaymentStatus.PendingVerification)
            return Result<PaymentStatusDto>.Success(await PaymentAccess.StatusAsync(dbContext, payment, cancellationToken));

        var open = await PaymentBlocks.EnsureOpenAsync(dbContext, payment.Country, DateTime.UtcNow, cancellationToken);
        if (open.IsFailure)
            return Result<PaymentStatusDto>.Failure(open.Error);

        var now = DateTime.UtcNow;
        var transition = PaymentLifecycle.Transition(
            dbContext, payment, PaymentStatus.PendingVerification, PaymentActor.From(currentUserService), "Deposit slip issued", now);
        if (transition.IsFailure)
            return Result<PaymentStatusDto>.Failure(transition.Error);

        payment.SlipIssuedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<PaymentStatusDto>.Success(await PaymentAccess.StatusAsync(dbContext, payment, cancellationToken));
    }
}

public sealed class FinanceCancelPaymentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<FinanceCancelPaymentCommand, PaymentStatusDto>
{
    public async Task<Result<PaymentStatusDto>> Handle(FinanceCancelPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments.FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);
        if (payment is null)
            return Result<PaymentStatusDto>.Failure(DomainErrors.Payment.NotFound(request.PaymentId));

        if (payment.Status == PaymentStatus.Confirmed)
            return Result<PaymentStatusDto>.Failure(DomainErrors.Payment.AlreadyConfirmed);
        if (payment.Status == PaymentStatus.Cancelled)
            return Result<PaymentStatusDto>.Failure(DomainErrors.Payment.AlreadyCancelled);
        if (!ReceiptCancellationPolicy.FinanceCanCancel(payment.Status))
            return Result<PaymentStatusDto>.Failure(DomainErrors.Payment.InvalidStatus);

        var cancelled = await PaymentLifecycle.CancelAsync(
            dbContext, payment, PaymentActor.From(currentUserService), PaymentCancellationRoles.Finance, request.Reason.Trim(),
            DateTime.UtcNow, cancellationToken);
        if (cancelled.IsFailure)
            return Result<PaymentStatusDto>.Failure(cancelled.Error);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<PaymentStatusDto>.Success(await PaymentAccess.StatusAsync(dbContext, payment, cancellationToken));
    }
}

public sealed class GetPaymentOperationsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetPaymentOperationsQuery, IReadOnlyList<PaymentOperationDto>>
{
    private const int MaxRows = 200;

    public async Task<Result<IReadOnlyList<PaymentOperationDto>>> Handle(GetPaymentOperationsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.PaymentOutboxMessages.AsNoTracking();

        query = string.IsNullOrWhiteSpace(request.Status)
            ? query.Where(m => m.Status == PaymentOutboxStatus.Stuck || (m.Status == PaymentOutboxStatus.Pending && m.Attempts > 0))
            : query.Where(m => m.Status == request.Status);

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var paymentIds = messages.Select(m => m.PaymentId).Distinct().ToList();
        var numbers = await dbContext.Payments.AsNoTracking()
            .Where(p => paymentIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.PaymentNumber, cancellationToken);

        IReadOnlyList<PaymentOperationDto> items = messages
            .Select(m => PaymentPostProcessor.ToDto(m, numbers.GetValueOrDefault(m.PaymentId) ?? string.Empty))
            .ToList();
        return Result<IReadOnlyList<PaymentOperationDto>>.Success(items);
    }
}

public sealed class RetryPaymentOperationCommandHandler(
    IApplicationDbContext dbContext,
    PaymentPostProcessor processor)
    : ICommandHandler<RetryPaymentOperationCommand, PaymentOperationDto>
{
    public async Task<Result<PaymentOperationDto>> Handle(RetryPaymentOperationCommand request, CancellationToken cancellationToken)
    {
        var message = await dbContext.PaymentOutboxMessages.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (message is null)
            return Result<PaymentOperationDto>.Failure(DomainErrors.PaymentFlow.OperationNotFound(request.Id));
        if (message.Status == PaymentOutboxStatus.Succeeded)
            return Result<PaymentOperationDto>.Failure(DomainErrors.PaymentFlow.OperationNotRetryable);

        // Reintento manual: se dispone de un nuevo ciclo completo de intentos.
        message.Status = PaymentOutboxStatus.Pending;
        message.Attempts = 0;
        var now = DateTime.UtcNow;
        message.NextAttemptAt = now;
        await processor.ExecuteAsync(message, now, cancellationToken);

        var number = await dbContext.Payments.AsNoTracking()
            .Where(p => p.Id == message.PaymentId)
            .Select(p => p.PaymentNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return Result<PaymentOperationDto>.Success(PaymentPostProcessor.ToDto(message, number ?? string.Empty));
    }
}

public sealed class ProcessPaymentOutboxCommandHandler(PaymentPostProcessor processor)
    : ICommandHandler<ProcessPaymentOutboxCommand, int>
{
    public async Task<Result<int>> Handle(ProcessPaymentOutboxCommand request, CancellationToken cancellationToken) =>
        Result<int>.Success(await processor.ProcessDueAsync(Math.Clamp(request.BatchSize, 1, 500), DateTime.UtcNow, cancellationToken));
}

public sealed class GetPaymentReconciliationQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetPaymentReconciliationQuery, IReadOnlyList<PaymentReconciliationDto>>
{
    private const int MaxRows = 1000;

    public async Task<Result<IReadOnlyList<PaymentReconciliationDto>>> Handle(GetPaymentReconciliationQuery request, CancellationToken cancellationToken)
    {
        // NF-04: una imputación a la línea de crédito (M5-10) no mueve dinero; no se concilia.
        var query = dbContext.Payments.AsNoTracking().Where(p => p.Origin != PaymentOrigins.CreditLine);

        if (request.Country is not null)
            query = query.Where(p => p.Country == request.Country);
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(p => p.Status == request.Status);

        // El período se aplica sobre la fecha del pago con un día de margen y luego en la hora local del país.
        if (request.From is { } from)
        {
            var fromUtc = from.AddDays(-1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(p => p.PaymentDate >= fromUtc);
        }

        if (request.To is { } to)
        {
            var toUtc = to.AddDays(2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(p => p.PaymentDate < toUtc);
        }

        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PaymentReconciliationDto> rows = payments
            .Where(p => InPeriod(p, request.From, request.To))
            .Select(p => new PaymentReconciliationDto(
                p.Id, p.PaymentNumber, p.Country, p.Status, p.PaymentMethodCode ?? p.PaymentMethod, p.Currency, p.TotalAmount,
                p.CreatedAt == default ? p.PaymentDate : p.CreatedAt, p.ConfirmedAt, p.ExternalReference, p.ProviderReference,
                p.ProviderTransactionId, p.ReceiptNumber, p.SlipNumber, p.PayerTaxId, StatusOf(p)))
            .ToList();

        return Result<IReadOnlyList<PaymentReconciliationDto>>.Success(rows);
    }

    private static bool InPeriod(Payment payment, DateOnly? from, DateOnly? to)
    {
        var local = Domain.Charges.BusinessCalendar.LocalDate(payment.Country, payment.PaymentDate);
        return (from is null || local >= from) && (to is null || local <= to);
    }

    /// <summary>
    /// NF-04: confirmado, con comprobante y con la referencia de la transacción del proveedor (o, sin
    /// proveedor, la boleta de depósito confirmada por Finanzas).
    /// </summary>
    public static string StatusOf(Payment payment)
    {
        if (payment.Status != PaymentStatus.Confirmed)
            return ReconciliationStatus.NotSettled;
        if (payment.ReceiptNumber is null)
            return ReconciliationStatus.MissingReceipt;

        var hasReference = payment.ProviderKey is null
            ? payment.SlipNumber is not null || payment.ExternalReference is not null
            : payment.ProviderTransactionId is not null || payment.ProviderReference is not null;

        return hasReference ? ReconciliationStatus.Matched : ReconciliationStatus.MissingTransactionReference;
    }
}
