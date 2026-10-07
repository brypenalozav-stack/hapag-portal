namespace HapagPortal.Application.Counter;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Registro de Counter de un BL (M8-09) y su sincronización con Nexus.</summary>
public sealed record CounterRecordDto(
    Guid Id,
    Guid BillOfLadingId,
    string BlNumber,
    string Country,
    DateOnly? ExchangeDate,
    bool HblReceived,
    DateOnly? HblReceivedAt,
    bool Deconsolidated,
    DateOnly? DeconsolidatedAt,
    string? Notes,
    string SyncStatus,
    DateTime? SyncedAt,
    string? SyncError,
    string? SourceReference,
    string RecordedBy,
    DateTime RecordedAt,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? ModifiedAt,
    string? ModifiedBy);

public sealed record CounterShipmentDto(
    Guid Id,
    string BlNumber,
    string? BookingNumber,
    string Country,
    string Operation,
    string? Vessel,
    string? Voyage,
    string? PortOfDischarge,
    string? PlaceOfDelivery);

/// <summary>
/// Estado de Counter de un BL: el registro local (nulo si aún no se registró) y el estado vigente en Nexus
/// (<see cref="SourceAvailable"/> = Nexus respondió; si no, <see cref="SourceErrorCode"/>, NF-11).
/// </summary>
public sealed record CounterDetailDto(
    CounterShipmentDto Shipment,
    CounterRecordDto? Record,
    bool SourceAvailable,
    CounterSourceRecord? Source,
    string? SourceErrorCode);

/// <summary>Instantánea para el registro de cambios (NF-15).</summary>
public sealed record CounterRecordSnapshot(
    string BlNumber,
    string Country,
    DateOnly? ExchangeDate,
    bool HblReceived,
    DateOnly? HblReceivedAt,
    bool Deconsolidated,
    DateOnly? DeconsolidatedAt,
    string? Notes,
    string SyncStatus,
    string? SourceReference)
{
    public static CounterRecordSnapshot From(CounterRecord r) =>
        new(r.BlNumber, r.Country, r.ExchangeDate, r.HblReceived, r.HblReceivedAt, r.Deconsolidated, r.DeconsolidatedAt, r.Notes,
            r.SyncStatus, r.SourceReference);
}

