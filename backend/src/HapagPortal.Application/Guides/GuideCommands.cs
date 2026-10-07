namespace HapagPortal.Application.Guides;

using System.Text.Json;
using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Paso de una guía (M1-27): pantalla (<see cref="Route"/>), elemento que se resalta (<see cref="ElementKey"/>, el
/// atributo <c>data-guide</c> de la interfaz) y la acción esperada en ES y EN.
/// </summary>
public sealed record GuideStepDto(
    int Order,
    string Route,
    string ElementKey,
    string TitleEs,
    string TitleEn,
    string TextEs,
    string TextEn);

/// <summary>Guía activa para el usuario, con su estado (M1-27). <c>Status</c> nulo = no la ha completado ni descartado.</summary>
public sealed record GuideDto(
    string Code,
    string NameEs,
    string NameEn,
    string? DescriptionEs,
    string? DescriptionEn,
    string Route,
    int Version,
    IReadOnlyList<GuideStepDto> Steps,
    string? Status,
    int? LastStep,
    DateTime? StatusAt);

/// <summary>Guía en el mantenedor interno.</summary>
public sealed record GuideDefinitionDto(
    Guid Id,
    string Code,
    string NameEs,
    string NameEn,
    string? DescriptionEs,
    string? DescriptionEn,
    string Route,
    string Audience,
    bool IsActive,
    int DisplayOrder,
    int Version,
    IReadOnlyList<GuideStepDto> Steps,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? ModifiedAt,
    string? ModifiedBy);

/// <summary>Instantánea de una guía para el registro de cambios (NF-15).</summary>
public sealed record GuideSnapshot(
    string Code,
    string NameEs,
    string NameEn,
    string Route,
    string Audience,
    bool IsActive,
    int DisplayOrder,
    int Version,
    IReadOnlyList<GuideStepDto> Steps)
{
    public static GuideSnapshot From(GuideDefinition g) =>
        new(g.Code, g.NameEs, g.NameEn, g.Route, g.Audience, g.IsActive, g.DisplayOrder, g.Version, GuideSteps.Parse(g.StepsJson));
}

/// <summary>Guías activas para el usuario actual (clientes o internos), opcionalmente las de una ruta (M1-27).</summary>
public sealed record GetGuidesQuery(string? Route = null) : IQuery<IReadOnlyList<GuideDto>>;

public sealed record GetGuideQuery(string Code) : IQuery<GuideDto>;

/// <summary>Completa, descarta o vuelve a ofrecer (<c>Reset</c>) una guía para el usuario actual.</summary>
public sealed record SetGuideStateCommand(string Code, string Status, int? LastStep = null) : ICommand<GuideDto>;

public sealed record GetGuideDefinitionsQuery : IQuery<IReadOnlyList<GuideDefinitionDto>>;

public sealed record CreateGuideCommand(
    string Code,
    string NameEs,
    string NameEn,
    string? DescriptionEs,
    string? DescriptionEn,
    string Route,
    string Audience,
    bool IsActive,
    int DisplayOrder,
    IReadOnlyList<GuideStepDto> Steps) : ICommand<GuideDefinitionDto>;

public sealed record UpdateGuideCommand(
    Guid Id,
    string NameEs,
    string NameEn,
    string? DescriptionEs,
    string? DescriptionEn,
    string Route,
    string Audience,
    bool IsActive,
    int DisplayOrder,
    IReadOnlyList<GuideStepDto> Steps) : ICommand<GuideDefinitionDto>;

public sealed record DeleteGuideCommand(Guid Id) : ICommand;

public sealed record GetGuideHistoryQuery(Guid Id) : IQuery<IReadOnlyList<PaymentMaintainerChangeDto<GuideSnapshot>>>;

public static class GuideSteps
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<GuideStepDto> Parse(string json) =>
        string.IsNullOrWhiteSpace(json)
            ? []
            : (JsonSerializer.Deserialize<List<GuideStepDto>>(json, JsonOptions) ?? []).OrderBy(s => s.Order).ToList();

    /// <summary>Pasos ordenados y numerados desde 1, en el orden indicado.</summary>
    public static string Serialize(IEnumerable<GuideStepDto> steps) =>
        JsonSerializer.Serialize(
            steps.OrderBy(s => s.Order).Select((s, i) => s with
            {
                Order = i + 1,
                Route = s.Route.Trim(),
                ElementKey = s.ElementKey.Trim(),
                TitleEs = s.TitleEs.Trim(),
                TitleEn = s.TitleEn.Trim(),
                TextEs = s.TextEs.Trim(),
                TextEn = s.TextEn.Trim()
            }).ToList(),
            JsonOptions);

    public static void Rules<T>(AbstractValidator<T> validator, Func<T, IReadOnlyList<GuideStepDto>> steps)
    {
        validator.RuleFor(x => steps(x)).NotNull().Must(s => s.Count is >= 1 and <= 30)
            .WithName("Steps").WithMessage("A guide has between 1 and 30 steps.");
        validator.RuleForEach(x => steps(x)).ChildRules(step =>
        {
            step.RuleFor(s => s.Route).NotEmpty().MaximumLength(200).Must(r => r.StartsWith('/')).WithMessage("Route must start with '/'.");
            step.RuleFor(s => s.ElementKey).NotEmpty().MaximumLength(100).Matches("^[a-z0-9][a-z0-9.\\-]*$")
                .WithMessage("ElementKey must use lowercase letters, digits, '.' or '-'.");
            step.RuleFor(s => s.TitleEs).NotEmpty().MaximumLength(150);
            step.RuleFor(s => s.TitleEn).NotEmpty().MaximumLength(150);
            step.RuleFor(s => s.TextEs).NotEmpty().MaximumLength(1000);
            step.RuleFor(s => s.TextEn).NotEmpty().MaximumLength(1000);
        }).WithName("Steps");
    }

    public static void Header<T>(
        AbstractValidator<T> validator,
        Func<T, string> nameEs,
        Func<T, string> nameEn,
        Func<T, string> route,
        Func<T, string> audience)
    {
        validator.RuleFor(x => nameEs(x)).NotEmpty().MaximumLength(150).WithName("NameEs");
        validator.RuleFor(x => nameEn(x)).NotEmpty().MaximumLength(150).WithName("NameEn");
        validator.RuleFor(x => route(x)).NotEmpty().MaximumLength(200).Must(r => r.StartsWith('/'))
            .WithName("Route").WithMessage("Route must start with '/'.");
        validator.RuleFor(x => audience(x)).Must(a => GuideAudiences.Values.Contains(a))
            .WithName("Audience").WithMessage("Audience must be Client, Internal or All.");
    }

    public static GuideDefinitionDto ToDefinitionDto(GuideDefinition g) => new(
        g.Id, g.Code, g.NameEs, g.NameEn, g.DescriptionEs, g.DescriptionEn, g.Route, g.Audience, g.IsActive, g.DisplayOrder, g.Version,
        Parse(g.StepsJson), g.CreatedAt, g.CreatedBy, g.ModifiedAt, g.ModifiedBy);

    /// <summary>Un estado de una versión anterior de la guía no cuenta: la guía cambió y se vuelve a ofrecer.</summary>
    public static GuideDto ToDto(GuideDefinition g, UserGuideState? state)
    {
        var current = state is not null && state.GuideVersion == g.Version ? state : null;
        return new GuideDto(
            g.Code, g.NameEs, g.NameEn, g.DescriptionEs, g.DescriptionEn, g.Route, g.Version, Parse(g.StepsJson),
            current?.Status, current?.LastStep, current?.UpdatedAt);
    }

    /// <summary>Audiencia del usuario: interno (organización Hapag-Lloyd o sin organización) o cliente.</summary>
    public static async Task<string> AudienceOfAsync(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        var organizationType = currentUserService.ClientId is { } clientId
            ? await dbContext.Clients.AsNoTracking().Where(c => c.Id == clientId).Select(c => c.OrganizationType).FirstOrDefaultAsync(cancellationToken)
            : null;

        return organizationType is null or OrganizationTypes.Internal ? GuideAudiences.Internal : GuideAudiences.Client;
    }
}

