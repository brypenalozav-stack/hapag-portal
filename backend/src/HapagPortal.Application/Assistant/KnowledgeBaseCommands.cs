namespace HapagPortal.Application.Assistant;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Artículo de la base de conocimiento del asistente (M10-02).</summary>
public sealed record KnowledgeArticleDto(
    Guid Id,
    string Country,
    string Topic,
    string Title,
    string Content,
    string? Keywords,
    int SortOrder,
    bool IsActive,
    Guid? SourceFaqId,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? ModifiedAt,
    string? ModifiedBy);

/// <summary>Instantánea del artículo para el registro de cambios (NF-15).</summary>
public sealed record KnowledgeArticleSnapshot(
    string Country,
    string Topic,
    string Title,
    string Content,
    string? Keywords,
    int SortOrder,
    bool IsActive)
{
    public static KnowledgeArticleSnapshot From(KnowledgeArticle a) =>
        new(a.Country, a.Topic, a.Title, a.Content, a.Keywords, a.SortOrder, a.IsActive);
}

public sealed record KnowledgeArticleChangeDto(
    Guid Id,
    Guid ArticleId,
    string Action,
    DateTime ChangedAt,
    string ChangedBy,
    Guid? ChangedByUserId,
    KnowledgeArticleSnapshot? Previous,
    KnowledgeArticleSnapshot? Current);

/// <summary>Casilla de derivación por país y tema (M10-02).</summary>
public sealed record AssistantMailboxDto(
    Guid Id,
    string Country,
    string Topic,
    string Email,
    string? Notes,
    bool IsActive,
    DateTime? ModifiedAt,
    string? ModifiedBy);

public sealed record AssistantMailboxSnapshot(string Country, string Topic, string Email, string? Notes, bool IsActive)
{
    public static AssistantMailboxSnapshot From(AssistantMailbox m) => new(m.Country, m.Topic, m.Email, m.Notes, m.IsActive);
}

public sealed record CreateKnowledgeArticleCommand(
    string Country,
    string Topic,
    string Title,
    string Content,
    string? Keywords,
    int SortOrder) : ICommand<KnowledgeArticleDto>;

public sealed record UpdateKnowledgeArticleCommand(
    Guid Id,
    string Country,
    string Topic,
    string Title,
    string Content,
    string? Keywords,
    int SortOrder) : ICommand<KnowledgeArticleDto>;

public sealed record DeactivateKnowledgeArticleCommand(Guid Id) : ICommand;

public sealed record GetKnowledgeArticlesQuery(string? Country = null, string? Topic = null, bool IncludeInactive = false)
    : IQuery<IReadOnlyList<KnowledgeArticleDto>>;

public sealed record GetKnowledgeArticleHistoryQuery(Guid Id) : IQuery<IReadOnlyList<KnowledgeArticleChangeDto>>;

public sealed record GetAssistantMailboxesQuery : IQuery<IReadOnlyList<AssistantMailboxDto>>;

/// <summary>Crea o actualiza la casilla de un país y tema (NF-15).</summary>
public sealed record UpsertAssistantMailboxCommand(string Country, string Topic, string Email, string? Notes, bool IsActive = true)
    : ICommand<AssistantMailboxDto>;

internal static class KnowledgeArticleFields
{
    public static void Apply<T>(AbstractValidator<T> validator, Func<T, KnowledgeArticleSnapshot> fields)
    {
        validator.RuleFor(x => fields(x).Country)
            .Must(c => CountryCodes.ValidCountries.Contains(c))
            .WithName("Country")
            .WithMessage("Country must be CL or BO.");
        validator.RuleFor(x => fields(x).Topic)
            .Must(t => AssistantTopics.All.Contains(t))
            .WithName("Topic")
            .WithMessage($"Topic must be one of {string.Join(", ", AssistantTopics.All)}.");
        validator.RuleFor(x => fields(x).Title).NotEmpty().MaximumLength(200).WithName("Title");
        validator.RuleFor(x => fields(x).Content).NotEmpty().MaximumLength(4000).WithName("Content");
        validator.RuleFor(x => fields(x).Keywords).MaximumLength(500).WithName("Keywords");
        validator.RuleFor(x => fields(x).SortOrder).GreaterThanOrEqualTo(0).WithName("SortOrder");
    }

    public static KnowledgeArticleSnapshot Normalize(
        string country, string topic, string title, string content, string? keywords, int sortOrder) =>
        new(
            (country ?? string.Empty).Trim().ToUpperInvariant(),
            (topic ?? string.Empty).Trim().ToUpperInvariant(),
            (title ?? string.Empty).Trim(),
            (content ?? string.Empty).Trim(),
            string.IsNullOrWhiteSpace(keywords) ? null : keywords.Trim(),
            sortOrder,
            true);

