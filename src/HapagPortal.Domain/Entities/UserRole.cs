using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class UserRole : GuidEntity
{
    public required string RoleName { get; set; }
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
}
