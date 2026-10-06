using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Casilla de correo a la que el asistente deriva por país y tema cuando no dispone de la respuesta o la
/// consulta está fuera de su alcance (M10-02). El tema <c>GENERAL</c> es la casilla por defecto del país.
/// </summary>
public sealed class AssistantMailbox : BaseAuditableEntity
{
    public required string Country { get; set; }
    public required string Topic { get; set; }
    public required string Email { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
