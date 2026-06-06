namespace HapagPortal.Application.Demurrage.Read.GetByBL;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetDemurrageByBLQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetDemurrageByBLQuery, List<DemurrageChargeDto>>
{
    public async Task<Result<List<DemurrageChargeDto>>> Handle(
        GetDemurrageByBLQuery request,
        CancellationToken cancellationToken)
    {
        var bl = await dbContext.BillsOfLading
            .AsNoTracking()
            .Include(b => b.DemurrageCharges)
            .FirstOrDefaultAsync(b => b.BLNumber == request.BLNumber, cancellationToken);

        if (bl is null)
            return Result<List<DemurrageChargeDto>>.Failure(
                DomainErrors.BillOfLading.NotFoundByNumber(request.BLNumber));

        var charges = bl.DemurrageCharges?.Select(dc => new DemurrageChargeDto(
            dc.Id,
            bl.Id,
            bl.BLNumber,
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
            bl.Country)).ToList() ?? [];

        return Result<List<DemurrageChargeDto>>.Success(charges);
    }
}
