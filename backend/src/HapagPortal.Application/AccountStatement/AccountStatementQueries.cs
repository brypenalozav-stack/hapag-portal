namespace HapagPortal.Application.AccountStatement;

using FluentValidation;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;

/// <summary>
/// Estado de cuenta en línea de la organización indicada (o la propia), segregado como M7-01 (M7-03). Filtros por
/// BL, booking, fecha (emisión de la factura, registro del cargo o de la imputación), estado, moneda y tipo de
/// documento; orden <c>dueDate</c> (predeterminado), <c>issueDate</c>, <c>amount</c> o <c>blNumber</c>.
/// </summary>
public sealed record GetAccountStatementQuery(
    Guid? OrganizationId = null,
    string? BlNumber = null,
    string? BookingNumber = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? Status = null,
    string? Currency = null,
    string? DocumentType = null,
    string? Sort = null,
    string? Direction = null) : IQuery<AccountStatementDto>;

/// <summary>Exporta el estado de cuenta (mismos filtros) a planilla: <c>xlsx</c> (predeterminado) o <c>csv</c>; encabezados en ES, EN o PT.</summary>
public sealed record ExportAccountStatementQuery(
    Guid? OrganizationId = null,
    string? BlNumber = null,
    string? BookingNumber = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? Status = null,
    string? Currency = null,
    string? DocumentType = null,
    string? Sort = null,
    string? Direction = null,
    string? Format = null,
    string? Language = null) : IQuery<StatementFileDto>;

internal static class StatementRules
{
    public static void Filters<T>(
        AbstractValidator<T> validator,
        Func<T, string?> bl,
        Func<T, string?> booking,
        Func<T, DateOnly?> from,
        Func<T, DateOnly?> to,
        Func<T, string?> status,
        Func<T, string?> currency,
        Func<T, string?> documentType,
        Func<T, string?> sort,
        Func<T, string?> direction)
    {
        validator.RuleFor(x => bl(x)).MaximumLength(50).WithName("BlNumber");
        validator.RuleFor(x => booking(x)).MaximumLength(50).WithName("BookingNumber");
        validator.RuleFor(x => status(x))
            .Must(s => s is null || StatementStatuses.All.Contains(s))
            .WithName("Status")
            .WithMessage("Status must be Pending, Overdue, DueSoon, Uninvoiced, CreditImputed or Covered.");
        validator.RuleFor(x => currency(x))
            .Matches("^[A-Za-z]{3}$").When(x => !string.IsNullOrWhiteSpace(currency(x)))
            .WithName("Currency")
            .WithMessage("Currency must be an ISO 4217 code.");
        validator.RuleFor(x => documentType(x))
            .Must(t => t is null || StatementDocumentTypes.All.Contains(t))
            .WithName("DocumentType")
            .WithMessage("DocumentType must be Invoice, ExemptInvoice, DebitNote, LocalCharge, ServiceCharge, Demurrage or Freight.");
        validator.RuleFor(x => sort(x))
            .Must(s => s is null || StatementSortFields.All.Contains(s))
            .WithName("Sort")
            .WithMessage("Sort must be dueDate, issueDate, amount or blNumber.");
        validator.RuleFor(x => direction(x))
            .Must(d => d is null || d is "asc" or "desc")
            .WithName("Direction")
            .WithMessage("Direction must be asc or desc.");
        validator.RuleFor(x => x)
            .Must(x => from(x) is null || to(x) is null || from(x) <= to(x))
            .WithName("To")
            .WithMessage("To must be on or after From.");
    }
}

public sealed class GetAccountStatementQueryValidator : AbstractValidator<GetAccountStatementQuery>
{
    public GetAccountStatementQueryValidator()
    {
        StatementRules.Filters(this, x => x.BlNumber, x => x.BookingNumber, x => x.From, x => x.To, x => x.Status, x => x.Currency,
            x => x.DocumentType, x => x.Sort, x => x.Direction);
    }
}

public sealed class ExportAccountStatementQueryValidator : AbstractValidator<ExportAccountStatementQuery>
{
    public ExportAccountStatementQueryValidator()
    {
        StatementRules.Filters(this, x => x.BlNumber, x => x.BookingNumber, x => x.From, x => x.To, x => x.Status, x => x.Currency,
            x => x.DocumentType, x => x.Sort, x => x.Direction);
        RuleFor(x => x.Format)
            .Must(f => f is null || StatementExportFormats.All.Contains(f))
            .WithMessage("Format must be xlsx or csv.");
        RuleFor(x => x.Language)
            .Must(ExportLanguages.IsValid)
            .WithMessage(ExportLanguages.ValidationMessage);
    }
}

public sealed class GetAccountStatementQueryHandler(AccountStatementBuilder builder)
    : IQueryHandler<GetAccountStatementQuery, AccountStatementDto>
{
    public Task<Result<AccountStatementDto>> Handle(GetAccountStatementQuery request, CancellationToken cancellationToken) =>
        builder.BuildAsync(
            new StatementFilter(request.OrganizationId, request.BlNumber, request.BookingNumber, request.From, request.To,
                request.Status, request.Currency, request.DocumentType, request.Sort, request.Direction == "desc"),
            cancellationToken);
}

