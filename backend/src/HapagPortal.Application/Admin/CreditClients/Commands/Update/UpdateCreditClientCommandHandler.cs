namespace HapagPortal.Application.Admin.CreditClients.Commands.Update;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class UpdateCreditClientCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<UpdateCreditClientCommand, CreditClientResponseDto>
{
    public async Task<Result<CreditClientResponseDto>> Handle(
        UpdateCreditClientCommand request,
        CancellationToken cancellationToken)
    {
        var creditClient = await dbContext.CreditClients
            .Include(cc => cc.Client)
            .FirstOrDefaultAsync(cc => cc.Id == request.Id, cancellationToken);

        if (creditClient is null)
            return Result<CreditClientResponseDto>.Failure(DomainErrors.CreditClient.NotFound(request.Id));

        if (request.CreditLimit is not null)
            creditClient.CreditLimit = request.CreditLimit.Value;

        if (request.CreditStatus is not null)
            creditClient.CreditStatus = request.CreditStatus;

        if (request.ExpiresAt is not null)
            creditClient.ExpiresAt = request.ExpiresAt;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreditClientResponseDto>.Success(new CreditClientResponseDto(
            creditClient.Id,
            creditClient.Client?.Name ?? "Unknown",
            creditClient.Client?.TaxId ?? "",
            creditClient.Country,
            creditClient.CreditLimit,
            0m,
            creditClient.Country == "CL" ? "CLP" : "BOB",
            creditClient.CreditStatus,
            creditClient.ApprovedBy,
            creditClient.ApprovedAt,
            creditClient.ExpiresAt));
    }
}
