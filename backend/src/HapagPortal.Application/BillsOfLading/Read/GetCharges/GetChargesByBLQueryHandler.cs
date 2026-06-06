namespace HapagPortal.Application.BillsOfLading.Read.GetCharges;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetChargesByBLQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetChargesByBLQuery, BLChargesResponseDto>
{
    public async Task<Result<BLChargesResponseDto>> Handle(
        GetChargesByBLQuery request,
        CancellationToken cancellationToken)
    {
        var bl = await dbContext.BillsOfLading
            .AsNoTracking()
            .Include(b => b.LocalCharges)
            .Include(b => b.DemurrageCharges)
            .FirstOrDefaultAsync(b => b.BLNumber == request.BLNumber, cancellationToken);

        if (bl is null)
            return Result<BLChargesResponseDto>.Failure(
                DomainErrors.BillOfLading.NotFoundByNumber(request.BLNumber));

        var localCharges = bl.LocalCharges?.Select(lc => new LocalChargeDto(
            lc.Id,
            bl.Id,
            bl.BLNumber,
            lc.ChargeType,
            lc.Description,
            lc.Amount,
            lc.Currency,
            lc.Status,
            lc.IsTaxable,
            lc.TaxRate,
            lc.TaxAmount,
            lc.TotalAmount,
            bl.Country)).ToList() ?? [];

        var demurrageCharges = bl.DemurrageCharges?.Select(dc => new DemurrageChargeDto(
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

        var dto = new BLChargesResponseDto(bl.BLNumber, localCharges, demurrageCharges);

        return Result<BLChargesResponseDto>.Success(dto);
    }
}
