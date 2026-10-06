namespace HapagPortal.Application.WarehouseChanges.Bulk;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Application.WarehouseChanges.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Línea de una solicitud masiva (lista cargada o selección múltiple en la interfaz).</summary>
public sealed record WarehouseChangeBatchItemRequest(
    string BlNumber,
    string? ContainerNumber,
    string? FromWarehouse,
    string ToWarehouse,
    string? TariffCode = null);

public sealed record WarehouseChangeBatchItemDto(
    int LineNumber,
    string BlNumber,
    string? ContainerNumber,
    string? FromWarehouse,
    string ToWarehouse,
    string? TariffCode,
    string Status,
    string? ErrorCode,
    string? ErrorMessage,
    Guid? WarehouseChangeId,
    DateTime? ProcessedAt);

/// <summary>Avance y resultado de una solicitud masiva (NF-19).</summary>
public sealed record WarehouseChangeBatchDto(
    Guid Id,
    string Status,
    int TotalItems,
    int ProcessedItems,
    int SucceededItems,
    int FailedItems,
    int ProgressPercent,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    IReadOnlyList<WarehouseChangeBatchItemDto>? Items);

/// <summary>
/// Solicitud masiva de cambios de almacén (M3-05) para clientes de alto volumen. El acceso y el permiso
/// sobre cada BL se validan al recibirla (NF-05); las líneas sin acceso quedan fallidas de inmediato y
/// el resto se procesa en segundo plano por tramos, sin degradar la operación individual (NF-19).
/// </summary>
public sealed record SubmitWarehouseChangeBatchCommand(IReadOnlyList<WarehouseChangeBatchItemRequest> Items)
    : ICommand<WarehouseChangeBatchDto>;

/// <summary>Procesa un tramo de líneas pendientes de las solicitudes masivas (proceso en segundo plano).</summary>
public sealed record ProcessWarehouseChangeBatchesCommand(int MaxItems = 50) : ICommand<int>;

public sealed record GetWarehouseChangeBatchQuery(Guid Id) : IQuery<WarehouseChangeBatchDto>;

public sealed record GetWarehouseChangeBatchesQuery : IQuery<IReadOnlyList<WarehouseChangeBatchDto>>;

public sealed class SubmitWarehouseChangeBatchCommandValidator : AbstractValidator<SubmitWarehouseChangeBatchCommand>
{
    public const int MaxItems = 500;

    public SubmitWarehouseChangeBatchCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty().Must(i => i.Count <= MaxItems)
            .WithMessage($"A bulk request accepts up to {MaxItems} lines.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.BlNumber).NotEmpty().MaximumLength(50);
            item.RuleFor(i => i.ContainerNumber).MaximumLength(20);
            item.RuleFor(i => i.FromWarehouse).MaximumLength(100);
            item.RuleFor(i => i.ToWarehouse).NotEmpty().MaximumLength(100);
            item.RuleFor(i => i.TariffCode).MaximumLength(20);
        });
    }
}

internal static class BatchMapper
{
    public static WarehouseChangeBatchDto ToDto(WarehouseChangeBatch batch, IEnumerable<WarehouseChangeBatchItem>? items) => new(
        batch.Id,
        batch.Status,
        batch.TotalItems,
        batch.ProcessedItems,
        batch.SucceededItems,
        batch.FailedItems,
        batch.TotalItems == 0 ? 100 : (int)Math.Floor(batch.ProcessedItems * 100m / batch.TotalItems),
        batch.CreatedAt,
        batch.StartedAt,
        batch.CompletedAt,
        items?.OrderBy(i => i.LineNumber)
            .Select(i => new WarehouseChangeBatchItemDto(
                i.LineNumber, i.BlNumber, i.ContainerNumber, i.FromWarehouse, i.ToWarehouse, i.TariffCode,
                i.Status, i.ErrorCode, i.ErrorMessage, i.WarehouseChangeId, i.ProcessedAt))
            .ToList());

    public static void Fail(WarehouseChangeBatch batch, WarehouseChangeBatchItem item, Error error, DateTime now)
    {
        item.Status = BulkItemStatus.Failed;
        item.ErrorCode = error.Code;
        item.ErrorMessage = error.Message.Length > 500 ? error.Message[..500] : error.Message;
        item.ProcessedAt = now;
        batch.ProcessedItems++;
        batch.FailedItems++;
    }

