namespace HapagPortal.Application.ServiceRequests.Definitions;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.ServiceRequests;
using Microsoft.EntityFrameworkCore;

/// <summary>Campos editables de una definición de servicio on demand (M2-03, M2-04).</summary>
public sealed record ServiceDefinitionInput(
    string Code,
    string NameEs,
    string NameEn,
    string? DescriptionEs,
    string? DescriptionEn,
    IReadOnlyList<string> Operations,
    IReadOnlyList<string> Countries,
    string ReferenceType,
    IReadOnlyList<string>? RequiredBlStatuses,
    string AvailabilityWindow,
    bool RequiresContainers,
    bool AllowMultiplePerBl,
    IReadOnlyList<ServiceInputField>? InputSchema,
    bool BillingDataRequired,
    bool TariffAcceptanceRequired,
    string PricingMode,
    string? ChargeConceptCode,
    string? TariffCode,
    string? LateTariffCode,
    string QuantityMode,
    string? MeasureFieldKey,
    string Milestone,
    int MilestoneOffsetHours,
    string? DeadlineRuleCode,
    string TimingRule,
    bool Taxable,
    string? ExemptionConcept,
    bool ExcludeShipperOwnedContainers,
    string ApprovalTeam,
    string FulfillmentTeam,
    bool RequiresOutputDocument,
    string ActionCode,
    int DisplayOrder,
    bool IsActive = true);

public sealed record CreateServiceDefinitionCommand(ServiceDefinitionInput Input) : ICommand<ServiceDefinitionDto>;

public sealed record UpdateServiceDefinitionCommand(Guid Id, ServiceDefinitionInput Input) : ICommand<ServiceDefinitionDto>;

public sealed record DeactivateServiceDefinitionCommand(Guid Id) : ICommand;

public sealed record GetServiceDefinitionsQuery(bool IncludeInactive = false, string? Country = null, string? Operation = null)
    : IQuery<IReadOnlyList<ServiceDefinitionDto>>;

public sealed record GetServiceDefinitionQuery(Guid Id) : IQuery<ServiceDefinitionDto>;

public sealed record GetServiceDefinitionHistoryQuery(Guid Id) : IQuery<IReadOnlyList<ServiceDefinitionChangeDto>>;

public static class ServiceDefinitionMapper
{
    public static ServiceDefinitionDto ToDto(ServiceDefinition d) => new(
        d.Id, d.Code, d.NameEs, d.NameEn, d.DescriptionEs, d.DescriptionEn,
        ServiceCatalogEvaluator.Csv(d.Operations), ServiceCatalogEvaluator.Csv(d.Countries), d.ReferenceType,
        ServiceCatalogEvaluator.Csv(d.RequiredBlStatuses), d.AvailabilityWindow, d.RequiresContainers, d.AllowMultiplePerBl,
        ServiceInputSchema.Parse(d.InputSchemaJson) ?? [], d.BillingDataRequired, d.TariffAcceptanceRequired,
        d.PricingMode, d.ChargeConceptCode, d.TariffCode, d.LateTariffCode, d.QuantityMode, d.MeasureFieldKey, d.Milestone,
        d.MilestoneOffsetHours, d.DeadlineRuleCode, d.TimingRule, d.Taxable, d.ExemptionConcept, d.ExcludeShipperOwnedContainers,
        d.ApprovalTeam, d.FulfillmentTeam, d.RequiresOutputDocument, d.ActionCode, d.DisplayOrder, d.IsActive,
        d.CreatedAt, d.CreatedBy, d.ModifiedAt, d.ModifiedBy);

    public static ServiceDefinitionSnapshot Snapshot(ServiceDefinition d) => new(
        d.Code, d.NameEs, d.NameEn, d.DescriptionEs, d.DescriptionEn, d.Operations, d.Countries, d.ReferenceType,
        d.RequiredBlStatuses, d.AvailabilityWindow, d.RequiresContainers, d.AllowMultiplePerBl, d.InputSchemaJson,
        d.BillingDataRequired, d.TariffAcceptanceRequired, d.PricingMode, d.ChargeConceptCode, d.TariffCode, d.LateTariffCode,
        d.QuantityMode, d.MeasureFieldKey, d.Milestone, d.MilestoneOffsetHours, d.DeadlineRuleCode, d.TimingRule, d.Taxable,
        d.ExemptionConcept, d.ExcludeShipperOwnedContainers, d.ApprovalTeam, d.FulfillmentTeam, d.RequiresOutputDocument,
        d.ActionCode, d.DisplayOrder, d.IsActive);