public sealed record GetCounterRecordsQuery(
    string? BlNumber = null,
    string? Country = null,
    string? SyncStatus = null,
    bool? Deconsolidated = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<CounterRecordDto>>;

public sealed record GetCounterRecordQuery(string BlNumber) : IQuery<CounterDetailDto>;

/// <summary>Registra o modifica el Counter de un BL y lo propaga a Nexus (M8-09).</summary>
public sealed record UpsertCounterRecordCommand(
    string BlNumber,
    string Country,
    DateOnly? ExchangeDate,
    bool HblReceived,
    DateOnly? HblReceivedAt,
    bool Deconsolidated,
    DateOnly? DeconsolidatedAt,
    string? Notes) : ICommand<CounterRecordDto>;

/// <summary>Reintenta la propagación a Nexus de un registro pendiente o fallido.</summary>
public sealed record SyncCounterRecordCommand(string BlNumber) : ICommand<CounterRecordDto>;

public sealed record GetCounterHistoryQuery(string BlNumber)
    : IQuery<IReadOnlyList<PaymentMaintainerChangeDto<CounterRecordSnapshot>>>;

public sealed class UpsertCounterRecordCommandValidator : AbstractValidator<UpsertCounterRecordCommand>
{
    public UpsertCounterRecordCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Country)
            .Must(c => CountryCodes.ValidCountries.Contains(c?.Trim().ToUpperInvariant()))
            .WithMessage("Country must be CL or BO.");
        RuleFor(x => x.HblReceivedAt).Null().When(x => !x.HblReceived)
            .WithMessage("HblReceivedAt applies only when the HBL was received.");
        RuleFor(x => x.DeconsolidatedAt).Null().When(x => !x.Deconsolidated)
            .WithMessage("DeconsolidatedAt applies only when the shipment is deconsolidated.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class GetCounterRecordsQueryValidator : AbstractValidator<GetCounterRecordsQuery>
{
    public GetCounterRecordsQueryValidator()
    {
        RuleFor(x => x.SyncStatus).Must(s => s is null || CounterSyncStatus.All.Contains(s)).WithMessage("SyncStatus is not valid.");
        RuleFor(x => x.BlNumber).MaximumLength(50);
    }
}

public static class CounterViews
{
    public static CounterRecordDto ToDto(CounterRecord r) => new(
        r.Id, r.BillOfLadingId, r.BlNumber, r.Country, r.ExchangeDate, r.HblReceived, r.HblReceivedAt, r.Deconsolidated, r.DeconsolidatedAt,
        r.Notes, r.SyncStatus, r.SyncedAt, r.SyncError, r.SourceReference, r.RecordedBy, r.RecordedAt, r.CreatedAt, r.CreatedBy,
        r.ModifiedAt, r.ModifiedBy);

    public static CounterShipmentDto Shipment(BillOfLading bl) => new(
        bl.Id, bl.BLNumber, bl.BookingNumber, bl.Country, bl.ShipmentType.Trim().ToUpperInvariant(), bl.Vessel, bl.Voyage,
        bl.PortOfDischarge, bl.PlaceOfDelivery);
}

/// <summary>
/// Propagación del registro de Counter a Nexus (<see cref="ICounterRecorder"/>). El registro local se guarda primero; si
/// Nexus no responde queda <c>Failed</c> con el error y se reintenta con <see cref="SyncCounterRecordCommand"/>. La clave
/// de idempotencia es la entrada del registro de cambios, de modo que reintentar el mismo cambio no lo duplica.
/// </summary>
public sealed class CounterSynchronizer(IApplicationDbContext dbContext, ICounterRecorder counterRecorder)
{
    public async Task PropagateAsync(CounterRecord record, Guid changeId, DateTime now, CancellationToken cancellationToken)
    {
        var result = await counterRecorder.RecordAsync(
            new CounterRecordRequest(record.BlNumber, record.Country, record.ExchangeDate, record.HblReceived, record.HblReceivedAt,
                record.Deconsolidated, record.DeconsolidatedAt, record.Notes, record.RecordedBy),
            $"counter-{changeId:N}",
            cancellationToken);

        if (result.IsSuccess)
        {
            record.SyncStatus = CounterSyncStatus.Synced;
            record.SyncedAt = now;
            record.SyncError = null;
            record.SourceReference = result.Value.SourceReference;
        }
        else
        {
            record.SyncStatus = CounterSyncStatus.Failed;
            record.SyncError = result.Error.Code;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class GetCounterRecordsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetCounterRecordsQuery, PagedResult<CounterRecordDto>>
{
    public async Task<Result<PagedResult<CounterRecordDto>>> Handle(GetCounterRecordsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var query = dbContext.CounterRecords.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.BlNumber))
        {
            var term = request.BlNumber.Trim().ToUpper();
            query = query.Where(r => r.BlNumber.ToUpper().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            var country = request.Country.Trim().ToUpperInvariant();
            query = query.Where(r => r.Country == country);
        }

        if (!string.IsNullOrWhiteSpace(request.SyncStatus))
            query = query.Where(r => r.SyncStatus == request.SyncStatus);
        if (request.Deconsolidated is { } deconsolidated)
            query = query.Where(r => r.Deconsolidated == deconsolidated);

        var total = await query.CountAsync(cancellationToken);
        var records = await query.OrderByDescending(r => r.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<CounterRecordDto>>.Success(
            new PagedResult<CounterRecordDto>(records.Select(CounterViews.ToDto).ToList(), total, page, pageSize));
    }
}

public sealed class GetCounterRecordQueryHandler(IApplicationDbContext dbContext, ICounterRecorder counterRecorder)
    : IQueryHandler<GetCounterRecordQuery, CounterDetailDto>
{
    public async Task<Result<CounterDetailDto>> Handle(GetCounterRecordQuery request, CancellationToken cancellationToken)
    {
        var blNumber = request.BlNumber.Trim();
        var bl = await dbContext.BillsOfLading.AsNoTracking().FirstOrDefaultAsync(b => b.BLNumber == blNumber, cancellationToken);
        if (bl is null)
            return Result<CounterDetailDto>.Failure(DomainErrors.BillOfLading.NotFoundByNumber(blNumber));

        var record = await dbContext.CounterRecords.AsNoTracking().FirstOrDefaultAsync(r => r.BillOfLadingId == bl.Id, cancellationToken);
        var source = await counterRecorder.GetAsync(bl.BLNumber, cancellationToken);

        return Result<CounterDetailDto>.Success(new CounterDetailDto(
            CounterViews.Shipment(bl),
            record is null ? null : CounterViews.ToDto(record),
            source.IsSuccess,
            source.IsSuccess ? source.Value : null,
            source.IsFailure ? source.Error.Code : null));
    }
}

public sealed class UpsertCounterRecordCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    CounterSynchronizer synchronizer)
    : ICommandHandler<UpsertCounterRecordCommand, CounterRecordDto>
{
    public async Task<Result<CounterRecordDto>> Handle(UpsertCounterRecordCommand request, CancellationToken cancellationToken)
    {
        var blNumber = request.BlNumber.Trim();
        var bl = await dbContext.BillsOfLading.AsNoTracking().FirstOrDefaultAsync(b => b.BLNumber == blNumber, cancellationToken);
        if (bl is null)
            return Result<CounterRecordDto>.Failure(DomainErrors.BillOfLading.NotFoundByNumber(blNumber));

        var country = request.Country.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;
        var today = BusinessCalendar.LocalDate(country, now);
        if (new[] { request.ExchangeDate, request.HblReceivedAt, request.DeconsolidatedAt }.Any(d => d is not null && d > today))
            return Result<CounterRecordDto>.Failure(DomainErrors.Counter.Invalid("Counter dates cannot be in the future."));

        var actor = PaymentActor.From(currentUserService);
        var record = await dbContext.CounterRecords.FirstOrDefaultAsync(r => r.BillOfLadingId == bl.Id, cancellationToken);
        var previous = record is null ? null : CounterRecordSnapshot.From(record);

        if (record is null)
        {
            record = new CounterRecord
            {
                BillOfLadingId = bl.Id,
                BlNumber = bl.BLNumber,
                Country = country,
                SyncStatus = CounterSyncStatus.Pending,
                RecordedBy = actor.Name,
                CreatedAt = now,
                CreatedBy = actor.Name
            };
            dbContext.CounterRecords.Add(record);
        }
        else
        {
            record.ModifiedAt = now;
            record.ModifiedBy = actor.Name;
        }

        record.Country = country;
        record.ExchangeDate = request.ExchangeDate;
        record.HblReceived = request.HblReceived;
        record.HblReceivedAt = request.HblReceived ? request.HblReceivedAt ?? today : null;
        record.Deconsolidated = request.Deconsolidated;
        record.DeconsolidatedAt = request.Deconsolidated ? request.DeconsolidatedAt ?? today : null;
        record.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        record.SyncStatus = CounterSyncStatus.Pending;
        record.RecordedByUserId = currentUserService.UserId;
        record.RecordedBy = actor.Name;
        record.RecordedAt = now;

        var change = MaintainerChangeLogger.Log(dbContext, currentUserService, MaintainerNames.CounterRecord, record.Id,
            previous is null ? MaintainerActions.Created : MaintainerActions.Updated, previous, CounterRecordSnapshot.From(record), now);
        await dbContext.SaveChangesAsync(cancellationToken);

        await synchronizer.PropagateAsync(record, change.Id, now, cancellationToken);
        return Result<CounterRecordDto>.Success(CounterViews.ToDto(record));
    }
}

public sealed class SyncCounterRecordCommandHandler(
    IApplicationDbContext dbContext,
    CounterSynchronizer synchronizer)
    : ICommandHandler<SyncCounterRecordCommand, CounterRecordDto>
{
    public async Task<Result<CounterRecordDto>> Handle(SyncCounterRecordCommand request, CancellationToken cancellationToken)
    {
        var blNumber = request.BlNumber.Trim();
        var record = await dbContext.CounterRecords.FirstOrDefaultAsync(r => r.BlNumber == blNumber, cancellationToken);
        if (record is null)
            return Result<CounterRecordDto>.Failure(DomainErrors.Counter.NotFound(blNumber));
        if (record.SyncStatus == CounterSyncStatus.Synced)
            return Result<CounterRecordDto>.Failure(DomainErrors.Counter.NothingToSync);

        // Misma clave que el último cambio: Nexus no lo registra dos veces.
        var lastChange = await dbContext.MaintainerChangeLogs.AsNoTracking()
            .Where(c => c.Maintainer == MaintainerNames.CounterRecord && c.EntityId == record.Id)
            .OrderByDescending(c => c.ChangedAt)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        await synchronizer.PropagateAsync(record, lastChange ?? record.Id, DateTime.UtcNow, cancellationToken);
        return Result<CounterRecordDto>.Success(CounterViews.ToDto(record));
    }
}

public sealed class GetCounterHistoryQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetCounterHistoryQuery, IReadOnlyList<PaymentMaintainerChangeDto<CounterRecordSnapshot>>>
{
    public async Task<Result<IReadOnlyList<PaymentMaintainerChangeDto<CounterRecordSnapshot>>>> Handle(
        GetCounterHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var blNumber = request.BlNumber.Trim();
        var record = await dbContext.CounterRecords.AsNoTracking().FirstOrDefaultAsync(r => r.BlNumber == blNumber, cancellationToken);
        if (record is null)
            return Result<IReadOnlyList<PaymentMaintainerChangeDto<CounterRecordSnapshot>>>.Failure(DomainErrors.Counter.NotFound(blNumber));

        var changes = await dbContext.MaintainerChangeLogs.AsNoTracking()
            .Where(c => c.Maintainer == MaintainerNames.CounterRecord && c.EntityId == record.Id)
            .OrderByDescending(c => c.ChangedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PaymentMaintainerChangeDto<CounterRecordSnapshot>> items = changes
            .Select(c => new PaymentMaintainerChangeDto<CounterRecordSnapshot>(
                c.Id, c.EntityId, c.Action, c.ChangedAt, c.ChangedBy, c.ChangedByUserId,
                MaintainerChangeLogger.Read<CounterRecordSnapshot>(c.PreviousValue),
                MaintainerChangeLogger.Read<CounterRecordSnapshot>(c.NewValue)))
            .ToList();

        return Result<IReadOnlyList<PaymentMaintainerChangeDto<CounterRecordSnapshot>>>.Success(items);
    }
}