public sealed class ExportAccountStatementQueryHandler(AccountStatementBuilder builder)
    : IQueryHandler<ExportAccountStatementQuery, StatementFileDto>
{
    public async Task<Result<StatementFileDto>> Handle(ExportAccountStatementQuery request, CancellationToken cancellationToken)
    {
        var built = await builder.BuildAsync(
            new StatementFilter(request.OrganizationId, request.BlNumber, request.BookingNumber, request.From, request.To,
                request.Status, request.Currency, request.DocumentType, request.Sort, request.Direction == "desc"),
            cancellationToken);
        if (built.IsFailure)
            return Result<StatementFileDto>.Failure(built.Error);

        var statement = built.Value;
        var texts = StatementTexts.For(request.Language);
        var format = request.Format ?? StatementExportFormats.Xlsx;
        var name = $"{texts.FilePrefix}-{statement.Organization.TaxId}-{statement.AsOf:yyyyMMdd}";

        var lines = StatementExport.Lines(statement, texts);
        if (format == StatementExportFormats.Csv)
            return Result<StatementFileDto>.Success(new StatementFileDto(SpreadsheetWriter.ToCsv(lines), SpreadsheetWriter.CsvContentType, $"{name}.csv"));

        var sheets = new List<SpreadsheetSheet>
        {
            new(texts.StatementSheet, lines),
            new(texts.SummarySheet, StatementExport.Summary(statement, texts)),
            new(texts.AdvancesSheet, StatementExport.Advances(statement, texts))
        };
        return Result<StatementFileDto>.Success(new StatementFileDto(SpreadsheetWriter.ToXlsx(sheets), SpreadsheetWriter.XlsxContentType, $"{name}.xlsx"));
    }
}

/// <summary>Textos de la planilla del estado de cuenta (M7-03) por idioma: prefijo del archivo, hojas, encabezados y etiquetas.</summary>
public sealed record StatementTexts(
    string FilePrefix,
    string StatementSheet,
    string SummarySheet,
    string AdvancesSheet,
    IReadOnlyList<string> LineHeaders,
    string Yes,
    IReadOnlyList<string> SummaryHeaders,
    string Organization,
    string AsOf,
    string LastUpdated,
    string TimeZone,
    string CreditDays,
    string CreditLimit,
    string AvailableCredit,
    IReadOnlyList<string> BalanceHeaders,
    string Currency,
    IReadOnlyList<string> AdvanceHeaders)
{
    public static readonly StatementTexts Spanish = new(
        "estado-de-cuenta", "Estado de cuenta", "Resumen", "Anticipos",
        ["Tipo", "Tipo de documento", "Número", "Folio SII", "N° origen", "Concepto", "Descripción", "BL", "Booking", "Razón social",
            "RUT", "Fecha", "Emisión", "Vencimiento", "Estado", "Por vencer", "Días vencidos", "Tramo", "Moneda", "Neto", "Impuesto",
            "Total", "Saldo", "Cubierta por pago", "Comprobante", "Solicitud", "Imputación a crédito"],
        "Sí",
        ["Dato", "Valor"],
        "Organización", "Al", "Última actualización (UTC)", "Huso horario", "Días de crédito", "Cupo de crédito", "Crédito disponible",
        ["Moneda", "Saldo total", "Facturado", "Vencido", "Por vencer", "No vencido", "No facturado", "Imputado a crédito", "Anticipos sin factura"],
        "Moneda",
        ["Pago", "Comprobante", "Estado", "BL", "Booking", "Concepto", "Monto", "Moneda", "Pagado", "Moneda de pago", "Pagado (UTC)", "Factura", "Cruce (UTC)", "Recibo del anticipo"]);

    public static readonly StatementTexts English = new(
        "account-statement", "Statement", "Summary", "Advances",
        ["Kind", "Document type", "Number", "SII folio", "Source number", "Concept", "Description", "BL", "Booking", "Legal name",
            "Tax ID", "Date", "Issue date", "Due date", "Status", "Due soon", "Days overdue", "Aging bucket", "Currency", "Net", "Tax",
            "Total", "Balance", "Covered by payment", "Receipt", "Service request", "Credit imputation"],
        "Yes",
        ["Item", "Value"],
        "Organization", "As of", "Last updated (UTC)", "Time zone", "Credit days", "Credit limit", "Available credit",
        ["Currency", "Total balance", "Invoiced", "Overdue", "Due soon", "Not yet due", "Uninvoiced", "Credit imputed", "Unapplied advances"],
        "Currency",
        ["Payment", "Receipt", "Status", "BL", "Booking", "Concept", "Amount", "Currency", "Paid", "Paid currency", "Paid at (UTC)", "Invoice", "Matched at (UTC)", "Advance receipt"]);

    public static readonly StatementTexts Portuguese = new(
        "extrato-de-conta", "Extrato de conta", "Resumo", "Adiantamentos",
        ["Tipo", "Tipo de documento", "Número", "Folio SII", "Nº de origem", "Conceito", "Descrição", "BL", "Booking", "Razão social",
            "ID fiscal", "Data", "Emissão", "Vencimento", "Status", "A vencer", "Dias em atraso", "Faixa de vencimento", "Moeda", "Valor líquido",
            "Imposto", "Total", "Saldo", "Coberta por pagamento", "Comprovante", "Solicitação", "Imputação a crédito"],
        "Sim",
        ["Dado", "Valor"],
        "Organização", "Posição em", "Última atualização (UTC)", "Fuso horário", "Dias de crédito", "Limite de crédito", "Crédito disponível",
        ["Moeda", "Saldo total", "Faturado", "Vencido", "A vencer", "Não vencido", "Não faturado", "Imputado a crédito", "Adiantamentos sem fatura"],
        "Moeda",
        ["Pagamento", "Comprovante", "Status", "BL", "Booking", "Conceito", "Valor", "Moeda", "Pago", "Moeda do pagamento", "Pago em (UTC)", "Fatura",
            "Conciliado em (UTC)", "Recibo do adiantamento"]);

    /// <summary>Textos del idioma pedido; sin idioma, español.</summary>
    public static StatementTexts For(string? language) => ExportLanguages.Of(language) switch
    {
        ExportLanguages.En => English,
        ExportLanguages.Pt => Portuguese,
        _ => Spanish
    };
}

