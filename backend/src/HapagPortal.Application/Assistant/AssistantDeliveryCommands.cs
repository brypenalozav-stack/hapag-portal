namespace HapagPortal.Application.Assistant;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Descarga de un documento entregado por el asistente (M10-04): solo para el dueño de la conversación y solo documentos del
/// repositorio del embarque (los recibos y facturas se descargan por sus propias rutas). Ejecuta la misma descarga que el
/// repositorio, con el canal <c>Assistant</c>: vuelve a validar los permisos del usuario en ese momento (M1-11, NF-05) y
/// queda registrada (NF-14).
/// </summary>
public sealed record DownloadAssistantDeliveryCommand(Guid SessionId, Guid DeliveryId) : ICommand<DocumentFileDto>;

public sealed class DownloadAssistantDeliveryCommandValidator : AbstractValidator<DownloadAssistantDeliveryCommand>
{
    public DownloadAssistantDeliveryCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.DeliveryId).NotEmpty();
    }
}

public sealed class DownloadAssistantDeliveryCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ISender sender)
    : ICommandHandler<DownloadAssistantDeliveryCommand, DocumentFileDto>
{
    public async Task<Result<DocumentFileDto>> Handle(DownloadAssistantDeliveryCommand request, CancellationToken cancellationToken)
    {
        var authorized = await AssistantSessions.AuthorizeAsync(accessEvaluator, cancellationToken);
        if (authorized.IsFailure)
            return Result<DocumentFileDto>.Failure(authorized.Error);

        var scope = authorized.Value;
        var session = await AssistantSessions.LoadOwnAsync(dbContext, scope, request.SessionId, cancellationToken);
        if (session.IsFailure)
            return Result<DocumentFileDto>.Failure(session.Error);

        var delivery = await dbContext.AssistantDocumentDeliveries.FirstOrDefaultAsync(
            d => d.Id == request.DeliveryId && d.SessionId == request.SessionId && d.UserId == scope.UserId, cancellationToken);
        if (delivery is null || delivery.DocumentKind != AssistantDeliveryKinds.ShipmentDocument)
            return Result<DocumentFileDto>.Failure(DomainErrors.AssistantSession.DeliveryNotFound(request.DeliveryId));

        var file = await sender.Send(
            new DownloadShipmentDocumentCommand(delivery.BlNumber, delivery.DocumentId, DocumentChannels.Assistant), cancellationToken);
        if (file.IsFailure)
            return file;

        delivery.DownloadedAt ??= DateTime.UtcNow;
        delivery.DownloadCount++;
        await dbContext.SaveChangesAsync(cancellationToken);

        return file;
    }
}
