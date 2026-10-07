namespace HapagPortal.Application.Reports.Transactions;

using FluentValidation;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Agrupación de las transacciones por servicio (M9-01).</summary>
public static class TransactionServiceCategories
{
    public const string LocalCharge = "LocalCharge";
    public const string OnDemandService = "OnDemandService";
    public const string Demurrage = "Demurrage";
    public const string Freight = "Freight";
    public const string WarehouseChange = "WarehouseChange";
    public const string Invoice = "Invoice";
    public const string Other = "Other";

    public static readonly string[] All = [LocalCharge, OnDemandService, Demurrage, Freight, WarehouseChange, Invoice, Other];
}

/// <summary>Tipo de excepción aplicada a un cargo (M9-01): exenciones de Nexus, IPO, cambio de almacén gratuito y crédito.</summary>
public static class ReportExceptionTypes
{
    public const string GateInExemption = "GateInExemption";
    public const string EdsExemption = "EdsExemption";
    public const string GateOutExemption = "GateOutExemption";
    public const string OtherExemption = "OtherExemption";
    public const string IpoExclusion = "IpoExclusion";
    public const string FreeWarehouseChange = "FreeWarehouseChange";
    public const string CreditImputation = "CreditImputation";

    public static readonly string[] All =
        [GateInExemption, EdsExemption, GateOutExemption, OtherExemption, IpoExclusion, FreeWarehouseChange, CreditImputation];

    public static string ForExemption(string conceptCode) => conceptCode switch
    {
        ChargeConceptCodes.GateIn => GateInExemption,
        ChargeConceptCodes.Eds => EdsExemption,
        ChargeConceptCodes.GateOut => GateOutExemption,
        _ => OtherExemption
    };
}

public static class ReportExportFormats
{
    public const string Xlsx = "xlsx";
    public const string Csv = "csv";

    public static readonly string[] All = [Xlsx, Csv];
}

public sealed record ReportOrganizationDto(Guid Id, string Name, string TaxId);

/// <summary>Ítem pagado de una transacción confirmada, identificado por servicio, BL y cliente (M9-01).</summary>
public sealed record TransactionRowDto(
    Guid PaymentId,
    string PaymentNumber,
    string? ReceiptNumber,
    DateTime ConfirmedAt,
    string Country,
    string Origin,
    string? PaymentMethodCode,
    ReportOrganizationDto? Organization,
    ReportOrganizationDto? OnBehalfOf,
    string? BlNumber,
    string? BookingNumber,
    string? ItemType,
    string Category,
    string Service,
    string ServiceName,
    string ConceptCode,
    string? Description,
    string? BillingTaxId,
    string? ServiceRequestNumber,
    string Currency,
    decimal Amount,
    decimal TaxAmount,
    decimal Total,
    decimal? OriginalAmount,
    string? OriginalCurrency);

/// <summary>Totales por servicio y moneda: transacciones (pagos distintos), ítems y montos.</summary>
public sealed record TransactionServiceSummaryDto(
    string Category,
    string Service,
    string ServiceName,
    string Currency,
    int Transactions,
    int Items,
    decimal Amount,
    decimal TaxAmount,
    decimal Total);

public sealed record ReportCurrencyTotalDto(string Currency, int Transactions, int Items, decimal Total);

public sealed record TransactionReportDto(
    DateOnly From,
    DateOnly To,
    string? Country,
    string TimeZone,
    DateTime GeneratedAt,
    IReadOnlyList<TransactionServiceSummaryDto> Summary,
    IReadOnlyList<ReportCurrencyTotalDto> Totals,
    PagedResult<TransactionRowDto> Items);

/// <summary>Excepción aplicada, identificando el embarque y el cliente sobre el que se aplicó (M9-01).</summary>
public sealed record ExceptionRowDto(
    string Type,
    DateTime OccurredAt,
    string Country,
    Guid? BillOfLadingId,
    string? BlNumber,
    string? BookingNumber,
    ReportOrganizationDto? Organization,
    string? PartyTaxId,
    string? ExemptParty,
    string ConceptCode,
    decimal Amount,
    string Currency,
    string Source,
    string? Reference,
    string? Detail);

public sealed record ExceptionSummaryDto(string Type, string Currency, int Count, decimal Amount);

/// <summary>
/// Reporte de excepciones (M9-01). <see cref="IpoExclusionsAvailable"/> = Nexus respondió las condiciones de crédito
/// con las que se excluye el IPO (M4-03); si no, las exclusiones de IPO no se listan (NF-11).
/// </summary>
public sealed record ExceptionReportDto(
    DateOnly From,
    DateOnly To,
    string? Country,
    string TimeZone,
    DateTime GeneratedAt,
    bool IpoExclusionsAvailable,
    IReadOnlyList<ExceptionSummaryDto> Summary,
    PagedResult<ExceptionRowDto> Items);

