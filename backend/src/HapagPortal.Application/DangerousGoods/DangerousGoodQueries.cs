namespace HapagPortal.Application.DangerousGoods;

using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Entrada de la base de referencia DG con el nombre de su clase en español e inglés.</summary>
public sealed record DangerousGoodDto(
    Guid Id,
    string? UnNumber,
    string ProperShippingNameEs,
    string ProperShippingNameEn,
    string? HazardClass,
    string? HazardClassNameEs,
    string? HazardClassNameEn,
    string? SubsidiaryRisk,
    string? PackingGroup,
    string? Notes,
    bool Classified,
    string Source);

/// <summary>
/// Resultado del buscador (M10-06). <c>ResultCode</c>: <c>CLASSIFIED</c>, <c>NOT_CLASSIFIED</c> (la carga figura
/// como no clasificada) o <c>NO_MATCH</c> (sin clasificación en la base de referencia); <c>Message</c> lo explica
/// de forma explícita. <c>Disclaimer</c>: el resultado es informativo, no una aprobación operacional.
/// </summary>
public sealed record DangerousGoodSearchResultDto(
    string Query,
    string ResultCode,
    bool Classified,
    string Message,
    IReadOnlyList<DangerousGoodDto> Items,
    string Disclaimer,
    string DataSource);

public sealed record DangerousGoodImportErrorDto(int Line, string Reason);

public sealed record DangerousGoodImportResultDto(
    int TotalRows,
    int Created,
    int Updated,
    int Skipped,
    IReadOnlyList<DangerousGoodImportErrorDto> Errors);

/// <summary>Consulta libre por nombre, descripción o número ONU; no requiere un BL ni booking (M10-06).</summary>
public sealed record SearchDangerousGoodsQuery(string Query) : IQuery<DangerousGoodSearchResultDto>;

/// <summary>
/// Carga interna de la base de referencia desde un CSV (separador ";" o ","), para incorporar la lista completa
/// cuando Hapag-Lloyd defina la fuente y su licencia. Encabezado obligatorio con <c>unNumber</c>, <c>nameEs</c>,
/// <c>nameEn</c> y <c>class</c>; opcionales <c>subsidiaryRisk</c>, <c>packingGroup</c>, <c>notes</c>,
/// <c>keywords</c> y <c>classified</c> (true/false). Una fila existente (mismo número ONU y nombre en inglés) se
/// actualiza. Cada alta o cambio queda en el registro de NF-15.
/// </summary>
public sealed record ImportDangerousGoodsCommand(string Content) : ICommand<DangerousGoodImportResultDto>;

public sealed class SearchDangerousGoodsQueryValidator : AbstractValidator<SearchDangerousGoodsQuery>
{
    public SearchDangerousGoodsQueryValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MinimumLength(2).MaximumLength(100);
    }
}

public sealed class ImportDangerousGoodsCommandValidator : AbstractValidator<ImportDangerousGoodsCommand>
{
    public const int MaxContentLength = 5 * 1024 * 1024;

    public ImportDangerousGoodsCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(MaxContentLength);
    }
}

/// <summary>Textos fijos del buscador DG y normalización de las entradas.</summary>
public static partial class DangerousGoodCatalog
{
    public const string Disclaimer =
        "Resultado de carácter informativo según la base de referencia del portal. No constituye la aprobación " +
        "operacional del embarque de mercancía peligrosa: la declaración y aceptación de la carga se rigen por el " +
        "Código IMDG vigente y la validación de Hapag-Lloyd.";

    public const string DataSource =
        "Muestra de referencia con números ONU, nombres y clases de la Lista de mercancías peligrosas de las " +
        "Recomendaciones de las Naciones Unidas (Reglamentación Modelo). La lista completa se carga por importación " +
        "desde la fuente que Hapag-Lloyd valide, con su licencia; el portal no reproduce el texto del Código IMDG.";

    public static string BuildSearchText(DangerousGood entry) => SearchText.Normalize(string.Join(' ',
        entry.UnNumber is null ? string.Empty : $"un{entry.UnNumber} {entry.UnNumber}",
        entry.ProperShippingNameEs,
        entry.ProperShippingNameEn,
        entry.Keywords ?? string.Empty));

    public static string? NormalizeUnNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var match = UnNumberPattern().Match(value.Trim());
        return match.Success ? match.Groups[1].Value : null;
    }

    public static DangerousGoodDto ToDto(DangerousGood d)
    {
        var name = DangerousGoodClasses.NameOf(d.HazardClass);
        return new DangerousGoodDto(
            d.Id,
            d.UnNumber is null ? null : $"UN{d.UnNumber}",
            d.ProperShippingNameEs,
            d.ProperShippingNameEn,
            d.HazardClass,
            name?.Es,
            name?.En,
            d.SubsidiaryRisk,
            d.PackingGroup,
            d.Notes,
            d.IsClassified,
            d.Source);
    }

    [GeneratedRegex(@"^(?:UN\s*)?(\d{4})$", RegexOptions.IgnoreCase)]
    private static partial Regex UnNumberPattern();
}

