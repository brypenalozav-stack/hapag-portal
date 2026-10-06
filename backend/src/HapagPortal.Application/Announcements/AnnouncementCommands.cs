namespace HapagPortal.Application.Announcements;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Comunicado en el mantenedor interno (M1-26, M8-05).</summary>
public sealed record AnnouncementDto(
    Guid Id,
    string TitleEs,
    string TitleEn,
    string BodyEs,
    string BodyEn,
    IReadOnlyList<string> Countries,
    string Operation,
    string Severity,
    DateTime ValidFrom,
    DateTime? ValidTo,
    string Status,
    bool IsCurrent,
    DateTime? PublishedAt,
    string? PublishedBy,
    DateTime? UnpublishedAt,
    string? UnpublishedBy,
    bool NotifyOnPublish,
    DateTime? NotifiedAt,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? ModifiedAt,
    string? ModifiedBy);

/// <summary>Comunicado vigente para el cliente, con su fecha de publicación (M1-26).</summary>
public sealed record PublishedAnnouncementDto(
    Guid Id,
    string TitleEs,
    string TitleEn,
    string BodyEs,
    string BodyEn,
    IReadOnlyList<string> Countries,
    string Operation,
    string Severity,
    DateTime PublishedAt,
    DateTime ValidFrom,
    DateTime? ValidTo);

/// <summary>Instantánea de un comunicado para el registro de cambios (NF-15).</summary>
public sealed record AnnouncementSnapshot(
    string TitleEs,
    string TitleEn,
    string BodyEs,
    string BodyEn,
    string Countries,
    string Operation,
    string Severity,
    DateTime ValidFrom,
    DateTime? ValidTo,
    string Status,
    bool NotifyOnPublish)
{
    public static AnnouncementSnapshot From(Announcement a) =>
        new(a.TitleEs, a.TitleEn, a.BodyEs, a.BodyEn, a.Countries, a.Operation, a.Severity, a.ValidFrom, a.ValidTo, a.Status, a.NotifyOnPublish);
}

/// <summary>Comunicados vigentes para el usuario: país (por defecto el del usuario) y operación opcional (M1-26, M1-04, M2-07).</summary>
public sealed record GetCurrentAnnouncementsQuery(string? Country = null, string? Operation = null)
    : IQuery<IReadOnlyList<PublishedAnnouncementDto>>;

public sealed record GetAnnouncementsQuery(string? Status = null, string? Country = null)
    : IQuery<IReadOnlyList<AnnouncementDto>>;

public sealed record GetAnnouncementQuery(Guid Id) : IQuery<AnnouncementDto>;

public sealed record CreateAnnouncementCommand(
    string TitleEs,
    string TitleEn,
    string BodyEs,
    string BodyEn,
    IReadOnlyList<string> Countries,
    string Operation,
    string? Severity,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    bool NotifyOnPublish = false,
    bool Publish = false) : ICommand<AnnouncementDto>;

public sealed record UpdateAnnouncementCommand(
    Guid Id,
    string TitleEs,
    string TitleEn,
    string BodyEs,
    string BodyEn,
    IReadOnlyList<string> Countries,
    string Operation,
    string? Severity,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    bool NotifyOnPublish = false) : ICommand<AnnouncementDto>;

/// <summary>Publica el comunicado (borrador o despublicado) y, si corresponde, avisa al segmento en la bandeja.</summary>
public sealed record PublishAnnouncementCommand(Guid Id) : ICommand<AnnouncementDto>;

/// <summary>Retira un comunicado publicado: deja de mostrarse aunque siga vigente.</summary>
public sealed record UnpublishAnnouncementCommand(Guid Id) : ICommand<AnnouncementDto>;

/// <summary>Elimina el comunicado (borrado lógico, con su historial).</summary>
public sealed record DeleteAnnouncementCommand(Guid Id) : ICommand;

public sealed record GetAnnouncementHistoryQuery(Guid Id)
    : IQuery<IReadOnlyList<PaymentMaintainerChangeDto<AnnouncementSnapshot>>>;

