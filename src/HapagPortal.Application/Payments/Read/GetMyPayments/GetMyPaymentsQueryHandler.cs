namespace HapagPortal.Application.Payments.Read.GetMyPayments;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetMyPaymentsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyPaymentsQuery, List<PaymentResponseDto>>
{
    public async Task<Result<List<PaymentResponseDto>>> Handle(
        GetMyPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
            return Result<List<PaymentResponseDto>>.Failure(
                new Error("Error.Unauthorized", "User is not authenticated."));

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserService.UserId.Value, cancellationToken);

        if (user is null)
            return Result<List<PaymentResponseDto>>.Failure(
                DomainErrors.User.NotFound(currentUserService.UserId.Value));

        if (user.ClientId is null)
            return Result<List<PaymentResponseDto>>.Success([]);

        var entities = await dbContext.Payments
            .AsNoTracking()
            .Include(p => p.BillOfLading)
            .Include(p => p.Client)
            .Include(p => p.Details)
            .Where(p => p.ClientId == user.ClientId.Value)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

        var payments = entities.Select(p => new PaymentResponseDto(
            p.Id,
            p.PaymentNumber,
            p.PaymentType,
            p.PaymentMethod,
            p.Amount,
            p.TaxAmount,
            p.TotalAmount,
            p.Currency,
            p.Status,
            p.BillOfLading?.BLNumber ?? "",
            p.BillOfLadingId,
            p.ClientId,
            p.Client?.Name,
            p.Country,
            p.DepositProofUrl,
            p.CreatedAt,
            p.ConfirmedAt,
            p.Details?.Select(d => new PaymentDetailDto(
                d.Id,
                d.ConceptType,
                d.Description,
                d.Amount,
                d.Currency)).ToList())).ToList();

        return Result<List<PaymentResponseDto>>.Success(payments);
    }
}
