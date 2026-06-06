namespace HapagPortal.Application.WarehouseChanges.Read.GetById;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetWarehouseChangeByIdQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetWarehouseChangeByIdQuery, WarehouseChangeResponseDto>
{
    public async Task<Result<WarehouseChangeResponseDto>> Handle(
        GetWarehouseChangeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var wc = await dbContext.WarehouseChanges
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (wc is null)
            return Result<WarehouseChangeResponseDto>.Failure(
                DomainErrors.WarehouseChange.NotFound(request.Id));

        return Result<WarehouseChangeResponseDto>.Success(
            new WarehouseChangeResponseDto(
                wc.Id,
                wc.FromWarehouse,
                wc.ToWarehouse,
                wc.Amount,
                wc.Currency,
                wc.Status,
                wc.Country,
                wc.BillOfLadingId,
                wc.CreatedAt));
    }
}
