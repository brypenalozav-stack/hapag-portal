namespace HapagPortal.Application.ServiceOrders.Read.GetMyOrders;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetMyServiceOrdersQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyServiceOrdersQuery, List<ServiceOrderResponseDto>>
{
    public async Task<Result<List<ServiceOrderResponseDto>>> Handle(
        GetMyServiceOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
            return Result<List<ServiceOrderResponseDto>>.Failure(
                new Error("Error.Unauthorized", "User is not authenticated."));

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserService.UserId.Value, cancellationToken);

        if (user is null)
            return Result<List<ServiceOrderResponseDto>>.Failure(
                DomainErrors.User.NotFound(currentUserService.UserId.Value));

        if (user.ClientId is null)
            return Result<List<ServiceOrderResponseDto>>.Success([]);

        var orders = await dbContext.ServiceOrders
            .AsNoTracking()
            .Where(so => so.ClientId == user.ClientId.Value)
            .OrderByDescending(so => so.RequestedAt)
            .Select(so => new ServiceOrderResponseDto(
                so.Id,
                so.OrderNumber,
                so.OrderType,
                so.Status,
                so.Description,
                so.Country,
                so.RequestedAt,
                so.CompletedAt,
                so.BillOfLadingId,
                so.ClientId))
            .ToListAsync(cancellationToken);

        return Result<List<ServiceOrderResponseDto>>.Success(orders);
    }
}
