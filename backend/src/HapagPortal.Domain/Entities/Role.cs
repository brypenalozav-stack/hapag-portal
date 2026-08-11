using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Rol del sistema (Administrador, Coordinador, Supervisor, ExternalApi, y los de cliente).
/// Reemplaza progresivamente el string suelto de <see cref="UserRole.RoleName"/>.
/// </summary>
public sealed class Role : BaseAuditableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsSystem { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