    /// <summary>Copia los campos normalizados (códigos en mayúsculas, listas separadas por coma).</summary>
    public static void Write(ServiceDefinition d, ServiceDefinitionInput input)
    {
        d.Code = input.Code.Trim().ToUpperInvariant();
        d.NameEs = input.NameEs.Trim();
        d.NameEn = input.NameEn.Trim();
        d.DescriptionEs = Optional(input.DescriptionEs);
        d.DescriptionEn = Optional(input.DescriptionEn);
        d.Operations = Join(input.Operations);
        d.Countries = Join(input.Countries);
        d.ReferenceType = input.ReferenceType;
        d.RequiredBlStatuses = input.RequiredBlStatuses is { Count: > 0 } statuses ? Join(statuses) : null;
        d.AvailabilityWindow = input.AvailabilityWindow;
        d.RequiresContainers = input.RequiresContainers;
        d.AllowMultiplePerBl = input.AllowMultiplePerBl;
        d.InputSchemaJson = ServiceInputSchema.Serialize(input.InputSchema ?? []);
        d.BillingDataRequired = input.BillingDataRequired;
        d.TariffAcceptanceRequired = input.TariffAcceptanceRequired;
        d.PricingMode = input.PricingMode;
        d.ChargeConceptCode = Code(input.ChargeConceptCode);
        d.TariffCode = Code(input.TariffCode);
        d.LateTariffCode = Code(input.LateTariffCode);
        d.QuantityMode = input.QuantityMode;
        d.MeasureFieldKey = Optional(input.MeasureFieldKey);
        d.Milestone = input.Milestone;
        d.MilestoneOffsetHours = input.MilestoneOffsetHours;
        d.DeadlineRuleCode = Code(input.DeadlineRuleCode);
        d.TimingRule = input.TimingRule;
        d.Taxable = input.Taxable;
        d.ExemptionConcept = Code(input.ExemptionConcept);
        d.ExcludeShipperOwnedContainers = input.ExcludeShipperOwnedContainers;
        d.ApprovalTeam = input.ApprovalTeam;
        d.FulfillmentTeam = input.FulfillmentTeam;
        d.RequiresOutputDocument = input.RequiresOutputDocument;
        d.ActionCode = input.ActionCode.Trim();
        d.DisplayOrder = input.DisplayOrder;
        d.IsActive = input.IsActive;
    }

