namespace HapagPortal.Application.Receipts.Read.GetMyReceipts;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetMyReceiptsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyReceiptsQuery, List<ReceiptResponseDto>>
{
    public async Task<Result<List<ReceiptResponseDto>>> Handle(
        GetMyReceiptsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
            return Result<List<ReceiptResponseDto>>.Failure(
                new Error("Error.Unauthorized", "User is not authenticated."));

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserService.UserId.Value, cancellationToken);

        if (user is null)
            return Result<List<ReceiptResponseDto>>.Failure(
                DomainErrors.User.NotFound(currentUserService.UserId.Value));

        if (user.ClientId is null)
            return Result<List<ReceiptResponseDto>>.Success([]);

        var receipts = await dbContext.Payments
            .AsNoTracking()
            .Where(p => p.ClientId == user.ClientId.Value && p.ReceiptNumber != null)
            .OrderByDescending(p => p.ConfirmedAt)
            .Select(p => new ReceiptResponseDto(
                p.Id,
                p.ReceiptNumber!,
                p.Id,
                p.PaymentNumber,
                p.Amount,
                p.TaxAmount,
                p.TotalAmount,
                p.Currency,
                p.Country,
                p.ConfirmedAt ?? DateTime.UtcNow))
            .ToListAsync(cancellationToken);

        return Result<List<ReceiptResponseDto>>.Success(receipts);
    }
}
