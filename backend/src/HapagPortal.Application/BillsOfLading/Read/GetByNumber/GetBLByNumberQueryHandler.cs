namespace HapagPortal.Application.BillsOfLading.Read.GetByNumber;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetBLByNumberQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetBLByNumberQuery, BillOfLadingResponseDto>
{
    public async Task<Result<BillOfLadingResponseDto>> Handle(
        GetBLByNumberQuery request,
        CancellationToken cancellationToken)
    {
        var bl = await dbContext.BillsOfLading
            .AsNoTracking()
            .Include(b => b.Containers)
            .Include(b => b.Client)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BLNumber == request.BLNumber, cancellationToken);

        if (bl is null)
            return Result<BillOfLadingResponseDto>.Failure(
                DomainErrors.BillOfLading.NotFoundByNumber(request.BLNumber));

        var freightStatus = bl.Payments?.Any(p => p.PaymentType == "Freight" && p.Status == "Confirmed") == true
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
                c.Status)).ToList());

        return Result<BillOfLadingResponseDto>.Success(dto);
    }
}
