namespace HapagPortal.Application.BillsOfLading.Read.GetByNumber;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetBLByNumberQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetBLByNumberQuery, BillOfLadingResponseDto>
{
    public async Task<Result<BillOfLadingResponseDto>> Handle(
        GetBLByNumberQuery request,
        CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);

        var bl = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope)
            .Include(b => b.Containers)
            .Include(b => b.Client)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BLNumber == request.BLNumber, cancellationToken);

        // Sin acceso se responde NotFound: no se revela la existencia de un BL ajeno.
        if (bl is null ||
            !(await accessEvaluator.EvaluateAsync(scope, bl, cancellationToken)).Can(ShipmentActionCodes.ViewShipment))
        {
            return Result<BillOfLadingResponseDto>.Failure(
                DomainErrors.BillOfLading.NotFoundByNumber(request.BLNumber));
        }

        var freightStatus = (bl.FreightPaidAt != null || bl.Payments?.Any(p => p.PaymentType == "Freight" && p.Status == "Confirmed") == true)
            ? "PAID" : "PENDING";

        var dto = new BillOfLadingResponseDto(
            bl.Id,
            bl.BLNumber,
            bl.ShipmentType,
            bl.Vessel,
            bl.Voyage,
            bl.PortOfLoading,
            bl.PortOfDischarge,
            bl.PlaceOfDelivery,
            bl.ETD,
            bl.ETA,
            bl.FreightAmount,
            bl.FreightCurrency,
            freightStatus,
            bl.Status,
            bl.Country,
            bl.ClientId,
            bl.Client?.Name,
            bl.CreatedAt,
            bl.Containers?.Select(c => new BLContainerDto(
                c.Id,
                c.ContainerNumber,
                c.ContainerType,
                c.SealNumber,
                c.Weight,
                c.Status)).ToList(),
            bl.BookingNumber);

        return Result<BillOfLadingResponseDto>.Success(dto);
    }
}
