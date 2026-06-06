namespace HapagPortal.Application.Admin.DemurrageExemptions.Commands.Create;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;

public sealed class CreateDemurrageExemptionCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<CreateDemurrageExemptionCommand, DemurrageExemptionResponseDto>
{
    public async Task<Result<DemurrageExemptionResponseDto>> Handle(
        CreateDemurrageExemptionCommand request,
        CancellationToken cancellationToken)
    {
        var exemption = new DemurrageExemption
        {
            ClientName = request.ClientName,
            TaxId = request.TaxId,
            Country = request.Country,
            Reason = request.Reason,
            IsActive = true
        };

        dbContext.DemurrageExemptions.Add(exemption);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<DemurrageExemptionResponseDto>.Success(new DemurrageExemptionResponseDto(
            exemption.Id,
            exemption.ClientName,
            exemption.TaxId,
            exemption.Country,
            exemption.Reason,
            exemption.IsActive));
    }
}