internal static class AnnouncementRules
{
    public static void Content<T>(
        AbstractValidator<T> validator,
        Func<T, string> titleEs,
        Func<T, string> titleEn,
        Func<T, string> bodyEs,
        Func<T, string> bodyEn,
        Func<T, IReadOnlyList<string>> countries,
        Func<T, string> operation,
        Func<T, string?> severity,
        Func<T, DateTime?> validFrom,
        Func<T, DateTime?> validTo)
    {
        validator.RuleFor(x => titleEs(x)).NotEmpty().MaximumLength(200).WithName("TitleEs");
        validator.RuleFor(x => titleEn(x)).NotEmpty().MaximumLength(200).WithName("TitleEn");
        validator.RuleFor(x => bodyEs(x)).NotEmpty().MaximumLength(4000).WithName("BodyEs");
        validator.RuleFor(x => bodyEn(x)).NotEmpty().MaximumLength(4000).WithName("BodyEn");
        validator.RuleFor(x => countries(x))
            .Must(c => c is { Count: > 0 } && c.All(v => CountryCodes.ValidCountries.Contains(v?.Trim().ToUpperInvariant())))
            .WithName("Countries")
            .WithMessage("Countries must contain CL and/or BO.");
        validator.RuleFor(x => operation(x))
            .Must(o => AnnouncementOperations.All.Contains(o))
            .WithName("Operation")
            .WithMessage("Operation must be Import, Export or Both.");
        validator.RuleFor(x => severity(x))
            .Must(s => s is null || AnnouncementSeverities.All.Contains(s))
            .WithName("Severity")
            .WithMessage("Severity must be Info or Important.");
        validator.RuleFor(x => x)
            .Must(x => validTo(x) is null || validTo(x) > (validFrom(x) ?? DateTime.MinValue))
            .WithName("ValidTo")
            .WithMessage("ValidTo must be later than ValidFrom.");
    }

    public static string Countries(IEnumerable<string> countries) =>
        string.Join(",", countries.Select(c => c.Trim().ToUpperInvariant()).Distinct().OrderBy(c => c == CountryCodes.Chile ? 0 : 1));

    public static IReadOnlyList<string> ParseCountries(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static bool IsCurrent(Announcement a, DateTime now) =>
        a.Status == AnnouncementStatus.Published && a.ValidFrom <= now && (a.ValidTo is null || a.ValidTo > now);

    public static AnnouncementDto ToDto(Announcement a, DateTime now) => new(
        a.Id, a.TitleEs, a.TitleEn, a.BodyEs, a.BodyEn, ParseCountries(a.Countries), a.Operation, a.Severity, a.ValidFrom, a.ValidTo,
        a.Status, IsCurrent(a, now), a.PublishedAt, a.PublishedBy, a.UnpublishedAt, a.UnpublishedBy, a.NotifyOnPublish, a.NotifiedAt,
        a.CreatedAt, a.CreatedBy, a.ModifiedAt, a.ModifiedBy);

    public static DateTime Utc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
}

public sealed class CreateAnnouncementCommandValidator : AbstractValidator<CreateAnnouncementCommand>
{
    public CreateAnnouncementCommandValidator() =>
        AnnouncementRules.Content(this, x => x.TitleEs, x => x.TitleEn, x => x.BodyEs, x => x.BodyEn, x => x.Countries, x => x.Operation,
            x => x.Severity, x => x.ValidFrom, x => x.ValidTo);
}

public sealed class UpdateAnnouncementCommandValidator : AbstractValidator<UpdateAnnouncementCommand>
{
    public UpdateAnnouncementCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        AnnouncementRules.Content(this, x => x.TitleEs, x => x.TitleEn, x => x.BodyEs, x => x.BodyEn, x => x.Countries, x => x.Operation,
            x => x.Severity, x => x.ValidFrom, x => x.ValidTo);
    }
}

public sealed class GetCurrentAnnouncementsQueryValidator : AbstractValidator<GetCurrentAnnouncementsQuery>
{
    public GetCurrentAnnouncementsQueryValidator()
    {
        RuleFor(x => x.Country)
            .Must(c => CountryCodes.ValidCountries.Contains(c!.Trim().ToUpperInvariant()))
            .When(x => !string.IsNullOrWhiteSpace(x.Country))
            .WithMessage("Country must be 'CL' or 'BO'.");
        RuleFor(x => x.Operation)
            .Must(o => o is AnnouncementOperations.Import or AnnouncementOperations.Export)
            .When(x => !string.IsNullOrWhiteSpace(x.Operation))
            .WithMessage("Operation must be Import or Export.");
    }
}

public sealed class GetCurrentAnnouncementsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetCurrentAnnouncementsQuery, IReadOnlyList<PublishedAnnouncementDto>>
{
    public async Task<Result<IReadOnlyList<PublishedAnnouncementDto>>> Handle(
        GetCurrentAnnouncementsQuery request,
        CancellationToken cancellationToken)
    {
        var country = string.IsNullOrWhiteSpace(request.Country)
            ? currentUserService.Country?.Trim().ToUpperInvariant()
            : request.Country.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;

        var published = await dbContext.Announcements.AsNoTracking()
            .Where(a => a.Status == AnnouncementStatus.Published && a.ValidFrom <= now && (a.ValidTo == null || a.ValidTo > now))
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PublishedAnnouncementDto> items = published
            .Where(a => country is null || AnnouncementRules.ParseCountries(a.Countries).Contains(country))
            .Where(a => request.Operation is null or "" || a.Operation == AnnouncementOperations.Both || a.Operation == request.Operation)
            .Select(a => new PublishedAnnouncementDto(
                a.Id, a.TitleEs, a.TitleEn, a.BodyEs, a.BodyEn, AnnouncementRules.ParseCountries(a.Countries), a.Operation, a.Severity,
                a.PublishedAt ?? a.ValidFrom, a.ValidFrom, a.ValidTo))
            .ToList();

        return Result<IReadOnlyList<PublishedAnnouncementDto>>.Success(items);
    }
}

public sealed class GetAnnouncementsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAnnouncementsQuery, IReadOnlyList<AnnouncementDto>>
{
    public async Task<Result<IReadOnlyList<AnnouncementDto>>> Handle(GetAnnouncementsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Announcements.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(a => a.Status == request.Status);

        var now = DateTime.UtcNow;
        var announcements = await query.OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);
        var country = request.Country?.Trim().ToUpperInvariant();

        IReadOnlyList<AnnouncementDto> items = announcements
            .Where(a => string.IsNullOrEmpty(country) || AnnouncementRules.ParseCountries(a.Countries).Contains(country))
            .Select(a => AnnouncementRules.ToDto(a, now))
            .ToList();
        return Result<IReadOnlyList<AnnouncementDto>>.Success(items);
    }
}

public sealed class GetAnnouncementQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAnnouncementQuery, AnnouncementDto>
{
    public async Task<Result<AnnouncementDto>> Handle(GetAnnouncementQuery request, CancellationToken cancellationToken)
    {
        var announcement = await dbContext.Announcements.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        return announcement is null
            ? Result<AnnouncementDto>.Failure(DomainErrors.Announcement.NotFound(request.Id))
            : Result<AnnouncementDto>.Success(AnnouncementRules.ToDto(announcement, DateTime.UtcNow));
    }
}

public sealed class CreateAnnouncementCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    AnnouncementPublisher announcementPublisher)
    : ICommandHandler<CreateAnnouncementCommand, AnnouncementDto>
{
    public async Task<Result<AnnouncementDto>> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var announcement = new Announcement
        {
            TitleEs = request.TitleEs.Trim(),
            TitleEn = request.TitleEn.Trim(),
            BodyEs = request.BodyEs.Trim(),
            BodyEn = request.BodyEn.Trim(),
            Countries = AnnouncementRules.Countries(request.Countries),
            Operation = request.Operation,
            Severity = request.Severity ?? AnnouncementSeverities.Info,
            ValidFrom = request.ValidFrom is null ? now : AnnouncementRules.Utc(request.ValidFrom.Value),
            ValidTo = request.ValidTo is null ? null : AnnouncementRules.Utc(request.ValidTo.Value),
            Status = AnnouncementStatus.Draft,
            NotifyOnPublish = request.NotifyOnPublish,
            CreatedAt = now,
            CreatedBy = PaymentActor.From(currentUserService).Name
        };

        dbContext.Announcements.Add(announcement);
        MaintainerChangeLogger.Log(dbContext, currentUserService, MaintainerNames.Announcement, announcement.Id,
            MaintainerActions.Created, null, AnnouncementSnapshot.From(announcement), now);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.Publish)
        {
            var published = await announcementPublisher.PublishAsync(announcement, now, cancellationToken);
            if (published.IsFailure)
                return Result<AnnouncementDto>.Failure(published.Error);
        }

        return Result<AnnouncementDto>.Success(AnnouncementRules.ToDto(announcement, now));
    }
}