    public static void Write(KnowledgeArticle article, KnowledgeArticleSnapshot fields)
    {
        article.Country = fields.Country;
        article.Topic = fields.Topic;
        article.Title = fields.Title;
        article.Content = fields.Content;
        article.Keywords = fields.Keywords;
        article.SortOrder = fields.SortOrder;
    }

    public static KnowledgeArticleDto ToDto(KnowledgeArticle a) => new(
        a.Id, a.Country, a.Topic, a.Title, a.Content, a.Keywords, a.SortOrder, a.IsActive, a.SourceFaqId,
        a.CreatedAt, a.CreatedBy, a.ModifiedAt, a.ModifiedBy);

    public static AssistantMailboxDto ToDto(AssistantMailbox m) =>
        new(m.Id, m.Country, m.Topic, m.Email, m.Notes, m.IsActive, m.ModifiedAt ?? m.CreatedAt, m.ModifiedBy ?? m.CreatedBy);
}

public sealed class CreateKnowledgeArticleCommandValidator : AbstractValidator<CreateKnowledgeArticleCommand>
{
    public CreateKnowledgeArticleCommandValidator()
    {
        KnowledgeArticleFields.Apply(this, x => KnowledgeArticleFields.Normalize(x.Country, x.Topic, x.Title, x.Content, x.Keywords, x.SortOrder));
    }
}

public sealed class UpdateKnowledgeArticleCommandValidator : AbstractValidator<UpdateKnowledgeArticleCommand>
{
    public UpdateKnowledgeArticleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        KnowledgeArticleFields.Apply(this, x => KnowledgeArticleFields.Normalize(x.Country, x.Topic, x.Title, x.Content, x.Keywords, x.SortOrder));
    }
}

public sealed class UpsertAssistantMailboxCommandValidator : AbstractValidator<UpsertAssistantMailboxCommand>
{
    public UpsertAssistantMailboxCommandValidator()
    {
        RuleFor(x => x.Country)
            .Must(c => CountryCodes.ValidCountries.Contains((c ?? string.Empty).Trim().ToUpperInvariant()))
            .WithMessage("Country must be CL or BO.");
        RuleFor(x => x.Topic)
            .Must(t => AssistantTopics.All.Contains((t ?? string.Empty).Trim().ToUpperInvariant()))
            .WithMessage($"Topic must be one of {string.Join(", ", AssistantTopics.All)}.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Notes).MaximumLength(300);
    }
}

public sealed class CreateKnowledgeArticleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateKnowledgeArticleCommand, KnowledgeArticleDto>
{
    public async Task<Result<KnowledgeArticleDto>> Handle(CreateKnowledgeArticleCommand request, CancellationToken cancellationToken)
    {
        var fields = KnowledgeArticleFields.Normalize(
            request.Country, request.Topic, request.Title, request.Content, request.Keywords, request.SortOrder);

        var article = new KnowledgeArticle
        {
            Country = fields.Country,
            Topic = fields.Topic,
            Title = fields.Title,
            Content = fields.Content
        };
        KnowledgeArticleFields.Write(article, fields);
        dbContext.KnowledgeArticles.Add(article);

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.KnowledgeArticle, article.Id, MaintainerActions.Created,
            null, KnowledgeArticleSnapshot.From(article), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<KnowledgeArticleDto>.Success(KnowledgeArticleFields.ToDto(article));
    }
}

public sealed class UpdateKnowledgeArticleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateKnowledgeArticleCommand, KnowledgeArticleDto>
{
    public async Task<Result<KnowledgeArticleDto>> Handle(UpdateKnowledgeArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await dbContext.KnowledgeArticles
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.IsActive, cancellationToken);
        if (article is null)
            return Result<KnowledgeArticleDto>.Failure(DomainErrors.KnowledgeArticle.NotFound(request.Id));

        var previous = KnowledgeArticleSnapshot.From(article);
        KnowledgeArticleFields.Write(article, KnowledgeArticleFields.Normalize(
            request.Country, request.Topic, request.Title, request.Content, request.Keywords, request.SortOrder));

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.KnowledgeArticle, article.Id, MaintainerActions.Updated,
            previous, KnowledgeArticleSnapshot.From(article), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<KnowledgeArticleDto>.Success(KnowledgeArticleFields.ToDto(article));
    }
}

public sealed class DeactivateKnowledgeArticleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<DeactivateKnowledgeArticleCommand>
{
    public async Task<Result> Handle(DeactivateKnowledgeArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await dbContext.KnowledgeArticles
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.IsActive, cancellationToken);
        if (article is null)
            return Result.Failure(DomainErrors.KnowledgeArticle.NotFound(request.Id));

