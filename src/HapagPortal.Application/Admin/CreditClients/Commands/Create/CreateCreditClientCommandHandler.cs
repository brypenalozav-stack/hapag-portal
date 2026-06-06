namespace HapagPortal.Application.Admin.CreditClients.Commands.Create;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class CreateCreditClientCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateCreditClientCommand, CreditClientResponseDto>
{
    public async Task<Result<CreditClientResponseDto>> Handle(
        CreateCreditClientCommand request,
        CancellationToken cancellationToken)
    {
        var client = await dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
            return Result<CreditClientResponseDto>.Failure(DomainErrors.Client.NotFound(request.ClientId));

        var creditClient = new CreditClient
        {
            Country = request.Country,
            CreditLimit = request.CreditLimit,
            CreditStatus = CreditStatus.Active,
            ApprovedBy = currentUserService.Email,
            ApprovedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt,
            ClientId = request.ClientId
        };

        dbContext.CreditClients.Add(creditClient);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreditClientResponseDto>.Success(new CreditClientResponseDto(
            creditClient.Id,
            client.Name,
            client.TaxId,
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
