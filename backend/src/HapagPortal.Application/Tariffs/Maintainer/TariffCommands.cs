namespace HapagPortal.Application.Tariffs.Maintainer;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Tariffs.Common;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Alta de una tarifa en el mantenedor (M8-01). Queda registrada con usuario y fecha (NF-15).</summary>
public sealed record CreateTariffCommand(
    string ConceptCode,
    string? Code,
    string Country,
    string Currency,
    string? ContainerType,
    string? Description,
    decimal Amount,
    string? TierUnit,
    string? TierMode,
    IReadOnlyList<TariffTierDto>? Tiers,
    DateOnly ValidFrom,
    DateOnly? ValidTo) : ICommand<TariffDto>;

/// <summary>Modificación de una tarifa; el registro guarda el valor anterior y el nuevo (NF-15).</summary>
public sealed record UpdateTariffCommand(
    Guid Id,
    string ConceptCode,
    string? Code,
    string Country,
    string Currency,
    string? ContainerType,
    string? Description,
    decimal Amount,
    string? TierUnit,
    string? TierMode,
    IReadOnlyList<TariffTierDto>? Tiers,
    DateOnly ValidFrom,
    DateOnly? ValidTo) : ICommand<TariffDto>;

/// <summary>Baja lógica de una tarifa: deja de aplicarse y su historial se conserva (NF-15, NF-16).</summary>
public sealed record DeactivateTariffCommand(Guid Id) : ICommand;

internal static class TariffRules
{
    public static void Apply<T>(AbstractValidator<T> validator, Func<T, TariffFields> fields)
    {
        validator.RuleFor(x => fields(x).ConceptCode).NotEmpty().MaximumLength(50).WithName("ConceptCode");
        validator.RuleFor(x => fields(x).Code).MaximumLength(20).WithName("Code");
        validator.RuleFor(x => fields(x).Country)
            .Must(c => CountryCodes.ValidCountries.Contains(c))
            .WithName("Country")
            .WithMessage("Country must be CL or BO.");
        validator.RuleFor(x => fields(x).Currency)
            .NotEmpty()
            .Matches("^[A-Z]{3}$")
            .WithName("Currency")
            .WithMessage("Currency must be an ISO 4217 code (CLP, USD, EUR, BOB).");
        validator.RuleFor(x => fields(x).ContainerType).MaximumLength(10).WithName("ContainerType");
        validator.RuleFor(x => fields(x).Description).MaximumLength(300).WithName("Description");
        validator.RuleFor(x => fields(x).Amount).GreaterThanOrEqualTo(0m).WithName("Amount");
        validator.RuleFor(x => fields(x).TierUnit)
            .Must(u => TariffTierUnits.All.Contains(u))
            .WithName("TierUnit")
            .WithMessage("TierUnit must be None, Hours, CalendarDays, BusinessDays or Units.");
        validator.RuleFor(x => fields(x).TierMode)
            .Must(m => TariffTierModes.All.Contains(m))
            .WithName("TierMode")
            .WithMessage("TierMode must be Flat or PerUnit.");
        validator.RuleFor(x => fields(x))
            .Must(f => f.ValidTo is null || f.ValidTo >= f.ValidFrom)
            .WithName("ValidTo")
            .WithMessage("ValidTo must be on or after ValidFrom.");
        validator.RuleFor(x => fields(x))
            .Must(f => (f.Tiers.Count == 0) == (f.TierUnit == TariffTierUnits.None))
            .WithName("Tiers")
            .WithMessage("Tiers require a TierUnit other than None, and TierUnit None does not allow tiers.");
        validator.RuleFor(x => fields(x))
            .Must(f => TariffCalculator.ValidateTiers(TariffMapper.ToDefinitions(f.Tiers)) is null)
            .WithName("Tiers")
            .WithMessage(x => TariffCalculator.ValidateTiers(TariffMapper.ToDefinitions(fields(x).Tiers)) ?? string.Empty);
    }