public sealed record ReportFileDto(byte[] Content, string ContentType, string FileName);

/// <summary>Transacciones confirmadas de todos los servicios del portal, por servicio (M9-01). Fechas locales del país.</summary>
public sealed record GetTransactionReportQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    string? Country = null,
    string? Category = null,
    string? Service = null,
    string? Currency = null,
    Guid? OrganizationId = null,
    string? BlNumber = null,
    int Page = 1,
    int PageSize = 50) : IQuery<TransactionReportDto>;

public sealed record ExportTransactionReportQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    string? Country = null,
    string? Category = null,
    string? Service = null,
    string? Currency = null,
    Guid? OrganizationId = null,
    string? BlNumber = null,
    string? Format = null,
    string? Language = null) : IQuery<ReportFileDto>;

/// <summary>Excepciones aplicadas a los cargos: Gate In, EDS, Gate Out, IPO, cambio de almacén gratuito y crédito (M9-01).</summary>
public sealed record GetExceptionReportQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    string? Country = null,
    string? Type = null,
    Guid? OrganizationId = null,
    string? BlNumber = null,
    int Page = 1,
    int PageSize = 50) : IQuery<ExceptionReportDto>;

public sealed record ExportExceptionReportQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    string? Country = null,
    string? Type = null,
    Guid? OrganizationId = null,
    string? BlNumber = null,
    string? Format = null,
    string? Language = null) : IQuery<ReportFileDto>;

internal static class ReportRules
{
    public const int MaxRangeDays = 366;
    public const int MaxPageSize = 200;

    public static void Range<T>(AbstractValidator<T> validator, Func<T, DateOnly?> from, Func<T, DateOnly?> to, Func<T, string?> country)
    {
        validator.RuleFor(x => x)
            .Must(x => from(x) is null || to(x) is null || from(x) <= to(x))
            .WithName("To").WithMessage("To must be on or after From.");
        validator.RuleFor(x => x)
            .Must(x => from(x) is null || to(x) is null || to(x)!.Value.DayNumber - from(x)!.Value.DayNumber < MaxRangeDays)
            .WithName("From").WithMessage($"The range cannot exceed {MaxRangeDays} days.");
        validator.RuleFor(x => country(x))
            .Must(c => CountryCodes.ValidCountries.Contains(c!.Trim().ToUpperInvariant()))
            .When(x => !string.IsNullOrWhiteSpace(country(x)))
            .WithName("Country").WithMessage("Country must be 'CL' or 'BO'.");
    }

    public static void Export<T>(AbstractValidator<T> validator, Func<T, string?> format, Func<T, string?> language)
    {
        validator.RuleFor(x => format(x)).Must(f => f is null || ReportExportFormats.All.Contains(f))
            .WithName("Format").WithMessage("Format must be xlsx or csv.");
        validator.RuleFor(x => language(x)).Must(l => l is null || l is "es" or "en")
            .WithName("Language").WithMessage("Language must be es or en.");
    }

    /// <summary>Rango de fechas locales (por defecto, del primer día del mes a hoy) y su equivalente en UTC.</summary>
    public static (DateOnly From, DateOnly To, DateTime FromUtc, DateTime ToUtc, string Zone) Window(DateOnly? from, DateOnly? to, string? country)
    {
        var zoneCountry = string.IsNullOrWhiteSpace(country) ? CountryCodes.Chile : country.Trim().ToUpperInvariant();
        var today = BusinessCalendar.LocalDate(zoneCountry, DateTime.UtcNow);
        var end = to ?? today;
        var start = from ?? new DateOnly(end.Year, end.Month, 1);
        return (start, end, BusinessCalendar.StartOfLocalDayUtc(zoneCountry, start),
            BusinessCalendar.StartOfLocalDayUtc(zoneCountry, end.AddDays(1)), BusinessCalendar.TimeZoneId(zoneCountry));
    }

    public static string? Country(string? country) =>
        string.IsNullOrWhiteSpace(country) ? null : country.Trim().ToUpperInvariant();

    public static (int Page, int PageSize) Paging(int page, int pageSize) =>
        (page < 1 ? 1 : page, pageSize is < 1 or > MaxPageSize ? 50 : pageSize);

