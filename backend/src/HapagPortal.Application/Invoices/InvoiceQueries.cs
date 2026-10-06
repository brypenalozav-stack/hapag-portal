namespace HapagPortal.Application.Invoices;

using System.IO.Compression;
using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Factura local del cliente (M7-01) con el estado calculado (vencida según la fecha del país).</summary>
public sealed record InvoiceDto(
    Guid Id,
    string? SiiNumber,
    string SourceNumber,
    string DocumentType,
    DateOnly IssueDate,
    DateOnly? DueDate,
    Guid? BlId,
    string? BlNumber,
    string? BookingNumber,
    string LegalName,
    string TaxId,
    decimal NetAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    string Status,
    string? SiiStatus,
    bool CanDownload,
    bool IsPayable,
    bool InCart,
    DateTime SyncedAt);

/// <summary>Facturas de una organización, segregadas (M7-01), con la última actualización desde la fuente.</summary>
public sealed record InvoiceListDto(
    InvoiceOrganizationDto Organization,
    IReadOnlyList<InvoiceDto> Items,
    int Total,
    int Page,
    int PageSize,
    DateTime? LastUpdatedAt,
    string TimeZone);

public sealed record InvoiceFileDto(byte[] Content, string ContentType, string FileName);

public sealed record InvoiceRefreshResultDto(Guid OrganizationId, int Checked, int Updated, DateTime LastUpdatedAt);

public sealed record GetInvoiceOrganizationsQuery : IQuery<IReadOnlyList<InvoiceOrganizationDto>>;

public sealed record GetInvoicesQuery(
    Guid? OrganizationId = null,
    string? BlNumber = null,
    string? BookingNumber = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? Status = null,
    string? Currency = null,
    string? DocumentType = null,
    int Page = 1,
    int PageSize = 20) : IQuery<InvoiceListDto>;

public sealed record GetInvoicePdfQuery(Guid Id) : IQuery<InvoiceFileDto>;

/// <summary>Descarga múltiple en un ZIP; solo facturas con folio emitido (M7-01).</summary>
public sealed record DownloadInvoicesQuery(IReadOnlyList<Guid> Ids) : IQuery<InvoiceFileDto>;

/// <summary>Actualiza el estado SII de las facturas con folio desde la fuente de facturación.</summary>
public sealed record RefreshInvoicesCommand(Guid? OrganizationId) : ICommand<InvoiceRefreshResultDto>;

public sealed class GetInvoicesQueryValidator : AbstractValidator<GetInvoicesQuery>
{
    public GetInvoicesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.BlNumber).MaximumLength(50);
        RuleFor(x => x.BookingNumber).MaximumLength(50);
        RuleFor(x => x.Status)
            .Must(s => s is null || InvoiceStatus.All.Contains(s))
            .WithMessage("Status must be Pending, Overdue, Paid or Cancelled.");
        RuleFor(x => x.DocumentType)
            .Must(t => t is null || InvoiceDocumentTypes.All.Contains(t))
            .WithMessage("DocumentType must be Invoice, ExemptInvoice, CreditNote or DebitNote.");
        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithName("To")
            .WithMessage("To must be on or after From.");
    }
}

public sealed class DownloadInvoicesQueryValidator : AbstractValidator<DownloadInvoicesQuery>
{
    public DownloadInvoicesQueryValidator()
    {
        RuleFor(x => x.Ids).NotEmpty();
        RuleFor(x => x.Ids.Count).LessThanOrEqualTo(50).WithName("Ids");
    }
}

/// <summary>Estado visible de la factura: una pendiente con el vencimiento cumplido en el país está vencida.</summary>
internal static class InvoiceView
{
    public static string StatusOf(CustomerInvoice invoice, DateOnly today) =>
        invoice.Status == InvoiceStatus.Pending && invoice.DueDate is { } due && due < today
            ? InvoiceStatus.Overdue
            : invoice.Status;

