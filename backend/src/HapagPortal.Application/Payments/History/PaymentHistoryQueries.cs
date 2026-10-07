namespace HapagPortal.Application.Payments.History;

using FluentValidation;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.Payments.Lifecycle;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Historial de pagos y boletas (M7-02): solo pagos hechos en el portal, de la organización del usuario
/// (como pagadora o como mandante, NF-14). Distingue el RUT del pagador del RUT de facturación de cada ítem.
/// El administrador interno puede indicar la organización.
/// </summary>
public sealed record GetPaymentHistoryQuery(
    Guid? OrganizationId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? Status = null,
    string? BlNumber = null,
    int Page = 1,
    int PageSize = 20,
    string? Sort = null,
    string? Direction = null) : IQuery<PagedResult<PaymentHistoryItemDto>>
{
    /// <summary>Columnas por las que se puede ordenar el historial (<c>sort</c>, con <c>direction</c> asc|desc).</summary>
    public static readonly SortMap<Domain.Entities.Payment> Sorts = new SortMap<Domain.Entities.Payment>()
        .Add("paymentDate", p => p.PaymentDate)
        .Add("paymentNumber", p => p.PaymentNumber)
        .Add("totalAmount", p => p.TotalAmount)
        .Add("currency", p => p.Currency)
        .Add("status", p => p.Status);
}

public sealed record GetPaymentHistoryItemQuery(Guid PaymentId) : IQuery<PaymentHistoryItemDto>;

public sealed record PaymentReceiptFileDto(byte[] Content, string FileName);

/// <summary>Boleta o comprobante del pago en PDF (plantilla común de documentos, Ola E).</summary>
public sealed record GetPaymentReceiptQuery(Guid PaymentId) : IQuery<PaymentReceiptFileDto>;

public sealed class GetPaymentHistoryQueryValidator : AbstractValidator<GetPaymentHistoryQuery>
{
    public GetPaymentHistoryQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.Status)
            .Must(s => s is null || PaymentStatus.All.Contains(s))
            .WithMessage("Status must be a valid payment status.");
        RuleFor(x => x.BlNumber).MaximumLength(50);
        RuleFor(x => x.Sort)
            .Must(GetPaymentHistoryQuery.Sorts.IsValid)
            .WithMessage($"Sort must be one of: {string.Join(", ", GetPaymentHistoryQuery.Sorts.Names)}.");

        RuleFor(x => x.Direction)
            .Must(SortMap<Domain.Entities.Payment>.IsValidDirection)
            .WithMessage("Direction must be 'asc' or 'desc'.");
        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithName("To")
            .WithMessage("To must be on or after From.");
    }
}

