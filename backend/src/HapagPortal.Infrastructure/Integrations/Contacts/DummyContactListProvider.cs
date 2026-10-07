using System.Collections.Concurrent;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Contacts;

/// <summary>
/// Registro de contactos simulado (CT-CONTACTS, P0060, M1-06). Trae listas fijas para las cuentas de demostración y
/// guarda en memoria los cambios (la misma clave de idempotencia no se aplica dos veces). Un Match Code que empieza
/// por <c>MCFAIL</c> simula que el registro no está disponible.
/// </summary>
public sealed class DummyContactListProvider(ILogger<DummyContactListProvider> logger) : IContactListProvider
{
    public const string FailurePrefix = "MCFAIL";

    private static readonly DateTime SeededAt = new(2026, 9, 15, 13, 0, 0, DateTimeKind.Utc);

    private readonly ConcurrentDictionary<(string MatchCode, string ReportType), ContactDistributionList> _lists = new(
    [
        // Importadora Demo SpA (MC100010)
        Seed("MC100010", ContactReportTypes.ArrivalNotice, "operaciones@importadorademo.cl", "demo@importadorademo.cl"),
        Seed("MC100010", ContactReportTypes.BlCopies, "documentos@importadorademo.cl"),
        Seed("MC100010", ContactReportTypes.Invoices, "finanzas@importadorademo.cl"),
        Seed("MC100010", ContactReportTypes.FreeTime, "operaciones@importadorademo.cl"),
        // Comercial Altiplano SRL (MC100020)
        Seed("MC100020", ContactReportTypes.ArrivalNotice, "demo@altiplano.bo"),
        Seed("MC100020", ContactReportTypes.Invoices, "contabilidad@altiplano.bo"),
        // Agencia Marítima del Pacífico (MC100030)
        Seed("MC100030", ContactReportTypes.ArrivalNotice, "agente@maritimpacifico.cl"),
        Seed("MC100030", ContactReportTypes.Demurrage, "agente@maritimpacifico.cl", "despachos@maritimpacifico.cl"),
    ]);

    private readonly ConcurrentDictionary<string, ContactListUpdateResult> _applied = new(StringComparer.Ordinal);

    public Task<Result<IReadOnlyList<ContactDistributionList>>> GetListsAsync(
        string matchCode,
        string country,
        CancellationToken cancellationToken = default)
    {
        if (matchCode.StartsWith(FailurePrefix, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(Result<IReadOnlyList<ContactDistributionList>>.Failure(DomainErrors.Integration.Unavailable("Contacts")));

        IReadOnlyList<ContactDistributionList> lists = _lists
            .Where(kv => kv.Key.MatchCode == matchCode.Trim().ToUpperInvariant())
            .Select(kv => kv.Value)
            .OrderBy(l => Array.IndexOf(ContactReportTypes.All, l.ReportType))
            .ToList();

        return Task.FromResult(Result<IReadOnlyList<ContactDistributionList>>.Success(lists));
    }

    public Task<Result<ContactListUpdateResult>> UpdateListAsync(
        string matchCode,
        string country,
        string reportType,
        IReadOnlyList<string> emails,
        string updatedBy,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (matchCode.StartsWith(FailurePrefix, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(Result<ContactListUpdateResult>.Failure(DomainErrors.Integration.Unavailable("Contacts")));

        var result = _applied.GetOrAdd(idempotencyKey, key =>
        {
            var list = new ContactDistributionList(reportType, emails.ToList(), DateTime.UtcNow, updatedBy);
            _lists[(matchCode.Trim().ToUpperInvariant(), reportType)] = list;
            return new ContactListUpdateResult(list, $"P0060-{(key.Length > 8 ? key[^8..] : key).ToUpperInvariant()}");
        });

        logger.LogDebug("Contacts (dummy) - MatchCode: {MatchCode}, Report: {ReportType}, Emails: {Count}", matchCode, reportType, emails.Count);
        return Task.FromResult(Result<ContactListUpdateResult>.Success(result));
    }

    private static KeyValuePair<(string, string), ContactDistributionList> Seed(string matchCode, string reportType, params string[] emails) =>
        new((matchCode, reportType), new ContactDistributionList(reportType, emails, SeededAt, "P0060"));
}
