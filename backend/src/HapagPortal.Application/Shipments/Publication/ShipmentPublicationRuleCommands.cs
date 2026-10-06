namespace HapagPortal.Application.Shipments.Publication;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.Shipments;
using Microsoft.EntityFrameworkCore;

/// <summary>Regla de publicación por DIFU de destino final (M2-01).</summary>
public sealed record ShipmentPublicationRuleDto(
    Guid Id,
    string Country,
    string FinalDestinationCode,
    string? FinalDestinationName,
    string? DischargePortCode,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? ModifiedAt,
    string? ModifiedBy);

/// <summary>Instantánea de la regla para el registro de cambios (NF-15).</summary>
public sealed record ShipmentPublicationRuleSnapshot(
    string Country,
    string FinalDestinationCode,
    string? FinalDestinationName,
    string? DischargePortCode,
    string? Description,
    bool IsActive)
{
    public static ShipmentPublicationRuleSnapshot From(ShipmentPublicationRule rule) => new(
        rule.Country, rule.FinalDestinationCode, rule.FinalDestinationName, rule.DischargePortCode, rule.Description, rule.IsActive);
}

public sealed record ShipmentPublicationRuleChangeDto(
    Guid Id,
    Guid RuleId,
    string Action,
    DateTime ChangedAt,
    string ChangedBy,
    Guid? ChangedByUserId,
    ShipmentPublicationRuleSnapshot? Previous,
    ShipmentPublicationRuleSnapshot? Current);

public sealed record CreateShipmentPublicationRuleCommand(
    string Country,
    string FinalDestinationCode,
    string? FinalDestinationName,
    string? DischargePortCode,
    string? Description) : ICommand<ShipmentPublicationRuleDto>;

public sealed record UpdateShipmentPublicationRuleCommand(
    Guid Id,
    string Country,
    string FinalDestinationCode,
    string? FinalDestinationName,
    string? DischargePortCode,
    string? Description) : ICommand<ShipmentPublicationRuleDto>;

public sealed record DeactivateShipmentPublicationRuleCommand(Guid Id) : ICommand;

public sealed record GetShipmentPublicationRulesQuery(string? Country = null, bool IncludeInactive = false)
    : IQuery<IReadOnlyList<ShipmentPublicationRuleDto>>;

public sealed record GetShipmentPublicationRuleHistoryQuery(Guid Id) : IQuery<IReadOnlyList<ShipmentPublicationRuleChangeDto>>;

internal static class ShipmentPublicationRuleFields
{
    private const string LocodePattern = "^[A-Z]{2}[A-Z0-9]{3}$";

    public static void Apply<T>(AbstractValidator<T> validator, Func<T, ShipmentPublicationRuleSnapshot> fields)
    {
        validator.RuleFor(x => fields(x).Country)
            .Must(c => CountryCodes.ValidCountries.Contains(c))
            .WithName("Country")
            .WithMessage("Country must be CL or BO.");
        validator.RuleFor(x => fields(x).FinalDestinationCode)
            .Matches(LocodePattern)
            .WithName("FinalDestinationCode")
            .WithMessage("FinalDestinationCode must be a UN/LOCODE (e.g. CLANF).");
        validator.RuleFor(x => fields(x).DischargePortCode)
            .Matches(LocodePattern)
            .When(x => fields(x).DischargePortCode is not null)
            .WithName("DischargePortCode")
            .WithMessage("DischargePortCode must be a UN/LOCODE (e.g. CLSAI).");
        validator.RuleFor(x => fields(x))
            .Must(f => f.DischargePortCode != f.FinalDestinationCode)
            .WithName("DischargePortCode")
            .WithMessage("The discharge port must differ from the final destination: a final destination equal to the discharge port needs no DIFU.");
        validator.RuleFor(x => fields(x).FinalDestinationName).MaximumLength(100).WithName("FinalDestinationName");
        validator.RuleFor(x => fields(x).Description).MaximumLength(500).WithName("Description");
    }

