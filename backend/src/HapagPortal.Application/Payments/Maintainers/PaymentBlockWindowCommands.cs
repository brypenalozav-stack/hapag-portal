namespace HapagPortal.Application.Payments.Maintainers;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Payments;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Ventana de bloqueo de pagos (M8-07). Fechas y horas locales del país (o de cada país si no tiene);
/// <c>Status</c> se calcula en el momento de la consulta: Scheduled, Active, Ended o Cancelled.
/// </summary>
public sealed record PaymentBlockWindowDto(
    Guid Id,
    string? Country,
    DateOnly StartDate,
    TimeOnly StartTime,
    DateOnly EndDate,
    TimeOnly EndTime,
    string Reason,
    string ClientMessage,
    string Status,
    string TimeZone,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? ModifiedAt,
    string? ModifiedBy);

/// <summary>Instantánea de la ventana para el registro de cambios (M8-07 criterio 8, NF-15).</summary>
public sealed record PaymentBlockWindowSnapshot(
    string? Country,
    DateOnly StartDate,
    TimeOnly StartTime,
    DateOnly EndDate,
    TimeOnly EndTime,
    string Reason,
    string ClientMessage,
    bool IsActive)
{
    public static PaymentBlockWindowSnapshot From(PaymentBlockWindow w) =>
        new(w.Country, w.StartDate, w.StartTime, w.EndDate, w.EndTime, w.Reason, w.ClientMessage, w.IsActive);
}

public sealed record GetPaymentBlockWindowsQuery(string? Country, bool IncludeCancelled) : IQuery<IReadOnlyList<PaymentBlockWindowDto>>;

public sealed record CreatePaymentBlockWindowCommand(
    string? Country,
    DateOnly StartDate,
    TimeOnly StartTime,
    DateOnly EndDate,
    TimeOnly EndTime,
    string Reason,
    string ClientMessage) : ICommand<PaymentBlockWindowDto>;

/// <summary>Modificación de una programación que todavía no comienza (M8-07, criterio 7).</summary>
public sealed record UpdatePaymentBlockWindowCommand(
    Guid Id,
    string? Country,
    DateOnly StartDate,
    TimeOnly StartTime,
    DateOnly EndDate,
    TimeOnly EndTime,
    string Reason,
    string ClientMessage) : ICommand<PaymentBlockWindowDto>;

/// <summary>Cancela una programación (antes de su inicio, o la termina antes si está activa).</summary>
public sealed record CancelPaymentBlockWindowCommand(Guid Id) : ICommand;

public sealed record GetPaymentBlockWindowHistoryQuery(Guid Id)
    : IQuery<IReadOnlyList<PaymentMaintainerChangeDto<PaymentBlockWindowSnapshot>>>;

/// <summary>Bloqueo vigente para el cliente (las consultas siguen disponibles durante el bloqueo).</summary>
public sealed record GetPaymentBlockStatusQuery(string? Country) : IQuery<PaymentBlockStatusDto>;

internal static class PaymentBlockWindowFields
{
    public static void Apply<T>(AbstractValidator<T> validator, Func<T, PaymentBlockWindowSnapshot> fields)
    {
        validator.RuleFor(x => fields(x).Country)
            .Must(c => c is null || CountryCodes.ValidCountries.Contains(c))
            .WithName("Country")
            .WithMessage("Country must be CL, BO or empty (all countries).");
        validator.RuleFor(x => fields(x))
            .Must(f => f.EndDate.ToDateTime(f.EndTime) > f.StartDate.ToDateTime(f.StartTime))
            .WithName("EndDate")
            .WithMessage("The end must be after the start.");
        validator.RuleFor(x => fields(x).Reason).NotEmpty().MaximumLength(300).WithName("Reason");
        validator.RuleFor(x => fields(x).ClientMessage).NotEmpty().MaximumLength(500).WithName("ClientMessage");
    }

    public static PaymentBlockWindowSnapshot Normalize(
        string? country, DateOnly startDate, TimeOnly startTime, DateOnly endDate, TimeOnly endTime, string reason, string clientMessage) =>
        new(
            string.IsNullOrWhiteSpace(country) ? null : country.Trim().ToUpperInvariant(),
            startDate,
            new TimeOnly(startTime.Hour, startTime.Minute),
            endDate,
            new TimeOnly(endTime.Hour, endTime.Minute),
            (reason ?? string.Empty).Trim(),
            (clientMessage ?? string.Empty).Trim(),
            true);