public sealed class SearchDangerousGoodsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<SearchDangerousGoodsQuery, DangerousGoodSearchResultDto>
{
    private const int MaxResults = 20;

    public async Task<Result<DangerousGoodSearchResultDto>> Handle(SearchDangerousGoodsQuery request, CancellationToken cancellationToken)
    {
        var query = request.Query.Trim();
        var normalized = SearchText.Normalize(query);
        var entries = dbContext.DangerousGoods.AsNoTracking().Where(d => d.IsActive);

        List<DangerousGood> found;
        var unNumber = DangerousGoodCatalog.NormalizeUnNumber(query);
        if (unNumber is not null)
        {
            found = await entries.Where(d => d.UnNumber == unNumber).ToListAsync(cancellationToken);
        }
        else
        {
            var filtered = entries;
            foreach (var word in SearchText.Words(query, minLength: 2))
                filtered = filtered.Where(d => d.SearchText.Contains(word));

            found = (await filtered.Take(MaxResults * 5).ToListAsync(cancellationToken))
                .OrderBy(d => Rank(d, normalized))
                .ThenBy(d => d.UnNumber)
                .ThenBy(d => d.ProperShippingNameEs)
                .ToList();
        }

        var items = found.Take(MaxResults).Select(DangerousGoodCatalog.ToDto).ToList();
        var classified = items.Any(i => i.Classified);
        var (code, message) = items.Count == 0
            ? (DangerousGoodResultCodes.NoMatch,
                $"La carga consultada («{query}») no tiene clasificación de mercancía peligrosa en la base de referencia del portal. " +
                "Verifique el nombre o el número ONU y, ante dudas, consulte a Hapag-Lloyd antes de declarar la carga.")
            : classified
                ? (DangerousGoodResultCodes.Classified,
                    $"Se encontraron {items.Count(i => i.Classified)} clasificaciones de mercancía peligrosa para «{query}».")
                : (DangerousGoodResultCodes.NotClassified,
                    $"La carga consultada («{query}») no está clasificada como mercancía peligrosa según la base de referencia.");

        return Result<DangerousGoodSearchResultDto>.Success(new DangerousGoodSearchResultDto(
            query, code, classified, message, items, DangerousGoodCatalog.Disclaimer, DangerousGoodCatalog.DataSource));
    }

    /// <summary>0 = nombre exacto, 1 = nombre que empieza con la consulta, 2 = el resto.</summary>
    private static int Rank(DangerousGood entry, string normalized)
    {
        var es = SearchText.Normalize(entry.ProperShippingNameEs);
        var en = SearchText.Normalize(entry.ProperShippingNameEn);
        if (es == normalized || en == normalized)
            return 0;
        return es.StartsWith(normalized, StringComparison.Ordinal) || en.StartsWith(normalized, StringComparison.Ordinal) ? 1 : 2;
    }
}

public sealed class ImportDangerousGoodsCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<ImportDangerousGoodsCommand, DangerousGoodImportResultDto>
{
    private const int MaxRows = 20000;
    private const int MaxReportedErrors = 100;
    private static readonly string[] RequiredColumns = ["unNumber", "nameEs", "nameEn", "class"];
    private static readonly string[] PackingGroups = ["I", "II", "III"];

    private sealed record DangerousGoodSnapshot(
        string? UnNumber, string NameEs, string NameEn, string? HazardClass, string? SubsidiaryRisk,
        string? PackingGroup, string? Notes, string? Keywords, bool IsClassified, string Source, bool IsActive)
    {
        public static DangerousGoodSnapshot From(DangerousGood d) => new(
            d.UnNumber, d.ProperShippingNameEs, d.ProperShippingNameEn, d.HazardClass, d.SubsidiaryRisk, d.PackingGroup,
            d.Notes, d.Keywords, d.IsClassified, d.Source, d.IsActive);
    }

    public async Task<Result<DangerousGoodImportResultDto>> Handle(ImportDangerousGoodsCommand request, CancellationToken cancellationToken)
    {
        var lines = request.Content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var headerIndex = Array.FindIndex(lines, l => !string.IsNullOrWhiteSpace(l));
        if (headerIndex < 0)
            return Result<DangerousGoodImportResultDto>.Failure(DomainErrors.DangerousGood.InvalidImport("the file is empty."));

        var separator = lines[headerIndex].Contains(';') ? ';' : ',';
        var header = ParseLine(lines[headerIndex].TrimStart('﻿'), separator)
            .Select(h => h.Trim())
            .ToList();
        int Column(string name) => header.FindIndex(h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));