public sealed class SetGuideStateCommandValidator : AbstractValidator<SetGuideStateCommand>
{
    public SetGuideStateCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Status).Must(s => GuideStates.Settable.Contains(s)).WithMessage("Status must be Completed, Dismissed or Reset.");
        RuleFor(x => x.LastStep).GreaterThanOrEqualTo(1).When(x => x.LastStep is not null);
    }
}

public sealed class CreateGuideCommandValidator : AbstractValidator<CreateGuideCommand>
{
    public CreateGuideCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(60).Matches("^[a-z0-9][a-z0-9\\-]*$")
            .WithMessage("Code must use lowercase letters, digits or '-'.");
        GuideSteps.Header(this, x => x.NameEs, x => x.NameEn, x => x.Route, x => x.Audience);
        RuleFor(x => x.DescriptionEs).MaximumLength(500);
        RuleFor(x => x.DescriptionEn).MaximumLength(500);
        GuideSteps.Rules(this, x => x.Steps);
    }
}

public sealed class UpdateGuideCommandValidator : AbstractValidator<UpdateGuideCommand>
{
    public UpdateGuideCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        GuideSteps.Header(this, x => x.NameEs, x => x.NameEn, x => x.Route, x => x.Audience);
        RuleFor(x => x.DescriptionEs).MaximumLength(500);
        RuleFor(x => x.DescriptionEn).MaximumLength(500);
        GuideSteps.Rules(this, x => x.Steps);
    }
}