public sealed class UpdateAnnouncementCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateAnnouncementCommand, AnnouncementDto>
{
    public async Task<Result<AnnouncementDto>> Handle(UpdateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await dbContext.Announcements.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (announcement is null)
            return Result<AnnouncementDto>.Failure(DomainErrors.Announcement.NotFound(request.Id));

        var now = DateTime.UtcNow;
        var previous = AnnouncementSnapshot.From(announcement);

        announcement.TitleEs = request.TitleEs.Trim();
        announcement.TitleEn = request.TitleEn.Trim();
        announcement.BodyEs = request.BodyEs.Trim();
        announcement.BodyEn = request.BodyEn.Trim();
        announcement.Countries = AnnouncementRules.Countries(request.Countries);
        announcement.Operation = request.Operation;
        announcement.Severity = request.Severity ?? announcement.Severity;
        announcement.ValidFrom = request.ValidFrom is null ? announcement.ValidFrom : AnnouncementRules.Utc(request.ValidFrom.Value);
        announcement.ValidTo = request.ValidTo is null ? null : AnnouncementRules.Utc(request.ValidTo.Value);
        announcement.NotifyOnPublish = request.NotifyOnPublish;
        announcement.ModifiedAt = now;
        announcement.ModifiedBy = PaymentActor.From(currentUserService).Name;

        MaintainerChangeLogger.Log(dbContext, currentUserService, MaintainerNames.Announcement, announcement.Id,
            MaintainerActions.Updated, previous, AnnouncementSnapshot.From(announcement), now);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AnnouncementDto>.Success(AnnouncementRules.ToDto(announcement, now));
    }
}

public sealed class PublishAnnouncementCommandHandler(
    IApplicationDbContext dbContext,
    AnnouncementPublisher announcementPublisher)
    : ICommandHandler<PublishAnnouncementCommand, AnnouncementDto>
{
    public async Task<Result<AnnouncementDto>> Handle(PublishAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await dbContext.Announcements.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (announcement is null)
            return Result<AnnouncementDto>.Failure(DomainErrors.Announcement.NotFound(request.Id));

        var now = DateTime.UtcNow;
        var published = await announcementPublisher.PublishAsync(announcement, now, cancellationToken);
        return published.IsFailure
            ? Result<AnnouncementDto>.Failure(published.Error)
            : Result<AnnouncementDto>.Success(AnnouncementRules.ToDto(announcement, now));
    }
}

public sealed class UnpublishAnnouncementCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<UnpublishAnnouncementCommand, AnnouncementDto>
{
    public async Task<Result<AnnouncementDto>> Handle(UnpublishAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await dbContext.Announcements.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (announcement is null)
            return Result<AnnouncementDto>.Failure(DomainErrors.Announcement.NotFound(request.Id));
        if (announcement.Status != AnnouncementStatus.Published)
            return Result<AnnouncementDto>.Failure(
                DomainErrors.Announcement.InvalidTransition(announcement.Status, AnnouncementStatus.Unpublished));

        var now = DateTime.UtcNow;
        var previous = AnnouncementSnapshot.From(announcement);
        announcement.Status = AnnouncementStatus.Unpublished;
        announcement.UnpublishedAt = now;
        announcement.UnpublishedBy = PaymentActor.From(currentUserService).Name;

        MaintainerChangeLogger.Log(dbContext, currentUserService, MaintainerNames.Announcement, announcement.Id,
            MaintainerActions.Updated, previous, AnnouncementSnapshot.From(announcement), now);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AnnouncementDto>.Success(AnnouncementRules.ToDto(announcement, now));
    }
}

public sealed class DeleteAnnouncementCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<DeleteAnnouncementCommand>
{
    public async Task<Result> Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await dbContext.Announcements.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (announcement is null)
            return Result.Failure(DomainErrors.Announcement.NotFound(request.Id));

