namespace HapagPortal.Application.AccountPayments;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Concepto imputable a la línea de crédito en un país (M5-10).</summary>
public sealed record CreditImputationRuleDto(
    Guid Id,
    string Country,
    string ConceptCode,
    string ConceptName,
    string NexusCreditConcept,
    bool IsEnabled,
    string? Notes,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? ModifiedAt,
    string? ModifiedBy);

/// <summary>Instantánea de una regla para el registro de cambios (NF-15).</summary>
public sealed record CreditImputationRuleSnapshot(string Country, string ConceptCode, string NexusCreditConcept, bool IsEnabled, string? Notes)
{
    public static CreditImputationRuleSnapshot From(CreditImputationRule rule) =>
        new(rule.Country, rule.ConceptCode, rule.NexusCreditConcept, rule.IsEnabled, rule.Notes);
}

public sealed record GetCreditImputationRulesQuery(string? Country = null, bool IncludeDisabled = true)
    : IQuery<IReadOnlyList<CreditImputationRuleDto>>;

public sealed record CreateCreditImputationRuleCommand(
    string Country,
    string ConceptCode,
    string NexusCreditConcept,
    bool IsEnabled,
    string? Notes) : ICommand<CreditImputationRuleDto>;

public sealed record UpdateCreditImputationRuleCommand(
    Guid Id,
    string NexusCreditConcept,
    bool IsEnabled,
    string? Notes) : ICommand<CreditImputationRuleDto>;

/// <summary>Quita la regla (borrado lógico, con su historial).</summary>
public sealed record DeleteCreditImputationRuleCommand(Guid Id) : ICommand;

public sealed record GetCreditImputationRuleHistoryQuery(Guid Id)
    : IQuery<IReadOnlyList<PaymentMaintainerChangeDto<CreditImputationRuleSnapshot>>>;

public sealed class CreateCreditImputationRuleCommandValidator : AbstractValidator<CreateCreditImputationRuleCommand>
{
    public CreateCreditImputationRuleCommandValidator()
    {
        RuleFor(x => x.Country)
            .Must(c => CountryCodes.ValidCountries.Contains(c?.Trim().ToUpperInvariant()))
            .WithMessage("Country must be CL or BO.");
        RuleFor(x => x.ConceptCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.NexusCreditConcept)
            .Must(c => CreditCoverageConcepts.All.Contains(c?.Trim().ToUpperInvariant()))
            .WithMessage("NexusCreditConcept must be LOCAL_CHARGES, MHD, FREIGHT or STORAGE.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class UpdateCreditImputationRuleCommandValidator : AbstractValidator<UpdateCreditImputationRuleCommand>
{
    public UpdateCreditImputationRuleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NexusCreditConcept)
            .Must(c => CreditCoverageConcepts.All.Contains(c?.Trim().ToUpperInvariant()))
            .WithMessage("NexusCreditConcept must be LOCAL_CHARGES, MHD, FREIGHT or STORAGE.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

/// <summary>
/// Elegibilidad de un ítem para imputarlo a la línea de crédito (M5-10): solo recargos locales (los demás tipos
/// quedan fuera hasta que Finanzas los confirme), con una regla habilitada para su concepto en el país y con el
/// concepto de Nexus de la regla incluido en la condición de crédito vigente del cliente (M8-02).
/// </summary>
public sealed class CreditImputationEligibility
{
    private readonly IReadOnlyDictionary<(string Country, string Concept), CreditImputationRule> _rules;

    private CreditImputationEligibility(IReadOnlyDictionary<(string, string), CreditImputationRule> rules) => _rules = rules;

    public static async Task<CreditImputationEligibility> LoadAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var rules = await dbContext.CreditImputationRules.AsNoTracking()
            .Where(r => r.IsEnabled)
            .ToListAsync(cancellationToken);

        return new CreditImputationEligibility(rules
            .GroupBy(r => (r.Country, r.ConceptCode))
            .ToDictionary(g => g.Key, g => g.First()));
    }

    public bool IsEligible(CommercialConditionsDto conditions, string itemType, string country, string conceptCode) =>
        conditions.Available
        && conditions.HasCredit
        && itemType == PayableItemTypes.LocalCharge
        && _rules.TryGetValue((country, conceptCode), out var rule)
        && conditions.CreditConcepts.Contains(rule.NexusCreditConcept, StringComparer.OrdinalIgnoreCase);
}

internal static class CreditImputationRuleViews
{
    public static async Task<IReadOnlyDictionary<string, string>> ConceptNamesAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        await dbContext.ChargeConcepts.AsNoTracking().ToDictionaryAsync(c => c.Code, c => c.Name, cancellationToken);

    public static CreditImputationRuleDto ToDto(CreditImputationRule r, IReadOnlyDictionary<string, string> names) => new(
        r.Id, r.Country, r.ConceptCode, names.GetValueOrDefault(r.ConceptCode) ?? r.ConceptCode, r.NexusCreditConcept, r.IsEnabled,
        r.Notes, r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy);
}

public sealed class GetCreditImputationRulesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetCreditImputationRulesQuery, IReadOnlyList<CreditImputationRuleDto>>
{
    public async Task<Result<IReadOnlyList<CreditImputationRuleDto>>> Handle(GetCreditImputationRulesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.CreditImputationRules.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            var country = request.Country.Trim().ToUpperInvariant();
            query = query.Where(r => r.Country == country);
        }

        if (!request.IncludeDisabled)
            query = query.Where(r => r.IsEnabled);

        var rules = await query.OrderBy(r => r.Country).ThenBy(r => r.ConceptCode).ToListAsync(cancellationToken);
        var names = await CreditImputationRuleViews.ConceptNamesAsync(dbContext, cancellationToken);

        IReadOnlyList<CreditImputationRuleDto> items = rules.Select(r => CreditImputationRuleViews.ToDto(r, names)).ToList();
        return Result<IReadOnlyList<CreditImputationRuleDto>>.Success(items);
    }
}

public sealed class CreateCreditImputationRuleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateCreditImputationRuleCommand, CreditImputationRuleDto>
{
    public async Task<Result<CreditImputationRuleDto>> Handle(CreateCreditImputationRuleCommand request, CancellationToken cancellationToken)
    {
        var country = request.Country.Trim().ToUpperInvariant();
        var concept = request.ConceptCode.Trim().ToUpperInvariant();

        var names = await CreditImputationRuleViews.ConceptNamesAsync(dbContext, cancellationToken);
        if (!names.ContainsKey(concept))
            return Result<CreditImputationRuleDto>.Failure(DomainErrors.ChargeConcept.NotFound(concept));

        if (await dbContext.CreditImputationRules.AnyAsync(r => r.Country == country && r.ConceptCode == concept, cancellationToken))
            return Result<CreditImputationRuleDto>.Failure(DomainErrors.CreditImputationRule.AlreadyExists);

        var now = DateTime.UtcNow;
        var rule = new CreditImputationRule
        {
            Country = country,
            ConceptCode = concept,
            NexusCreditConcept = request.NexusCreditConcept.Trim().ToUpperInvariant(),
            IsEnabled = request.IsEnabled,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = now,
            CreatedBy = PaymentActor.From(currentUserService).Name
        };

        dbContext.CreditImputationRules.Add(rule);
        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.CreditImputationRule, rule.Id, MaintainerActions.Created,
            null, CreditImputationRuleSnapshot.From(rule), now);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreditImputationRuleDto>.Success(CreditImputationRuleViews.ToDto(rule, names));
    }
}

public sealed class UpdateCreditImputationRuleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateCreditImputationRuleCommand, CreditImputationRuleDto>
{
    public async Task<Result<CreditImputationRuleDto>> Handle(UpdateCreditImputationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await dbContext.CreditImputationRules.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (rule is null)
            return Result<CreditImputationRuleDto>.Failure(DomainErrors.CreditImputationRule.NotFound(request.Id));

        var now = DateTime.UtcNow;
        var previous = CreditImputationRuleSnapshot.From(rule);
        rule.NexusCreditConcept = request.NexusCreditConcept.Trim().ToUpperInvariant();
        rule.IsEnabled = request.IsEnabled;
        rule.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        rule.ModifiedAt = now;
        rule.ModifiedBy = PaymentActor.From(currentUserService).Name;

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.CreditImputationRule, rule.Id, MaintainerActions.Updated,
            previous, CreditImputationRuleSnapshot.From(rule), now);
        await dbContext.SaveChangesAsync(cancellationToken);

