using System.Security.Cryptography;

interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string? storedHash);
}

sealed class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(Derive(password, salt))}";
    }

    public bool Verify(string password, string? storedHash)
    {
        // Perform the derivation even for unknown accounts to avoid a fast failure path.
        var parts = storedHash?.Split(':');
        var salt = parts is null ? new byte[16] : Convert.FromBase64String(parts[0]);
        var actual = Derive(password, salt);
        return parts is not null && CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(parts[1]));
    }

    private static byte[] Derive(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
}
