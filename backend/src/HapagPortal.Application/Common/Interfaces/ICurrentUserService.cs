namespace HapagPortal.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Country { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}