    public static InvoiceDto ToDto(CustomerInvoice i, DateOnly today, bool inCart)
    {
        var status = StatusOf(i, today);
        var payable = i.IsPayable
            && status is InvoiceStatus.Pending or InvoiceStatus.Overdue
            && i.DocumentType != InvoiceDocumentTypes.CreditNote
            && i.TotalAmount > 0m;

        return new InvoiceDto(
            i.Id, i.SiiNumber, i.SourceNumber, i.DocumentType, i.IssueDate, i.DueDate, i.BillOfLadingId, i.BlNumber,
            i.BookingNumber, i.LegalName, i.TaxId, i.NetAmount, i.TaxAmount, i.TotalAmount, i.Currency, status,
            i.SiiStatus, CanDownload: i.SiiNumber is not null, payable, inCart, i.SyncedAt);
    }

    public static async Task<Result<(InvoiceScope Scope, InvoiceOrganizationDto Organization)>> OrganizationAsync(
        IApplicationDbContext dbContext,
        IShipmentAccessEvaluator accessEvaluator,
        Guid? organizationId,
        CancellationToken cancellationToken)
    {
        var scope = await InvoiceAccess.LoadAsync(dbContext, accessEvaluator, cancellationToken);
        if (scope.Organizations.Count == 0)
            return Result<(InvoiceScope, InvoiceOrganizationDto)>.Failure(Error.Forbidden);

        var organization = organizationId is null
            ? scope.Organizations[0]
            : scope.Organizations.FirstOrDefault(o => o.Id == organizationId.Value);

        // NF-05: una organización ajena no se distingue de una inexistente.
        return organization is null
            ? Result<(InvoiceScope, InvoiceOrganizationDto)>.Failure(Error.Forbidden)
            : Result<(InvoiceScope, InvoiceOrganizationDto)>.Success((scope, organization));
    }

    public static async Task<Result<CustomerInvoice>> VisibleAsync(
        IApplicationDbContext dbContext,
        IShipmentAccessEvaluator accessEvaluator,
        Guid id,
        CancellationToken cancellationToken)
    {
        var invoice = await dbContext.CustomerInvoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
            return Result<CustomerInvoice>.Failure(DomainErrors.Invoice.NotFound(id));

        var scope = await InvoiceAccess.LoadAsync(dbContext, accessEvaluator, cancellationToken);
        return InvoiceAccess.CanView(scope, invoice)
            ? Result<CustomerInvoice>.Success(invoice)
            : Result<CustomerInvoice>.Failure(DomainErrors.Invoice.NotFound(id));
    }

    public static async Task<Result<byte[]>> PdfAsync(
        IInvoiceProvider invoiceProvider,
        CustomerInvoice invoice,
        CancellationToken cancellationToken)
    {
        if (invoice.SiiNumber is null)
            return Result<byte[]>.Failure(DomainErrors.Invoice.PdfNotAvailable);

        var pdf = await invoiceProvider.GetPdfAsync(invoice.SiiNumber, cancellationToken);
        if (pdf.IsFailure)
            return Result<byte[]>.Failure(pdf.Error);

        return pdf.Value is { Length: > 0 } bytes
            ? Result<byte[]>.Success(bytes)
            : Result<byte[]>.Failure(DomainErrors.Invoice.PdfNotAvailable);
    }

    public static string FileName(CustomerInvoice invoice) =>
        $"factura-{invoice.SiiNumber ?? invoice.SourceNumber}.pdf";
}

public sealed class GetInvoiceOrganizationsQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetInvoiceOrganizationsQuery, IReadOnlyList<InvoiceOrganizationDto>>
{
    public async Task<Result<IReadOnlyList<InvoiceOrganizationDto>>> Handle(GetInvoiceOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var scope = await InvoiceAccess.LoadAsync(dbContext, accessEvaluator, cancellationToken);
        return scope.Organizations.Count == 0
            ? Result<IReadOnlyList<InvoiceOrganizationDto>>.Failure(Error.Forbidden)
            : Result<IReadOnlyList<InvoiceOrganizationDto>>.Success(scope.Organizations);
    }
}

