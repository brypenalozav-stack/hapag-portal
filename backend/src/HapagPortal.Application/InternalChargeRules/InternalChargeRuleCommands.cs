namespace HapagPortal.Application.InternalChargeRules;

using FluentValidation;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Regla interna del portal (M3-04 cambio de almacén gratuito, M3-16 demoras anticipadas).</summary>
public sealed record InternalChargeRuleDto(
    Guid Id,
    string RuleType,
    string Country,
    string? TaxId,
    string? MatchCode,
    string? AccountName,
    string? Reason,
    int? MaxUsesPerBl,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

/// <summary>Instantánea de la regla para el registro de cambios (NF-15).</summary>
public sealed record InternalChargeRuleSnapshot(
    string RuleType,
    string Country,
    string? TaxId,
    string? MatchCode,
    string? AccountName,
    string? Reason,
    int? MaxUsesPerBl,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsActive)
{
    public static InternalChargeRuleSnapshot From(InternalChargeRule rule) => new(
        rule.RuleType, rule.Country, rule.TaxId, rule.MatchCode, rule.AccountName, rule.Reason,
        rule.MaxUsesPerBl, rule.ValidFrom, rule.ValidTo, rule.IsActive);
}

public sealed record InternalChargeRuleChangeDto(
    Guid Id,
    Guid RuleId,
    string Action,
    DateTime ChangedAt,
    string ChangedBy,
    Guid? ChangedByUserId,
    InternalChargeRuleSnapshot? Previous,
    InternalChargeRuleSnapshot? Current);

public sealed record CreateInternalChargeRuleCommand(
    string RuleType,
    string Country,
    string? TaxId,
    string? MatchCode,
    string? AccountName,
    string? Reason,
    int? MaxUsesPerBl,
    DateOnly ValidFrom,
    DateOnly? ValidTo) : ICommand<InternalChargeRuleDto>;

public sealed record UpdateInternalChargeRuleCommand(
    Guid Id,
    string RuleType,
    string Country,
    string? TaxId,
    string? MatchCode,
    string? AccountName,
    string? Reason,
    int? MaxUsesPerBl,
    DateOnly ValidFrom,
    DateOnly? ValidTo) : ICommand<InternalChargeRuleDto>;

public sealed record DeactivateInternalChargeRuleCommand(Guid Id) : ICommand;

public sealed record GetInternalChargeRulesQuery(string? RuleType = null, string? Country = null, bool IncludeInactive = false)
    : IQuery<IReadOnlyList<InternalChargeRuleDto>>;

public sealed record GetInternalChargeRuleHistoryQuery(Guid Id) : IQuery<IReadOnlyList<InternalChargeRuleChangeDto>>;

/// <summary>
/// Búsqueda de la regla interna vigente aplicable a una cuenta, por RUT/NIT normalizado o Match Code,
/// en la fecha local del país (NF-22).
/// </summary>
public static class InternalChargeRuleMatcher
{
    public static async Task<InternalChargeRule?> FindAsync(
        IApplicationDbContext dbContext,
        string ruleType,
        string country,
        Client account,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var rules = await dbContext.InternalChargeRules.AsNoTracking()
            .Where(r => r.IsActive && r.RuleType == ruleType && r.Country == country)
            .ToListAsync(cancellationToken);

        return rules
            .Where(r => TariffCalculator.IsInForce(r.ValidFrom, r.ValidTo, today))
            .Where(r => TaxIdNormalizer.AreEqual(r.TaxId, account.TaxId)
                || (!string.IsNullOrWhiteSpace(r.MatchCode)
                    && string.Equals(r.MatchCode, account.MatchCode, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefault();
    }
}

internal static class InternalChargeRuleFields
{
    public static void Apply<T>(AbstractValidator<T> validator, Func<T, InternalChargeRuleSnapshot> fields)
    {
        validator.RuleFor(x => fields(x).RuleType)
            .Must(t => InternalChargeRuleTypes.All.Contains(t))
            .WithName("RuleType")
            .WithMessage("RuleType must be FreeWarehouseChange or AdvanceDemurrageRequired.");
        validator.RuleFor(x => fields(x).Country)
            .Must(c => CountryCodes.ValidCountries.Contains(c))
            .WithName("Country")
            .WithMessage("Country must be CL or BO.");
        validator.RuleFor(x => fields(x))
            .Must(f => !string.IsNullOrWhiteSpace(f.TaxId) || !string.IsNullOrWhiteSpace(f.MatchCode))
            .WithName("TaxId")
            .WithMessage("The rule must identify the account by tax ID or Match Code.");
        validator.RuleFor(x => fields(x).TaxId).MaximumLength(20).WithName("TaxId");
        validator.RuleFor(x => fields(x).MatchCode).MaximumLength(20).WithName("MatchCode");
        validator.RuleFor(x => fields(x).AccountName).MaximumLength(200).WithName("AccountName");
        validator.RuleFor(x => fields(x).Reason).MaximumLength(500).WithName("Reason");
        validator.RuleFor(x => fields(x).MaxUsesPerBl).GreaterThan(0).When(x => fields(x).MaxUsesPerBl is not null).WithName("MaxUsesPerBl");
        validator.RuleFor(x => fields(x))
            .Must(f => f.ValidTo is null || f.ValidTo >= f.ValidFrom)
            .WithName("ValidTo")
            .WithMessage("ValidTo must be on or after ValidFrom.");
    }

    public static InternalChargeRuleSnapshot Normalize(
        string ruleType, string country, string? taxId, string? matchCode, string? accountName, string? reason,
        int? maxUsesPerBl, DateOnly validFrom, DateOnly? validTo) =>
        new(
            (ruleType ?? string.Empty).Trim(),
            (country ?? string.Empty).Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(taxId) ? null : TaxIdNormalizer.Normalize(taxId),
            string.IsNullOrWhiteSpace(matchCode) ? null : matchCode.Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(accountName) ? null : accountName.Trim(),
            string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            maxUsesPerBl,
            validFrom,
            validTo,
            true);

    public static void Write(InternalChargeRule rule, InternalChargeRuleSnapshot fields)
    {
        rule.RuleType = fields.RuleType;
        rule.Country = fields.Country;
        rule.TaxId = fields.TaxId;
        rule.MatchCode = fields.MatchCode;
        rule.AccountName = fields.AccountName;
        rule.Reason = fields.Reason;
        rule.MaxUsesPerBl = fields.MaxUsesPerBl;
        rule.ValidFrom = fields.ValidFrom;
        rule.ValidTo = fields.ValidTo;
    }

    public static InternalChargeRuleDto ToDto(InternalChargeRule r) => new(
        r.Id, r.RuleType, r.Country, r.TaxId, r.MatchCode, r.AccountName, r.Reason, r.MaxUsesPerBl,
        r.ValidFrom, r.ValidTo, r.IsActive, r.CreatedAt, r.ModifiedAt);
}

public sealed class CreateInternalChargeRuleCommandValidator : AbstractValidator<CreateInternalChargeRuleCommand>
{
    public CreateInternalChargeRuleCommandValidator()
    {
        InternalChargeRuleFields.Apply(this, x => InternalChargeRuleFields.Normalize(
            x.RuleType, x.Country, x.TaxId, x.MatchCode, x.AccountName, x.Reason, x.MaxUsesPerBl, x.ValidFrom, x.ValidTo));
    }
}

public sealed class UpdateInternalChargeRuleCommandValidator : AbstractValidator<UpdateInternalChargeRuleCommand>
{
    public UpdateInternalChargeRuleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        InternalChargeRuleFields.Apply(this, x => InternalChargeRuleFields.Normalize(
            x.RuleType, x.Country, x.TaxId, x.MatchCode, x.AccountName, x.Reason, x.MaxUsesPerBl, x.ValidFrom, x.ValidTo));
    }
}

public sealed class CreateInternalChargeRuleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateInternalChargeRuleCommand, InternalChargeRuleDto>
{
    public async Task<Result<InternalChargeRuleDto>> Handle(CreateInternalChargeRuleCommand request, CancellationToken cancellationToken)
    {
        var fields = InternalChargeRuleFields.Normalize(
            request.RuleType, request.Country, request.TaxId, request.MatchCode, request.AccountName, request.Reason,
            request.MaxUsesPerBl, request.ValidFrom, request.ValidTo);

        var rule = new InternalChargeRule { RuleType = fields.RuleType, Country = fields.Country };
        InternalChargeRuleFields.Write(rule, fields);
        dbContext.InternalChargeRules.Add(rule);

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.InternalChargeRule, rule.Id, MaintainerActions.Created,
            null, InternalChargeRuleSnapshot.From(rule), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<InternalChargeRuleDto>.Success(InternalChargeRuleFields.ToDto(rule));
    }
}

public sealed class UpdateInternalChargeRuleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateInternalChargeRuleCommand, InternalChargeRuleDto>
{
    public async Task<Result<InternalChargeRuleDto>> Handle(UpdateInternalChargeRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await dbContext.InternalChargeRules
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.IsActive, cancellationToken);
        if (rule is null)
            return Result<InternalChargeRuleDto>.Failure(DomainErrors.InternalChargeRule.NotFound(request.Id));

        var previous = InternalChargeRuleSnapshot.From(rule);
        InternalChargeRuleFields.Write(rule, InternalChargeRuleFields.Normalize(
            request.RuleType, request.Country, request.TaxId, request.MatchCode, request.AccountName, request.Reason,
            request.MaxUsesPerBl, request.ValidFrom, request.ValidTo));

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.InternalChargeRule, rule.Id, MaintainerActions.Updated,
            previous, InternalChargeRuleSnapshot.From(rule), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<InternalChargeRuleDto>.Success(InternalChargeRuleFields.ToDto(rule));
    }
}

public sealed class DeactivateInternalChargeRuleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<DeactivateInternalChargeRuleCommand>
{
    public async Task<Result> Handle(DeactivateInternalChargeRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await dbContext.InternalChargeRules
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.IsActive, cancellationToken);
        if (rule is null)
            return Result.Failure(DomainErrors.InternalChargeRule.NotFound(request.Id));

        var previous = InternalChargeRuleSnapshot.From(rule);
        rule.IsActive = false;

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.InternalChargeRule, rule.Id, MaintainerActions.Deactivated,
            previous, InternalChargeRuleSnapshot.From(rule), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class GetInternalChargeRulesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetInternalChargeRulesQuery, IReadOnlyList<InternalChargeRuleDto>>
{
    public async Task<Result<IReadOnlyList<InternalChargeRuleDto>>> Handle(GetInternalChargeRulesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.InternalChargeRules.AsNoTracking();

        if (!request.IncludeInactive)
            query = query.Where(r => r.IsActive);
        if (!string.IsNullOrWhiteSpace(request.RuleType))
            query = query.Where(r => r.RuleType == request.RuleType);
        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            var country = request.Country.Trim().ToUpperInvariant();
            query = query.Where(r => r.Country == country);
        }

        var rules = await query
            .OrderBy(r => r.RuleType)
            .ThenBy(r => r.Country)
            .ThenBy(r => r.AccountName)
            .ToListAsync(cancellationToken);

        IReadOnlyList<InternalChargeRuleDto> items = rules.Select(InternalChargeRuleFields.ToDto).ToList();
        return Result<IReadOnlyList<InternalChargeRuleDto>>.Success(items);
    }
}

public sealed class GetInternalChargeRuleHistoryQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetInternalChargeRuleHistoryQuery, IReadOnlyList<InternalChargeRuleChangeDto>>
{
    public async Task<Result<IReadOnlyList<InternalChargeRuleChangeDto>>> Handle(GetInternalChargeRuleHistoryQuery request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.InternalChargeRules.AsNoTracking().AnyAsync(r => r.Id == request.Id, cancellationToken);
        if (!exists)
            return Result<IReadOnlyList<InternalChargeRuleChangeDto>>.Failure(DomainErrors.InternalChargeRule.NotFound(request.Id));

        var changes = await dbContext.MaintainerChangeLogs.AsNoTracking()
            .Where(c => c.Maintainer == MaintainerNames.InternalChargeRule && c.EntityId == request.Id)
            .OrderByDescending(c => c.ChangedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<InternalChargeRuleChangeDto> items = changes
            .Select(c => new InternalChargeRuleChangeDto(
                c.Id, c.EntityId, c.Action, c.ChangedAt, c.ChangedBy, c.ChangedByUserId,
                MaintainerChangeLogger.Read<InternalChargeRuleSnapshot>(c.PreviousValue),
                MaintainerChangeLogger.Read<InternalChargeRuleSnapshot>(c.NewValue)))
            .ToList();

        return Result<IReadOnlyList<InternalChargeRuleChangeDto>>.Success(items);
    }
}
