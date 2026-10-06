using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Cambio de la lista de distribución de un tipo de reporte hecho por el cliente (M1-06), append-only: correos
/// anteriores y nuevos, quién y cuándo, y el resultado de la propagación al registro de contactos (P0060).
/// </summary>
public sealed class ContactListChange : GuidEntity
{
    public Guid OrganizationId { get; set; }
    public required string ReportType { get; set; }
    public string? PreviousEmails { get; set; }
    public required string NewEmails { get; set; }

    /// <summary><c>ContactListChangeStatus</c>: Propagated o Failed.</summary>
    public required string Status { get; set; }

    public string? SourceReference { get; set; }
    public string? ErrorCode { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public required string ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
}
