using System.Security.Cryptography;

namespace Foodprint.Core.Auth;

/// <summary>Opaque tokens for registration links and sessions: 256 bits, stored only as a SHA-256 hash.</summary>
public static class Tokens
{
    public static string Generate() => Base64Url(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(rawToken));
        return Convert.ToHexStringLower(bytes);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
