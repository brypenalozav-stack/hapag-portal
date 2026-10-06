using HapagPortal.Domain.Results;

namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Registro de contactos y listas de distribución de reportes de Hapag-Lloyd (P0060, CT-CONTACTS, M1-06): lee los correos
/// registrados por tipo de reporte para una cuenta (Match Code) y propaga los cambios que hace el cliente. Las fallas
/// del sistema externo vuelven como <c>DomainErrors.Integration.*</c>.
/// </summary>
public interface IContactListProvider
{
    Task<Result<IReadOnlyList<ContactDistributionList>>> GetListsAsync(
        string matchCode,
        string country,
        CancellationToken cancellationToken = default);

    Task<Result<ContactListUpdateResult>> UpdateListAsync(
        string matchCode,
        string country,
        string reportType,
        IReadOnlyList<string> emails,
        string updatedBy,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

/// <summary>Correos registrados para un tipo de reporte (<c>ContactReportTypes</c>).</summary>
public sealed record ContactDistributionList(
    string ReportType,
    IReadOnlyList<string> Emails,
    DateTime? UpdatedAt,
    string? UpdatedBy);

/// <summary>Lista tal como quedó en el origen y su referencia del cambio.</summary>
public sealed record ContactListUpdateResult(
    ContactDistributionList List,
    string SourceReference);
