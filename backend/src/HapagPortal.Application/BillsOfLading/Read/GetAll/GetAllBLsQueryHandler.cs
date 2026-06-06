namespace HapagPortal.Application.BillsOfLading.Read.GetAll;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetAllBLsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAllBLsQuery, List<BillOfLadingResponseDto>>
{
    public async Task<Result<List<BillOfLadingResponseDto>>> Handle(
        GetAllBLsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.BillsOfLading
            .AsNoTracking()
            .Include(b => b.Containers)
            .Include(b => b.Client)
            .Include(b => b.Payments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Country))
            query = query.Where(b => b.Country == request.Country);

        if (request.ClientId.HasValue)
            query = query.Where(b => b.ClientId == request.ClientId.Value);

        var entities = await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        var bls = entities.Select(bl => new BillOfLadingResponseDto(
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
            bl.Payments?.Any(p => p.PaymentType == "Freight" && p.Status == "Confirmed") == true ? "PAID" : "PENDING",
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
                c.Status)).ToList())).ToList();

        return Result<List<BillOfLadingResponseDto>>.Success(bls);
    }
}
