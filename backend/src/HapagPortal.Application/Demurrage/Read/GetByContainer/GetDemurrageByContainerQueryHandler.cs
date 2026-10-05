namespace HapagPortal.Application.Demurrage.Read.GetByContainer;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetDemurrageByContainerQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetDemurrageByContainerQuery, List<DemurrageChargeDto>>
{
    public async Task<Result<List<DemurrageChargeDto>>> Handle(
        GetDemurrageByContainerQuery request,
        CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);

        var blIds = await dbContext.DemurrageCharges
            .AsNoTracking()
            .Where(dc => dc.ContainerNumber == request.ContainerNumber)
            .Select(dc => dc.BillOfLadingId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var accessibleBls = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope)
            .Where(b => blIds.Contains(b.Id))
            .ToListAsync(cancellationToken);

        var allowedBlIds = new List<Guid>();
        foreach (var bl in accessibleBls)
        {
            if ((await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken)).Can(ShipmentActionCodes.PayImportDemurrage))
                allowedBlIds.Add(bl.Id);
        }

        var entities = await dbContext.DemurrageCharges
            .AsNoTracking()
            .Include(dc => dc.BillOfLading)
            .Where(dc => dc.ContainerNumber == request.ContainerNumber
                && allowedBlIds.Contains(dc.BillOfLadingId))
            .ToListAsync(cancellationToken);

        var charges = entities.Select(dc => new DemurrageChargeDto(
            dc.Id,
            dc.BillOfLadingId,
            dc.BillOfLading?.BLNumber ?? "",
            dc.ContainerNumber,
            dc.FreeDays,
            dc.DemurrageDays,
            dc.DailyRate,
            dc.TotalAmount,
            dc.Currency,
            dc.StartDate,
            dc.EndDate,
            dc.Status,
            dc.IsExempt,
            dc.ExemptReason,
            dc.BillOfLading?.Country ?? "")).ToList();

        return Result<List<DemurrageChargeDto>>.Success(charges);
    }
}
