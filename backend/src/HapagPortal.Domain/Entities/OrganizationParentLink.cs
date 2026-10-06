using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Asociación de una organización (filial) con su empresa matriz (M1-21). La pide el administrador de la filial y la
/// aprueba Hapag-Lloyd; con el vínculo activo, la filial decide si su matriz ve sus BL (<see cref="VisibilityEnabled"/>).
/// Cada paso queda en la auditoría de accesos (M1-23).
/// </summary>
public sealed class OrganizationParentLink : GuidEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ParentOrganizationId { get; set; }

    /// <summary><c>ParentLinkStatus</c>: Pending, Active, Rejected o Removed.</summary>
    public required string Status { get; set; }

    public bool VisibilityEnabled { get; set; }
    public DateTime? VisibilityChangedAt { get; set; }
    public string? VisibilityChangedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public required string RequestedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }
    public string? DecisionNotes { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? EndedBy { get; set; }
}
