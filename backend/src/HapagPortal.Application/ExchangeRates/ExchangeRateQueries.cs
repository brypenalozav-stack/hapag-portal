namespace HapagPortal.Application.ExchangeRates;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Tipo de cambio leído desde Nexus con su vigencia (M5-05).</summary>
public sealed record ExchangeRateDto(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateOnly EffectiveDate,
    string Source,
    bool Approved);

/// <summary>Tipo de cambio y vigencia registrados en una transacción (M5-05).</summary>
public sealed record TransactionExchangeRateDto(
    Guid Id,
    string TransactionType,
    Guid TransactionId,
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateOnly EffectiveDate,
    string Source,
    bool Approved,
    decimal SourceAmount,
    decimal ConvertedAmount,
    DateTime CapturedAt);

/// <summary>
/// Tipo de cambio vigente entre dos monedas (USD, EUR, BOB, CLP) según Nexus. Sin fecha, se usa la
/// fecha local del país del usuario (NF-22).
/// </summary>
public sealed record GetExchangeRateQuery(string From, string To, DateOnly? Date = null) : IQuery<ExchangeRateDto>;

/// <summary>Tipos de cambio registrados para una transacción accesible por el usuario (M5-05).</summary>
public sealed record GetTransactionExchangeRatesQuery(string TransactionType, Guid TransactionId)
    : IQuery<IReadOnlyList<TransactionExchangeRateDto>>;

public sealed class GetExchangeRateQueryValidator : AbstractValidator<GetExchangeRateQuery>
{
    public GetExchangeRateQueryValidator()
    {
        RuleFor(x => x.From).NotEmpty().Matches("^[A-Za-z]{3}$");
        RuleFor(x => x.To).NotEmpty().Matches("^[A-Za-z]{3}$");
    }
}

public sealed class GetTransactionExchangeRatesQueryValidator : AbstractValidator<GetTransactionExchangeRatesQuery>
{
    private static readonly string[] Types =
    [
        ExchangeRateTransactionTypes.WarehouseChange,
        ExchangeRateTransactionTypes.LocalCharge,
        ExchangeRateTransactionTypes.Payment
    ];

    public GetTransactionExchangeRatesQueryValidator()
    {
        RuleFor(x => x.TransactionType)
            .Must(t => Types.Contains(t))
            .WithMessage("TransactionType must be WarehouseChange, LocalCharge or Payment.");
        RuleFor(x => x.TransactionId).NotEmpty();
    }
}

public sealed class GetExchangeRateQueryHandler(
    IExchangeRateService exchangeRateService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetExchangeRateQuery, ExchangeRateDto>
{
    public async Task<Result<ExchangeRateDto>> Handle(GetExchangeRateQuery request, CancellationToken cancellationToken)
    {
        var country = currentUserService.Country == CountryCodes.Bolivia ? CountryCodes.Bolivia : CountryCodes.Chile;
        var date = request.Date ?? BusinessCalendar.LocalDate(country, DateTime.UtcNow);

        var quote = await exchangeRateService.GetQuoteAsync(request.From, request.To, date, cancellationToken);
        if (quote.IsFailure)
            return Result<ExchangeRateDto>.Failure(quote.Error);

        var q = quote.Value;
        return Result<ExchangeRateDto>.Success(new ExchangeRateDto(q.FromCurrency, q.ToCurrency, q.Rate, q.EffectiveDate, q.Source, q.Approved));
    }
}

public sealed class GetTransactionExchangeRatesQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetTransactionExchangeRatesQuery, IReadOnlyList<TransactionExchangeRateDto>>
{
    public async Task<Result<IReadOnlyList<TransactionExchangeRateDto>>> Handle(
        GetTransactionExchangeRatesQuery request,
        CancellationToken cancellationToken)
    {
        // La transacción se ve solo si su BL es accesible para el usuario (NF-05).
        Guid? blId = request.TransactionType switch
        {
            ExchangeRateTransactionTypes.WarehouseChange => await dbContext.WarehouseChanges.AsNoTracking()
                .Where(w => w.Id == request.TransactionId).Select(w => (Guid?)w.BillOfLadingId).FirstOrDefaultAsync(cancellationToken),
            ExchangeRateTransactionTypes.LocalCharge => await dbContext.LocalCharges.AsNoTracking()
                .Where(c => c.Id == request.TransactionId).Select(c => (Guid?)c.BillOfLadingId).FirstOrDefaultAsync(cancellationToken),
            _ => await dbContext.Payments.AsNoTracking()
                .Where(p => p.Id == request.TransactionId).Select(p => (Guid?)p.BillOfLadingId).FirstOrDefaultAsync(cancellationToken)
        };

        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        var accessible = blId is not null && await accessEvaluator
            .FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), scope)
            .AnyAsync(b => b.Id == blId.Value, cancellationToken);

        // Ola D: un pago del carro puede reunir varios BL; lo ve la organización que pagó o el mandante.
        if (!accessible && request.TransactionType == ExchangeRateTransactionTypes.Payment && scope.OrganizationId is { } organizationId)
        {
            accessible = await dbContext.Payments.AsNoTracking().AnyAsync(
                p => p.Id == request.TransactionId && (p.ClientId == organizationId || p.OnBehalfOfClientId == organizationId),
                cancellationToken);
        }

        if (!accessible)
            return Result<IReadOnlyList<TransactionExchangeRateDto>>.Failure(
                Error.NotFound(request.TransactionType));

        var records = await dbContext.ExchangeRateRecords.AsNoTracking()
            .Where(r => r.TransactionType == request.TransactionType && r.TransactionId == request.TransactionId)
            .OrderBy(r => r.CapturedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<TransactionExchangeRateDto> items = records
            .Select(r => new TransactionExchangeRateDto(
                r.Id, r.TransactionType, r.TransactionId, r.FromCurrency, r.ToCurrency, r.Rate, r.EffectiveDate,
                r.Source, r.Approved, r.SourceAmount, r.ConvertedAmount, r.CapturedAt))
            .ToList();

        return Result<IReadOnlyList<TransactionExchangeRateDto>>.Success(items);
    }
}
