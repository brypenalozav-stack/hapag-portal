namespace HapagPortal.Application.Organizations.Documents;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Adjunta documentación de respaldo al registro de la propia organización (M1-07) y deja registro
/// de lo adjuntado. El archivo se guarda por el puerto de almacenamiento (CT-STORAGE).
/// </summary>
public sealed record UploadOrganizationDocumentCommand(
    string DocumentType,
    string FileName,
    string ContentType,
    byte[] Content) : ICommand<OrganizationDocumentDto>;

/// <summary>Documentos adjuntos de la propia organización.</summary>
public sealed record GetMyOrganizationDocumentsQuery : IQuery<List<OrganizationDocumentDto>>;

/// <summary>Contenido de un documento de una organización, para la revisión interna (M8-04).</summary>
public sealed record GetOrganizationDocumentContentQuery(Guid OrganizationId, Guid DocumentId)
    : IQuery<OrganizationDocumentContentDto>;

public sealed record OrganizationDocumentContentDto(byte[] Content, string ContentType, string FileName);

public sealed class UploadOrganizationDocumentCommandValidator : AbstractValidator<UploadOrganizationDocumentCommand>
{
    public const int MaxSizeBytes = 10 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes = ["application/pdf", "image/png", "image/jpeg"];

    public UploadOrganizationDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentType)
            .Must(t => OrganizationDocumentTypes.All.Contains(t))
            .WithMessage("Document type must be RegistrationLetter, CreditAuthorization, TaxCertificate or Other.");
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType)
            .Must(c => AllowedContentTypes.Contains(c))
            .WithMessage("Only PDF, PNG or JPEG files are accepted.");
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("The file is empty.")
            .Must(c => c.Length <= MaxSizeBytes).WithMessage("The file must not exceed 10 MB.");
    }
}

public sealed class UploadOrganizationDocumentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IFileStorage fileStorage)
    : ICommandHandler<UploadOrganizationDocumentCommand, OrganizationDocumentDto>
{
    private const string StorageContainer = "organization-documents";

    public async Task<Result<OrganizationDocumentDto>> Handle(
        UploadOrganizationDocumentCommand request,
        CancellationToken cancellationToken)
    {
        // Se permite antes de la aprobación: es parte del registro que se valida.
        var membership = await CurrentOrganization.LoadAsync(
            dbContext, currentUserService, requireApproved: false, cancellationToken);

        if (membership.IsFailure)
            return Result<OrganizationDocumentDto>.Failure(membership.Error);

        var (user, organization) = membership.Value;

        using var stream = new MemoryStream(request.Content, writable: false);
        var stored = await fileStorage.SaveAsync(
            stream, request.FileName, request.ContentType, StorageContainer, cancellationToken);

        if (stored.IsFailure)
            return Result<OrganizationDocumentDto>.Failure(stored.Error);

        var document = new OrganizationDocument
        {
            ClientId = organization.Id,
            DocumentType = request.DocumentType,
            FileName = Path.GetFileName(request.FileName),
            ContentType = request.ContentType,
            SizeBytes = request.Content.LongLength,
            StorageKey = stored.Value,
            UploadedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.OrganizationDocuments.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<OrganizationDocumentDto>.Success(OrganizationMapper.ToDto(document));
    }
}

public sealed class GetMyOrganizationDocumentsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyOrganizationDocumentsQuery, List<OrganizationDocumentDto>>
{
    public async Task<Result<List<OrganizationDocumentDto>>> Handle(
        GetMyOrganizationDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var membership = await CurrentOrganization.LoadAsync(
            dbContext, currentUserService, requireApproved: false, cancellationToken);

        if (membership.IsFailure)
            return Result<List<OrganizationDocumentDto>>.Failure(membership.Error);

        var organizationId = membership.Value.Organization.Id;

        var documents = await dbContext.OrganizationDocuments
            .AsNoTracking()
            .Where(d => d.ClientId == organizationId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<List<OrganizationDocumentDto>>.Success(documents.Select(OrganizationMapper.ToDto).ToList());
    }
}

public sealed class GetOrganizationDocumentContentQueryHandler(
    IApplicationDbContext dbContext,
    IFileStorage fileStorage)
    : IQueryHandler<GetOrganizationDocumentContentQuery, OrganizationDocumentContentDto>
{
    public async Task<Result<OrganizationDocumentContentDto>> Handle(
        GetOrganizationDocumentContentQuery request,
        CancellationToken cancellationToken)
    {
        var document = await dbContext.OrganizationDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.Id == request.DocumentId && d.ClientId == request.OrganizationId,
                cancellationToken);

        if (document is null)
            return Result<OrganizationDocumentContentDto>.Failure(
                DomainErrors.Organization.DocumentNotFound(request.DocumentId));

        var opened = await fileStorage.OpenReadAsync(document.StorageKey, cancellationToken);
        if (opened.IsFailure)
            return Result<OrganizationDocumentContentDto>.Failure(opened.Error);

        if (opened.Value is null)
            return Result<OrganizationDocumentContentDto>.Failure(
                DomainErrors.Organization.DocumentNotFound(request.DocumentId));

        await using var stream = opened.Value;
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);

        return Result<OrganizationDocumentContentDto>.Success(
            new OrganizationDocumentContentDto(buffer.ToArray(), document.ContentType, document.FileName));
    }
}