        var previous = KnowledgeArticleSnapshot.From(article);
        article.IsActive = false;

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.KnowledgeArticle, article.Id, MaintainerActions.Deactivated,
            previous, KnowledgeArticleSnapshot.From(article), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class GetKnowledgeArticlesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetKnowledgeArticlesQuery, IReadOnlyList<KnowledgeArticleDto>>
{
    public async Task<Result<IReadOnlyList<KnowledgeArticleDto>>> Handle(GetKnowledgeArticlesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.KnowledgeArticles.AsNoTracking();

        if (!request.IncludeInactive)
            query = query.Where(a => a.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            var country = request.Country.Trim().ToUpperInvariant();
            query = query.Where(a => a.Country == country);
        }

        if (!string.IsNullOrWhiteSpace(request.Topic))
        {
            var topic = request.Topic.Trim().ToUpperInvariant();
            query = query.Where(a => a.Topic == topic);
        }

        var articles = await query
            .OrderBy(a => a.Country)
            .ThenBy(a => a.Topic)
            .ThenBy(a => a.SortOrder)
            .ThenBy(a => a.Title)
            .ToListAsync(cancellationToken);

        IReadOnlyList<KnowledgeArticleDto> items = articles.Select(KnowledgeArticleFields.ToDto).ToList();
        return Result<IReadOnlyList<KnowledgeArticleDto>>.Success(items);
    }
}

public sealed class GetKnowledgeArticleHistoryQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetKnowledgeArticleHistoryQuery, IReadOnlyList<KnowledgeArticleChangeDto>>
{
    public async Task<Result<IReadOnlyList<KnowledgeArticleChangeDto>>> Handle(GetKnowledgeArticleHistoryQuery request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.KnowledgeArticles.AsNoTracking().AnyAsync(a => a.Id == request.Id, cancellationToken);
        if (!exists)
            return Result<IReadOnlyList<KnowledgeArticleChangeDto>>.Failure(DomainErrors.KnowledgeArticle.NotFound(request.Id));

        var changes = await dbContext.MaintainerChangeLogs.AsNoTracking()
            .Where(c => c.Maintainer == MaintainerNames.KnowledgeArticle && c.EntityId == request.Id)
            .OrderByDescending(c => c.ChangedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<KnowledgeArticleChangeDto> items = changes
            .Select(c => new KnowledgeArticleChangeDto(
                c.Id, c.EntityId, c.Action, c.ChangedAt, c.ChangedBy, c.ChangedByUserId,
                MaintainerChangeLogger.Read<KnowledgeArticleSnapshot>(c.PreviousValue),
                MaintainerChangeLogger.Read<KnowledgeArticleSnapshot>(c.NewValue)))
            .ToList();

        return Result<IReadOnlyList<KnowledgeArticleChangeDto>>.Success(items);
    }
}

public sealed class GetAssistantMailboxesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAssistantMailboxesQuery, IReadOnlyList<AssistantMailboxDto>>
{
    public async Task<Result<IReadOnlyList<AssistantMailboxDto>>> Handle(GetAssistantMailboxesQuery request, CancellationToken cancellationToken)
    {
        var mailboxes = await dbContext.AssistantMailboxes.AsNoTracking()
            .OrderBy(m => m.Country)
            .ThenBy(m => m.Topic)
            .ToListAsync(cancellationToken);

        IReadOnlyList<AssistantMailboxDto> items = mailboxes.Select(KnowledgeArticleFields.ToDto).ToList();
        return Result<IReadOnlyList<AssistantMailboxDto>>.Success(items);
    }
}

public sealed class UpsertAssistantMailboxCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpsertAssistantMailboxCommand, AssistantMailboxDto>
{
    public async Task<Result<AssistantMailboxDto>> Handle(UpsertAssistantMailboxCommand request, CancellationToken cancellationToken)
    {
        var country = request.Country.Trim().ToUpperInvariant();
        var topic = request.Topic.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;

        var mailbox = await dbContext.AssistantMailboxes
            .FirstOrDefaultAsync(m => m.Country == country && m.Topic == topic, cancellationToken);

        var previous = mailbox is null ? null : AssistantMailboxSnapshot.From(mailbox);
        if (mailbox is null)
        {
            mailbox = new AssistantMailbox { Country = country, Topic = topic, Email = request.Email.Trim() };
            dbContext.AssistantMailboxes.Add(mailbox);
        }

        mailbox.Email = request.Email.Trim();
        mailbox.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        mailbox.IsActive = request.IsActive;

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.AssistantMailbox, mailbox.Id,
            previous is null ? MaintainerActions.Created : MaintainerActions.Updated,
            previous, AssistantMailboxSnapshot.From(mailbox), now);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<AssistantMailboxDto>.Success(KnowledgeArticleFields.ToDto(mailbox));
    }
}
