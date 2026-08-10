namespace HapagPortal.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? ClientId { get; }
    string? Email { get; }
    string? Country { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    bool HasPermission(string permission);
    bool IsAuthenticated { get; }
}
