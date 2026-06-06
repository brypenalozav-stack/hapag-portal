namespace HapagPortal.Application.ServiceOrders.Commands.Create;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class CreateServiceOrderCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateServiceOrderCommand, ServiceOrderResponseDto>
{
    public async Task<Result<ServiceOrderResponseDto>> Handle(
        CreateServiceOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
            return Result<ServiceOrderResponseDto>.Failure(
                new Error("Error.Unauthorized", "User is not authenticated."));

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserService.UserId.Value, cancellationToken);

        if (user is null)
            return Result<ServiceOrderResponseDto>.Failure(
                DomainErrors.User.NotFound(currentUserService.UserId.Value));

        if (user.ClientId is null)
            return Result<ServiceOrderResponseDto>.Failure(
                DomainErrors.Client.NotFound(Guid.Empty));

        var bl = await dbContext.BillsOfLading
            .FirstOrDefaultAsync(b => b.Id == request.BillOfLadingId, cancellationToken);

        if (bl is null)
            return Result<ServiceOrderResponseDto>.Failure(
                DomainErrors.BillOfLading.NotFound(request.BillOfLadingId));

        var orderNumber = $"{DocumentPrefixes.ServiceOrder}{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";

        var serviceOrder = new ServiceOrder
        {
            OrderNumber = orderNumber,
            OrderType = request.OrderType,
            Status = ServiceOrderStatus.Requested,
            Description = request.Description,
            Country = request.Country,
            RequestedAt = DateTime.UtcNow,
            BillOfLadingId = request.BillOfLadingId,
            ClientId = user.ClientId.Value
        };

        dbContext.ServiceOrders.Add(serviceOrder);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ServiceOrderResponseDto>.Success(
            new ServiceOrderResponseDto(
                serviceOrder.Id,
                serviceOrder.OrderNumber,
                serviceOrder.OrderType,
                serviceOrder.Status,
                serviceOrder.Description,
                serviceOrder.Country,
                serviceOrder.RequestedAt,
                serviceOrder.CompletedAt,
                serviceOrder.BillOfLadingId,
                serviceOrder.ClientId));
    }
}
