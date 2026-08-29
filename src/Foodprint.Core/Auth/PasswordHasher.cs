using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Foodprint.Core.Auth;

/// <summary>Argon2id password hashing. Encoded string carries all parameters and the salt.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string encoded);
}

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MemoryKb = 19 * 1024;
    private const int Iterations = 2;
    private const int Parallelism = 1;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derive(password, salt);
        return $"$argon2id$v=19$m={MemoryKb},t={Iterations},p={Parallelism}$" +
               $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string encoded)
    {
        try
        {
            var parts = encoded.Split('$', StringSplitOptions.RemoveEmptyEntries);
            // ["argon2id", "v=19", "m=..,t=..,p=..", salt, hash]
            if (parts.Length != 5 || parts[0] != "argon2id")
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[3]);
            var expected = Convert.FromBase64String(parts[4]);
            var actual = Derive(password, salt, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] Derive(string password, byte[] salt, int size = HashSize)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = MemoryKb,
            Iterations = Iterations,
            DegreeOfParallelism = Parallelism,
        };
        return argon2.GetBytes(size);
    }
}
