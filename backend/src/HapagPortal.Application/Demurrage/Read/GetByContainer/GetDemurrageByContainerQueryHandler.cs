namespace HapagPortal.Application.Demurrage.Read.GetByContainer;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetDemurrageByContainerQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetDemurrageByContainerQuery, List<DemurrageChargeDto>>
{
    public async Task<Result<List<DemurrageChargeDto>>> Handle(
        GetDemurrageByContainerQuery request,
        CancellationToken cancellationToken)
    {
        var entities = await dbContext.DemurrageCharges
            .AsNoTracking()
            .Include(dc => dc.BillOfLading)
            .Where(dc => dc.ContainerNumber == request.ContainerNumber)
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
