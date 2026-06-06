namespace HapagPortal.Application.Admin.DemurrageExemptions.Commands.Deactivate;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class DeactivateDemurrageExemptionCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<DeactivateDemurrageExemptionCommand>
{
    public async Task<Result> Handle(
        DeactivateDemurrageExemptionCommand request,
        CancellationToken cancellationToken)
    {
        var exemption = await dbContext.DemurrageExemptions
            .FirstOrDefaultAsync(de => de.Id == request.Id, cancellationToken);

        if (exemption is null)
            return Result.Failure(DomainErrors.DemurrageExemption.NotFound(request.Id));

        exemption.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
