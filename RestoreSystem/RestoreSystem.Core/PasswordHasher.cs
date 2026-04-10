using System.Security.Cryptography;
using System.Text;

namespace RestoreSystem.Core;

public static class PasswordHasher
{
    public static string Hash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
        return Convert.ToBase64String(bytes);
    }

    public static bool Verify(string input, string expectedHash)
    {
        return string.Equals(Hash(input), expectedHash ?? string.Empty, StringComparison.Ordinal);
    }
}
