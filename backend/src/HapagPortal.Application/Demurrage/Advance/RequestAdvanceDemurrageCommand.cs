namespace HapagPortal.Application.Demurrage.Advance;

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
/// Genera el cargo de demoras anticipadas de Bolivia (M3-16, BO-IMP-16) para una cuenta sujeta a la
/// regla interna, con el monto del mantenedor de tarifas, listo para el carro (Ola D). Es idempotente:
/// si el cargo ya existe, lo devuelve. El tipo de cambio de Nexus a la moneda local queda registrado
/// cuando la tarifa está en otra moneda (M5-05). El CLD sigue bloqueado hasta que el pago se confirme.
/// </summary>
public sealed record RequestAdvanceDemurrageCommand(string BlNumber) : ICommand<DemurrageStatusDto>;

public sealed class RequestAdvanceDemurrageCommandValidator : AbstractValidator<RequestAdvanceDemurrageCommand>
{
    public RequestAdvanceDemurrageCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class RequestAdvanceDemurrageCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    IExchangeRateService exchangeRateService,
    DemurrageStatusBuilder statusBuilder)
    : ICommandHandler<RequestAdvanceDemurrageCommand, DemurrageStatusDto>
{
    public async Task<Result<DemurrageStatusDto>> Handle(RequestAdvanceDemurrageCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: true, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<DemurrageStatusDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var bl = context.BillOfLading;

        if (!context.Permissions.CanExecute(ShipmentActionCodes.PayImportDemurrage))
            return Result<DemurrageStatusDto>.Failure(Error.Forbidden);

        var today = BusinessCalendar.LocalDate(bl.Country, DateTime.UtcNow);
        var rule = await statusBuilder.FindAdvanceRuleAsync(bl, today, cancellationToken);
        if (rule is null)
            return Result<DemurrageStatusDto>.Failure(DomainErrors.Demurrage.AdvanceNotRequired);

        var exists = await dbContext.LocalCharges.AsNoTracking()
            .AnyAsync(c => c.BillOfLadingId == bl.Id && c.ChargeType == ChargeConceptCodes.AdvanceDemurrageBo, cancellationToken);

        if (!exists)
        {
            var containers = await dbContext.BLContainers.AsNoTracking()
                .Where(c => c.BillOfLadingId == bl.Id)
                .ToListAsync(cancellationToken);

            var quote = await statusBuilder.QuoteAdvanceAsync(bl, containers, today, cancellationToken);
            if (quote is null)
                return Result<DemurrageStatusDto>.Failure(DomainErrors.Tariff.NotInForce(ChargeConceptCodes.AdvanceDemurrageBo, bl.Country));

            var charge = new LocalCharge
            {
                BillOfLadingId = bl.Id,
                ChargeType = ChargeConceptCodes.AdvanceDemurrageBo,
                Description = $"Demoras anticipadas ({containers.Count} contenedor(es))",
                Amount = quote.Value.Amount,
                Currency = quote.Value.Currency,
                Status = ChargeStatus.Pending,
                IsTaxable = false,
                TaxRate = 0m,
                TaxAmount = 0m,
                TotalAmount = quote.Value.Amount
            };
            dbContext.LocalCharges.Add(charge);

            var localCurrency = CountryCodes.GetCurrency(bl.Country);
            if (charge.Currency != localCurrency)
            {
                var rate = await exchangeRateService.GetQuoteAsync(charge.Currency, localCurrency, today, cancellationToken);
                if (rate.IsFailure)
                    return Result<DemurrageStatusDto>.Failure(rate.Error);

                exchangeRateService.Record(
                    ExchangeRateTransactionTypes.LocalCharge,
                    charge.Id,
                    rate.Value,
                    charge.TotalAmount,
                    MoneyRounding.Round(charge.TotalAmount * rate.Value.Rate, localCurrency));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var status = await statusBuilder.BuildAsync(bl, context.Permissions, cancellationToken);
        return Result<DemurrageStatusDto>.Success(status);
    }
}
