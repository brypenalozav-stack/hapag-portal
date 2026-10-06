namespace HapagPortal.Application.BillsOfLading.Read.GetMyBLs;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetMyBLsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetMyBLsQuery, List<BillOfLadingResponseDto>>
{
    public async Task<Result<List<BillOfLadingResponseDto>>> Handle(
        GetMyBLsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
            return Result<List<BillOfLadingResponseDto>>.Failure(
                new Error("Error.Unauthorized", "User is not authenticated."));

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserService.UserId.Value, cancellationToken);

        if (user is null)
            return Result<List<BillOfLadingResponseDto>>.Failure(
                DomainErrors.User.NotFound(currentUserService.UserId.Value));

        if (user.ClientId is null)
            return Result<List<BillOfLadingResponseDto>>.Success([]);

        // Embarques propios de la organización: titular o con rol en el BL (M1-11, M2-06).
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (scope.IsAdmin)
            scope = scope with { IsAdmin = false };

        var entities = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope)
            .Include(b => b.Containers)
            .Include(b => b.Client)
            .Include(b => b.Payments)
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
            (bl.FreightPaidAt != null || bl.Payments?.Any(p => p.PaymentType == "Freight" && p.Status == "Confirmed") == true) ? "PAID" : "PENDING",
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
            bl.BookingNumber)).ToList();

        return Result<List<BillOfLadingResponseDto>>.Success(bls);
    }
}
