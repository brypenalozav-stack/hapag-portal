namespace HapagPortal.Application.LocalCharges.Read.GetByContainer;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetLocalChargesByContainerQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetLocalChargesByContainerQuery, List<LocalChargeDto>>
{
    public async Task<Result<List<LocalChargeDto>>> Handle(
        GetLocalChargesByContainerQuery request,
        CancellationToken cancellationToken)
    {
        var blIds = await dbContext.BLContainers
            .AsNoTracking()
            .Where(c => c.ContainerNumber == request.ContainerNumber)
            .Select(c => c.BillOfLadingId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var entities = await dbContext.LocalCharges
            .AsNoTracking()
            .Include(lc => lc.BillOfLading)
            .Where(lc => blIds.Contains(lc.BillOfLadingId))
            .ToListAsync(cancellationToken);

        var charges = entities.Select(lc => new LocalChargeDto(
            lc.Id,
            lc.BillOfLadingId,
            lc.BillOfLading?.BLNumber ?? "",
            lc.ChargeType,
            lc.Description,
            lc.Amount,
            lc.Currency,
            lc.Status,
            lc.IsTaxable,
            lc.TaxRate,
            lc.TaxAmount,
            lc.TotalAmount,
            lc.BillOfLading?.Country ?? "")).ToList();

        return Result<List<LocalChargeDto>>.Success(charges);
    }
}
