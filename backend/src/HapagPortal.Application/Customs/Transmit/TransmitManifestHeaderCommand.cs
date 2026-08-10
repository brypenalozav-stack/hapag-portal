namespace HapagPortal.Application.Customs.Transmit;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Customs.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Transmite el encabezado del manifiesto (etapa 1). No re-transmite si ya fue aceptado.</summary>
public sealed record TransmitManifestHeaderCommand(Guid ManifestId) : ICommand<TransmissionDto>;

public sealed class TransmitManifestHeaderCommandHandler(
    IApplicationDbContext dbContext,
    ICustomsTransmitter transmitter)
    : ICommandHandler<TransmitManifestHeaderCommand, TransmissionDto>
{
    public async Task<Result<TransmissionDto>> Handle(
        TransmitManifestHeaderCommand request,
        CancellationToken cancellationToken)
    {
        var manifest = await dbContext.CustomsManifests
            .FirstOrDefaultAsync(m => m.Id == request.ManifestId, cancellationToken);

        if (manifest is null)
            return Result<TransmissionDto>.Failure(DomainErrors.Customs.ManifestNotFound(request.ManifestId));

        var existing = await dbContext.CustomsTransmissions
            .Where(t => t.ManifestId == manifest.Id && t.Stage == CustomsStages.Header
                        && t.Kind == CustomsTransmissionKind.Original)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null && CustomsTransmissionStatus.IsTerminal(existing.Status))
            return Result<TransmissionDto>.Failure(DomainErrors.Customs.AlreadyAccepted);

        var transmission = existing ?? new CustomsTransmission
        {
            ManifestId = manifest.Id,
            Stage = CustomsStages.Header,
            Kind = CustomsTransmissionKind.Original,
            Status = CustomsTransmissionStatus.Draft
        };
        if (existing is null)
            dbContext.CustomsTransmissions.Add(transmission);

        var payload = $"<manifest imo=\"{manifest.VesselImo}\" voyage=\"{manifest.Voyage}\" port=\"{manifest.Port}\" direction=\"{manifest.Direction}\"/>";
        await TransmissionApplier.RunAsync(
            transmission, $"{manifest.VesselImo}/{manifest.Voyage}", payload, transmitter, dbContext, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<TransmissionDto>.Success(transmission.ToDto(null));
    }
}