        var names = await CreditImputationRuleViews.ConceptNamesAsync(dbContext, cancellationToken);
        return Result<CreditImputationRuleDto>.Success(CreditImputationRuleViews.ToDto(rule, names));
    }
}

public sealed class DeleteCreditImputationRuleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<DeleteCreditImputationRuleCommand>
{
    public async Task<Result> Handle(DeleteCreditImputationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await dbContext.CreditImputationRules.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (rule is null)
            return Result.Failure(DomainErrors.CreditImputationRule.NotFound(request.Id));

        var now = DateTime.UtcNow;
        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.CreditImputationRule, rule.Id, MaintainerActions.Deactivated,
            CreditImputationRuleSnapshot.From(rule), null, now);
        rule.DeletedAt = now;
        rule.DeletedBy = PaymentActor.From(currentUserService).Name;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class GetCreditImputationRuleHistoryQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetCreditImputationRuleHistoryQuery, IReadOnlyList<PaymentMaintainerChangeDto<CreditImputationRuleSnapshot>>>
{
    public async Task<Result<IReadOnlyList<PaymentMaintainerChangeDto<CreditImputationRuleSnapshot>>>> Handle(
        GetCreditImputationRuleHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var changes = await dbContext.MaintainerChangeLogs.AsNoTracking()
            .Where(c => c.Maintainer == MaintainerNames.CreditImputationRule && c.EntityId == request.Id)
            .OrderByDescending(c => c.ChangedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PaymentMaintainerChangeDto<CreditImputationRuleSnapshot>> items = changes
            .Select(c => new PaymentMaintainerChangeDto<CreditImputationRuleSnapshot>(
                c.Id, c.EntityId, c.Action, c.ChangedAt, c.ChangedBy, c.ChangedByUserId,
                MaintainerChangeLogger.Read<CreditImputationRuleSnapshot>(c.PreviousValue),
                MaintainerChangeLogger.Read<CreditImputationRuleSnapshot>(c.NewValue)))
            .ToList();

        return Result<IReadOnlyList<PaymentMaintainerChangeDto<CreditImputationRuleSnapshot>>>.Success(items);
    }
}