    public static void Close(WarehouseChangeBatch batch, DateTime now)
    {
        if (batch.ProcessedItems < batch.TotalItems)
            return;

        batch.Status = batch.FailedItems > 0 ? BulkRequestStatus.CompletedWithErrors : BulkRequestStatus.Completed;
        batch.CompletedAt = now;
    }
}

public sealed class SubmitWarehouseChangeBatchCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : ICommandHandler<SubmitWarehouseChangeBatchCommand, WarehouseChangeBatchDto>
{
    public async Task<Result<WarehouseChangeBatchDto>> Handle(SubmitWarehouseChangeBatchCommand request, CancellationToken cancellationToken)
    {
        var membership = await CurrentOrganization.LoadAsync(dbContext, currentUserService, requireApproved: true, cancellationToken);
        if (membership.IsFailure)
            return Result<WarehouseChangeBatchDto>.Failure(membership.Error);

        // La solicitud es de la propia organización: la visibilidad total del administrador no habilita operar.
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (scope.IsAdmin)
            scope = scope with { IsAdmin = false };

        var numbers = request.Items.Select(i => i.BlNumber.Trim()).Distinct().ToList();
        var bills = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope)
            .Where(b => numbers.Contains(b.BLNumber))
            .ToListAsync(cancellationToken);

        var allowed = new Dictionary<string, BillOfLading>(StringComparer.OrdinalIgnoreCase);
        foreach (var bl in bills)
        {
            var permissions = await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken);
            if (permissions.CanExecute(ShipmentActionCodes.RequestWarehouseChange))
                allowed[bl.BLNumber] = bl;
        }

        var now = DateTime.UtcNow;
        var batch = new WarehouseChangeBatch
        {
            ClientId = membership.Value.Organization.Id,
            RequestedByUserId = membership.Value.User.Id,
            Status = BulkRequestStatus.Queued,
            TotalItems = request.Items.Count
        };

        var line = 0;
        foreach (var requested in request.Items)
        {
            var number = requested.BlNumber.Trim();
            var item = new WarehouseChangeBatchItem
            {
                BatchId = batch.Id,
                LineNumber = ++line,
                BlNumber = number,
                ContainerNumber = string.IsNullOrWhiteSpace(requested.ContainerNumber) ? null : requested.ContainerNumber.Trim(),
                FromWarehouse = string.IsNullOrWhiteSpace(requested.FromWarehouse) ? null : requested.FromWarehouse.Trim(),
                ToWarehouse = requested.ToWarehouse.Trim(),
                TariffCode = string.IsNullOrWhiteSpace(requested.TariffCode) ? null : requested.TariffCode.Trim(),
                Status = BulkItemStatus.Pending
            };

            if (allowed.TryGetValue(number, out var bl))
                item.BillOfLadingId = bl.Id;
            else if (bills.Any(b => string.Equals(b.BLNumber, number, StringComparison.OrdinalIgnoreCase)))
                BatchMapper.Fail(batch, item, Error.Forbidden, now);
            else
                BatchMapper.Fail(batch, item, DomainErrors.BillOfLading.NotFoundByNumber(number), now);

            batch.Items.Add(item);
            dbContext.WarehouseChangeBatchItems.Add(item);
        }

        BatchMapper.Close(batch, now);
        dbContext.WarehouseChangeBatches.Add(batch);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<WarehouseChangeBatchDto>.Success(BatchMapper.ToDto(batch, batch.Items));
    }
}