public sealed class GetGuidesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetGuidesQuery, IReadOnlyList<GuideDto>>
{
    public async Task<Result<IReadOnlyList<GuideDto>>> Handle(GetGuidesQuery request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
            return Result<IReadOnlyList<GuideDto>>.Failure(Error.Unauthorized);

        var audience = await GuideSteps.AudienceOfAsync(dbContext, currentUserService, cancellationToken);
        var query = dbContext.GuideDefinitions.AsNoTracking()
            .Where(g => g.IsActive && (g.Audience == GuideAudiences.All || g.Audience == audience));

        if (!string.IsNullOrWhiteSpace(request.Route))
        {
            var route = request.Route.Trim();
            query = query.Where(g => g.Route == route);
        }

        var guides = await query.OrderBy(g => g.DisplayOrder).ThenBy(g => g.Code).ToListAsync(cancellationToken);
        var codes = guides.Select(g => g.Code).ToList();
        var states = await dbContext.UserGuideStates.AsNoTracking()
            .Where(s => s.UserId == userId && codes.Contains(s.GuideCode))
            .ToListAsync(cancellationToken);

        IReadOnlyList<GuideDto> items = guides
            .Select(g => GuideSteps.ToDto(g, states.FirstOrDefault(s => s.GuideCode == g.Code)))
            .ToList();
        return Result<IReadOnlyList<GuideDto>>.Success(items);
    }
}

public sealed class GetGuideQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetGuideQuery, GuideDto>
{
    public async Task<Result<GuideDto>> Handle(GetGuideQuery request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
            return Result<GuideDto>.Failure(Error.Unauthorized);

        var guide = await GuideLoader.ActiveForUserAsync(dbContext, currentUserService, request.Code, cancellationToken);
        if (guide is null)
            return Result<GuideDto>.Failure(DomainErrors.Guide.NotFound(request.Code));

        var state = await dbContext.UserGuideStates.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.GuideCode == guide.Code, cancellationToken);
        return Result<GuideDto>.Success(GuideSteps.ToDto(guide, state));
    }
}

public sealed class SetGuideStateCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<SetGuideStateCommand, GuideDto>
{
    public async Task<Result<GuideDto>> Handle(SetGuideStateCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
            return Result<GuideDto>.Failure(Error.Unauthorized);

        var guide = await GuideLoader.ActiveForUserAsync(dbContext, currentUserService, request.Code, cancellationToken);
        if (guide is null)
            return Result<GuideDto>.Failure(DomainErrors.Guide.NotFound(request.Code));

        var state = await dbContext.UserGuideStates
            .FirstOrDefaultAsync(s => s.UserId == userId && s.GuideCode == guide.Code, cancellationToken);

        if (request.Status == GuideStates.Reset)
        {
            if (state is not null)
                dbContext.UserGuideStates.Remove(state);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<GuideDto>.Success(GuideSteps.ToDto(guide, null));
        }

        if (state is null)
        {
            state = new UserGuideState { UserId = userId, GuideCode = guide.Code, Status = request.Status };
            dbContext.UserGuideStates.Add(state);
        }

        state.Status = request.Status;
        state.GuideVersion = guide.Version;
        state.LastStep = request.LastStep;
        state.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<GuideDto>.Success(GuideSteps.ToDto(guide, state));
    }
}

internal static class GuideLoader
{
    public static async Task<GuideDefinition?> ActiveForUserAsync(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        string code,
        CancellationToken cancellationToken)
    {
        var audience = await GuideSteps.AudienceOfAsync(dbContext, currentUserService, cancellationToken);
        var normalized = code.Trim().ToLowerInvariant();
        return await dbContext.GuideDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Code == normalized && g.IsActive
                && (g.Audience == GuideAudiences.All || g.Audience == audience), cancellationToken);
    }
}

public sealed class GetGuideDefinitionsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetGuideDefinitionsQuery, IReadOnlyList<GuideDefinitionDto>>
{
    public async Task<Result<IReadOnlyList<GuideDefinitionDto>>> Handle(GetGuideDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var guides = await dbContext.GuideDefinitions.AsNoTracking()
            .OrderBy(g => g.DisplayOrder).ThenBy(g => g.Code)
            .ToListAsync(cancellationToken);
        IReadOnlyList<GuideDefinitionDto> items = guides.Select(GuideSteps.ToDefinitionDto).ToList();
        return Result<IReadOnlyList<GuideDefinitionDto>>.Success(items);
    }
}

