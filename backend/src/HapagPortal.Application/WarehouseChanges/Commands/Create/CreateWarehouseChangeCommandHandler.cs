namespace HapagPortal.Application.WarehouseChanges.Commands.Create;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class CreateWarehouseChangeCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<CreateWarehouseChangeCommand, WarehouseChangeResponseDto>
{
    public async Task<Result<WarehouseChangeResponseDto>> Handle(
        CreateWarehouseChangeCommand request,
        CancellationToken cancellationToken)
    {
        var bl = await dbContext.BillsOfLading
            .FirstOrDefaultAsync(b => b.Id == request.BillOfLadingId, cancellationToken);

        if (bl is null)
            return Result<WarehouseChangeResponseDto>.Failure(
                DomainErrors.BillOfLading.NotFound(request.BillOfLadingId));

        var currency = request.Currency ?? CountryCodes.GetCurrency(request.Country);

        var warehouseChange = new WarehouseChange
        {
            FromWarehouse = request.FromWarehouse,
            ToWarehouse = request.ToWarehouse,
            Amount = request.Amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            Country = request.Country,
            BillOfLadingId = request.BillOfLadingId
        };

        dbContext.WarehouseChanges.Add(warehouseChange);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<WarehouseChangeResponseDto>.Success(
            new WarehouseChangeResponseDto(
                warehouseChange.Id,
                warehouseChange.FromWarehouse,
                warehouseChange.ToWarehouse,
                warehouseChange.Amount,
                warehouseChange.Currency,
                warehouseChange.Status,
                warehouseChange.Country,
                warehouseChange.BillOfLadingId,
                warehouseChange.CreatedAt));
    }
}