        var missing = RequiredColumns.Where(c => Column(c) < 0).ToList();
        if (missing.Count > 0)
            return Result<DangerousGoodImportResultDto>.Failure(DomainErrors.DangerousGood.InvalidImport($"missing columns: {string.Join(", ", missing)}."));

        var rows = lines.Skip(headerIndex + 1).Select((text, index) => (Line: headerIndex + index + 2, Text: text))
            .Where(r => !string.IsNullOrWhiteSpace(r.Text))
            .ToList();
        if (rows.Count > MaxRows)
            return Result<DangerousGoodImportResultDto>.Failure(DomainErrors.DangerousGood.InvalidImport($"more than {MaxRows} rows."));

        var existing = await dbContext.DangerousGoods.ToListAsync(cancellationToken);
        var byKey = existing
            .GroupBy(d => Key(d.UnNumber, d.ProperShippingNameEn))
            .ToDictionary(g => g.Key, g => g.First());

        var errors = new List<DangerousGoodImportErrorDto>();
        int created = 0, updated = 0, skipped = 0;
        var now = DateTime.UtcNow;

        foreach (var (line, text) in rows)
        {
            var cells = ParseLine(text, separator);
            string? Cell(string name)
            {
                var index = Column(name);
                return index >= 0 && index < cells.Count && !string.IsNullOrWhiteSpace(cells[index]) ? cells[index].Trim() : null;
            }

            var classifiedText = Cell("classified");
            var hazardClass = Cell("class");
            var isClassified = classifiedText is null ? hazardClass is not null : bool.TryParse(classifiedText, out var c) && c;
            var unNumber = DangerousGoodCatalog.NormalizeUnNumber(Cell("unNumber"));
            var nameEs = Cell("nameEs");
            var nameEn = Cell("nameEn");
            var packingGroup = Cell("packingGroup")?.ToUpperInvariant();

            var reason =
                nameEs is null || nameEn is null ? "nameEs and nameEn are required" :
                nameEs.Length > 300 || nameEn.Length > 300 ? "names must be at most 300 characters" :
                isClassified && unNumber is null ? "unNumber must have 4 digits (e.g. 1203 or UN1203)" :
                isClassified && !DangerousGoodClasses.IsValid(hazardClass) ? $"unknown class '{hazardClass}'" :
                packingGroup is not null && !PackingGroups.Contains(packingGroup) ? "packingGroup must be I, II or III" :
                null;

            if (reason is not null)
            {
                skipped++;
                if (errors.Count < MaxReportedErrors)
                    errors.Add(new DangerousGoodImportErrorDto(line, reason));
                continue;
            }

            var key = Key(unNumber, nameEn!);
            byKey.TryGetValue(key, out var entry);
            var previous = entry is null ? null : DangerousGoodSnapshot.From(entry);

            entry ??= new DangerousGood
            {
                ProperShippingNameEs = nameEs!,
                ProperShippingNameEn = nameEn!,
                Source = DangerousGoodSources.Import,
                SearchText = string.Empty,
                CreatedAt = now,
                CreatedBy = currentUserService.Email ?? "system"
            };

            entry.UnNumber = unNumber;
            entry.ProperShippingNameEs = nameEs!;
            entry.ProperShippingNameEn = nameEn!;
            entry.HazardClass = isClassified ? hazardClass : null;
            entry.SubsidiaryRisk = Cell("subsidiaryRisk");
            entry.PackingGroup = packingGroup;
            entry.Notes = Cell("notes");
            entry.Keywords = Cell("keywords");
            entry.IsClassified = isClassified;
            entry.Source = DangerousGoodSources.Import;
            entry.IsActive = true;
            entry.SearchText = DangerousGoodCatalog.BuildSearchText(entry);

            var current = DangerousGoodSnapshot.From(entry);
            if (previous is null)
            {
                dbContext.DangerousGoods.Add(entry);
                byKey[key] = entry;
                created++;
            }
            else if (previous == current)
            {
                continue;
            }
            else
            {
                updated++;
            }

            MaintainerChangeLogger.Log(
                dbContext, currentUserService, MaintainerNames.DangerousGood, entry.Id,
                previous is null ? MaintainerActions.Created : MaintainerActions.Updated,
                previous, current, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<DangerousGoodImportResultDto>.Success(new DangerousGoodImportResultDto(rows.Count, created, updated, skipped, errors));
    }

    private static string Key(string? unNumber, string nameEn) => $"{unNumber}|{SearchText.Normalize(nameEn)}";

    /// <summary>Separa una línea CSV respetando campos entre comillas dobles ("" = comilla literal).</summary>
    private static List<string> ParseLine(string line, char separator)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (ch == '"')
                {
                    quoted = false;
                }
                else
                {
                    current.Append(ch);
                }
            }
            else if (ch == '"')
            {
                quoted = true;
            }
            else if (ch == separator)
            {
                cells.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        cells.Add(current.ToString());
        return cells;
    }
}