public sealed class CreateGuideCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateGuideCommand, GuideDefinitionDto>
{
    public async Task<Result<GuideDefinitionDto>> Handle(CreateGuideCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        if (await dbContext.GuideDefinitions.AnyAsync(g => g.Code == code, cancellationToken))
            return Result<GuideDefinitionDto>.Failure(DomainErrors.Guide.AlreadyExists);

        var now = DateTime.UtcNow;
        var guide = new GuideDefinition
        {
            Code = code,
            NameEs = request.NameEs.Trim(),
            NameEn = request.NameEn.Trim(),
            DescriptionEs = string.IsNullOrWhiteSpace(request.DescriptionEs) ? null : request.DescriptionEs.Trim(),
            DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim(),
            Route = request.Route.Trim(),
            Audience = request.Audience,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
            Version = 1,
            StepsJson = GuideSteps.Serialize(request.Steps),
            CreatedAt = now,
            CreatedBy = PaymentActor.From(currentUserService).Name
        };

        dbContext.GuideDefinitions.Add(guide);
        MaintainerChangeLogger.Log(dbContext, currentUserService, MaintainerNames.Guide, guide.Id, MaintainerActions.Created,
            null, GuideSnapshot.From(guide), now);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<GuideDefinitionDto>.Success(GuideSteps.ToDefinitionDto(guide));
    }
}

public sealed class UpdateGuideCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateGuideCommand, GuideDefinitionDto>
{
    public async Task<Result<GuideDefinitionDto>> Handle(UpdateGuideCommand request, CancellationToken cancellationToken)
    {
        var guide = await dbContext.GuideDefinitions.FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);
        if (guide is null)
            return Result<GuideDefinitionDto>.Failure(DomainErrors.Guide.NotFound(request.Id.ToString()));

        var now = DateTime.UtcNow;
        var previous = GuideSnapshot.From(guide);
        var steps = GuideSteps.Serialize(request.Steps);

        // M1-27: si cambian los pasos, la guía se vuelve a ofrecer a quienes ya la habían completado o descartado.
        if (steps != guide.StepsJson)
            guide.Version++;

        guide.NameEs = request.NameEs.Trim();
        guide.NameEn = request.NameEn.Trim();
        guide.DescriptionEs = string.IsNullOrWhiteSpace(request.DescriptionEs) ? null : request.DescriptionEs.Trim();
        guide.DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim();
        guide.Route = request.Route.Trim();
        guide.Audience = request.Audience;
        guide.IsActive = request.IsActive;
        guide.DisplayOrder = request.DisplayOrder;
        guide.StepsJson = steps;
        guide.ModifiedAt = now;
        guide.ModifiedBy = PaymentActor.From(currentUserService).Name;

        MaintainerChangeLogger.Log(dbContext, currentUserService, MaintainerNames.Guide, guide.Id, MaintainerActions.Updated,
            previous, GuideSnapshot.From(guide), now);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<GuideDefinitionDto>.Success(GuideSteps.ToDefinitionDto(guide));
    }
}

public sealed class DeleteGuideCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<DeleteGuideCommand>
{
    public async Task<Result> Handle(DeleteGuideCommand request, CancellationToken cancellationToken)
    {
        var guide = await dbContext.GuideDefinitions.FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);
        if (guide is null)
            return Result.Failure(DomainErrors.Guide.NotFound(request.Id.ToString()));

        var now = DateTime.UtcNow;
        MaintainerChangeLogger.Log(dbContext, currentUserService, MaintainerNames.Guide, guide.Id, MaintainerActions.Deactivated,
            GuideSnapshot.From(guide), null, now);
        guide.DeletedAt = now;
        guide.DeletedBy = PaymentActor.From(currentUserService).Name;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class GetGuideHistoryQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetGuideHistoryQuery, IReadOnlyList<PaymentMaintainerChangeDto<GuideSnapshot>>>
{
    public async Task<Result<IReadOnlyList<PaymentMaintainerChangeDto<GuideSnapshot>>>> Handle(
        GetGuideHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var changes = await dbContext.MaintainerChangeLogs.AsNoTracking()
            .Where(c => c.Maintainer == MaintainerNames.Guide && c.EntityId == request.Id)
            .OrderByDescending(c => c.ChangedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PaymentMaintainerChangeDto<GuideSnapshot>> items = changes
            .Select(c => new PaymentMaintainerChangeDto<GuideSnapshot>(
                c.Id, c.EntityId, c.Action, c.ChangedAt, c.ChangedBy, c.ChangedByUserId,
                MaintainerChangeLogger.Read<GuideSnapshot>(c.PreviousValue),
                MaintainerChangeLogger.Read<GuideSnapshot>(c.NewValue)))
            .ToList();

        return Result<IReadOnlyList<PaymentMaintainerChangeDto<GuideSnapshot>>>.Success(items);
    }
}
