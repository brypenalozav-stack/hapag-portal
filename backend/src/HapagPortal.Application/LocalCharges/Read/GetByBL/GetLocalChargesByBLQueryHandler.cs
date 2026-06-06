namespace HapagPortal.Application.LocalCharges.Read.GetByBL;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetLocalChargesByBLQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetLocalChargesByBLQuery, List<LocalChargeDto>>
{
    public async Task<Result<List<LocalChargeDto>>> Handle(
        GetLocalChargesByBLQuery request,
        CancellationToken cancellationToken)
    {
        var bl = await dbContext.BillsOfLading
            .AsNoTracking()
            .Include(b => b.LocalCharges)
            .FirstOrDefaultAsync(b => b.BLNumber == request.BLNumber, cancellationToken);

        if (bl is null)
            return Result<List<LocalChargeDto>>.Failure(
                DomainErrors.BillOfLading.NotFoundByNumber(request.BLNumber));

        var charges = bl.LocalCharges?.Select(lc => new LocalChargeDto(
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

        return Result<List<LocalChargeDto>>.Success(charges);
    }
}
