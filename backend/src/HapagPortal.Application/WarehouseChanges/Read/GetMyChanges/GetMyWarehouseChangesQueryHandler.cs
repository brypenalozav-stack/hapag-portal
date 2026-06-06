namespace HapagPortal.Application.WarehouseChanges.Read.GetMyChanges;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetMyWarehouseChangesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyWarehouseChangesQuery, List<WarehouseChangeResponseDto>>
{
    public async Task<Result<List<WarehouseChangeResponseDto>>> Handle(
        GetMyWarehouseChangesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
            return Result<List<WarehouseChangeResponseDto>>.Failure(
                new Error("Error.Unauthorized", "User is not authenticated."));

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserService.UserId.Value, cancellationToken);

        if (user is null)
            return Result<List<WarehouseChangeResponseDto>>.Failure(
                DomainErrors.User.NotFound(currentUserService.UserId.Value));

        if (user.ClientId is null)
            return Result<List<WarehouseChangeResponseDto>>.Success([]);

        var clientBLIds = await dbContext.BillsOfLading
            .AsNoTracking()
            .Where(b => b.ClientId == user.ClientId.Value)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var changes = await dbContext.WarehouseChanges
            .AsNoTracking()
            .Where(wc => clientBLIds.Contains(wc.BillOfLadingId))
            .OrderByDescending(wc => wc.CreatedAt)
            .Select(wc => new WarehouseChangeResponseDto(
                wc.Id,
                wc.FromWarehouse,
                wc.ToWarehouse,
                wc.Amount,
                wc.Currency,
                wc.Status,
                wc.Country,
                wc.BillOfLadingId,
                wc.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<List<WarehouseChangeResponseDto>>.Success(changes);
    }
}
