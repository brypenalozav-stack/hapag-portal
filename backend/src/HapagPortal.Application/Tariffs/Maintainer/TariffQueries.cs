namespace HapagPortal.Application.Tariffs.Maintainer;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Tariffs.Common;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Tarifas del mantenedor (M8-01), con filtros por país, concepto, vigencia y estado.</summary>
public sealed record GetTariffsQuery(
    string? Country = null,
    string? Concept = null,
    DateOnly? InForceOn = null,
    bool IncludeInactive = false) : IQuery<IReadOnlyList<TariffDto>>;

public sealed record GetTariffQuery(Guid Id) : IQuery<TariffDto>;

/// <summary>Historial de cambios de una tarifa: usuario, fecha, valor anterior y nuevo (NF-15).</summary>
public sealed record GetTariffHistoryQuery(Guid Id) : IQuery<IReadOnlyList<TariffChangeDto>>;

/// <summary>
/// Tarifas vigentes de un concepto (M8-01) con la precedencia portal → Nexus. La fecha de vigencia es la
/// fecha local del país en el instante <c>At</c> (NF-22) o la fecha <c>Date</c> indicada. Con
/// <c>AsOf</c> reconstruye el mantenedor tal como estaba en ese instante (NF-15). Si se indica
/// <c>Units</c> o el hito <c>ElapsedFrom</c>, calcula el valor del tramo con el tiempo medido en UTC y el
/// calendario de negocio del país.
/// </summary>
public sealed record GetTariffsInForceQuery(
    string Country,
    string Concept,
    DateTime? At = null,
    DateOnly? Date = null,
    string? ContainerType = null,
    string? Code = null,
    DateTime? AsOf = null,
    DateTime? ElapsedFrom = null,
    int? Units = null) : IQuery<IReadOnlyList<TariffInForceDto>>;

/// <summary>Catálogo de conceptos de cobro activos (Fase 1, Ola C).</summary>
public sealed record GetChargeConceptsQuery(string? Country = null) : IQuery<IReadOnlyList<ChargeConceptDto>>;

public sealed class GetTariffsInForceQueryValidator : AbstractValidator<GetTariffsInForceQuery>
{
    public GetTariffsInForceQueryValidator()
    {
        RuleFor(x => x.Country)
            .Must(c => CountryCodes.ValidCountries.Contains(c?.ToUpperInvariant()))
            .WithMessage("Country must be CL or BO.");
        RuleFor(x => x.Concept).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Units).GreaterThanOrEqualTo(0).When(x => x.Units is not null);
    }
}

public sealed class GetTariffsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetTariffsQuery, IReadOnlyList<TariffDto>>
{
    public async Task<Result<IReadOnlyList<TariffDto>>> Handle(GetTariffsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Tariffs.AsNoTracking().Include(t => t.Tiers).AsQueryable();

        if (!request.IncludeInactive)
            query = query.Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            var country = request.Country.Trim().ToUpperInvariant();
            query = query.Where(t => t.Country == country);
        }

        if (!string.IsNullOrWhiteSpace(request.Concept))
        {
            var concept = request.Concept.Trim().ToUpperInvariant();
            query = query.Where(t => t.ConceptCode == concept);
        }

        if (request.InForceOn is not null)
        {
            var date = request.InForceOn.Value;
            query = query.Where(t => t.ValidFrom <= date && (t.ValidTo == null || t.ValidTo >= date));
        }

        var tariffs = await query
            .OrderBy(t => t.Country)
            .ThenBy(t => t.ConceptCode)
            .ThenBy(t => t.Code)
            .ThenBy(t => t.ContainerType)
            .ThenByDescending(t => t.ValidFrom)
            .ToListAsync(cancellationToken);

        var names = await dbContext.ChargeConcepts.AsNoTracking()
            .ToDictionaryAsync(c => c.Code, c => c.Name, cancellationToken);

        IReadOnlyList<TariffDto> items = tariffs
            .Select(t => TariffMapper.ToDto(t, names.GetValueOrDefault(t.ConceptCode)))
            .ToList();

        return Result<IReadOnlyList<TariffDto>>.Success(items);
    }
}

public sealed class GetTariffQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetTariffQuery, TariffDto>
{
    public async Task<Result<TariffDto>> Handle(GetTariffQuery request, CancellationToken cancellationToken)
    {
        var tariff = await dbContext.Tariffs.AsNoTracking()
            .Include(t => t.Tiers)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (tariff is null)
            return Result<TariffDto>.Failure(DomainErrors.Tariff.NotFound(request.Id));

        var name = await CreateTariffCommandHandler.ConceptNameAsync(dbContext, tariff.ConceptCode, cancellationToken);
        return Result<TariffDto>.Success(TariffMapper.ToDto(tariff, name));
    }
}

