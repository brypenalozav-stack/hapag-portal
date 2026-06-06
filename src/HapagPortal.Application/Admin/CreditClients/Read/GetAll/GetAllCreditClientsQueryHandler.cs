namespace HapagPortal.Application.Admin.CreditClients.Read.GetAll;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetAllCreditClientsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAllCreditClientsQuery, List<CreditClientResponseDto>>
{
    public async Task<Result<List<CreditClientResponseDto>>> Handle(
        GetAllCreditClientsQuery request,
        CancellationToken cancellationToken)
    {
        var entities = await dbContext.CreditClients
            .AsNoTracking()
            .Include(cc => cc.Client)
            .ToListAsync(cancellationToken);

        var confirmedCreditLinePayments = await dbContext.Payments
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Confirmed && p.PaymentMethod == "CreditLine")
            .GroupBy(p => p.ClientId)
            .Select(g => new { ClientId = g.Key, TotalAmount = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.ClientId, x => x.TotalAmount, cancellationToken);

        var creditClients = entities.Select(cc =>
        {
            var usedCredit = confirmedCreditLinePayments.TryGetValue(cc.ClientId, out var total) ? total : 0m;
            return new CreditClientResponseDto(
                cc.Id,
                cc.Client?.Name ?? "Unknown",
                cc.Client?.TaxId ?? "",
                cc.Country,
                cc.CreditLimit,
                usedCredit,
                cc.Country == "CL" ? "CLP" : "BOB",
                cc.CreditStatus,
                cc.ApprovedBy,
                cc.ApprovedAt,
                cc.ExpiresAt);
        }).ToList();

        return Result<List<CreditClientResponseDto>>.Success(creditClients);
    }
}