    /// <summary>Concepto del catálogo, sin solapes con otra tarifa activa de la misma clave.</summary>
    public static async Task<Result> CheckAsync(
        IApplicationDbContext dbContext,
        TariffFields fields,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var conceptExists = await dbContext.ChargeConcepts.AsNoTracking()
            .AnyAsync(c => c.Code == fields.ConceptCode && c.IsActive, cancellationToken);
        if (!conceptExists)
            return Result.Failure(DomainErrors.ChargeConcept.NotFound(fields.ConceptCode));

        var siblings = await dbContext.Tariffs.AsNoTracking()
            .Where(t => t.IsActive
                && t.ConceptCode == fields.ConceptCode
                && t.Country == fields.Country
                && t.Currency == fields.Currency
                && t.ContainerType == fields.ContainerType
                && t.Code == fields.Code
                && (excludeId == null || t.Id != excludeId))
            .ToListAsync(cancellationToken);

        var end = fields.ValidTo ?? DateOnly.MaxValue;
        var overlaps = siblings.Any(t => t.ValidFrom <= end && (t.ValidTo ?? DateOnly.MaxValue) >= fields.ValidFrom);

        return overlaps ? Result.Failure(DomainErrors.Tariff.Overlaps) : Result.Success();
    }

    public static void Write(Tariff tariff, TariffFields fields, IApplicationDbContext dbContext)
    {
        tariff.ConceptCode = fields.ConceptCode;
        tariff.Code = fields.Code;
        tariff.Country = fields.Country;
        tariff.Currency = fields.Currency;
        tariff.ContainerType = fields.ContainerType;
        tariff.Description = fields.Description;
        tariff.Amount = fields.Amount;
        tariff.TierUnit = fields.TierUnit;
        tariff.TierMode = fields.TierMode;
        tariff.ValidFrom = fields.ValidFrom;
        tariff.ValidTo = fields.ValidTo;

        tariff.Tiers.Clear();
        foreach (var tier in fields.Tiers.OrderBy(t => t.FromUnit))
        {
            var entity = new TariffTier { TariffId = tariff.Id, FromUnit = tier.FromUnit, ToUnit = tier.ToUnit, Amount = tier.Amount };
            tariff.Tiers.Add(entity);
            dbContext.TariffTiers.Add(entity);
        }
    }
}

/// <summary>Campos normalizados de alta y modificación.</summary>
internal sealed record TariffFields(
    string ConceptCode,
    string? Code,
    string Country,
    string Currency,
    string? ContainerType,
    string? Description,
    decimal Amount,
    string TierUnit,
    string TierMode,
    IReadOnlyList<TariffTierDto> Tiers,
    DateOnly ValidFrom,
    DateOnly? ValidTo)
{
    public static TariffFields From(
        string conceptCode, string? code, string country, string currency, string? containerType, string? description,
        decimal amount, string? tierUnit, string? tierMode, IReadOnlyList<TariffTierDto>? tiers, DateOnly validFrom, DateOnly? validTo) =>
        new(
            (conceptCode ?? string.Empty).Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant(),
            (country ?? string.Empty).Trim().ToUpperInvariant(),
            (currency ?? string.Empty).Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(containerType) ? null : containerType.Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            amount,
            string.IsNullOrWhiteSpace(tierUnit) ? TariffTierUnits.None : tierUnit.Trim(),
            string.IsNullOrWhiteSpace(tierMode) ? TariffTierModes.Flat : tierMode.Trim(),
            tiers ?? [],
            validFrom,
            validTo);
}

public sealed class CreateTariffCommandValidator : AbstractValidator<CreateTariffCommand>
{
    public CreateTariffCommandValidator()
    {
        TariffRules.Apply(this, x => TariffFields.From(
            x.ConceptCode, x.Code, x.Country, x.Currency, x.ContainerType, x.Description,
            x.Amount, x.TierUnit, x.TierMode, x.Tiers, x.ValidFrom, x.ValidTo));
    }
}