internal static class PaymentHistoryView
{
    public static async Task<IReadOnlyList<PaymentHistoryItemDto>> BuildAsync(
        IApplicationDbContext dbContext,
        IReadOnlyList<Payment> payments,
        CancellationToken cancellationToken)
    {
        var ids = payments.Select(p => p.Id).ToList();
        var details = (await dbContext.PaymentDetails.AsNoTracking()
                .Where(d => ids.Contains(d.PaymentId))
                .ToListAsync(cancellationToken))
            .ToLookup(d => d.PaymentId);

        var organizationIds = payments.Select(p => p.ClientId)
            .Concat(payments.Where(p => p.OnBehalfOfClientId is not null).Select(p => p.OnBehalfOfClientId!.Value))
            .Distinct()
            .ToList();
        var organizations = await dbContext.Clients.AsNoTracking()
            .Where(c => organizationIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var blIds = payments.Where(p => p.BillOfLadingId is not null).Select(p => p.BillOfLadingId!.Value).Distinct().ToList();
        var bls = await dbContext.BillsOfLading.AsNoTracking()
            .Where(b => blIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        var methods = await dbContext.PaymentMethodConfigs.AsNoTracking().ToListAsync(cancellationToken);
        var users = await UserEmailsAsync(dbContext, payments, cancellationToken);

        return payments.Select(p =>
        {
            var items = details[p.Id].ToList();
            var payer = organizations.GetValueOrDefault(p.ClientId);
            var payerTaxId = p.PayerTaxId ?? (payer is null ? null : TaxIdNormalizer.Normalize(payer.TaxId));
            var billing = items.Where(d => d.BillingTaxId is not null).Select(d => d.BillingTaxId!).Distinct().ToList();
            var bl = p.BillOfLadingId is { } blId ? bls.GetValueOrDefault(blId) : null;

            var blNumbers = items.Where(d => d.BlNumber is not null).Select(d => d.BlNumber!)
                .Concat(bl is null ? [] : [bl.BLNumber]).Distinct().Order(StringComparer.Ordinal).ToList();
            var bookings = items.Where(d => d.BookingNumber is not null).Select(d => d.BookingNumber!)
                .Concat(bl?.BookingNumber is null ? [] : [bl.BookingNumber]).Distinct().Order(StringComparer.Ordinal).ToList();

            var methodCode = p.PaymentMethodCode ?? p.PaymentMethod;
            var method = methods.FirstOrDefault(m => m.Country == p.Country && m.Code == methodCode);
            var mandator = p.OnBehalfOfClientId is { } mandatorId ? organizations.GetValueOrDefault(mandatorId) : null;

            return new PaymentHistoryItemDto(
                p.Id,
                p.PaymentNumber,
                p.ReceiptNumber ?? p.SlipNumber,
                p.ReceiptNumber,
                p.SlipNumber,
                p.ConfirmedAt ?? p.PaymentDate,
                p.Status,
                p.Origin,
                methodCode,
                method?.Name,
                p.Country,
                p.Currency,
                p.Amount,
                p.TaxAmount,
                p.TotalAmount,
                payerTaxId,
                p.PayerName ?? payer?.Name,
                billing,
                PayerDiffersFromBilling: payerTaxId is not null && billing.Any(b => b != payerTaxId),
                blNumbers,
                bookings,
                mandator is null ? null : new PaymentOrganizationDto(mandator.Id, mandator.Name, TaxIdNormalizer.Normalize(mandator.TaxId)),
                p.CreatedByUserId is { } userId ? users.GetValueOrDefault(userId) ?? p.CreatedBy : p.CreatedBy,
                ReceiptAvailable: p.ReceiptNumber is not null || p.SlipNumber is not null,
                items.Select(PaymentViews.Item).ToList());
        }).ToList();
    }

    private static async Task<Dictionary<Guid, string>> UserEmailsAsync(
        IApplicationDbContext dbContext,
        IReadOnlyList<Payment> payments,
        CancellationToken cancellationToken)
    {
        var userIds = payments.Where(p => p.CreatedByUserId is not null).Select(p => p.CreatedByUserId!.Value).Distinct().ToList();
        return await dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);
    }
}

public sealed class GetPaymentHistoryQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetPaymentHistoryQuery, PagedResult<PaymentHistoryItemDto>>
{
    public async Task<Result<PagedResult<PaymentHistoryItemDto>>> Handle(GetPaymentHistoryQuery request, CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);

        // M7-02 muestra solo pagos: la imputación a la línea de crédito (M5-10) figura en el estado de cuenta.
        var query = dbContext.Payments.AsNoTracking().Where(p => p.Origin != PaymentOrigins.CreditLine);

