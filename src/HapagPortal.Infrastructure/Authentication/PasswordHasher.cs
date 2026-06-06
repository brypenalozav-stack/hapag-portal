using HapagPortal.Application.Auth.Common;

namespace HapagPortal.Infrastructure.Authentication;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int BcryptWorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: BcryptWorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
