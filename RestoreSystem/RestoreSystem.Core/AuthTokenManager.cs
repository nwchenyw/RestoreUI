namespace RestoreSystem.Core;

public static class AuthTokenManager
{
    public static string BuildToken(string password)
    {
        return PasswordHasher.Hash("RESTORE_TOKEN::" + (password ?? string.Empty));
    }

    public static bool ValidateToken(string token, RestoreConfig config)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (!string.IsNullOrWhiteSpace(config.AuthTokenHash))
            return string.Equals(token, config.AuthTokenHash, StringComparison.Ordinal);

        if (!string.IsNullOrWhiteSpace(config.PasswordHash))
            return string.Equals(token, config.PasswordHash, StringComparison.Ordinal);

        return false;
    }
}
