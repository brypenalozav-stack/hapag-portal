using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class User : BaseAuditableEntity
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string UserType { get; set; }
    public required string Country { get; set; }
    // Perfil visible en la consola de usuarios (imagen). Nullable para no romper el seed/tests actuales.
    public int? DisplayId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationTokenExpiry { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public Guid? ClientId { get; set; }
    // Vinculación a la organización (M1-08). Pending = solicitud por aprobar; no accede a la información.
    public string MembershipStatus { get; set; } = Constants.MembershipStatus.Active;
    public DateTime? MembershipDecidedAt { get; set; }
    public string? MembershipDecidedBy { get; set; }

    public Client? Client { get; set; }
    public ICollection<UserRole> Roles { get; set; } = [];
}
