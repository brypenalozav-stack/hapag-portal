using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Configuración general de acceso abierto por número de BL de una organización (M1-17): cuando está
/// activa, cualquier usuario autenticado que ingrese el número de un BL de su propiedad ve lo que
/// habilita el único conjunto de permisos (M1-15).
/// </summary>
public sealed class OpenAccessSetting : BaseAuditableEntity
{
    public Guid ClientId { get; set; }
    public bool IsEnabled { get; set; }

    /// <summary>Nulo = nivel base de M1-11 de quien consulta.</summary>
    public string? ActionCodes { get; set; }

    public DateTime? ChangedAt { get; set; }
    public Guid? ChangedByUserId { get; set; }

    public Client Client { get; set; } = null!;
}