    public static ShipmentPublicationRuleSnapshot Normalize(
        string country, string finalDestinationCode, string? finalDestinationName, string? dischargePortCode, string? description) =>
        new(
            (country ?? string.Empty).Trim().ToUpperInvariant(),
            ShipmentPublication.NormalizeCode(finalDestinationCode) ?? string.Empty,
            string.IsNullOrWhiteSpace(finalDestinationName) ? null : finalDestinationName.Trim(),
            ShipmentPublication.NormalizeCode(dischargePortCode),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            true);

    public static void Write(ShipmentPublicationRule rule, ShipmentPublicationRuleSnapshot fields)
    {
        rule.Country = fields.Country;
        rule.FinalDestinationCode = fields.FinalDestinationCode;
        rule.FinalDestinationName = fields.FinalDestinationName;
        rule.DischargePortCode = fields.DischargePortCode;
        rule.Description = fields.Description;
    }

    public static Task<bool> DuplicateExistsAsync(
        IApplicationDbContext dbContext, ShipmentPublicationRuleSnapshot fields, Guid? exceptId, CancellationToken cancellationToken) =>
        dbContext.ShipmentPublicationRules.AsNoTracking().AnyAsync(r =>
            r.IsActive
            && r.Id != exceptId
            && r.Country == fields.Country
            && r.FinalDestinationCode == fields.FinalDestinationCode
            && r.DischargePortCode == fields.DischargePortCode,
            cancellationToken);

    public static ShipmentPublicationRuleDto ToDto(ShipmentPublicationRule r) => new(
        r.Id, r.Country, r.FinalDestinationCode, r.FinalDestinationName, r.DischargePortCode, r.Description, r.IsActive,
        r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy);
}

public sealed class CreateShipmentPublicationRuleCommandValidator : AbstractValidator<CreateShipmentPublicationRuleCommand>
{
    public CreateShipmentPublicationRuleCommandValidator()
    {
        ShipmentPublicationRuleFields.Apply(this, x => ShipmentPublicationRuleFields.Normalize(
            x.Country, x.FinalDestinationCode, x.FinalDestinationName, x.DischargePortCode, x.Description));
    }
}

public sealed class UpdateShipmentPublicationRuleCommandValidator : AbstractValidator<UpdateShipmentPublicationRuleCommand>
{
    public UpdateShipmentPublicationRuleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        ShipmentPublicationRuleFields.Apply(this, x => ShipmentPublicationRuleFields.Normalize(
            x.Country, x.FinalDestinationCode, x.FinalDestinationName, x.DischargePortCode, x.Description));
    }
}

public sealed class CreateShipmentPublicationRuleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateShipmentPublicationRuleCommand, ShipmentPublicationRuleDto>
{
    public async Task<Result<ShipmentPublicationRuleDto>> Handle(CreateShipmentPublicationRuleCommand request, CancellationToken cancellationToken)
    {
        var fields = ShipmentPublicationRuleFields.Normalize(
            request.Country, request.FinalDestinationCode, request.FinalDestinationName, request.DischargePortCode, request.Description);

        if (await ShipmentPublicationRuleFields.DuplicateExistsAsync(dbContext, fields, null, cancellationToken))
            return Result<ShipmentPublicationRuleDto>.Failure(DomainErrors.ShipmentPublicationRule.AlreadyExists);

        var rule = new ShipmentPublicationRule { Country = fields.Country, FinalDestinationCode = fields.FinalDestinationCode };
        ShipmentPublicationRuleFields.Write(rule, fields);
        dbContext.ShipmentPublicationRules.Add(rule);

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.ShipmentPublicationRule, rule.Id, MaintainerActions.Created,
            null, ShipmentPublicationRuleSnapshot.From(rule), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ShipmentPublicationRuleDto>.Success(ShipmentPublicationRuleFields.ToDto(rule));
    }
}

public sealed class UpdateShipmentPublicationRuleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateShipmentPublicationRuleCommand, ShipmentPublicationRuleDto>
{
    public async Task<Result<ShipmentPublicationRuleDto>> Handle(UpdateShipmentPublicationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await dbContext.ShipmentPublicationRules
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.IsActive, cancellationToken);
        if (rule is null)
            return Result<ShipmentPublicationRuleDto>.Failure(DomainErrors.ShipmentPublicationRule.NotFound(request.Id));

