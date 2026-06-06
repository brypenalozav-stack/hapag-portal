using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class User : BaseAuditableEntity
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string UserType { get; set; }
    public required string Country { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    public string? EmailConfirmationToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public Guid? ClientId { get; set; }

    public Client? Client { get; set; }
    public ICollection<UserRole> Roles { get; set; } = [];
}
