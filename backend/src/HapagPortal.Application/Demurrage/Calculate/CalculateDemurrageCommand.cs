namespace HapagPortal.Application.Demurrage.Calculate;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Demurrage.Common;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Calculadora de demurrage (M3-18, CL-IMP-04, BO-IMP-06). Calcula por contenedor los días desde la
/// descarga (ETA) hasta <c>UntilDate</c> (por defecto hoy, en la fecha local del país, NF-22) con la
/// tarifa DEMURRAGE vigente del mantenedor (tramos por día, M8-01) y los días libres del embarque.
/// Con <c>Save</c> reemplaza las líneas calculadas y no pagadas, que quedan para agregar al carro. Si el
/// BL ya tiene una factura de demurrage, no se permite recalcular.
/// </summary>
public sealed record CalculateDemurrageCommand(
    string BlNumber,
    DateOnly? UntilDate = null,
    IReadOnlyList<string>? ContainerNumbers = null,
    bool Save = true) : ICommand<DemurrageCalculationDto>;

public sealed class CalculateDemurrageCommandValidator : AbstractValidator<CalculateDemurrageCommand>
{
    public CalculateDemurrageCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ContainerNumbers).Must(c => c is null || c.Count <= 200);
    }
}

public sealed class CalculateDemurrageCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    ITariffResolver tariffResolver,
    DemurrageStatusBuilder statusBuilder)
    : ICommandHandler<CalculateDemurrageCommand, DemurrageCalculationDto>
{
    public async Task<Result<DemurrageCalculationDto>> Handle(CalculateDemurrageCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: request.Save, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<DemurrageCalculationDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var bl = context.BillOfLading;

        if (!context.Permissions.Can(ShipmentActionCodes.PayImportDemurrage))
            return Result<DemurrageCalculationDto>.Failure(Error.Forbidden);

        if (request.Save && !context.Permissions.CanExecute(ShipmentActionCodes.PayImportDemurrage))
            return Result<DemurrageCalculationDto>.Failure(Error.Forbidden);

        if (!DemurrageStateEvaluator.IsImport(bl))
            return Result<DemurrageCalculationDto>.Failure(DomainErrors.Demurrage.NotImport);

        var now = DateTime.UtcNow;
        if (!DemurrageStateEvaluator.HasArrived(bl, now))
            return Result<DemurrageCalculationDto>.Failure(DomainErrors.Demurrage.NotArrived);

        var lines = await dbContext.DemurrageCharges
            .Where(d => d.BillOfLadingId == bl.Id)
            .ToListAsync(cancellationToken);

        // M3-18: con factura emitida no se habilita nuevamente la calculadora.
        if (lines.Any(l => l.InvoiceNumber is not null))
            return Result<DemurrageCalculationDto>.Failure(DomainErrors.Demurrage.InvoiceExists);

        var containers = await dbContext.BLContainers.AsNoTracking()
            .Where(c => c.BillOfLadingId == bl.Id)
            .OrderBy(c => c.ContainerNumber)
            .ToListAsync(cancellationToken);

        if (request.ContainerNumbers is { Count: > 0 })
        {
            var requested = request.ContainerNumbers.Select(n => n.Trim().ToUpperInvariant()).ToHashSet();
            var unknown = requested.FirstOrDefault(n => containers.All(c => !string.Equals(c.ContainerNumber, n, StringComparison.OrdinalIgnoreCase)));
            if (unknown is not null)
                return Result<DemurrageCalculationDto>.Failure(DomainErrors.WarehouseChange.ContainerNotFound(unknown));

            containers = containers.Where(c => requested.Contains(c.ContainerNumber.ToUpperInvariant())).ToList();
        }

        var country = bl.Country;
        var until = request.UntilDate ?? BusinessCalendar.LocalDate(country, now);
        var start = bl.ETA!.Value;
        var elapsed = BusinessCalendar.Elapsed(
            TariffTierUnits.CalendarDays, country, start, BusinessCalendar.StartOfLocalDayUtc(country, until));
        var (freeDays, _) = await statusBuilder.ResolveFreeDaysAsync(bl, lines, cancellationToken);

        var results = new List<DemurrageCalculationLineDto>();
        foreach (var container in containers)
        {
            var tariff = (await tariffResolver.GetInForceAsync(
                new TariffLookup(country, ChargeConceptCodes.Demurrage, until, container.ContainerType), cancellationToken))
                .FirstOrDefault();

            if (tariff is null)
                return Result<DemurrageCalculationDto>.Failure(DomainErrors.Tariff.NotInForce(ChargeConceptCodes.Demurrage, country));

            var computation = TariffCalculator.Compute(tariff.Amount, tariff.TierMode, tariff.Tiers, elapsed, freeDays ?? 0);
            var effectiveFree = freeDays ?? computation.Lines.Where(l => l.UnitAmount == 0m).Sum(l => l.Units);
            var demurrageDays = Math.Max(0, elapsed - effectiveFree);
            var total = MoneyRounding.Round(computation.Amount, tariff.Currency);

            results.Add(new DemurrageCalculationLineDto(
                container.ContainerNumber,
                container.ContainerType,
                start,
                until,
                elapsed,
                effectiveFree,
                demurrageDays,
                demurrageDays > 0 ? MoneyRounding.Round(total / demurrageDays, tariff.Currency) : 0m,
                total,
                tariff.Currency,
                tariff.Source,
                tariff.TariffId,
                computation.Lines));
        }

        if (request.Save)
        {
            var calculated = results.Select(r => r.ContainerNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Se reemplazan los cálculos previos no pagados (baja lógica, como el interceptor de auditoría).
            foreach (var previous in lines.Where(l => l.Status != DemurrageChargeStatus.Paid && !l.IsExempt && calculated.Contains(l.ContainerNumber)))
            {
                previous.DeletedAt = now;
                previous.DeletedBy = currentUserService.UserId?.ToString() ?? "system";
            }

            foreach (var line in results.Where(r => r.TotalAmount > 0m))
            {
                dbContext.DemurrageCharges.Add(new DemurrageCharge
                {
                    BillOfLadingId = bl.Id,
                    ContainerNumber = line.ContainerNumber,
                    FreeDays = line.FreeDays,
                    DemurrageDays = line.DemurrageDays,
                    DailyRate = line.DailyRate,
                    TotalAmount = line.TotalAmount,
                    Currency = line.Currency,
                    StartDate = line.StartDate,
                    EndDate = BusinessCalendar.StartOfLocalDayUtc(country, line.UntilDate),
                    Status = DemurrageChargeStatus.Pending
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var totals = results
            .Where(r => r.TotalAmount > 0m)
            .GroupBy(r => r.Currency)
            .Select(g => new CurrencyTotalDto(g.Key, g.Sum(r => r.TotalAmount), 0m, g.Sum(r => r.TotalAmount)))
            .ToList();

        var status = request.Save
            ? await statusBuilder.BuildAsync(bl, context.Permissions, cancellationToken)
            : null;

        return Result<DemurrageCalculationDto>.Success(new DemurrageCalculationDto(bl.BLNumber, request.Save, results, totals, status));
    }
}
