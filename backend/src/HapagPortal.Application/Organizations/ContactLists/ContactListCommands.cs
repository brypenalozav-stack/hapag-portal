namespace HapagPortal.Application.Organizations.ContactLists;

using FluentValidation;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Correos registrados para un tipo de reporte (M1-06).</summary>
public sealed record ContactListDto(string ReportType, IReadOnlyList<string> Emails, DateTime? UpdatedAt, string? UpdatedBy);

/// <summary>
/// Listas de distribución de la organización por tipo de reporte, leídas del registro de contactos (P0060).
/// <see cref="Available"/> = el registro respondió; si no, <see cref="ErrorCode"/> (NF-11) y las listas vacías no deben
/// presentarse como vigentes. <see cref="CanEdit"/> = la matriz (M1-11) y el perfil del usuario permiten actualizarlas.
/// </summary>
public sealed record ContactListsDto(
    Guid OrganizationId,
    string? MatchCode,
    bool Available,
    string? ErrorCode,
    bool CanEdit,
    IReadOnlyList<string> ReportTypes,
    IReadOnlyList<ContactListDto> Lists);

/// <summary>Cambio registrado (append-only) con su resultado de propagación.</summary>
public sealed record ContactListChangeDto(
    Guid Id,
    string ReportType,
    IReadOnlyList<string> PreviousEmails,
    IReadOnlyList<string> NewEmails,
    string Status,
    string? SourceReference,
    string? ErrorCode,
    string ChangedBy,
    DateTime ChangedAt);

public sealed record GetContactListsQuery : IQuery<ContactListsDto>;

/// <summary>Reemplaza los correos de un tipo de reporte y propaga el cambio al registro de contactos (M1-06).</summary>
public sealed record UpdateContactListCommand(string ReportType, IReadOnlyList<string> Emails) : ICommand<ContactListDto>;

public sealed record GetContactListHistoryQuery(string? ReportType = null) : IQuery<IReadOnlyList<ContactListChangeDto>>;

public sealed class UpdateContactListCommandValidator : AbstractValidator<UpdateContactListCommand>
{
    public UpdateContactListCommandValidator()
    {
        RuleFor(x => x.ReportType).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Emails).NotNull()
            .Must(e => e.Count <= ContactReportTypes.MaxEmails)
            .WithMessage($"A distribution list has at most {ContactReportTypes.MaxEmails} e-mails.");
        RuleForEach(x => x.Emails).NotEmpty().MaximumLength(256).EmailAddress();
    }
}

/// <summary>Usuario de una organización aprobada que la matriz (M1-11) habilita; <c>CanEdit</c> = su perfil opera (M1-02).</summary>
internal sealed record ContactListContext(OrganizationMembership Membership, bool CanEdit)
{
    public Client Organization => Membership.Organization;

    public static async Task<Result<ContactListContext>> LoadAsync(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IShipmentAccessEvaluator accessEvaluator,
        CancellationToken cancellationToken)
    {
        var membership = await CurrentOrganization.LoadAsync(dbContext, currentUserService, requireApproved: true, cancellationToken);
        if (membership.IsFailure)
            return Result<ContactListContext>.Failure(membership.Error);

        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (!scope.IsOperational || scope.IsAdmin)
            return Result<ContactListContext>.Failure(Error.Forbidden);

        var matrix = await accessEvaluator.GetMatrixAsync(cancellationToken);
        var allowed = matrix.OrganizationCan(membership.Value.Organization.OrganizationType, ShipmentActionCodes.UpdateDistributionList);
        if (!allowed)
            return Result<ContactListContext>.Failure(DomainErrors.ContactList.NotAllowed);

        return Result<ContactListContext>.Success(new ContactListContext(membership.Value, scope.CanOperate));
    }
}

internal static class ContactListViews
{
    public static IReadOnlyList<string> Normalize(IEnumerable<string> emails) =>
        emails.Select(EmailNormalizer.Normalize).Where(e => e.Length > 0).Distinct().ToList();

    public static IReadOnlyList<string> Parse(string? csv) =>
        string.IsNullOrWhiteSpace(csv) ? [] : csv.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static string Format(IEnumerable<string> emails) => string.Join(";", emails);

    public static ContactListDto ToDto(ContactDistributionList list) =>
        new(list.ReportType, list.Emails, list.UpdatedAt, list.UpdatedBy);
}