public sealed class GetTariffHistoryQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetTariffHistoryQuery, IReadOnlyList<TariffChangeDto>>
{
    public async Task<Result<IReadOnlyList<TariffChangeDto>>> Handle(GetTariffHistoryQuery request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Tariffs.AsNoTracking().AnyAsync(t => t.Id == request.Id, cancellationToken);
        if (!exists)
            return Result<IReadOnlyList<TariffChangeDto>>.Failure(DomainErrors.Tariff.NotFound(request.Id));

        var changes = await dbContext.MaintainerChangeLogs.AsNoTracking()
            .Where(c => c.Maintainer == MaintainerNames.Tariff && c.EntityId == request.Id)
            .OrderByDescending(c => c.ChangedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<TariffChangeDto> items = changes
            .Select(c => new TariffChangeDto(
                c.Id,
                c.EntityId,
                c.Action,
                c.ChangedAt,
                c.ChangedBy,
                c.ChangedByUserId,
                MaintainerChangeLogger.Read<TariffSnapshot>(c.PreviousValue),
                MaintainerChangeLogger.Read<TariffSnapshot>(c.NewValue)))
            .ToList();

        return Result<IReadOnlyList<TariffChangeDto>>.Success(items);
    }
}

public sealed class GetTariffsInForceQueryHandler(ITariffResolver tariffResolver)
    : IQueryHandler<GetTariffsInForceQuery, IReadOnlyList<TariffInForceDto>>
{
    public async Task<Result<IReadOnlyList<TariffInForceDto>>> Handle(GetTariffsInForceQuery request, CancellationToken cancellationToken)
    {
        var country = request.Country.Trim().ToUpperInvariant();
        var at = ToUtc(request.At) ?? DateTime.UtcNow;
        var date = request.Date ?? BusinessCalendar.LocalDate(country, at);

        var tariffs = await tariffResolver.GetInForceAsync(
            new TariffLookup(
                country,
                request.Concept.Trim().ToUpperInvariant(),
                date,
                request.ContainerType?.Trim().ToUpperInvariant(),
                request.Code?.Trim().ToUpperInvariant(),
                ToUtc(request.AsOf)),
            cancellationToken);

        IReadOnlySet<DateOnly>? holidays = null;
        var items = new List<TariffInForceDto>();

        foreach (var tariff in tariffs)
        {
            int? measured = request.Units;
            if (measured is null && request.ElapsedFrom is not null && TariffTierUnits.TimeBased.Contains(tariff.TierUnit))
            {
                if (tariff.TierUnit == TariffTierUnits.BusinessDays)
                    holidays ??= await tariffResolver.GetHolidaysAsync(country, cancellationToken);

                measured = BusinessCalendar.Elapsed(tariff.TierUnit, country, ToUtc(request.ElapsedFrom)!.Value, at, holidays);
            }

            TariffComputation? computation = measured is null
                ? null
                : TariffCalculator.Compute(tariff.Amount, tariff.TierMode, tariff.Tiers, measured.Value);

            items.Add(new TariffInForceDto(
                tariff.TariffId,
                tariff.Concept,
                tariff.Code,
                tariff.Country,
                tariff.Currency,
                tariff.ContainerType,
                tariff.Description,
                tariff.Amount,
                tariff.TierUnit,
                tariff.TierMode,
                tariff.Tiers.Select(t => new TariffTierDto(t.FromUnit, t.ToUnit, t.Amount)).ToList(),
                tariff.ValidFrom,
                tariff.ValidTo,
                tariff.Source,
                date,
                BusinessCalendar.TimeZoneId(country),
                measured,
                computation is null ? null : MoneyRounding.Round(computation.Amount, tariff.Currency),
                computation?.Covered,
                computation?.Lines ?? []));
        }

        return Result<IReadOnlyList<TariffInForceDto>>.Success(items);
    }

    /// <summary>Instantes sin zona se interpretan como UTC (NF-22).</summary>
    private static DateTime? ToUtc(DateTime? value)
    {
        if (value is null)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }
}

public sealed class GetChargeConceptsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetChargeConceptsQuery, IReadOnlyList<ChargeConceptDto>>
{
    public async Task<Result<IReadOnlyList<ChargeConceptDto>>> Handle(GetChargeConceptsQuery request, CancellationToken cancellationToken)
    {
        var concepts = await dbContext.ChargeConcepts.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);

        var country = request.Country?.Trim().ToUpperInvariant();

        IReadOnlyList<ChargeConceptDto> items = concepts
            .Select(c => new ChargeConceptDto(
                c.Code,
                c.Name,
                c.Category,
                CountryCodes.ParseOperatingCountries(c.Countries, CountryCodes.Chile),
                c.NexusTariff,
                c.NexusExemptible,
                c.DisplayOrder))
            .Where(c => string.IsNullOrEmpty(country) || c.Countries.Contains(country))
            .ToList();

        return Result<IReadOnlyList<ChargeConceptDto>>.Success(items);
    }
}