    public static ReportOrganizationDto? Organization(IReadOnlyDictionary<Guid, Client> organizations, Guid? id) =>
        id is { } key && organizations.TryGetValue(key, out var c) ? new ReportOrganizationDto(c.Id, c.Name, c.TaxId) : null;
}

public sealed class GetTransactionReportQueryValidator : AbstractValidator<GetTransactionReportQuery>
{
    public GetTransactionReportQueryValidator()
    {
        ReportRules.Range(this, x => x.From, x => x.To, x => x.Country);
        RuleFor(x => x.Category).Must(c => c is null || TransactionServiceCategories.All.Contains(c))
            .WithMessage("Category is not valid.");
        RuleFor(x => x.Currency).Matches("^[A-Za-z]{3}$").When(x => !string.IsNullOrWhiteSpace(x.Currency))
            .WithMessage("Currency must be an ISO 4217 code.");
    }
}

public sealed class ExportTransactionReportQueryValidator : AbstractValidator<ExportTransactionReportQuery>
{
    public ExportTransactionReportQueryValidator()
    {
        ReportRules.Range(this, x => x.From, x => x.To, x => x.Country);
        ReportRules.Export(this, x => x.Format, x => x.Language);
    }
}

public sealed class GetExceptionReportQueryValidator : AbstractValidator<GetExceptionReportQuery>
{
    public GetExceptionReportQueryValidator()
    {
        ReportRules.Range(this, x => x.From, x => x.To, x => x.Country);
        RuleFor(x => x.Type).Must(t => t is null || ReportExceptionTypes.All.Contains(t)).WithMessage("Type is not valid.");
    }
}

public sealed class ExportExceptionReportQueryValidator : AbstractValidator<ExportExceptionReportQuery>
{
    public ExportExceptionReportQueryValidator()
    {
        ReportRules.Range(this, x => x.From, x => x.To, x => x.Country);
        ReportRules.Export(this, x => x.Format, x => x.Language);
    }
}

/// <summary>Filtros comunes del reporte de transacciones.</summary>
public sealed record TransactionReportFilter(
    DateOnly? From,
    DateOnly? To,
    string? Country,
    string? Category,
    string? Service,
    string? Currency,
    Guid? OrganizationId,
    string? BlNumber);

/// <summary>Filtros comunes del reporte de excepciones.</summary>
public sealed record ExceptionReportFilter(
    DateOnly? From,
    DateOnly? To,
    string? Country,
    string? Type,
    Guid? OrganizationId,
    string? BlNumber);

