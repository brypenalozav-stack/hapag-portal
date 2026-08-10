namespace HapagPortal.Application.Customs.Amend;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Customs.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Aclaración al Manifiesto (Anexo 4 CNA): corrección posterior al arribo/zarpe. Se modela como
/// una transmisión de tipo Amendment sobre el encabezado o sobre un B/L concreto del manifiesto.
/// </summary>
public sealed record SubmitManifestAmendmentCommand(
    Guid ManifestId,
    Guid? BillOfLadingId,
    string Reason) : ICommand<TransmissionDto>;

public sealed class SubmitManifestAmendmentCommandHandler(
    IApplicationDbContext dbContext,
    ICustomsTransmitter transmitter)
    : ICommandHandler<SubmitManifestAmendmentCommand, TransmissionDto>
{
    public async Task<Result<TransmissionDto>> Handle(
        SubmitManifestAmendmentCommand request,
        CancellationToken cancellationToken)
    {
        var manifest = await dbContext.CustomsManifests
            .FirstOrDefaultAsync(m => m.Id == request.ManifestId, cancellationToken);
        if (manifest is null)
            return Result<TransmissionDto>.Failure(DomainErrors.Customs.ManifestNotFound(request.ManifestId));

        string? blNumber = null;
        var referenceKey = $"{manifest.VesselImo}/{manifest.Voyage}";
        var stage = CustomsStages.Header;

        if (request.BillOfLadingId is Guid blId)
        {
            var bl = await dbContext.BillsOfLading.FirstOrDefaultAsync(b => b.Id == blId, cancellationToken);
            if (bl is null)
                return Result<TransmissionDto>.Failure(DomainErrors.BillOfLading.NotFound(blId));
            blNumber = bl.BLNumber;
            referenceKey = bl.BLNumber;
            stage = CustomsStages.BillOfLading;
        }

        var transmission = new CustomsTransmission
        {
            ManifestId = manifest.Id,
            BillOfLadingId = request.BillOfLadingId,
            Stage = stage,
            Kind = CustomsTransmissionKind.Amendment,
            Status = CustomsTransmissionStatus.Draft
        };
        dbContext.CustomsTransmissions.Add(transmission);

        var payload = $"<amendment reason=\"{request.Reason}\" ref=\"{referenceKey}\"/>";
        await TransmissionApplier.RunAsync(
            transmission, referenceKey, payload, transmitter, dbContext, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<TransmissionDto>.Success(transmission.ToDto(blNumber));
    }
}
