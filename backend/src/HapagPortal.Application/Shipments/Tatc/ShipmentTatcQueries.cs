namespace HapagPortal.Application.Shipments.Tatc;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Demurrage.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.Shipments;
using Microsoft.EntityFrameworkCore;

/// <summary>TATC de un contenedor: estado del portal, código del origen, número y motivos pendientes.</summary>
public sealed record ContainerTatcDto(
    string ContainerNumber,
    string? TatcNumber,
    string Status,
    string SourceStatus,
    DateTime? IssuedAt,
    string? WarehouseCode,
    IReadOnlyList<string> PendingReasons);

/// <summary>
/// Estado del BL y de su TATC (M2-09, CL-IMP-13, BO-IMP-13), leído del sistema de TATC en el momento (solo
/// caché corta). <c>Available</c> = el sistema respondió; si no, <c>ErrorCode</c> (NF-11) y sin contenedores.
/// <c>Status</c> es el agregado del BL (<c>TatcStatuses</c>); <c>SourceUpdatedAt</c>, la fecha del dato en el origen.
/// </summary>
public sealed record ShipmentTatcDto(
    string BlNumber,
    string? BookingNumber,
    string Country,
    string BlStatus,
    DateTime? Eta,
    bool Available,
    string? Status,
    IReadOnlyList<ContainerTatcDto> Containers,
    DateTime? SourceUpdatedAt,
    DateTime RetrievedAt,
    string? ErrorCode,
    bool CanRequestGeneration);

public sealed record TatcBatchItemDto(
    int LineNumber,
    string BlNumber,
    Guid? BillOfLadingId,
    string Status,
    string? ReasonCode);

/// <summary>Solicitud masiva de TATC (M2-09) con el resultado por BL (sin líneas en el listado).</summary>
public sealed record TatcBatchDto(
    Guid Id,
    string Country,
    string LocationCode,
    string Status,
    string? SourceRequestId,
    string? ErrorCode,
    int TotalItems,
    int AcceptedItems,
    int RejectedItems,
    string RequestedBy,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    IReadOnlyList<TatcBatchItemDto>? Items);

public sealed record GetShipmentTatcQuery(string BlNumber) : IQuery<ShipmentTatcDto>;

/// <summary>
/// Generación masiva de TATC para BL de importación de una misma localidad (UN/LOCODE del puerto de descarga
/// o del destino final), p. ej. la operación por Iquique de un cliente de alto volumen (M2-09). Operación de la
/// propia organización con un perfil que opera; cada BL exige <c>tatc.download</c> (M1-11).
/// </summary>
public sealed record RequestTatcBatchCommand(string LocationCode, IReadOnlyList<string> BlNumbers) : ICommand<TatcBatchDto>;

public sealed record GetTatcBatchesQuery : IQuery<IReadOnlyList<TatcBatchDto>>;

public sealed record GetTatcBatchQuery(Guid Id) : IQuery<TatcBatchDto>;