    public static void Write(PaymentBlockWindow window, PaymentBlockWindowSnapshot fields)
    {
        window.Country = fields.Country;
        window.StartDate = fields.StartDate;
        window.StartTime = fields.StartTime;
        window.EndDate = fields.EndDate;
        window.EndTime = fields.EndTime;
        window.Reason = fields.Reason;
        window.ClientMessage = fields.ClientMessage;
    }

    public static PaymentBlockWindowDto ToDto(PaymentBlockWindow w, DateTime nowUtc) => new(
        w.Id, w.Country, w.StartDate, w.StartTime, w.EndDate, w.EndTime, w.Reason, w.ClientMessage,
        PaymentBlockSchedule.StatusOf(w, nowUtc), BusinessCalendar.TimeZoneId(w.Country ?? CountryCodes.Chile),
        w.CreatedAt, w.CreatedBy, w.ModifiedAt, w.ModifiedBy);
}

public sealed class CreatePaymentBlockWindowCommandValidator : AbstractValidator<CreatePaymentBlockWindowCommand>
{
    public CreatePaymentBlockWindowCommandValidator()
    {
        PaymentBlockWindowFields.Apply(this, x => PaymentBlockWindowFields.Normalize(
            x.Country, x.StartDate, x.StartTime, x.EndDate, x.EndTime, x.Reason, x.ClientMessage));
    }
}

public sealed class UpdatePaymentBlockWindowCommandValidator : AbstractValidator<UpdatePaymentBlockWindowCommand>
{
    public UpdatePaymentBlockWindowCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        PaymentBlockWindowFields.Apply(this, x => PaymentBlockWindowFields.Normalize(
            x.Country, x.StartDate, x.StartTime, x.EndDate, x.EndTime, x.Reason, x.ClientMessage));
    }
}

public sealed class GetPaymentBlockWindowsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetPaymentBlockWindowsQuery, IReadOnlyList<PaymentBlockWindowDto>>
{
    public async Task<Result<IReadOnlyList<PaymentBlockWindowDto>>> Handle(GetPaymentBlockWindowsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.PaymentBlockWindows.AsNoTracking();
        if (!request.IncludeCancelled)
            query = query.Where(w => w.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            var country = request.Country.Trim().ToUpperInvariant();
            query = query.Where(w => w.Country == null || w.Country == country);
        }

        var windows = await query
            .OrderByDescending(w => w.StartDate)
            .ThenByDescending(w => w.StartTime)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        IReadOnlyList<PaymentBlockWindowDto> items = windows.Select(w => PaymentBlockWindowFields.ToDto(w, now)).ToList();
        return Result<IReadOnlyList<PaymentBlockWindowDto>>.Success(items);
    }
}

public sealed class CreatePaymentBlockWindowCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreatePaymentBlockWindowCommand, PaymentBlockWindowDto>
{
    public async Task<Result<PaymentBlockWindowDto>> Handle(CreatePaymentBlockWindowCommand request, CancellationToken cancellationToken)
    {
        var fields = PaymentBlockWindowFields.Normalize(
            request.Country, request.StartDate, request.StartTime, request.EndDate, request.EndTime, request.Reason, request.ClientMessage);

        var window = new PaymentBlockWindow { Reason = fields.Reason, ClientMessage = fields.ClientMessage };
        PaymentBlockWindowFields.Write(window, fields);

        var now = DateTime.UtcNow;
        if (PaymentBlockSchedule.HasEnded(window, now))
            return Result<PaymentBlockWindowDto>.Failure(DomainErrors.PaymentBlockWindow.AlreadyEnded);

        dbContext.PaymentBlockWindows.Add(window);
        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.PaymentBlockWindow, window.Id, MaintainerActions.Created,
            null, PaymentBlockWindowSnapshot.From(window), now);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<PaymentBlockWindowDto>.Success(PaymentBlockWindowFields.ToDto(window, now));
    }
}

public sealed class UpdatePaymentBlockWindowCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdatePaymentBlockWindowCommand, PaymentBlockWindowDto>
{
    public async Task<Result<PaymentBlockWindowDto>> Handle(UpdatePaymentBlockWindowCommand request, CancellationToken cancellationToken)
    {
        var window = await dbContext.PaymentBlockWindows.FirstOrDefaultAsync(w => w.Id == request.Id && w.IsActive, cancellationToken);
        if (window is null)
            return Result<PaymentBlockWindowDto>.Failure(DomainErrors.PaymentBlockWindow.NotFound(request.Id));

        var now = DateTime.UtcNow;
        if (PaymentBlockSchedule.HasStarted(window, now))
            return Result<PaymentBlockWindowDto>.Failure(DomainErrors.PaymentBlockWindow.AlreadyStarted);

        var previous = PaymentBlockWindowSnapshot.From(window);
        PaymentBlockWindowFields.Write(window, PaymentBlockWindowFields.Normalize(
            request.Country, request.StartDate, request.StartTime, request.EndDate, request.EndTime, request.Reason, request.ClientMessage));

        if (PaymentBlockSchedule.HasEnded(window, now))
            return Result<PaymentBlockWindowDto>.Failure(DomainErrors.PaymentBlockWindow.AlreadyEnded);

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.PaymentBlockWindow, window.Id, MaintainerActions.Updated,
            previous, PaymentBlockWindowSnapshot.From(window), now);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<PaymentBlockWindowDto>.Success(PaymentBlockWindowFields.ToDto(window, now));
    }
}

public sealed class CancelPaymentBlockWindowCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CancelPaymentBlockWindowCommand>
{
    public async Task<Result> Handle(CancelPaymentBlockWindowCommand request, CancellationToken cancellationToken)
    {
        var window = await dbContext.PaymentBlockWindows.FirstOrDefaultAsync(w => w.Id == request.Id && w.IsActive, cancellationToken);
        if (window is null)
            return Result.Failure(DomainErrors.PaymentBlockWindow.NotFound(request.Id));

        var now = DateTime.UtcNow;
        if (PaymentBlockSchedule.HasEnded(window, now))
            return Result.Failure(DomainErrors.PaymentBlockWindow.AlreadyEnded);

        var previous = PaymentBlockWindowSnapshot.From(window);
        window.IsActive = false;

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.PaymentBlockWindow, window.Id, MaintainerActions.Deactivated,
            previous, PaymentBlockWindowSnapshot.From(window), now);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class GetPaymentBlockWindowHistoryQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetPaymentBlockWindowHistoryQuery, IReadOnlyList<PaymentMaintainerChangeDto<PaymentBlockWindowSnapshot>>>
{
    public async Task<Result<IReadOnlyList<PaymentMaintainerChangeDto<PaymentBlockWindowSnapshot>>>> Handle(
        GetPaymentBlockWindowHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.PaymentBlockWindows.AsNoTracking().AnyAsync(w => w.Id == request.Id, cancellationToken);
        if (!exists)
            return Result<IReadOnlyList<PaymentMaintainerChangeDto<PaymentBlockWindowSnapshot>>>.Failure(DomainErrors.PaymentBlockWindow.NotFound(request.Id));

        var changes = await dbContext.MaintainerChangeLogs.AsNoTracking()
            .Where(c => c.Maintainer == MaintainerNames.PaymentBlockWindow && c.EntityId == request.Id)
            .OrderByDescending(c => c.ChangedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PaymentMaintainerChangeDto<PaymentBlockWindowSnapshot>> items = changes
            .Select(c => new PaymentMaintainerChangeDto<PaymentBlockWindowSnapshot>(
                c.Id, c.EntityId, c.Action, c.ChangedAt, c.ChangedBy, c.ChangedByUserId,
                MaintainerChangeLogger.Read<PaymentBlockWindowSnapshot>(c.PreviousValue),
                MaintainerChangeLogger.Read<PaymentBlockWindowSnapshot>(c.NewValue)))
            .ToList();

        return Result<IReadOnlyList<PaymentMaintainerChangeDto<PaymentBlockWindowSnapshot>>>.Success(items);
    }
}

public sealed class GetPaymentBlockStatusQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetPaymentBlockStatusQuery, PaymentBlockStatusDto>
{
    public async Task<Result<PaymentBlockStatusDto>> Handle(GetPaymentBlockStatusQuery request, CancellationToken cancellationToken)
    {
        var country = (request.Country ?? currentUserService.Country ?? CountryCodes.Chile).Trim().ToUpperInvariant();
        if (!CountryCodes.ValidCountries.Contains(country))
            country = CountryCodes.Chile;

        return Result<PaymentBlockStatusDto>.Success(
            await PaymentBlocks.StatusAsync(dbContext, country, DateTime.UtcNow, cancellationToken));
    }
}