/// <summary>
/// Facturas de una organización (M7-01) con filtros por BL, booking, fecha de emisión, estado, moneda y
/// tipo de documento. Sin organización indicada se usa la propia; la información no se mezcla entre ellas.
/// </summary>
public sealed class GetInvoicesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetInvoicesQuery, InvoiceListDto>
{
    public async Task<Result<InvoiceListDto>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var loaded = await InvoiceView.OrganizationAsync(dbContext, accessEvaluator, request.OrganizationId, cancellationToken);
        if (loaded.IsFailure)
            return Result<InvoiceListDto>.Failure(loaded.Error);

        var (scope, organization) = loaded.Value;
        var query = InvoiceAccess.Filter(dbContext.CustomerInvoices.AsNoTracking(), scope, organization.Id);

        if (!string.IsNullOrWhiteSpace(request.BlNumber))
        {
            var bl = request.BlNumber.Trim().ToUpperInvariant();
            query = query.Where(i => i.BlNumber != null && i.BlNumber.ToUpper() == bl);
        }

        if (!string.IsNullOrWhiteSpace(request.BookingNumber))
        {
            var booking = request.BookingNumber.Trim().ToUpperInvariant();
            query = query.Where(i => i.BookingNumber != null && i.BookingNumber.ToUpper() == booking);
        }

        if (request.From is { } from)
            query = query.Where(i => i.IssueDate >= from);
        if (request.To is { } to)
            query = query.Where(i => i.IssueDate <= to);
        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            var currency = request.Currency.Trim().ToUpperInvariant();
            query = query.Where(i => i.Currency == currency);
        }

        if (!string.IsNullOrWhiteSpace(request.DocumentType))
            query = query.Where(i => i.DocumentType == request.DocumentType);

        var invoices = await query
            .OrderByDescending(i => i.IssueDate)
            .ThenByDescending(i => i.SourceNumber)
            .ToListAsync(cancellationToken);

        var country = await dbContext.Clients.AsNoTracking()
            .Where(c => c.Id == organization.Id)
            .Select(c => c.Country)
            .FirstOrDefaultAsync(cancellationToken) ?? CountryCodes.Chile;
        var today = BusinessCalendar.LocalDate(country, DateTime.UtcNow);

        // "Vencida" depende de la fecha del país: el filtro por estado se aplica sobre el estado calculado.
        var filtered = invoices
            .Where(i => request.Status is null || InvoiceView.StatusOf(i, today) == request.Status)
            .ToList();

        var inCart = await InCartAsync(filtered.Select(i => i.Id).ToList(), cancellationToken);

        var items = filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => InvoiceView.ToDto(i, today, inCart.Contains(i.Id)))
            .ToList();

        DateTime? lastUpdated = invoices.Count == 0 ? null : invoices.Max(i => i.SyncedAt);

        return Result<InvoiceListDto>.Success(new InvoiceListDto(
            organization, items, filtered.Count, request.Page, request.PageSize, lastUpdated, BusinessCalendar.TimeZoneId(country)));
    }

    private async Task<HashSet<Guid>> InCartAsync(IReadOnlyList<Guid> invoiceIds, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null || invoiceIds.Count == 0)
            return [];

        var userId = currentUserService.UserId.Value;
        var cartIds = await dbContext.Carts.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var ids = await dbContext.CartItems.AsNoTracking()
            .Where(i => cartIds.Contains(i.CartId) && i.ItemType == PayableItemTypes.Invoice && invoiceIds.Contains(i.SourceId))
            .Select(i => i.SourceId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }
}

public sealed class GetInvoicePdfQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    IInvoiceProvider invoiceProvider)
    : IQueryHandler<GetInvoicePdfQuery, InvoiceFileDto>
{
    public async Task<Result<InvoiceFileDto>> Handle(GetInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        var invoice = await InvoiceView.VisibleAsync(dbContext, accessEvaluator, request.Id, cancellationToken);
        if (invoice.IsFailure)
            return Result<InvoiceFileDto>.Failure(invoice.Error);

        var pdf = await InvoiceView.PdfAsync(invoiceProvider, invoice.Value, cancellationToken);
        return pdf.IsFailure
            ? Result<InvoiceFileDto>.Failure(pdf.Error)
            : Result<InvoiceFileDto>.Success(new InvoiceFileDto(pdf.Value, "application/pdf", InvoiceView.FileName(invoice.Value)));
    }
}

