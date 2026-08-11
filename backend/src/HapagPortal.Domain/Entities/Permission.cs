using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Permiso atómico (p. ej. users.manage, customs.transmit) asignable a roles.
/// </summary>
public sealed class Permission : GuidEntity
{
    public required string Code { get; set; }
    public string? Description { get; set; }
}
