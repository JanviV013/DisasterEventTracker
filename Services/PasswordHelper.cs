using System.Security.Cryptography;

namespace DisasterEventTracker.Services;

/// <summary>
/// Password hashing with PBKDF2 (built into .NET). Format stored in the database:
/// iterations.salt.hash  (salt and hash are Base64)
/// </summary>
public static class PasswordHelper
{
    private const int SaltSize = 16;        // 128 bit random salt per password
    private const int KeySize = 32;         // 256 bit hash
    private const int Iterations = 210_000; // slows down brute force attacks

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            // Constant-time comparison so timing cannot reveal anything
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
