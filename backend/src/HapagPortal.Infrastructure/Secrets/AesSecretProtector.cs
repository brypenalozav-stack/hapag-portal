using System.Security.Cryptography;
using System.Text;
using HapagPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace HapagPortal.Infrastructure.Secrets;

/// <summary>
/// Cifrado AES-GCM con clave maestra de configuración (`Secrets:MasterKey`, base64 de 32 bytes,
/// provista por variable de entorno). Formato almacenado: base64(nonce | tag | ciphertext).
/// </summary>
public sealed class AesSecretProtector(IConfiguration configuration) : ISecretProtector
{
    private const int NonceSize = 12; // AES-GCM estándar
    private const int TagSize = 16;

    private byte[] GetKey()
    {
        var raw = configuration["Secrets:MasterKey"];
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Secrets:MasterKey is not configured.");

        var key = Convert.FromBase64String(raw);
        if (key.Length != 32)
            throw new InvalidOperationException("Secrets:MasterKey must be 32 bytes (base64).");

        return key;
    }

    public string Protect(string plaintext)
    {
        var key = GetKey();
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        var output = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, output, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, output, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, output, NonceSize + TagSize, cipher.Length);

        return Convert.ToBase64String(output);
    }

    public string Unprotect(string protectedValue)
    {
        var key = GetKey();
        var data = Convert.FromBase64String(protectedValue);

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipher = new byte[data.Length - NonceSize - TagSize];
        Buffer.BlockCopy(data, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(data, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(data, NonceSize + TagSize, cipher, 0, cipher.Length);

        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }
}