public sealed class UpdateTariffCommandValidator : AbstractValidator<UpdateTariffCommand>
{
    public UpdateTariffCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        TariffRules.Apply(this, x => TariffFields.From(
            x.ConceptCode, x.Code, x.Country, x.Currency, x.ContainerType, x.Description,
            x.Amount, x.TierUnit, x.TierMode, x.Tiers, x.ValidFrom, x.ValidTo));
    }
}

public sealed class CreateTariffCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateTariffCommand, TariffDto>
{
    public async Task<Result<TariffDto>> Handle(CreateTariffCommand request, CancellationToken cancellationToken)
    {
        var fields = TariffFields.From(
            request.ConceptCode, request.Code, request.Country, request.Currency, request.ContainerType, request.Description,
            request.Amount, request.TierUnit, request.TierMode, request.Tiers, request.ValidFrom, request.ValidTo);

        var check = await TariffRules.CheckAsync(dbContext, fields, null, cancellationToken);
        if (check.IsFailure)
            return Result<TariffDto>.Failure(check.Error);

        var tariff = new Tariff { ConceptCode = fields.ConceptCode, Country = fields.Country, Currency = fields.Currency };
        TariffRules.Write(tariff, fields, dbContext);
        dbContext.Tariffs.Add(tariff);

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.Tariff, tariff.Id, MaintainerActions.Created,
            null, TariffSnapshot.From(tariff), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        var conceptName = await ConceptNameAsync(dbContext, tariff.ConceptCode, cancellationToken);
        return Result<TariffDto>.Success(TariffMapper.ToDto(tariff, conceptName));
    }

    internal static Task<string?> ConceptNameAsync(IApplicationDbContext dbContext, string code, CancellationToken cancellationToken) =>
        dbContext.ChargeConcepts.AsNoTracking()
            .Where(c => c.Code == code)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);
}

public sealed class UpdateTariffCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateTariffCommand, TariffDto>
{
    public async Task<Result<TariffDto>> Handle(UpdateTariffCommand request, CancellationToken cancellationToken)
    {
        var tariff = await dbContext.Tariffs
            .Include(t => t.Tiers)
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.IsActive, cancellationToken);

        if (tariff is null)
            return Result<TariffDto>.Failure(DomainErrors.Tariff.NotFound(request.Id));

        var fields = TariffFields.From(
            request.ConceptCode, request.Code, request.Country, request.Currency, request.ContainerType, request.Description,
            request.Amount, request.TierUnit, request.TierMode, request.Tiers, request.ValidFrom, request.ValidTo);

        var check = await TariffRules.CheckAsync(dbContext, fields, tariff.Id, cancellationToken);
        if (check.IsFailure)
            return Result<TariffDto>.Failure(check.Error);

        var previous = TariffSnapshot.From(tariff);

        // Los tramos se reemplazan completos (los huérfanos se eliminan); los anteriores quedan en la
        // instantánea previa del registro de cambios.
        TariffRules.Write(tariff, fields, dbContext);

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.Tariff, tariff.Id, MaintainerActions.Updated,
            previous, TariffSnapshot.From(tariff), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        var conceptName = await CreateTariffCommandHandler.ConceptNameAsync(dbContext, tariff.ConceptCode, cancellationToken);
        return Result<TariffDto>.Success(TariffMapper.ToDto(tariff, conceptName));
    }
}

public sealed class DeactivateTariffCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<DeactivateTariffCommand>
{
    public async Task<Result> Handle(DeactivateTariffCommand request, CancellationToken cancellationToken)
    {
        var tariff = await dbContext.Tariffs
            .Include(t => t.Tiers)
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.IsActive, cancellationToken);

        if (tariff is null)
            return Result.Failure(DomainErrors.Tariff.NotFound(request.Id));

        var previous = TariffSnapshot.From(tariff);
        tariff.IsActive = false;

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.Tariff, tariff.Id, MaintainerActions.Deactivated,
            previous, TariffSnapshot.From(tariff), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