/// <summary>
/// Procesa hasta <c>MaxItems</c> líneas pendientes, de las solicitudes más antiguas primero. Cada línea
/// se guarda por separado: una falla no afecta a las demás ni a la operación del resto de los usuarios.
/// </summary>
public sealed class ProcessWarehouseChangeBatchesCommandHandler(
    IApplicationDbContext dbContext,
    WarehouseChangeService warehouseChangeService)
    : ICommandHandler<ProcessWarehouseChangeBatchesCommand, int>
{
    public async Task<Result<int>> Handle(ProcessWarehouseChangeBatchesCommand request, CancellationToken cancellationToken)
    {
        var batches = await dbContext.WarehouseChangeBatches
            .Where(b => b.Status == BulkRequestStatus.Queued || b.Status == BulkRequestStatus.Processing)
            .OrderBy(b => b.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var processed = 0;

        foreach (var batch in batches)
        {
            if (processed >= request.MaxItems)
                break;

            var items = await dbContext.WarehouseChangeBatchItems
                .Where(i => i.BatchId == batch.Id && i.Status == BulkItemStatus.Pending)
                .OrderBy(i => i.LineNumber)
                .Take(request.MaxItems - processed)
                .ToListAsync(cancellationToken);

            var requester = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == batch.ClientId, cancellationToken);
            var now = DateTime.UtcNow;

            if (batch.Status == BulkRequestStatus.Queued)
            {
                batch.Status = BulkRequestStatus.Processing;
                batch.StartedAt = now;
            }

            foreach (var item in items)
            {
                var bl = item.BillOfLadingId is null
                    ? null
                    : await dbContext.BillsOfLading.AsNoTracking().FirstOrDefaultAsync(b => b.Id == item.BillOfLadingId.Value, cancellationToken);

                if (bl is null || requester is null)
                {
                    BatchMapper.Fail(batch, item, DomainErrors.BillOfLading.NotFoundByNumber(item.BlNumber), DateTime.UtcNow);
                }
                else
                {
                    var created = await warehouseChangeService.CreateAsync(
                        new WarehouseChangeInput(
                            bl, requester, batch.RequestedByUserId, item.ContainerNumber, item.FromWarehouse,
                            item.ToWarehouse, item.TariffCode, batch.Id),
                        cancellationToken);

                    if (created.IsFailure)
                    {
                        BatchMapper.Fail(batch, item, created.Error, DateTime.UtcNow);
                    }
                    else
                    {
                        item.Status = BulkItemStatus.Succeeded;
                        item.WarehouseChangeId = created.Value.Id;
                        item.ProcessedAt = DateTime.UtcNow;
                        batch.ProcessedItems++;
                        batch.SucceededItems++;
                    }
                }

                BatchMapper.Close(batch, DateTime.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                processed++;
            }

            if (items.Count == 0)
            {
                BatchMapper.Close(batch, now);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return Result<int>.Success(processed);
    }
}

public sealed class GetWarehouseChangeBatchQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetWarehouseChangeBatchQuery, WarehouseChangeBatchDto>
{
    public async Task<Result<WarehouseChangeBatchDto>> Handle(GetWarehouseChangeBatchQuery request, CancellationToken cancellationToken)
    {
        var membership = await CurrentOrganization.LoadAsync(dbContext, currentUserService, requireApproved: false, cancellationToken);
        if (membership.IsFailure)
            return Result<WarehouseChangeBatchDto>.Failure(membership.Error);

        var organizationId = membership.Value.Organization.Id;
        var batch = await dbContext.WarehouseChangeBatches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.Id && b.ClientId == organizationId, cancellationToken);

        if (batch is null)
            return Result<WarehouseChangeBatchDto>.Failure(DomainErrors.WarehouseChange.BatchNotFound(request.Id));

        var items = await dbContext.WarehouseChangeBatchItems.AsNoTracking()
            .Where(i => i.BatchId == batch.Id)
            .ToListAsync(cancellationToken);

        return Result<WarehouseChangeBatchDto>.Success(BatchMapper.ToDto(batch, items));
    }
}

public sealed class GetWarehouseChangeBatchesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetWarehouseChangeBatchesQuery, IReadOnlyList<WarehouseChangeBatchDto>>
{
    private const int MaxBatches = 50;

    public async Task<Result<IReadOnlyList<WarehouseChangeBatchDto>>> Handle(GetWarehouseChangeBatchesQuery request, CancellationToken cancellationToken)
    {
        var membership = await CurrentOrganization.LoadAsync(dbContext, currentUserService, requireApproved: false, cancellationToken);
        if (membership.IsFailure)
            return Result<IReadOnlyList<WarehouseChangeBatchDto>>.Failure(membership.Error);

        var organizationId = membership.Value.Organization.Id;
        var batches = await dbContext.WarehouseChangeBatches.AsNoTracking()
            .Where(b => b.ClientId == organizationId)
            .OrderByDescending(b => b.CreatedAt)
            .Take(MaxBatches)
            .ToListAsync(cancellationToken);

        IReadOnlyList<WarehouseChangeBatchDto> items = batches.Select(b => BatchMapper.ToDto(b, null)).ToList();
        return Result<IReadOnlyList<WarehouseChangeBatchDto>>.Success(items);
    }
}