    private static string Join(IEnumerable<string> values) =>
        string.Join(',', values.Select(v => v.Trim().ToUpperInvariant()).Where(v => v.Length > 0).Distinct(StringComparer.Ordinal));

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Code(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}

internal static class ServiceDefinitionRules
{
    public static void Apply<T>(AbstractValidator<T> validator, Func<T, ServiceDefinitionInput> input)
    {
        validator.RuleFor(x => input(x).Code).NotEmpty().Matches("^[A-Za-z][A-Za-z0-9_]{1,49}$").WithName("Code")
            .WithMessage("Code must start with a letter and contain only letters, digits and underscores (max 50).");
        validator.RuleFor(x => input(x).NameEs).NotEmpty().MaximumLength(150).WithName("NameEs");
        validator.RuleFor(x => input(x).NameEn).NotEmpty().MaximumLength(150).WithName("NameEn");
        validator.RuleFor(x => input(x).DescriptionEs).MaximumLength(1000).WithName("DescriptionEs");
        validator.RuleFor(x => input(x).DescriptionEn).MaximumLength(1000).WithName("DescriptionEn");
        validator.RuleFor(x => input(x).Operations)
            .Must(o => o is { Count: > 0 } && o.All(v => ServiceOperations.All.Contains(v.Trim().ToUpperInvariant())))
            .WithName("Operations").WithMessage("Operations must be IMPORT and/or EXPORT.");
        validator.RuleFor(x => input(x).Countries)
            .Must(c => c is { Count: > 0 } && c.All(v => CountryCodes.ValidCountries.Contains(v.Trim().ToUpperInvariant())))
            .WithName("Countries").WithMessage("Countries must be CL and/or BO.");
        validator.RuleFor(x => input(x).RequiredBlStatuses)
            .Must(s => s is null || s.All(v => !string.IsNullOrWhiteSpace(v) && v.Length <= 30))
            .WithName("RequiredBlStatuses");
        Enum(validator, x => input(x).ReferenceType, ServiceReferenceTypes.All, "ReferenceType");
        Enum(validator, x => input(x).AvailabilityWindow, ServiceAvailabilityWindows.All, "AvailabilityWindow");
        Enum(validator, x => input(x).PricingMode, ServicePricingModes.All, "PricingMode");
        Enum(validator, x => input(x).QuantityMode, ServiceQuantityModes.All, "QuantityMode");
        Enum(validator, x => input(x).Milestone, ServiceMilestones.All, "Milestone");
        Enum(validator, x => input(x).TimingRule, ServiceTimingRules.All, "TimingRule");
        Enum(validator, x => input(x).ApprovalTeam, ServiceTeams.All, "ApprovalTeam");
        Enum(validator, x => input(x).FulfillmentTeam, ServiceTeams.All, "FulfillmentTeam");
        validator.RuleFor(x => input(x).ActionCode).NotEmpty().MaximumLength(60).WithName("ActionCode");
        validator.RuleFor(x => input(x).ChargeConceptCode).MaximumLength(50).WithName("ChargeConceptCode");
        validator.RuleFor(x => input(x).TariffCode).MaximumLength(20).WithName("TariffCode");
        validator.RuleFor(x => input(x).LateTariffCode).MaximumLength(20).WithName("LateTariffCode");
        validator.RuleFor(x => input(x).ExemptionConcept).MaximumLength(50).WithName("ExemptionConcept");
        validator.RuleFor(x => input(x).MilestoneOffsetHours).InclusiveBetween(-8760, 8760).WithName("MilestoneOffsetHours");

        validator.RuleFor(x => input(x))
            .Must(i => i.PricingMode == ServicePricingModes.None || !string.IsNullOrWhiteSpace(i.ChargeConceptCode))
            .WithName("ChargeConceptCode").WithMessage("A charge concept is required when the service is charged.");
        validator.RuleFor(x => input(x))
            .Must(i => i.PricingMode != ServicePricingModes.SourceCharge || i.QuantityMode == ServiceQuantityModes.PerRequest)
            .WithName("QuantityMode").WithMessage("Source charges are charged as registered in the source (PerRequest).");
        validator.RuleFor(x => input(x))
            .Must(i => i.TimingRule == ServiceTimingRules.None || i.Milestone != ServiceMilestones.None)
            .WithName("Milestone").WithMessage("In-time or late rules need a milestone.");
        validator.RuleFor(x => input(x))
            .Must(i => i.Milestone != ServiceMilestones.CustomsDeadline || !string.IsNullOrWhiteSpace(i.DeadlineRuleCode))
            .WithName("DeadlineRuleCode").WithMessage("The customs deadline milestone needs the deadline rule code.");
        validator.RuleFor(x => input(x)).Custom((i, context) =>
        {
            var fields = i.InputSchema ?? [];
            foreach (var error in ServiceInputSchema.ValidateSchema(fields))
                context.AddFailure("InputSchema", error);

            if (!string.IsNullOrWhiteSpace(i.MeasureFieldKey)
                && !fields.Any(f => f.Key == i.MeasureFieldKey.Trim() && f.Type == ServiceInputFieldTypes.Number))
                context.AddFailure("MeasureFieldKey", "The measure field must be a number field of the form.");

            if (i.QuantityMode == ServiceQuantityModes.PerContainer && fields.All(f => f.Type != ServiceInputFieldTypes.Containers))
                context.AddFailure("QuantityMode", "Charging per container needs a containers field in the form.");
        });
    }

    private static void Enum<T>(AbstractValidator<T> validator, Func<T, string> value, string[] allowed, string name) =>
        validator.RuleFor(x => value(x))
            .Must(v => allowed.Contains(v))
            .WithName(name)
            .WithMessage($"{name} must be one of: {string.Join(", ", allowed)}.");

