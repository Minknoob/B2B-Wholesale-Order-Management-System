using System.Security.Cryptography;

namespace B2B.Core;

/// <summary>PBKDF2-SHA256 password hashing. Stored format: pbkdf2$iterations$saltBase64$hashBase64.</summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DefaultIterations = 120_000;

    public static string HashPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, DefaultIterations,
            HashAlgorithmName.SHA256, HashSize);
        return $"pbkdf2${DefaultIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(storedHash)) return false;
        string[] parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2" ||
            !int.TryParse(parts[1], out int iterations) || iterations < 10_000) return false;
        try
        {
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expected = Convert.FromBase64String(parts[3]);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations,
                HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }
}
