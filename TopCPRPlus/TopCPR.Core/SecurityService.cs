using System.Security.Cryptography;
using System.Text;

namespace TopCPR.Core;

public static class SecurityService
{
    public static string GenerateSalt()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(bytes);
    }

    public static string HashPassword(string password, string salt)
    {
        var payload = (password ?? string.Empty) + "::" + (salt ?? string.Empty);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    public static bool VerifyPassword(string password, string salt, string expectedHash)
    {
        return string.Equals(HashPassword(password, salt), expectedHash, StringComparison.Ordinal);
    }

    public static string CreateAuthToken(string password, string salt)
    {
        return HashPassword("TOKEN:" + (password ?? string.Empty), salt ?? string.Empty);
    }
}
