namespace HapagPortal.Application.Customs.Transmit;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Customs.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Reintenta una transmisión rechazada o con error. No permite reintentar una aceptada.</summary>
public sealed record RetryTransmissionCommand(Guid TransmissionId) : ICommand<TransmissionDto>;

public sealed class RetryTransmissionCommandHandler(
    IApplicationDbContext dbContext,
    ICustomsTransmitter transmitter,
    INotificationPublisher? publisher = null)
    : ICommandHandler<RetryTransmissionCommand, TransmissionDto>
{
    public async Task<Result<TransmissionDto>> Handle(
        RetryTransmissionCommand request,
        CancellationToken cancellationToken)
    {
        var transmission = await dbContext.CustomsTransmissions
            .FirstOrDefaultAsync(t => t.Id == request.TransmissionId, cancellationToken);

        if (transmission is null)
            return Result<TransmissionDto>.Failure(DomainErrors.Customs.TransmissionNotFound(request.TransmissionId));

        if (CustomsTransmissionStatus.IsTerminal(transmission.Status))
            return Result<TransmissionDto>.Failure(DomainErrors.Customs.AlreadyAccepted);

        string? blNumber = null;
        var referenceKey = transmission.Reference ?? transmission.Id.ToString();
        if (transmission.BillOfLadingId is Guid blId)
        {
            var bl = await dbContext.BillsOfLading
                .FirstOrDefaultAsync(b => b.Id == blId, cancellationToken);
            blNumber = bl?.BLNumber;
            referenceKey = blNumber ?? referenceKey;
        }

        var payload = $"<retry transmission=\"{transmission.Id}\" stage=\"{transmission.Stage}\"/>";
        await TransmissionApplier.RunAsync(
            transmission, referenceKey, payload, transmitter, dbContext, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await CustomsAlert.NotifyIfNotAcceptedAsync(
            transmission, blNumber ?? $"transmisión {transmission.Id}", publisher, cancellationToken);

        return Result<TransmissionDto>.Success(transmission.ToDto(blNumber));
    }
}