        if (scope.IsAdmin)
        {
            if (request.OrganizationId is { } organizationId)
                query = query.Where(p => p.ClientId == organizationId || p.OnBehalfOfClientId == organizationId);
        }
        else
        {
            if (!scope.IsOperational || scope.OrganizationId is null)
                return Result<PagedResult<PaymentHistoryItemDto>>.Failure(Error.Forbidden);

            // NF-05: el filtro por organización se aplica en el servidor; otra organización indicada no amplía nada.
            var own = scope.OrganizationId.Value;
            if (request.OrganizationId is not null && request.OrganizationId != own)
                return Result<PagedResult<PaymentHistoryItemDto>>.Failure(Error.Forbidden);

            query = query.Where(p => p.ClientId == own || p.OnBehalfOfClientId == own);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(p => p.Status == request.Status);

        if (request.From is { } from)
        {
            var fromUtc = from.AddDays(-1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(p => p.PaymentDate >= fromUtc);
        }

        if (request.To is { } to)
        {
            var toUtc = to.AddDays(2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(p => p.PaymentDate < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(request.BlNumber))
        {
            var bl = request.BlNumber.Trim().ToUpperInvariant();
            var paymentIds = dbContext.PaymentDetails.Where(d => d.BlNumber != null && d.BlNumber.ToUpper() == bl).Select(d => d.PaymentId);
            var blIds = dbContext.BillsOfLading.Where(b => b.BLNumber.ToUpper() == bl).Select(b => b.Id);
            query = query.Where(p => paymentIds.Contains(p.Id) || (p.BillOfLadingId != null && blIds.Contains(p.BillOfLadingId.Value)));
        }

        var loaded = (await query.ToListAsync(cancellationToken))
            .Where(p => InPeriod(p, request.From, request.To))
            .AsQueryable();
        var payments = GetPaymentHistoryQuery.Sorts
            .Apply(
                loaded,
                request.Sort,
                request.Direction,
                q => q.OrderByDescending(p => p.PaymentDate),
                q => q.ThenByDescending(p => p.PaymentDate))
            .ToList();

        var page = payments
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var items = await PaymentHistoryView.BuildAsync(dbContext, page, cancellationToken);
        return Result<PagedResult<PaymentHistoryItemDto>>.Success(
            new PagedResult<PaymentHistoryItemDto>(items, payments.Count, request.Page, request.PageSize));
    }

    /// <summary>El período se compara con la fecha del pago en el huso del país (NF-22).</summary>
    private static bool InPeriod(Payment payment, DateOnly? from, DateOnly? to)
    {
        var local = BusinessCalendar.LocalDate(payment.Country, payment.PaymentDate);
        return (from is null || local >= from) && (to is null || local <= to);
    }
}

public sealed class GetPaymentHistoryItemQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetPaymentHistoryItemQuery, PaymentHistoryItemDto>
{
    public async Task<Result<PaymentHistoryItemDto>> Handle(GetPaymentHistoryItemQuery request, CancellationToken cancellationToken)
    {
        var payment = await PaymentAccess.LoadAsync(
            dbContext, accessEvaluator, currentUserService, request.PaymentId, ownerOnly: false, tracking: false, cancellationToken);
        if (payment.IsFailure)
            return Result<PaymentHistoryItemDto>.Failure(payment.Error);

        var items = await PaymentHistoryView.BuildAsync(dbContext, [payment.Value], cancellationToken);
        return Result<PaymentHistoryItemDto>.Success(items[0]);
    }
}

public sealed class GetPaymentReceiptQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService,
    IPdfDocumentRenderer renderer,
    DocumentSettings settings)
    : IQueryHandler<GetPaymentReceiptQuery, PaymentReceiptFileDto>
{
    public async Task<Result<PaymentReceiptFileDto>> Handle(GetPaymentReceiptQuery request, CancellationToken cancellationToken)
    {
        var loaded = await PaymentAccess.LoadAsync(
            dbContext, accessEvaluator, currentUserService, request.PaymentId, ownerOnly: false, tracking: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<PaymentReceiptFileDto>.Failure(loaded.Error);

        var payment = loaded.Value;
        var number = payment.ReceiptNumber ?? payment.SlipNumber;
        if (number is null)
            return Result<PaymentReceiptFileDto>.Failure(DomainErrors.PaymentFlow.ReceiptNotAvailable);

        var content = await PortalPdfs.PaymentReceiptAsync(dbContext, renderer, settings, payment, number, cancellationToken);
        return Result<PaymentReceiptFileDto>.Success(new PaymentReceiptFileDto(content, $"{number}.pdf"));
    }
}