public sealed class GetShipmentTatcQueryValidator : AbstractValidator<GetShipmentTatcQuery>
{
    public GetShipmentTatcQueryValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class RequestTatcBatchCommandValidator : AbstractValidator<RequestTatcBatchCommand>
{
    public const int MaxItems = 500;

    public RequestTatcBatchCommandValidator()
    {
        RuleFor(x => x.LocationCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{2}[A-Za-z0-9]{3}$")
            .WithMessage("LocationCode must be a UN/LOCODE (e.g. CLIQQ).");
        RuleFor(x => x.BlNumbers).NotNull();
        RuleFor(x => x.BlNumbers.Count)
            .InclusiveBetween(1, MaxItems)
            .WithName("BlNumbers")
            .When(x => x.BlNumbers is not null);
        RuleForEach(x => x.BlNumbers).NotEmpty().MaximumLength(50);
    }
}

internal static class TatcViews
{
    public static TatcBatchDto ToDto(TatcBatch batch, bool includeItems) => new(
        batch.Id,
        batch.Country,
        batch.LocationCode,
        batch.Status,
        batch.SourceRequestId,
        batch.ErrorCode,
        batch.TotalItems,
        batch.AcceptedItems,
        batch.RejectedItems,
        batch.RequestedByEmail,
        batch.CreatedAt,
        batch.CompletedAt,
        includeItems
            ? batch.Items
                .OrderBy(i => i.LineNumber)
                .Select(i => new TatcBatchItemDto(i.LineNumber, i.BlNumber, i.BillOfLadingId, i.Status, i.ReasonCode))
                .ToList()
            : null);
}

public sealed class GetShipmentTatcQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ITatcProvider tatcProvider)
    : IQueryHandler<GetShipmentTatcQuery, ShipmentTatcDto>
{
    public async Task<Result<ShipmentTatcDto>> Handle(GetShipmentTatcQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: false, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<ShipmentTatcDto>.Failure(loaded.Error);

        var (bl, permissions, _, scope) = loaded.Value;
        if (!DemurrageStateEvaluator.IsImport(bl))
            return Result<ShipmentTatcDto>.Failure(DomainErrors.Tatc.NotApplicable);
        if (!permissions.Can(ShipmentActionCodes.DownloadTatc))
            return Result<ShipmentTatcDto>.Failure(Error.Forbidden);

        var now = DateTime.UtcNow;
        var result = await tatcProvider.GetByBlNumberAsync(bl.BLNumber, cancellationToken);
        if (result.IsFailure)
        {
            return Result<ShipmentTatcDto>.Success(new ShipmentTatcDto(
                bl.BLNumber, bl.BookingNumber, bl.Country, bl.Status, bl.ETA, Available: false, Status: null, [],
                null, now, result.Error.Code, CanRequestGeneration: false));
        }

        var containers = (result.Value?.Containers ?? [])
            .Select(c => new ContainerTatcDto(
                c.ContainerNumber,
                c.TatcNumber,
                TatcStatusMapper.MapContainer(c.Status),
                c.Status,
                c.IssuedAt,
                c.WarehouseCode,
                c.PendingReasons))
            .ToList();
        var status = TatcStatusMapper.Aggregate(containers.Select(c => c.Status).ToList());

        // La generación es de la propia organización: la visibilidad del administrador no la habilita.
        var canRequest = !scope.IsAdmin
            && permissions.CanExecute(ShipmentActionCodes.DownloadTatc)
            && status is not TatcStatuses.Issued and not TatcStatuses.Cancelled;

        return Result<ShipmentTatcDto>.Success(new ShipmentTatcDto(
            bl.BLNumber, bl.BookingNumber, bl.Country, bl.Status, bl.ETA, Available: true, status, containers,
            result.Value?.UpdatedAt, now, ErrorCode: null, canRequest));
    }
}

public sealed class RequestTatcBatchCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService,
    ITatcProvider tatcProvider)
    : ICommandHandler<RequestTatcBatchCommand, TatcBatchDto>
{
    public async Task<Result<TatcBatchDto>> Handle(RequestTatcBatchCommand request, CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (scope.IsAdmin || !scope.CanOperate || scope.OrganizationId is null)
            return Result<TatcBatchDto>.Failure(Error.Forbidden);

        var organization = await dbContext.Clients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == scope.OrganizationId.Value, cancellationToken);
        if (organization is null)
            return Result<TatcBatchDto>.Failure(Error.Unauthorized);

        var location = ShipmentPublication.NormalizeCode(request.LocationCode)!;
        var numbers = request.BlNumbers.Select(n => n.Trim().ToUpperInvariant()).ToList();
        var distinct = numbers.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var bls = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope)
            .Where(b => distinct.Contains(b.BLNumber))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var batch = new TatcBatch
        {
            ClientId = organization.Id,
            RequestedByUserId = scope.UserId ?? Guid.Empty,
            RequestedByEmail = currentUserService.Email ?? string.Empty,
            Country = string.Empty,
            LocationCode = location,
            Status = TatcBatchStatus.Failed,
            TotalItems = numbers.Count,
            CreatedAt = now
        };

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var valid = new List<TatcBatchItem>();
        var line = 0;

        foreach (var number in numbers)
        {
            var item = new TatcBatchItem
            {
                BatchId = batch.Id,
                LineNumber = ++line,
                BlNumber = number,
                Status = TatcBatchItemStatus.Failed
            };
            batch.Items.Add(item);

            if (!seen.Add(number))
            {
                item.ReasonCode = TatcBatchReasons.Duplicate;
                continue;
            }

            var bl = bls.FirstOrDefault(b => string.Equals(b.BLNumber, number, StringComparison.OrdinalIgnoreCase));
            if (bl is null)
            {
                item.ReasonCode = TatcBatchReasons.NotFound;
                continue;
            }

            item.BlNumber = bl.BLNumber;
            item.BillOfLadingId = bl.Id;

            var permissions = await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken);
            item.ReasonCode = Validate(bl, permissions, location, batch.Country);
            if (item.ReasonCode is not null)
                continue;

            if (batch.Country.Length == 0)
                batch.Country = bl.Country;

            valid.Add(item);
        }

        if (batch.Country.Length == 0)
            batch.Country = organization.Country;

        if (valid.Count == 0)
        {
            batch.ErrorCode = DomainErrors.Tatc.NoValidItems.Code;
        }
        else
        {
            var receipt = await tatcProvider.RequestGenerationAsync(
                new TatcGenerationRequest(
                    batch.Id.ToString(), batch.Country, location, organization.TaxId, valid.Select(i => i.BlNumber).ToList()),
                cancellationToken);

            if (receipt.IsFailure)
            {
                batch.ErrorCode = receipt.Error.Code;
                foreach (var item in valid)
                    item.ReasonCode = TatcBatchReasons.SourceUnavailable;
            }
            else
            {
                batch.SourceRequestId = receipt.Value.RequestId;
                foreach (var item in valid)
                {
                    var result = receipt.Value.Items.FirstOrDefault(r =>
                        string.Equals(r.BlNumber, item.BlNumber, StringComparison.OrdinalIgnoreCase));
                    item.Status = result?.Accepted == true ? TatcBatchItemStatus.Accepted : TatcBatchItemStatus.Rejected;
                    item.ReasonCode = result?.Accepted == true ? null : result?.ReasonCode ?? TatcPendingReasons.Other;
                }
            }
        }

        batch.AcceptedItems = batch.Items.Count(i => i.Status == TatcBatchItemStatus.Accepted);
        batch.RejectedItems = batch.Items.Count - batch.AcceptedItems;
        batch.Status = batch.AcceptedItems == 0
            ? TatcBatchStatus.Failed
            : batch.RejectedItems == 0 ? TatcBatchStatus.Completed : TatcBatchStatus.CompletedWithErrors;
        batch.CompletedAt = now;

        dbContext.TatcBatches.Add(batch);
        foreach (var item in batch.Items)
            dbContext.TatcBatchItems.Add(item);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<TatcBatchDto>.Success(TatcViews.ToDto(batch, includeItems: true));
    }

    private static string? Validate(BillOfLading bl, ShipmentPermissionSet permissions, string location, string batchCountry)
    {
        if (!permissions.CanExecute(ShipmentActionCodes.DownloadTatc))
            return TatcBatchReasons.NoPermission;
        if (!DemurrageStateEvaluator.IsImport(bl))
            return TatcBatchReasons.NotImport;
        if (bl.PortOfDischargeCode != location && bl.FinalDestinationCode != location)
            return TatcBatchReasons.OtherLocation;
        if (batchCountry.Length > 0 && bl.Country != batchCountry)
            return TatcBatchReasons.OtherCountry;
        return null;
    }
}