        var fields = ShipmentPublicationRuleFields.Normalize(
            request.Country, request.FinalDestinationCode, request.FinalDestinationName, request.DischargePortCode, request.Description);

        if (await ShipmentPublicationRuleFields.DuplicateExistsAsync(dbContext, fields, rule.Id, cancellationToken))
            return Result<ShipmentPublicationRuleDto>.Failure(DomainErrors.ShipmentPublicationRule.AlreadyExists);

        var previous = ShipmentPublicationRuleSnapshot.From(rule);
        ShipmentPublicationRuleFields.Write(rule, fields);

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.ShipmentPublicationRule, rule.Id, MaintainerActions.Updated,
            previous, ShipmentPublicationRuleSnapshot.From(rule), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ShipmentPublicationRuleDto>.Success(ShipmentPublicationRuleFields.ToDto(rule));
    }
}

public sealed class DeactivateShipmentPublicationRuleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<DeactivateShipmentPublicationRuleCommand>
{
    public async Task<Result> Handle(DeactivateShipmentPublicationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await dbContext.ShipmentPublicationRules
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.IsActive, cancellationToken);
        if (rule is null)
            return Result.Failure(DomainErrors.ShipmentPublicationRule.NotFound(request.Id));

        var previous = ShipmentPublicationRuleSnapshot.From(rule);
        rule.IsActive = false;

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.ShipmentPublicationRule, rule.Id, MaintainerActions.Deactivated,
            previous, ShipmentPublicationRuleSnapshot.From(rule), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class GetShipmentPublicationRulesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetShipmentPublicationRulesQuery, IReadOnlyList<ShipmentPublicationRuleDto>>
{
    public async Task<Result<IReadOnlyList<ShipmentPublicationRuleDto>>> Handle(GetShipmentPublicationRulesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.ShipmentPublicationRules.AsNoTracking();

        if (!request.IncludeInactive)
            query = query.Where(r => r.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            var country = request.Country.Trim().ToUpperInvariant();
            query = query.Where(r => r.Country == country);
        }

        var rules = await query
            .OrderBy(r => r.Country)
            .ThenBy(r => r.FinalDestinationCode)
            .ThenBy(r => r.DischargePortCode)
            .ToListAsync(cancellationToken);

        IReadOnlyList<ShipmentPublicationRuleDto> items = rules.Select(ShipmentPublicationRuleFields.ToDto).ToList();
        return Result<IReadOnlyList<ShipmentPublicationRuleDto>>.Success(items);
    }
}

public sealed class GetShipmentPublicationRuleHistoryQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetShipmentPublicationRuleHistoryQuery, IReadOnlyList<ShipmentPublicationRuleChangeDto>>
{
    public async Task<Result<IReadOnlyList<ShipmentPublicationRuleChangeDto>>> Handle(GetShipmentPublicationRuleHistoryQuery request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.ShipmentPublicationRules.AsNoTracking().AnyAsync(r => r.Id == request.Id, cancellationToken);
        if (!exists)
            return Result<IReadOnlyList<ShipmentPublicationRuleChangeDto>>.Failure(DomainErrors.ShipmentPublicationRule.NotFound(request.Id));

        var changes = await dbContext.MaintainerChangeLogs.AsNoTracking()
            .Where(c => c.Maintainer == MaintainerNames.ShipmentPublicationRule && c.EntityId == request.Id)
            .OrderByDescending(c => c.ChangedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<ShipmentPublicationRuleChangeDto> items = changes
            .Select(c => new ShipmentPublicationRuleChangeDto(
                c.Id, c.EntityId, c.Action, c.ChangedAt, c.ChangedBy, c.ChangedByUserId,
                MaintainerChangeLogger.Read<ShipmentPublicationRuleSnapshot>(c.PreviousValue),
                MaintainerChangeLogger.Read<ShipmentPublicationRuleSnapshot>(c.NewValue)))
            .ToList();

        return Result<IReadOnlyList<ShipmentPublicationRuleChangeDto>>.Success(items);
    }
}
