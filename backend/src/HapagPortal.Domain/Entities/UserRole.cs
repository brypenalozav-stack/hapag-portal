using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class UserRole : GuidEntity
{
    public required string RoleName { get; set; }
    // Vínculo aditivo a la entidad Role (la migración lo puebla desde RoleName).
    // Nullable durante la transición para no romper el seed/tests actuales.
    public Guid? RoleId { get; set; }
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
    public Role? Role { get; set; }
}