public sealed class GetContactListsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    IContactListProvider contactListProvider)
    : IQueryHandler<GetContactListsQuery, ContactListsDto>
{
    public async Task<Result<ContactListsDto>> Handle(GetContactListsQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ContactListContext.LoadAsync(dbContext, currentUserService, accessEvaluator, cancellationToken);
        if (loaded.IsFailure)
            return Result<ContactListsDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var organization = context.Organization;
        if (string.IsNullOrWhiteSpace(organization.MatchCode))
            return Result<ContactListsDto>.Failure(DomainErrors.ContactList.MatchCodeRequired);

        var source = await contactListProvider.GetListsAsync(organization.MatchCode, organization.Country, cancellationToken);
        var lists = source.IsSuccess ? source.Value : [];

        IReadOnlyList<ContactListDto> items = ContactReportTypes.All
            .Select(type => lists.FirstOrDefault(l => l.ReportType == type) is { } list
                ? ContactListViews.ToDto(list)
                : new ContactListDto(type, [], null, null))
            .ToList();

        return Result<ContactListsDto>.Success(new ContactListsDto(
            organization.Id, organization.MatchCode, source.IsSuccess, source.IsFailure ? source.Error.Code : null,
            context.CanEdit && source.IsSuccess, ContactReportTypes.All, items));
    }
}

public sealed class UpdateContactListCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    IContactListProvider contactListProvider)
    : ICommandHandler<UpdateContactListCommand, ContactListDto>
{
    public async Task<Result<ContactListDto>> Handle(UpdateContactListCommand request, CancellationToken cancellationToken)
    {
        var reportType = request.ReportType.Trim().ToUpperInvariant();
        if (!ContactReportTypes.All.Contains(reportType))
            return Result<ContactListDto>.Failure(DomainErrors.ContactList.UnknownReportType(request.ReportType));

        var loaded = await ContactListContext.LoadAsync(dbContext, currentUserService, accessEvaluator, cancellationToken);
        if (loaded.IsFailure)
            return Result<ContactListDto>.Failure(loaded.Error);
        if (!loaded.Value.CanEdit)
            return Result<ContactListDto>.Failure(Error.Forbidden);

        var organization = loaded.Value.Organization;
        if (string.IsNullOrWhiteSpace(organization.MatchCode))
            return Result<ContactListDto>.Failure(DomainErrors.ContactList.MatchCodeRequired);

        var emails = ContactListViews.Normalize(request.Emails);
        var current = await contactListProvider.GetListsAsync(organization.MatchCode, organization.Country, cancellationToken);
        var previous = current.IsSuccess ? current.Value.FirstOrDefault(l => l.ReportType == reportType)?.Emails : null;

        var actor = PaymentActor.From(currentUserService);
        var change = new ContactListChange
        {
            OrganizationId = organization.Id,
            ReportType = reportType,
            PreviousEmails = previous is null ? null : ContactListViews.Format(previous),
            NewEmails = ContactListViews.Format(emails),
            Status = ContactListChangeStatus.Propagated,
            ChangedByUserId = currentUserService.UserId,
            ChangedBy = actor.Name,
            ChangedAt = DateTime.UtcNow
        };

        var updated = await contactListProvider.UpdateListAsync(
            organization.MatchCode, organization.Country, reportType, emails, actor.Name, $"contacts-{change.Id:N}", cancellationToken);

        if (updated.IsFailure)
        {
            // El intento fallido también queda registrado (M1-06): el cliente sabe que el cambio no llegó al origen.
            change.Status = ContactListChangeStatus.Failed;
            change.ErrorCode = updated.Error.Code;
            dbContext.ContactListChanges.Add(change);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<ContactListDto>.Failure(updated.Error);
        }

        change.SourceReference = updated.Value.SourceReference;
        dbContext.ContactListChanges.Add(change);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ContactListDto>.Success(ContactListViews.ToDto(updated.Value.List));
    }
}

public sealed class GetContactListHistoryQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetContactListHistoryQuery, IReadOnlyList<ContactListChangeDto>>
{
    public async Task<Result<IReadOnlyList<ContactListChangeDto>>> Handle(GetContactListHistoryQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ContactListContext.LoadAsync(dbContext, currentUserService, accessEvaluator, cancellationToken);
        if (loaded.IsFailure)
            return Result<IReadOnlyList<ContactListChangeDto>>.Failure(loaded.Error);

        var organizationId = loaded.Value.Organization.Id;
        var query = dbContext.ContactListChanges.AsNoTracking().Where(c => c.OrganizationId == organizationId);
        if (!string.IsNullOrWhiteSpace(request.ReportType))
        {
            var reportType = request.ReportType.Trim().ToUpperInvariant();
            query = query.Where(c => c.ReportType == reportType);
        }

        var changes = await query.OrderByDescending(c => c.ChangedAt).Take(200).ToListAsync(cancellationToken);
        IReadOnlyList<ContactListChangeDto> items = changes
            .Select(c => new ContactListChangeDto(
                c.Id, c.ReportType, ContactListViews.Parse(c.PreviousEmails), ContactListViews.Parse(c.NewEmails), c.Status,
                c.SourceReference, c.ErrorCode, c.ChangedBy, c.ChangedAt))
            .ToList();

        return Result<IReadOnlyList<ContactListChangeDto>>.Success(items);
    }
}
