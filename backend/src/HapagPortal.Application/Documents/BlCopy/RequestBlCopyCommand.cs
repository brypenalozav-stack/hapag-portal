namespace HapagPortal.Application.Documents.BlCopy;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;

/// <summary>
/// Copia del BL (M6-05): la valorada incluye los valores comerciales (flete y cargos) y la piden el customer
/// y el consignee; la no valorada también el shipper; un tercero, la que su acceso habilite (M1-11). Se
/// publica en el repositorio (M6-09) y, por defecto, se envía al correo registrado de la organización. La
/// solicitud y cada envío quedan registrados (NF-14).
/// </summary>
public sealed record RequestBlCopyCommand(string BlNumber, bool Valued, bool SendEmail = true) : ICommand<DocumentDeliveryDto>;

public sealed class RequestBlCopyCommandValidator : AbstractValidator<RequestBlCopyCommand>
{
    public RequestBlCopyCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class RequestBlCopyCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService,
    ShipmentDocumentService documents)
    : ICommandHandler<RequestBlCopyCommand, DocumentDeliveryDto>
{
    public async Task<Result<DocumentDeliveryDto>> Handle(RequestBlCopyCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: true, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<DocumentDeliveryDto>.Failure(loaded.Error);

        var (bl, permissions, organization, scope) = loaded.Value;

        var action = request.Valued ? ShipmentActionCodes.RequestValuedBlCopy : ShipmentActionCodes.RequestUnvaluedBlCopy;
        if (!permissions.CanExecute(action))
            return Result<DocumentDeliveryDto>.Failure(Error.Forbidden);

        var type = request.Valued ? ShipmentDocumentTypes.BlCopyValued : ShipmentDocumentTypes.BlCopyNonValued;
        var now = DateTime.UtcNow;
        var actor = DocumentActors.From(currentUserService, scope, permissions.GrantFor(action));
        var data = await documents.LoadDataAsync(bl, cancellationToken);
        var header = documents.NewHeader(type, bl, now);
        var model = ShipmentDocumentTemplates.BlCopy(data, header, request.Valued, $"{organization.Name} ({currentUserService.Email})");

        var issued = await documents.IssueAsync(
            new DocumentIssue(
                type,
                bl,
                model,
                ShipmentDocumentOrigins.Request,
                actor,
                organization.Id,
                data.Containers.Select(c => c.ContainerNumber).ToList(),
                RecipientEmails: request.SendEmail ? [organization.Email] : null),
            cancellationToken);
        if (issued.IsFailure)
            return Result<DocumentDeliveryDto>.Failure(issued.Error);

        IReadOnlyList<string> sentTo = [];
        if (request.SendEmail)
        {
            var sent = await documents.SendAsync(issued.Value, [organization.Email], DocumentChannels.Email, actor, now, cancellationToken);
            if (sent.IsFailure)
                return Result<DocumentDeliveryDto>.Failure(sent.Error);
            sentTo = sent.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<DocumentDeliveryDto>.Success(new DocumentDeliveryDto(
            ShipmentDocumentService.ToDto(issued.Value, organization.Name), sentTo));
    }
}