        var now = DateTime.UtcNow;
        MaintainerChangeLogger.Log(dbContext, currentUserService, MaintainerNames.Announcement, announcement.Id,
            MaintainerActions.Deactivated, AnnouncementSnapshot.From(announcement), null, now);
        announcement.DeletedAt = now;
        announcement.DeletedBy = PaymentActor.From(currentUserService).Name;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class GetAnnouncementHistoryQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAnnouncementHistoryQuery, IReadOnlyList<PaymentMaintainerChangeDto<AnnouncementSnapshot>>>
{
    public async Task<Result<IReadOnlyList<PaymentMaintainerChangeDto<AnnouncementSnapshot>>>> Handle(
        GetAnnouncementHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var changes = await dbContext.MaintainerChangeLogs.AsNoTracking()
            .Where(c => c.Maintainer == MaintainerNames.Announcement && c.EntityId == request.Id)
            .OrderByDescending(c => c.ChangedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PaymentMaintainerChangeDto<AnnouncementSnapshot>> items = changes
            .Select(c => new PaymentMaintainerChangeDto<AnnouncementSnapshot>(
                c.Id, c.EntityId, c.Action, c.ChangedAt, c.ChangedBy, c.ChangedByUserId,
                MaintainerChangeLogger.Read<AnnouncementSnapshot>(c.PreviousValue),
                MaintainerChangeLogger.Read<AnnouncementSnapshot>(c.NewValue)))
            .ToList();

        return Result<IReadOnlyList<PaymentMaintainerChangeDto<AnnouncementSnapshot>>>.Success(items);
    }
}

/// <summary>
/// Publicación de un comunicado (M1-26): pasa a <c>Published</c> con fecha y autor (NF-15) y, si se pidió, avisa en la
/// bandeja (M1-25) a los usuarios activos de las organizaciones cliente que operan en alguno de sus países. El aviso se
/// envía una sola vez por comunicado; el correo depende de la preferencia de cada usuario (por defecto, solo bandeja).
/// </summary>
public sealed class AnnouncementPublisher(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    INotificationPublisher notificationPublisher)
{
    public async Task<Result> PublishAsync(Announcement announcement, DateTime now, CancellationToken cancellationToken)
    {
        if (announcement.Status == AnnouncementStatus.Published)
            return Result.Failure(DomainErrors.Announcement.InvalidTransition(announcement.Status, AnnouncementStatus.Published));
        if (announcement.ValidTo is not null && announcement.ValidTo <= now)
            return Result.Failure(DomainErrors.Announcement.Expired);

        var previous = AnnouncementSnapshot.From(announcement);
        announcement.Status = AnnouncementStatus.Published;
        announcement.PublishedAt = now;
        announcement.PublishedBy = PaymentActor.From(currentUserService).Name;
        announcement.UnpublishedAt = null;
        announcement.UnpublishedBy = null;

        MaintainerChangeLogger.Log(dbContext, currentUserService, MaintainerNames.Announcement, announcement.Id,
            MaintainerActions.Updated, previous, AnnouncementSnapshot.From(announcement), now);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (announcement.NotifyOnPublish && announcement.NotifiedAt is null)
        {
            await NotifySegmentAsync(announcement, cancellationToken);
            announcement.NotifiedAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    private async Task NotifySegmentAsync(Announcement announcement, CancellationToken cancellationToken)
    {
        var countries = AnnouncementRules.ParseCountries(announcement.Countries);

        var recipients = await (
                from user in dbContext.Users.AsNoTracking()
                join organization in dbContext.Clients.AsNoTracking() on user.ClientId equals organization.Id
                where user.IsActive
                      && user.MembershipStatus == MembershipStatus.Active
                      && organization.IsActive
                      && organization.RegistrationStatus == OrganizationStatus.Approved
                      && organization.OrganizationType != OrganizationTypes.Internal
                select new { user.Id, user.Email, Organization = organization })
            .ToListAsync(cancellationToken);

        foreach (var recipient in recipients.Where(r => OrganizationMapper.OperatingCountriesOf(r.Organization).Any(countries.Contains)))
        {
            await notificationPublisher.PublishAsync(
                new NotificationRequest(
                    NotificationTypes.AnnouncementPublished,
                    announcement.TitleEs,
                    announcement.BodyEs.Length > 500 ? announcement.BodyEs[..497] + "..." : announcement.BodyEs,
                    UserId: recipient.Id,
                    DedupKey: $"announcement:{announcement.Id}:{recipient.Id}",
                    Email: recipient.Email,
                    Link: new NotificationLink(NotificationEntityTypes.Announcement, announcement.Id.ToString(), announcement.TitleEs),
                    Action: new NotificationAction(NotificationActionTypes.OpenAnnouncement, announcement.Id.ToString())),
                cancellationToken);
        }
    }
}
