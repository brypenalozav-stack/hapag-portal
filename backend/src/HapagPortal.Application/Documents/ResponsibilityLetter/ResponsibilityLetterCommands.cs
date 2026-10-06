namespace HapagPortal.Application.Documents.ResponsibilityLetter;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Términos vigentes que el cliente acepta al emitir la carta (M6-06).</summary>
public sealed record GetResponsibilityLetterTermsQuery : IQuery<ResponsibilityLetterTermsDto>;

/// <summary>
/// Gestión de la carta de responsabilidad (M6-06, CL-IMP-11, BO-IMP-10): el cliente ingresa los datos
/// requeridos y acepta los términos vigentes; el portal genera el PDF, lo guarda asociado al BL y a la
/// organización, y reemplaza la carta anterior de la misma organización sobre el BL. Sin cobro. Una carta
/// vigente cumple M4-04: levanta el bloqueo del FFWW en cargos y carro.
/// </summary>
public sealed record IssueResponsibilityLetterCommand(
    string BlNumber,
    string SignatoryName,
    string SignatoryTaxId,
    string SignatoryPosition,
    string ContactEmail,
    string? ContactPhone,
    string? CargoDescription,
    string? Observations,
    bool AcceptTerms,
    string TermsVersion) : ICommand<ShipmentDocumentDto>;

public sealed class IssueResponsibilityLetterCommandValidator : AbstractValidator<IssueResponsibilityLetterCommand>
{
    public IssueResponsibilityLetterCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SignatoryName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SignatoryTaxId).NotEmpty().MaximumLength(30);
        RuleFor(x => x.SignatoryPosition).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.ContactPhone).MaximumLength(30);
        RuleFor(x => x.CargoDescription).MaximumLength(500);
        RuleFor(x => x.Observations).MaximumLength(1000);
        RuleFor(x => x.TermsVersion).NotEmpty().MaximumLength(50);
    }
}

public sealed class GetResponsibilityLetterTermsQueryHandler
    : IQueryHandler<GetResponsibilityLetterTermsQuery, ResponsibilityLetterTermsDto>
{
    public Task<Result<ResponsibilityLetterTermsDto>> Handle(GetResponsibilityLetterTermsQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result<ResponsibilityLetterTermsDto>.Success(new ResponsibilityLetterTermsDto(
            ResponsibilityLetterTerms.Version, ResponsibilityLetterTerms.Title, ResponsibilityLetterTerms.Text)));
}

public sealed class IssueResponsibilityLetterCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService,
    ShipmentDocumentService documents)
    : ICommandHandler<IssueResponsibilityLetterCommand, ShipmentDocumentDto>
{
    public async Task<Result<ShipmentDocumentDto>> Handle(IssueResponsibilityLetterCommand request, CancellationToken cancellationToken)
    {
        if (!request.AcceptTerms)
            return Result<ShipmentDocumentDto>.Failure(DomainErrors.ResponsibilityLetter.TermsNotAccepted);
        if (!string.Equals(request.TermsVersion.Trim(), ResponsibilityLetterTerms.Version, StringComparison.Ordinal))
            return Result<ShipmentDocumentDto>.Failure(DomainErrors.ResponsibilityLetter.TermsVersionMismatch(ResponsibilityLetterTerms.Version));

        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: true, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<ShipmentDocumentDto>.Failure(loaded.Error);

        var (bl, permissions, organization, scope) = loaded.Value;

        // M1-11: la carta la gestiona el consignee Freight Forwarder (o quien la reciba por un acceso otorgado).
        if (!permissions.CanExecute(ShipmentActionCodes.GenerateResponsibilityLetter))
            return Result<ShipmentDocumentDto>.Failure(Error.Forbidden);

        var now = DateTime.UtcNow;
        var actor = DocumentActors.From(currentUserService, scope, permissions.GrantFor(ShipmentActionCodes.GenerateResponsibilityLetter));
        var data = await documents.LoadDataAsync(bl, cancellationToken);
        var header = documents.NewHeader(ShipmentDocumentTypes.ResponsibilityLetter, bl, now);

        var letter = new ResponsibilityLetterData(
            organization.Name,
            TaxIdNormalizer.Normalize(organization.TaxId),
            request.SignatoryName.Trim(),
            request.SignatoryTaxId.Trim(),
            request.SignatoryPosition.Trim(),
            request.ContactEmail.Trim(),
            request.ContactPhone?.Trim(),
            request.CargoDescription?.Trim(),
            request.Observations?.Trim(),
            ResponsibilityLetterTerms.Version,
            now);

        var validityDays = documents.Settings.ResponsibilityLetterValidityDays;
        var issued = await documents.IssueAsync(
            new DocumentIssue(
                ShipmentDocumentTypes.ResponsibilityLetter,
                bl,
                ShipmentDocumentTemplates.ResponsibilityLetter(data, header, letter),
                ShipmentDocumentOrigins.Request,
                actor,
                organization.Id,
                data.Containers.Select(c => c.ContainerNumber).ToList(),
                TermsVersion: ResponsibilityLetterTerms.Version,
                TermsAcceptedAt: now,
                ValidUntil: validityDays > 0 ? now.AddDays(validityDays) : null),
            cancellationToken);
        if (issued.IsFailure)
            return Result<ShipmentDocumentDto>.Failure(issued.Error);

        // La carta nueva reemplaza la anterior de la organización sobre el mismo BL.
        var previous = await dbContext.ShipmentDocuments
            .Where(d => d.BillOfLadingId == bl.Id
                && d.IssuedForOrganizationId == organization.Id
                && d.DocumentType == ShipmentDocumentTypes.ResponsibilityLetter
                && d.Status == ShipmentDocumentStatus.Issued
                && d.Id != issued.Value.Id)
            .ToListAsync(cancellationToken);
        foreach (var old in previous)
            old.Status = ShipmentDocumentStatus.Superseded;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ShipmentDocumentDto>.Success(ShipmentDocumentService.ToDto(issued.Value, organization.Name));
    }
}
