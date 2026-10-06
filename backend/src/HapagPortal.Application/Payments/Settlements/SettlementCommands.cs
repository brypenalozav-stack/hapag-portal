namespace HapagPortal.Application.Payments.Settlements;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Anticipos e imputaciones a crédito para conciliación (NF-04, M3-19): por estado de cruce, tipo, país, BL y
/// período (fecha del pago en el huso del país, NF-22). Finanzas.
/// </summary>
public sealed record GetSettlementsQuery(
    string? Status = null,
    string? Kind = null,
    string? Country = null,
    string? BlNumber = null,
    DateOnly? From = null,
    DateOnly? To = null) : IQuery<IReadOnlyList<ChargeSettlementDto>>;

/// <summary>Cruza automáticamente los registros abiertos con las facturas ya recibidas (todas o de una organización).</summary>
public sealed record MatchSettlementsCommand(Guid? OrganizationId = null) : ICommand<SettlementMatchResultDto>;

/// <summary>Cruce manual de un registro con una factura del mismo BL y moneda (diferencias de monto con nota).</summary>
public sealed record MatchSettlementCommand(Guid Id, Guid InvoiceId, string? Note) : ICommand<ChargeSettlementDto>;

public sealed record SettlementMatchResultDto(int OpenBefore, int Matched, IReadOnlyList<Guid> InvoiceIds, DateTime MatchedAt);

public sealed class GetSettlementsQueryValidator : AbstractValidator<GetSettlementsQuery>
{
    public GetSettlementsQueryValidator()
    {
        RuleFor(x => x.Status).Must(s => s is null || SettlementStatus.All.Contains(s)).WithMessage("Status must be Open or Matched.");
        RuleFor(x => x.Kind).Must(k => k is null || SettlementKinds.All.Contains(k)).WithMessage("Kind must be Advance or CreditImputation.");
        RuleFor(x => x.Country).Must(c => c is null || CountryCodes.ValidCountries.Contains(c)).WithMessage("Country must be CL or BO.");
        RuleFor(x => x.BlNumber).MaximumLength(50);
        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithName("To")
            .WithMessage("To must be on or after From.");
    }
}

public sealed class MatchSettlementCommandValidator : AbstractValidator<MatchSettlementCommand>
{
    public MatchSettlementCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

/// <summary>Cruce automático reutilizado por la actualización de facturas (M7-01) y por Finanzas.</summary>
public static class SettlementMatching
{
    public static async Task<SettlementMatchResultDto> RunAsync(
        IApplicationDbContext dbContext,
        Guid? organizationId,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var blIds = await dbContext.ChargeSettlements.AsNoTracking()
            .Where(s => s.Status == SettlementStatus.Open && s.BillOfLadingId != null)
            .Select(s => s.BillOfLadingId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var openBefore = await dbContext.ChargeSettlements.AsNoTracking().CountAsync(s => s.Status == SettlementStatus.Open, cancellationToken);

        if (blIds.Count == 0)
            return new SettlementMatchResultDto(openBefore, 0, [], now);

        var query = dbContext.CustomerInvoices
            .Where(i => i.BillOfLadingId != null && blIds.Contains(i.BillOfLadingId.Value));
        if (organizationId is not null)
            query = query.Where(i => i.OrganizationId == organizationId.Value);

        var invoices = await query.ToListAsync(cancellationToken);
        var matched = await ChargeSettlements.MatchAsync(dbContext, invoices, actor, now, cancellationToken);
        return new SettlementMatchResultDto(openBefore, matched.Count, matched.Select(i => i.Id).ToList(), now);
    }
}

public sealed class GetSettlementsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetSettlementsQuery, IReadOnlyList<ChargeSettlementDto>>
{
    private const int MaxRows = 1000;

    public async Task<Result<IReadOnlyList<ChargeSettlementDto>>> Handle(GetSettlementsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.ChargeSettlements.AsNoTracking();

        if (request.Status is not null)
            query = query.Where(s => s.Status == request.Status);
        if (request.Kind is not null)
            query = query.Where(s => s.Kind == request.Kind);
        if (request.Country is not null)
            query = query.Where(s => s.Country == request.Country);
        if (!string.IsNullOrWhiteSpace(request.BlNumber))
        {
            var bl = request.BlNumber.Trim().ToUpperInvariant();
            query = query.Where(s => (s.BlNumber != null && s.BlNumber.ToUpper() == bl) || (s.BookingNumber != null && s.BookingNumber.ToUpper() == bl));
        }

        var settlements = (await query
                .OrderByDescending(s => s.SettledAt)
                .Take(MaxRows)
                .ToListAsync(cancellationToken))
            .Where(s =>
            {
                var local = BusinessCalendar.LocalDate(s.Country, s.SettledAt);
                return (request.From is null || local >= request.From) && (request.To is null || local <= request.To);
            })
            .ToList();

        return Result<IReadOnlyList<ChargeSettlementDto>>.Success(
            await ChargeSettlements.ToDtosAsync(dbContext, settlements, cancellationToken));
    }
}

public sealed class MatchSettlementsCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<MatchSettlementsCommand, SettlementMatchResultDto>
{
    public async Task<Result<SettlementMatchResultDto>> Handle(MatchSettlementsCommand request, CancellationToken cancellationToken)
    {
        var actor = PaymentActor.From(currentUserService).Name;
        var result = await SettlementMatching.RunAsync(dbContext, request.OrganizationId, actor, DateTime.UtcNow, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<SettlementMatchResultDto>.Success(result);
    }
}

public sealed class MatchSettlementCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<MatchSettlementCommand, ChargeSettlementDto>
{
    public async Task<Result<ChargeSettlementDto>> Handle(MatchSettlementCommand request, CancellationToken cancellationToken)
    {
        var settlement = await dbContext.ChargeSettlements.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (settlement is null)
            return Result<ChargeSettlementDto>.Failure(DomainErrors.Settlement.NotFound(request.Id));
        if (settlement.Status != SettlementStatus.Open)
            return Result<ChargeSettlementDto>.Failure(DomainErrors.Settlement.AlreadyMatched);

        var invoice = await dbContext.CustomerInvoices.FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);
        if (invoice is null)
            return Result<ChargeSettlementDto>.Failure(DomainErrors.Invoice.NotFound(request.InvoiceId));

        var sameShipment = invoice.BillOfLadingId is not null && invoice.BillOfLadingId == settlement.BillOfLadingId;
        if (!sameShipment
            || invoice.Currency != settlement.Currency
            || invoice.DocumentType == InvoiceDocumentTypes.CreditNote
            || invoice.Status is InvoiceStatus.Cancelled or InvoiceStatus.Superseded)
        {
            return Result<ChargeSettlementDto>.Failure(DomainErrors.Settlement.InvoiceNotMatchable);
        }

        ChargeSettlements.Apply([settlement], invoice, PaymentActor.From(currentUserService).Name, request.Note, DateTime.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await ChargeSettlements.ToDtosAsync(dbContext, [settlement], cancellationToken);
        return Result<ChargeSettlementDto>.Success(dto[0]);
    }
}