/// <summary>
/// Reportería general para el administrador de Hapag-Lloyd (M9-01). Transacciones: ítems de los pagos confirmados (no
/// las imputaciones a crédito, que son excepciones) clasificados por servicio: servicio on demand de la solicitud que
/// generó el cargo, concepto del recargo local, demurrage, flete, cambio de almacén o factura. Excepciones: exenciones de
/// Nexus registradas (Gate In, EDS, Gate Out), exclusiones del IPO por crédito vigente (M4-03, según Nexus al consultar),
/// cambios de almacén gratuitos (M3-04) e imputaciones a la línea de crédito (M5-10), cada una con su BL y su cliente.
/// </summary>
public sealed class TransactionReportBuilder(IApplicationDbContext dbContext, IChargeRulesService chargeRulesService)
{
    public async Task<(IReadOnlyList<TransactionRowDto> Rows, (DateOnly From, DateOnly To, string Zone) Window)> TransactionsAsync(
        TransactionReportFilter filter,
        CancellationToken cancellationToken)
    {
        var window = ReportRules.Window(filter.From, filter.To, filter.Country);
        var country = ReportRules.Country(filter.Country);

        var query =
            from detail in dbContext.PaymentDetails.AsNoTracking()
            join payment in dbContext.Payments.AsNoTracking() on detail.PaymentId equals payment.Id
            where payment.Status == PaymentStatus.Confirmed
                  && payment.Origin != PaymentOrigins.CreditLine
                  && (payment.ConfirmedAt ?? payment.PaymentDate) >= window.FromUtc
                  && (payment.ConfirmedAt ?? payment.PaymentDate) < window.ToUtc
            select new { Detail = detail, Payment = payment };

        if (country is not null)
            query = query.Where(r => r.Payment.Country == country);
        if (filter.OrganizationId is { } organizationId)
            query = query.Where(r => r.Payment.ClientId == organizationId || r.Payment.OnBehalfOfClientId == organizationId);
        if (!string.IsNullOrWhiteSpace(filter.BlNumber))
        {
            var bl = filter.BlNumber.Trim();
            query = query.Where(r => r.Detail.BlNumber == bl);
        }

        var rows = await query.ToListAsync(cancellationToken);

        var chargeIds = rows.Where(r => r.Detail.ItemType == PayableItemTypes.LocalCharge && r.Detail.SourceId != null)
            .Select(r => r.Detail.SourceId!.Value).Distinct().ToList();
        var requests = await (
                from link in dbContext.ServiceRequestCharges.AsNoTracking()
                join request in dbContext.ServiceRequests.AsNoTracking() on link.ServiceRequestId equals request.Id
                where chargeIds.Contains(link.LocalChargeId)
                select new { link.LocalChargeId, request.DefinitionCode, request.RequestNumber })
            .ToListAsync(cancellationToken);
        var definitions = await dbContext.ServiceDefinitions.AsNoTracking()
            .ToDictionaryAsync(d => d.Code, d => d.NameEs, cancellationToken);
        var concepts = await dbContext.ChargeConcepts.AsNoTracking().ToDictionaryAsync(c => c.Code, cancellationToken);

        var organizationIds = rows.SelectMany(r => new[] { r.Payment.ClientId, r.Payment.OnBehalfOfClientId ?? Guid.Empty }).Distinct().ToList();
        var organizations = await dbContext.Clients.AsNoTracking()
            .Where(c => organizationIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var result = rows.Select(r =>
        {
            var detail = r.Detail;
            var payment = r.Payment;
            var request = detail.SourceId is { } sourceId ? requests.FirstOrDefault(q => q.LocalChargeId == sourceId) : null;
            var (category, service, name) = Classify(detail, request?.DefinitionCode, definitions, concepts);
            return new TransactionRowDto(
                payment.Id, payment.PaymentNumber, payment.ReceiptNumber, payment.ConfirmedAt ?? payment.PaymentDate, payment.Country,
                payment.Origin, payment.PaymentMethodCode, ReportRules.Organization(organizations, payment.ClientId),
                ReportRules.Organization(organizations, detail.OnBehalfOfClientId ?? payment.OnBehalfOfClientId),
                detail.BlNumber, detail.BookingNumber, detail.ItemType, category, service, name, detail.ConceptType, detail.Description,
                detail.BillingTaxId, request?.RequestNumber, detail.Currency, detail.Amount, detail.TaxAmount, detail.Amount + detail.TaxAmount,
                detail.OriginalAmount, detail.OriginalCurrency);
        });

        if (!string.IsNullOrWhiteSpace(filter.Category))
            result = result.Where(r => r.Category == filter.Category);
        if (!string.IsNullOrWhiteSpace(filter.Service))
        {
            var service = filter.Service.Trim().ToUpperInvariant();
            result = result.Where(r => r.Service == service);
        }

        if (!string.IsNullOrWhiteSpace(filter.Currency))
        {
            var currency = filter.Currency.Trim().ToUpperInvariant();
            result = result.Where(r => r.Currency == currency);
        }

        return (result.OrderByDescending(r => r.ConfirmedAt).ThenBy(r => r.PaymentNumber).ThenBy(r => r.Service).ToList(),
            (window.From, window.To, window.Zone));
    }

    public static IReadOnlyList<TransactionServiceSummaryDto> Summarize(IReadOnlyList<TransactionRowDto> rows) =>
        rows.GroupBy(r => (r.Category, r.Service, r.ServiceName, r.Currency))
            .Select(g => new TransactionServiceSummaryDto(
                g.Key.Category, g.Key.Service, g.Key.ServiceName, g.Key.Currency,
                g.Select(r => r.PaymentId).Distinct().Count(), g.Count(),
                g.Sum(r => r.Amount), g.Sum(r => r.TaxAmount), g.Sum(r => r.Total)))
            .OrderBy(s => Array.IndexOf(TransactionServiceCategories.All, s.Category))
            .ThenBy(s => s.Service)
            .ThenBy(s => s.Currency)
            .ToList();

    public static IReadOnlyList<ReportCurrencyTotalDto> Totals(IReadOnlyList<TransactionRowDto> rows) =>
        rows.GroupBy(r => r.Currency)
            .Select(g => new ReportCurrencyTotalDto(g.Key, g.Select(r => r.PaymentId).Distinct().Count(), g.Count(), g.Sum(r => r.Total)))
            .OrderBy(t => t.Currency)
            .ToList();

    private static (string Category, string Service, string Name) Classify(
        PaymentDetail detail,
        string? definitionCode,
        IReadOnlyDictionary<string, string> definitions,
        IReadOnlyDictionary<string, ChargeConcept> concepts)
    {
        var concept = detail.ConceptType.Trim().ToUpperInvariant();
        string NameOf(string code) => concepts.TryGetValue(code, out var c) ? c.Name : code;

        return detail.ItemType switch
        {
            PayableItemTypes.LocalCharge when definitionCode is not null =>
                (TransactionServiceCategories.OnDemandService, definitionCode, definitions.GetValueOrDefault(definitionCode) ?? definitionCode),
            PayableItemTypes.LocalCharge when concepts.TryGetValue(concept, out var c) && c.Category == ChargeCategories.Demurrage =>
                (TransactionServiceCategories.Demurrage, concept, c.Name),
            PayableItemTypes.LocalCharge => (TransactionServiceCategories.LocalCharge, concept, NameOf(concept)),
            PayableItemTypes.Demurrage => (TransactionServiceCategories.Demurrage, ChargeConceptCodes.Demurrage, NameOf(ChargeConceptCodes.Demurrage)),
            PayableItemTypes.Freight => (TransactionServiceCategories.Freight, PaymentConcepts.Freight, "Flete"),
            PayableItemTypes.WarehouseChange =>
                (TransactionServiceCategories.WarehouseChange, ChargeConceptCodes.WarehouseChange, NameOf(ChargeConceptCodes.WarehouseChange)),
            PayableItemTypes.Invoice => (TransactionServiceCategories.Invoice, PaymentConcepts.Invoice, "Factura"),
            _ => (TransactionServiceCategories.Other, concept, NameOf(concept))
        };
    }

    public async Task<(IReadOnlyList<ExceptionRowDto> Rows, bool IpoAvailable, (DateOnly From, DateOnly To, string Zone) Window)> ExceptionsAsync(
        ExceptionReportFilter filter,
        CancellationToken cancellationToken)
    {
        var window = ReportRules.Window(filter.From, filter.To, filter.Country);
        var country = ReportRules.Country(filter.Country);
        var blFilter = string.IsNullOrWhiteSpace(filter.BlNumber) ? null : filter.BlNumber.Trim();
        var wants = (string type) => filter.Type is null || filter.Type == type;
        var rows = new List<ExceptionRowDto>();
        var organizationIds = new HashSet<Guid>();

        // Exenciones de Nexus registradas al aplicar las reglas (M4-02) o al pagar un servicio (XOM).
        var exemptions = await (
                from a in dbContext.AppliedExemptions.AsNoTracking()
                join b in dbContext.BillsOfLading.AsNoTracking() on a.BillOfLadingId equals b.Id
                where a.AppliedAt >= window.FromUtc && a.AppliedAt < window.ToUtc
                select new { Exemption = a, Bl = b })
            .ToListAsync(cancellationToken);
        var exemptionRows = exemptions
            .Where(e => (country is null || e.Bl.Country == country) && (blFilter is null || e.Bl.BLNumber == blFilter))
            .Where(e => wants(ReportExceptionTypes.ForExemption(e.Exemption.ConceptCode)))
            .ToList();
        organizationIds.UnionWith(exemptionRows.Select(e => e.Exemption.PayerClientId ?? e.Bl.ClientId));

        // Cambios de almacén gratuitos (M3-04).
        var freeChanges = wants(ReportExceptionTypes.FreeWarehouseChange)
            ? await (
                    from w in dbContext.WarehouseChanges.AsNoTracking()
                    join b in dbContext.BillsOfLading.AsNoTracking() on w.BillOfLadingId equals b.Id
                    where w.IsFree && w.CreatedAt >= window.FromUtc && w.CreatedAt < window.ToUtc
                    select new { Change = w, Bl = b })
                .ToListAsync(cancellationToken)
            : [];
        freeChanges = freeChanges
            .Where(e => (country is null || e.Bl.Country == country) && (blFilter is null || e.Bl.BLNumber == blFilter))
            .ToList();
        organizationIds.UnionWith(freeChanges.Select(e => e.Change.RequestedByClientId ?? e.Bl.ClientId));

        // Imputaciones a la línea de crédito confirmadas (M5-10).
        var imputations = wants(ReportExceptionTypes.CreditImputation)
            ? await (
                    from d in dbContext.PaymentDetails.AsNoTracking()
                    join p in dbContext.Payments.AsNoTracking() on d.PaymentId equals p.Id
                    where p.Origin == PaymentOrigins.CreditLine && p.Status == PaymentStatus.Confirmed
                          && (p.ConfirmedAt ?? p.PaymentDate) >= window.FromUtc && (p.ConfirmedAt ?? p.PaymentDate) < window.ToUtc
                    select new { Detail = d, Payment = p })
                .ToListAsync(cancellationToken)
            : [];
        imputations = imputations
            .Where(e => (country is null || e.Payment.Country == country) && (blFilter is null || e.Detail.BlNumber == blFilter))
            .ToList();
        organizationIds.UnionWith(imputations.Select(e => e.Payment.ClientId));

        // IPO de clientes con crédito vigente en Nexus (M4-03): no se presenta ni se cobra en el portal.
        var ipoAvailable = true;
        var ipoRows = new List<(LocalCharge Charge, BillOfLading Bl, Client Customer)>();
        if (wants(ReportExceptionTypes.IpoExclusion))
        {
            var ipoCharges = await (
                    from c in dbContext.LocalCharges.AsNoTracking()
                    join b in dbContext.BillsOfLading.AsNoTracking() on c.BillOfLadingId equals b.Id
                    join o in dbContext.Clients.AsNoTracking() on b.ClientId equals o.Id
                    where c.ChargeType == ChargeConceptCodes.Ipo && c.Status == ChargeStatus.Pending
                          && c.CreatedAt >= window.FromUtc && c.CreatedAt < window.ToUtc
                    select new { Charge = c, Bl = b, Customer = o })
                .ToListAsync(cancellationToken);

            foreach (var group in ipoCharges
                         .Where(e => (country is null || e.Bl.Country == country) && (blFilter is null || e.Bl.BLNumber == blFilter))
                         .GroupBy(e => e.Customer.Id))
            {
                var conditions = await chargeRulesService.GetConditionsAsync(group.First().Customer, cancellationToken);
                if (!conditions.Available)
                {
                    ipoAvailable = false;
                    continue;
                }

                if (conditions.IpoExcluded)
                    ipoRows.AddRange(group.Select(e => (e.Charge, e.Bl, e.Customer)));
            }

            organizationIds.UnionWith(ipoRows.Select(r => r.Customer.Id));
        }

        var organizations = await dbContext.Clients.AsNoTracking()
            .Where(c => organizationIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        rows.AddRange(exemptionRows.Select(e => new ExceptionRowDto(
            ReportExceptionTypes.ForExemption(e.Exemption.ConceptCode), e.Exemption.AppliedAt, e.Bl.Country, e.Bl.Id, e.Bl.BLNumber,
            e.Bl.BookingNumber, ReportRules.Organization(organizations, e.Exemption.PayerClientId ?? e.Bl.ClientId), e.Exemption.PartyTaxId,
            e.Exemption.ExemptParty, e.Exemption.ConceptCode, e.Exemption.ExemptAmount, e.Exemption.Currency, e.Exemption.Source,
            e.Exemption.PartyMatchCode,
            e.Exemption.ConditionAmount is null ? "Exención total" : $"Condición {e.Exemption.ConditionAmount:0.##} {e.Exemption.ConditionCurrency}")));

        rows.AddRange(freeChanges.Select(e => new ExceptionRowDto(
            ReportExceptionTypes.FreeWarehouseChange, e.Change.CreatedAt, e.Bl.Country, e.Bl.Id, e.Bl.BLNumber, e.Bl.BookingNumber,
            ReportRules.Organization(organizations, e.Change.RequestedByClientId ?? e.Bl.ClientId), null, null,
            ChargeConceptCodes.WarehouseChange, e.Change.Amount, e.Change.Currency, e.Change.EntitlementSource ?? RuleSources.Portal,
            e.Change.EntitlementReference, $"{e.Change.FromWarehouse} → {e.Change.ToWarehouse} {e.Change.ContainerNumber}".Trim())));

        rows.AddRange(imputations.Select(e => new ExceptionRowDto(
            ReportExceptionTypes.CreditImputation, e.Payment.ConfirmedAt ?? e.Payment.PaymentDate, e.Payment.Country, e.Detail.BillOfLadingId,
            e.Detail.BlNumber, e.Detail.BookingNumber, ReportRules.Organization(organizations, e.Payment.ClientId), e.Payment.PayerTaxId,
            null, e.Detail.ConceptType, e.Detail.Amount + e.Detail.TaxAmount, e.Detail.Currency, RuleSources.Nexus, e.Payment.PaymentNumber,
            e.Detail.Description)));

        rows.AddRange(ipoRows.Select(r => new ExceptionRowDto(
            ReportExceptionTypes.IpoExclusion, r.Charge.CreatedAt, r.Bl.Country, r.Bl.Id, r.Bl.BLNumber, r.Bl.BookingNumber,
            ReportRules.Organization(organizations, r.Customer.Id), r.Customer.TaxId, null, ChargeConceptCodes.Ipo,
            r.Charge.TotalAmount, r.Charge.Currency, RuleSources.Nexus, r.Customer.MatchCode, "Cliente con crédito vigente en Nexus (M4-03)")));

        IEnumerable<ExceptionRowDto> filtered = rows;
        if (filter.OrganizationId is { } organizationId)
            filtered = filtered.Where(r => r.Organization?.Id == organizationId);

        return (filtered.OrderByDescending(r => r.OccurredAt).ThenBy(r => r.BlNumber).ToList(), ipoAvailable, (window.From, window.To, window.Zone));
    }

    public static IReadOnlyList<ExceptionSummaryDto> Summarize(IReadOnlyList<ExceptionRowDto> rows) =>
        rows.GroupBy(r => (r.Type, r.Currency))
            .Select(g => new ExceptionSummaryDto(g.Key.Type, g.Key.Currency, g.Count(), g.Sum(r => r.Amount)))
            .OrderBy(s => Array.IndexOf(ReportExceptionTypes.All, s.Type))
            .ThenBy(s => s.Currency)
            .ToList();
}

public sealed class GetTransactionReportQueryHandler(TransactionReportBuilder builder)
    : IQueryHandler<GetTransactionReportQuery, TransactionReportDto>
{
    public async Task<Result<TransactionReportDto>> Handle(GetTransactionReportQuery request, CancellationToken cancellationToken)
    {
        var (rows, window) = await builder.TransactionsAsync(
            new TransactionReportFilter(request.From, request.To, request.Country, request.Category, request.Service, request.Currency,
                request.OrganizationId, request.BlNumber),
            cancellationToken);
        var (page, pageSize) = ReportRules.Paging(request.Page, request.PageSize);

        return Result<TransactionReportDto>.Success(new TransactionReportDto(
            window.From, window.To, ReportRules.Country(request.Country), window.Zone, DateTime.UtcNow,
            TransactionReportBuilder.Summarize(rows), TransactionReportBuilder.Totals(rows),
            new PagedResult<TransactionRowDto>(rows.Skip((page - 1) * pageSize).Take(pageSize).ToList(), rows.Count, page, pageSize)));
    }
}

public sealed class GetExceptionReportQueryHandler(TransactionReportBuilder builder)
    : IQueryHandler<GetExceptionReportQuery, ExceptionReportDto>
{
    public async Task<Result<ExceptionReportDto>> Handle(GetExceptionReportQuery request, CancellationToken cancellationToken)
    {
        var (rows, ipoAvailable, window) = await builder.ExceptionsAsync(
            new ExceptionReportFilter(request.From, request.To, request.Country, request.Type, request.OrganizationId, request.BlNumber),
            cancellationToken);
        var (page, pageSize) = ReportRules.Paging(request.Page, request.PageSize);

        return Result<ExceptionReportDto>.Success(new ExceptionReportDto(
            window.From, window.To, ReportRules.Country(request.Country), window.Zone, DateTime.UtcNow, ipoAvailable,
            TransactionReportBuilder.Summarize(rows),
            new PagedResult<ExceptionRowDto>(rows.Skip((page - 1) * pageSize).Take(pageSize).ToList(), rows.Count, page, pageSize)));
    }
}

public sealed class ExportTransactionReportQueryHandler(TransactionReportBuilder builder)
    : IQueryHandler<ExportTransactionReportQuery, ReportFileDto>
{
    public async Task<Result<ReportFileDto>> Handle(ExportTransactionReportQuery request, CancellationToken cancellationToken)
    {
        var (rows, window) = await builder.TransactionsAsync(
            new TransactionReportFilter(request.From, request.To, request.Country, request.Category, request.Service, request.Currency,
                request.OrganizationId, request.BlNumber),
            cancellationToken);
        var english = request.Language == "en";
        var name = $"{(english ? "transactions" : "transacciones")}-{window.From:yyyyMMdd}-{window.To:yyyyMMdd}";

        var lines = new List<IReadOnlyList<object?>>
        {
            english
                ? ["Confirmed at", "Payment", "Receipt", "Country", "Origin", "Method", "Customer", "Tax ID", "On behalf of", "BL", "Booking",
                    "Category", "Service", "Service name", "Concept", "Description", "Billing tax ID", "Service request", "Currency", "Net", "Tax", "Total"]
                : ["Confirmado", "Pago", "Comprobante", "País", "Origen", "Medio", "Cliente", "RUT/NIT", "Por cuenta de", "BL", "Booking",
                    "Categoría", "Servicio", "Nombre del servicio", "Concepto", "Descripción", "RUT de facturación", "Solicitud", "Moneda", "Neto", "Impuesto", "Total"]
        };
        lines.AddRange(rows.Select(r => (IReadOnlyList<object?>)
        [
            r.ConfirmedAt, r.PaymentNumber, r.ReceiptNumber, r.Country, r.Origin, r.PaymentMethodCode, r.Organization?.Name, r.Organization?.TaxId,
            r.OnBehalfOf?.Name, r.BlNumber, r.BookingNumber, r.Category, r.Service, r.ServiceName, r.ConceptCode, r.Description, r.BillingTaxId,
            r.ServiceRequestNumber, r.Currency, r.Amount, r.TaxAmount, r.Total
        ]));

        if (request.Format == ReportExportFormats.Csv)
            return Result<ReportFileDto>.Success(new ReportFileDto(SpreadsheetWriter.ToCsv(lines), SpreadsheetWriter.CsvContentType, $"{name}.csv"));

        var summary = new List<IReadOnlyList<object?>>
        {
            english
                ? ["Category", "Service", "Service name", "Currency", "Transactions", "Items", "Net", "Tax", "Total"]
                : ["Categoría", "Servicio", "Nombre del servicio", "Moneda", "Transacciones", "Ítems", "Neto", "Impuesto", "Total"]
        };
        summary.AddRange(TransactionReportBuilder.Summarize(rows).Select(s => (IReadOnlyList<object?>)
            [s.Category, s.Service, s.ServiceName, s.Currency, s.Transactions, s.Items, s.Amount, s.TaxAmount, s.Total]));

        var sheets = new List<SpreadsheetSheet>
        {
            new(english ? "Transactions" : "Transacciones", lines),
            new(english ? "By service" : "Por servicio", summary)
        };
        return Result<ReportFileDto>.Success(new ReportFileDto(SpreadsheetWriter.ToXlsx(sheets), SpreadsheetWriter.XlsxContentType, $"{name}.xlsx"));
    }
}

public sealed class ExportExceptionReportQueryHandler(TransactionReportBuilder builder)
    : IQueryHandler<ExportExceptionReportQuery, ReportFileDto>
{
    public async Task<Result<ReportFileDto>> Handle(ExportExceptionReportQuery request, CancellationToken cancellationToken)
    {
        var (rows, _, window) = await builder.ExceptionsAsync(
            new ExceptionReportFilter(request.From, request.To, request.Country, request.Type, request.OrganizationId, request.BlNumber),
            cancellationToken);
        var english = request.Language == "en";
        var name = $"{(english ? "exceptions" : "excepciones")}-{window.From:yyyyMMdd}-{window.To:yyyyMMdd}";

        var lines = new List<IReadOnlyList<object?>>
        {
            english
                ? ["Type", "Date", "Country", "BL", "Booking", "Customer", "Customer tax ID", "Party tax ID", "Exempt party", "Concept", "Currency",
                    "Amount", "Source", "Reference", "Detail"]
                : ["Tipo", "Fecha", "País", "BL", "Booking", "Cliente", "RUT/NIT cliente", "RUT/NIT de la figura", "Figura exenta", "Concepto", "Moneda",
                    "Monto", "Origen", "Referencia", "Detalle"]
        };
        lines.AddRange(rows.Select(r => (IReadOnlyList<object?>)
        [
            r.Type, r.OccurredAt, r.Country, r.BlNumber, r.BookingNumber, r.Organization?.Name, r.Organization?.TaxId, r.PartyTaxId, r.ExemptParty,
            r.ConceptCode, r.Currency, r.Amount, r.Source, r.Reference, r.Detail
        ]));

        if (request.Format == ReportExportFormats.Csv)
            return Result<ReportFileDto>.Success(new ReportFileDto(SpreadsheetWriter.ToCsv(lines), SpreadsheetWriter.CsvContentType, $"{name}.csv"));

        var summary = new List<IReadOnlyList<object?>>
        {
            english ? ["Type", "Currency", "Count", "Amount"] : ["Tipo", "Moneda", "Cantidad", "Monto"]
        };
        summary.AddRange(TransactionReportBuilder.Summarize(rows).Select(s => (IReadOnlyList<object?>)[s.Type, s.Currency, s.Count, s.Amount]));

        var sheets = new List<SpreadsheetSheet>
        {
            new(english ? "Exceptions" : "Excepciones", lines),
            new(english ? "Summary" : "Resumen", summary)
        };
        return Result<ReportFileDto>.Success(new ReportFileDto(SpreadsheetWriter.ToXlsx(sheets), SpreadsheetWriter.XlsxContentType, $"{name}.xlsx"));
    }
}