    /// <summary>Referencias a otros catálogos: concepto de cobro, acción de la matriz y regla de plazo.</summary>
    public static async Task<Result> CheckReferencesAsync(
        IApplicationDbContext dbContext,
        ServiceDefinitionInput input,
        CancellationToken cancellationToken)
    {
        var action = input.ActionCode.Trim();
        if (!await dbContext.ShipmentActions.AsNoTracking().AnyAsync(a => a.Code == action && a.IsActive, cancellationToken))
            return Result.Failure(DomainErrors.ServiceDefinition.UnknownAction(action));

        if (!string.IsNullOrWhiteSpace(input.ChargeConceptCode))
        {
            var concept = input.ChargeConceptCode.Trim().ToUpperInvariant();
            if (!await dbContext.ChargeConcepts.AsNoTracking().AnyAsync(c => c.Code == concept && c.IsActive, cancellationToken))
                return Result.Failure(DomainErrors.ChargeConcept.NotFound(concept));
        }

        if (!string.IsNullOrWhiteSpace(input.DeadlineRuleCode))
        {
            var rule = input.DeadlineRuleCode.Trim().ToUpperInvariant();
            if (!await dbContext.DeadlineRules.AsNoTracking().AnyAsync(r => r.Code == rule, cancellationToken))
                return Result.Failure(DomainErrors.ServiceDefinition.Invalid($"The deadline rule '{rule}' does not exist."));
        }

        return Result.Success();
    }
}

public sealed class CreateServiceDefinitionCommandValidator : AbstractValidator<CreateServiceDefinitionCommand>
{
    public CreateServiceDefinitionCommandValidator()
    {
        RuleFor(x => x.Input).NotNull();
        ServiceDefinitionRules.Apply(this, x => x.Input);
    }
}

public sealed class UpdateServiceDefinitionCommandValidator : AbstractValidator<UpdateServiceDefinitionCommand>
{
    public UpdateServiceDefinitionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Input).NotNull();
        ServiceDefinitionRules.Apply(this, x => x.Input);
    }
}

public sealed class CreateServiceDefinitionCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateServiceDefinitionCommand, ServiceDefinitionDto>
{
    public async Task<Result<ServiceDefinitionDto>> Handle(CreateServiceDefinitionCommand request, CancellationToken cancellationToken)
    {
        var references = await ServiceDefinitionRules.CheckReferencesAsync(dbContext, request.Input, cancellationToken);
        if (references.IsFailure)
            return Result<ServiceDefinitionDto>.Failure(references.Error);

        var code = request.Input.Code.Trim().ToUpperInvariant();
        if (await dbContext.ServiceDefinitions.AsNoTracking().AnyAsync(d => d.Code == code, cancellationToken))
            return Result<ServiceDefinitionDto>.Failure(DomainErrors.ServiceDefinition.AlreadyExists);

        var definition = new ServiceDefinition
        {
            Code = code,
            NameEs = string.Empty,
            NameEn = string.Empty,
            Operations = string.Empty,
            Countries = string.Empty,
            ActionCode = string.Empty
        };
        ServiceDefinitionMapper.Write(definition, request.Input);
        dbContext.ServiceDefinitions.Add(definition);

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.ServiceDefinition, definition.Id, MaintainerActions.Created,
            null, ServiceDefinitionMapper.Snapshot(definition), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ServiceDefinitionDto>.Success(ServiceDefinitionMapper.ToDto(definition));
    }
}

public sealed class UpdateServiceDefinitionCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateServiceDefinitionCommand, ServiceDefinitionDto>
{
    public async Task<Result<ServiceDefinitionDto>> Handle(UpdateServiceDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await dbContext.ServiceDefinitions.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
        if (definition is null)
            return Result<ServiceDefinitionDto>.Failure(DomainErrors.ServiceDefinition.NotFound(request.Id));

        var references = await ServiceDefinitionRules.CheckReferencesAsync(dbContext, request.Input, cancellationToken);
        if (references.IsFailure)
            return Result<ServiceDefinitionDto>.Failure(references.Error);

        var code = request.Input.Code.Trim().ToUpperInvariant();
        if (await dbContext.ServiceDefinitions.AsNoTracking().AnyAsync(d => d.Code == code && d.Id != definition.Id, cancellationToken))
            return Result<ServiceDefinitionDto>.Failure(DomainErrors.ServiceDefinition.AlreadyExists);

        var previous = ServiceDefinitionMapper.Snapshot(definition);
        ServiceDefinitionMapper.Write(definition, request.Input);

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.ServiceDefinition, definition.Id, MaintainerActions.Updated,
            previous, ServiceDefinitionMapper.Snapshot(definition), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ServiceDefinitionDto>.Success(ServiceDefinitionMapper.ToDto(definition));
    }
}

public sealed class DeactivateServiceDefinitionCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<DeactivateServiceDefinitionCommand>
{
    public async Task<Result> Handle(DeactivateServiceDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await dbContext.ServiceDefinitions.FirstOrDefaultAsync(d => d.Id == request.Id && d.IsActive, cancellationToken);
        if (definition is null)
            return Result.Failure(DomainErrors.ServiceDefinition.NotFound(request.Id));

        var previous = ServiceDefinitionMapper.Snapshot(definition);
        definition.IsActive = false;

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.ServiceDefinition, definition.Id, MaintainerActions.Deactivated,
            previous, ServiceDefinitionMapper.Snapshot(definition), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class GetServiceDefinitionsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetServiceDefinitionsQuery, IReadOnlyList<ServiceDefinitionDto>>
{
    public async Task<Result<IReadOnlyList<ServiceDefinitionDto>>> Handle(GetServiceDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var definitions = await dbContext.ServiceDefinitions.AsNoTracking()
            .Where(d => request.IncludeInactive || d.IsActive)
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.Code)
            .ToListAsync(cancellationToken);

        var country = request.Country?.Trim().ToUpperInvariant();
        var operation = request.Operation?.Trim().ToUpperInvariant();

        IReadOnlyList<ServiceDefinitionDto> items = definitions
            .Where(d => string.IsNullOrEmpty(country) || ServiceCatalogEvaluator.Csv(d.Countries).Contains(country))
            .Where(d => string.IsNullOrEmpty(operation) || ServiceCatalogEvaluator.Csv(d.Operations).Contains(operation))
            .Select(ServiceDefinitionMapper.ToDto)
            .ToList();

        return Result<IReadOnlyList<ServiceDefinitionDto>>.Success(items);
    }
}

public sealed class GetServiceDefinitionQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetServiceDefinitionQuery, ServiceDefinitionDto>
{
    public async Task<Result<ServiceDefinitionDto>> Handle(GetServiceDefinitionQuery request, CancellationToken cancellationToken)
    {
        var definition = await dbContext.ServiceDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
        return definition is null
            ? Result<ServiceDefinitionDto>.Failure(DomainErrors.ServiceDefinition.NotFound(request.Id))
            : Result<ServiceDefinitionDto>.Success(ServiceDefinitionMapper.ToDto(definition));
    }
}

public sealed class GetServiceDefinitionHistoryQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetServiceDefinitionHistoryQuery, IReadOnlyList<ServiceDefinitionChangeDto>>
{
    public async Task<Result<IReadOnlyList<ServiceDefinitionChangeDto>>> Handle(GetServiceDefinitionHistoryQuery request, CancellationToken cancellationToken)
    {
        if (!await dbContext.ServiceDefinitions.AsNoTracking().AnyAsync(d => d.Id == request.Id, cancellationToken))
            return Result<IReadOnlyList<ServiceDefinitionChangeDto>>.Failure(DomainErrors.ServiceDefinition.NotFound(request.Id));

        var changes = await dbContext.MaintainerChangeLogs.AsNoTracking()
            .Where(c => c.Maintainer == MaintainerNames.ServiceDefinition && c.EntityId == request.Id)
            .OrderByDescending(c => c.ChangedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<ServiceDefinitionChangeDto> items = changes
            .Select(c => new ServiceDefinitionChangeDto(
                c.Id, c.EntityId, c.Action, c.ChangedAt, c.ChangedBy, c.ChangedByUserId,
                MaintainerChangeLogger.Read<ServiceDefinitionSnapshot>(c.PreviousValue),
                MaintainerChangeLogger.Read<ServiceDefinitionSnapshot>(c.NewValue)))
            .ToList();

        return Result<IReadOnlyList<ServiceDefinitionChangeDto>>.Success(items);
    }
}