public sealed class GetTatcBatchesQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetTatcBatchesQuery, IReadOnlyList<TatcBatchDto>>
{
    public async Task<Result<IReadOnlyList<TatcBatchDto>>> Handle(GetTatcBatchesQuery request, CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (scope.OrganizationId is null || !scope.IsOperational || scope.IsAdmin)
            return Result<IReadOnlyList<TatcBatchDto>>.Success([]);

        var batches = await dbContext.TatcBatches.AsNoTracking()
            .Where(b => b.ClientId == scope.OrganizationId.Value)
            .OrderByDescending(b => b.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        IReadOnlyList<TatcBatchDto> items = batches.Select(b => TatcViews.ToDto(b, includeItems: false)).ToList();
        return Result<IReadOnlyList<TatcBatchDto>>.Success(items);
    }
}

public sealed class GetTatcBatchQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetTatcBatchQuery, TatcBatchDto>
{
    public async Task<Result<TatcBatchDto>> Handle(GetTatcBatchQuery request, CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);

        var batch = await dbContext.TatcBatches.AsNoTracking()
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        // NF-05: la solicitud de otra organización no se distingue de una inexistente.
        if (batch is null || (!scope.IsAdmin && batch.ClientId != scope.OrganizationId))
            return Result<TatcBatchDto>.Failure(DomainErrors.Tatc.BatchNotFound(request.Id));

        return Result<TatcBatchDto>.Success(TatcViews.ToDto(batch, includeItems: true));
    }
}