public sealed class DownloadInvoicesQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    IInvoiceProvider invoiceProvider)
    : IQueryHandler<DownloadInvoicesQuery, InvoiceFileDto>
{
    public async Task<Result<InvoiceFileDto>> Handle(DownloadInvoicesQuery request, CancellationToken cancellationToken)
    {
        var scope = await InvoiceAccess.LoadAsync(dbContext, accessEvaluator, cancellationToken);
        var ids = request.Ids.Distinct().ToList();
        var invoices = await dbContext.CustomerInvoices.AsNoTracking()
            .Where(i => ids.Contains(i.Id))
            .ToListAsync(cancellationToken);

        var missing = ids.FirstOrDefault(id => invoices.All(i => i.Id != id || !InvoiceAccess.CanView(scope, i)));
        if (missing != Guid.Empty)
            return Result<InvoiceFileDto>.Failure(DomainErrors.Invoice.NotFound(missing));

        if (invoices.Any(i => i.SiiNumber is null))
            return Result<InvoiceFileDto>.Failure(DomainErrors.Invoice.FolioRequired);

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var invoice in invoices.OrderBy(i => i.SiiNumber, StringComparer.Ordinal))
            {
                var pdf = await InvoiceView.PdfAsync(invoiceProvider, invoice, cancellationToken);
                if (pdf.IsFailure)
                    return Result<InvoiceFileDto>.Failure(pdf.Error);

                var entry = zip.CreateEntry(InvoiceView.FileName(invoice), CompressionLevel.Fastest);
                await using var stream = entry.Open();
                await stream.WriteAsync(pdf.Value, cancellationToken);
            }
        }

        return Result<InvoiceFileDto>.Success(new InvoiceFileDto(
            buffer.ToArray(), "application/zip", $"facturas-{DateTime.UtcNow:yyyyMMddHHmm}.zip"));
    }
}

/// <summary>
/// Consulta la fuente de facturación por cada folio de la organización y registra la hora de la última
/// actualización. Si la fuente no responde, no se guarda nada (NF-11: no se presenta información parcial).
/// </summary>
public sealed class RefreshInvoicesCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    IInvoiceProvider invoiceProvider)
    : ICommandHandler<RefreshInvoicesCommand, InvoiceRefreshResultDto>
{
    public async Task<Result<InvoiceRefreshResultDto>> Handle(RefreshInvoicesCommand request, CancellationToken cancellationToken)
    {
        var loaded = await InvoiceView.OrganizationAsync(dbContext, accessEvaluator, request.OrganizationId, cancellationToken);
        if (loaded.IsFailure)
            return Result<InvoiceRefreshResultDto>.Failure(loaded.Error);

        var (scope, organization) = loaded.Value;
        var invoices = await InvoiceAccess.Filter(dbContext.CustomerInvoices, scope, organization.Id)
            .ToListAsync(cancellationToken);

        var updated = 0;
        var checkedCount = 0;
        foreach (var invoice in invoices.Where(i => i.SiiNumber is not null))
        {
            var document = await invoiceProvider.GetAsync(invoice.SiiNumber!, cancellationToken);
            if (document.IsFailure)
                return Result<InvoiceRefreshResultDto>.Failure(document.Error);

            checkedCount++;
            if (document.Value is { } dto && dto.Status != invoice.SiiStatus)
            {
                invoice.SiiStatus = dto.Status;
                updated++;
            }
        }

        var now = DateTime.UtcNow;
        foreach (var invoice in invoices)
            invoice.SyncedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<InvoiceRefreshResultDto>.Success(new InvoiceRefreshResultDto(organization.Id, checkedCount, updated, now));
    }
}