/// <summary>Filas de la planilla del estado de cuenta (M7-03).</summary>
public static class StatementExport
{
    public static IReadOnlyList<IReadOnlyList<object?>> Lines(AccountStatementDto statement, StatementTexts texts)
    {
        var rows = new List<IReadOnlyList<object?>> { texts.LineHeaders };

        rows.AddRange(statement.Lines.Select(l => (IReadOnlyList<object?>)
        [
            l.Kind, l.DocumentType, l.Number, l.SiiNumber, l.SourceNumber, l.ConceptCode, l.Description, l.BlNumber, l.BookingNumber,
            l.LegalName, l.TaxId, l.ReferenceDate, l.IssueDate, l.DueDate, l.Status, l.DueSoon ? texts.Yes : null,
            l.DaysOverdue, l.AgingBucket, l.Currency, l.Amount, l.TaxAmount, l.TotalAmount, l.Balance, l.CoveredBy?.PaymentNumber,
            l.CoveredBy?.ReceiptNumber, l.ServiceRequestNumber, l.CreditImputationNumber
        ]));

        return rows;
    }

    public static IReadOnlyList<IReadOnlyList<object?>> Summary(AccountStatementDto statement, StatementTexts texts)
    {
        var rows = new List<IReadOnlyList<object?>>();
        rows.Add(texts.SummaryHeaders);
        rows.Add([texts.Organization, $"{statement.Organization.Name} ({statement.Organization.TaxId})"]);
        rows.Add([texts.AsOf, statement.AsOf]);
        rows.Add([texts.LastUpdated, statement.LastUpdatedAt]);
        rows.Add([texts.TimeZone, statement.TimeZone]);

        if (statement.Credit is { } credit)
        {
            rows.Add([texts.CreditDays, credit.CreditDays]);
            rows.Add([texts.CreditLimit, credit.Limit is null ? null : $"{credit.Limit} {credit.LimitCurrency}"]);
            rows.Add([texts.AvailableCredit, credit.Available is null ? credit.UnavailableReason : $"{credit.Available} {credit.LimitCurrency}"]);
        }

        rows.Add([]);
        rows.Add(texts.BalanceHeaders);
        rows.AddRange(statement.Summary.Select(s => (IReadOnlyList<object?>)
            [s.Currency, s.TotalBalance, s.InvoicedBalance, s.Overdue, s.DueSoon, s.NotYetDue, s.Uninvoiced, s.CreditImputed, s.AdvancesUnapplied]));

        rows.Add([]);
        rows.Add([texts.Currency, .. statement.Aging.Buckets.Select(b => (object?)b.Code), "Total"]);
        rows.AddRange(statement.Aging.Rows.Select(r => (IReadOnlyList<object?>)[r.Currency, .. r.Amounts.Select(a => (object?)a), r.Total]));
        return rows;
    }

    public static IReadOnlyList<IReadOnlyList<object?>> Advances(AccountStatementDto statement, StatementTexts texts)
    {
        var rows = new List<IReadOnlyList<object?>> { texts.AdvanceHeaders };

        rows.AddRange(statement.Advances.Select(a => (IReadOnlyList<object?>)
        [
            a.PaymentNumber, a.ReceiptNumber, a.Status, a.BlNumber, a.BookingNumber, a.ConceptCode, a.Amount, a.Currency, a.PaidAmount,
            a.PaidCurrency, a.SettledAt, a.MatchedInvoiceNumber, a.MatchedAt, a.ReceiptDocumentNumber
        ]));

        return rows;
    }
}
