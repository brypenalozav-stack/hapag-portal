namespace HapagPortal.Application.Customs.Transmit;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Customs.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Transmite un conocimiento de embarque (etapa 2). Requiere que el encabezado del manifiesto
/// esté aceptado, el B/L completo, y —si es Hijo/Nieto— que su padre ya esté aceptado.
/// </summary>
public sealed record TransmitBLCommand(Guid ManifestId, Guid BillOfLadingId) : ICommand<TransmissionDto>;

public sealed class TransmitBLCommandHandler(
    IApplicationDbContext dbContext,
    ICustomsTransmitter transmitter,
    INotificationPublisher? publisher = null)
    : ICommandHandler<TransmitBLCommand, TransmissionDto>
{
    public async Task<Result<TransmissionDto>> Handle(
        TransmitBLCommand request,
        CancellationToken cancellationToken)
    {
        var manifest = await dbContext.CustomsManifests
            .FirstOrDefaultAsync(m => m.Id == request.ManifestId, cancellationToken);
        if (manifest is null)
            return Result<TransmissionDto>.Failure(DomainErrors.Customs.ManifestNotFound(request.ManifestId));

        // El encabezado debe estar aceptado.
        var headerAccepted = await dbContext.CustomsTransmissions.AnyAsync(
            t => t.ManifestId == manifest.Id && t.Stage == CustomsStages.Header
                 && t.Status == CustomsTransmissionStatus.Accepted, cancellationToken);
        if (!headerAccepted)
            return Result<TransmissionDto>.Failure(DomainErrors.Customs.HeaderNotAccepted);

        var bl = await dbContext.BillsOfLading
            .Include(b => b.Parties)
            .Include(b => b.CargoItems)
            .FirstOrDefaultAsync(b => b.Id == request.BillOfLadingId, cancellationToken);
        if (bl is null)
            return Result<TransmissionDto>.Failure(DomainErrors.BillOfLading.NotFound(request.BillOfLadingId));

        var incomplete = CheckCompleteness(bl);
        if (incomplete is not null)
            return Result<TransmissionDto>.Failure(DomainErrors.Customs.IncompleteBL(incomplete));

        // Un B/L Hijo/Nieto requiere que su padre esté aceptado.
        if (bl.ParentBLId is Guid parentId)
        {
            var parentAccepted = await dbContext.CustomsTransmissions.AnyAsync(
                t => t.BillOfLadingId == parentId && t.Status == CustomsTransmissionStatus.Accepted,
                cancellationToken);
            if (!parentAccepted)
                return Result<TransmissionDto>.Failure(DomainErrors.Customs.ParentNotTransmitted);
        }

        var existing = await dbContext.CustomsTransmissions
            .Where(t => t.ManifestId == manifest.Id && t.BillOfLadingId == bl.Id
                        && t.Stage == CustomsStages.BillOfLading
                        && t.Kind == CustomsTransmissionKind.Original)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null && CustomsTransmissionStatus.IsTerminal(existing.Status))
            return Result<TransmissionDto>.Failure(DomainErrors.Customs.AlreadyAccepted);

        var transmission = existing ?? new CustomsTransmission
        {
            ManifestId = manifest.Id,
            BillOfLadingId = bl.Id,
            Stage = CustomsStages.BillOfLading,
            Kind = CustomsTransmissionKind.Original,
            Status = CustomsTransmissionStatus.Draft
        };
        if (existing is null)
            dbContext.CustomsTransmissions.Add(transmission);

        var payload = $"<bl number=\"{bl.BLNumber}\" type=\"{bl.BLType}\"/>";
        await TransmissionApplier.RunAsync(
            transmission, bl.BLNumber, payload, transmitter, dbContext, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await CustomsAlert.NotifyIfNotAcceptedAsync(
            transmission, $"B/L {bl.BLNumber}", publisher, cancellationToken);

        return Result<TransmissionDto>.Success(transmission.ToDto(bl.BLNumber));
    }

    private static string? CheckCompleteness(BillOfLading bl)
    {
        if (!bl.Parties.Any(p => p.Role == "Consignee"))
            return "falta el consignatario";
        if (!bl.CargoItems.Any(c => !string.IsNullOrWhiteSpace(c.HsCode)))
            return "falta el código HS en la mercancía";
        if (string.IsNullOrWhiteSpace(bl.PortOfLoading) || string.IsNullOrWhiteSpace(bl.PortOfDischarge))
            return "faltan los puertos de carga/descarga";
        return null;
    }
}
