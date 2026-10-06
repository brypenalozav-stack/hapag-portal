namespace HapagPortal.Application.WebService;

using System.Security.Cryptography;
using System.Text;

/// <summary>Clave nueva: el valor completo (se muestra una sola vez), su prefijo público y su SHA-256.</summary>
public sealed record GeneratedApiKey(string Key, string Prefix, string Hash);

/// <summary>
/// Claves del canal Web Service (M3-17, NF-09): <c>hlws_PREFIJO_SECRETO</c>, con un prefijo de 12 caracteres para
/// ubicar la clave sin revelarla y un secreto de 256 bits aleatorios (base64url). Solo se guarda el SHA-256 de la clave
/// completa, que alcanza por la entropía del secreto; la comparación es en tiempo constante.
/// </summary>
public static class ApiKeys
{
    public const string Scheme = "hlws";
    public const int PrefixLength = 12;
    private const string PrefixAlphabet = "abcdefghijkmnopqrstuvwxyz23456789";

    public static GeneratedApiKey Generate()
    {
        var prefix = new string(Enumerable.Range(0, PrefixLength)
            .Select(_ => PrefixAlphabet[RandomNumberGenerator.GetInt32(PrefixAlphabet.Length)])
            .ToArray());
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var key = $"{Scheme}_{prefix}_{secret}";
        return new GeneratedApiKey(key, prefix, Hash(key));
    }

    public static string Hash(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim()))).ToLowerInvariant();

    /// <summary>Prefijo de una clave con el formato del canal; nulo si no lo tiene.</summary>
    public static string? PrefixOf(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var parts = key.Trim().Split('_', 3);
        return parts.Length == 3 && parts[0] == Scheme && parts[1].Length == PrefixLength && parts[2].Length >= 32
            ? parts[1]
            : null;
    }

    /// <summary>Compara el hash de la clave presentada con el guardado, en tiempo constante.</summary>
    public static bool Matches(string presentedKey, string storedHash)
    {
        var presented = Encoding.ASCII.GetBytes(Hash(presentedKey));
        var stored = Encoding.ASCII.GetBytes(storedHash);
        return CryptographicOperations.FixedTimeEquals(presented, stored);
    }
}
