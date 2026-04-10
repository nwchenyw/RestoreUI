namespace TopCPR.Core;

public sealed class AppConfig
{
    public bool RestoreModeEnabled { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string AuthTokenHash { get; set; } = string.Empty;
    public int SessionTimeoutMinutes { get; set; } = 10;
    public int BootTimeoutSeconds { get; set; } = 5;
    public string NormalBootGuid { get; set; } = string.Empty;
    public string RestoreBootGuid { get; set; } = string.Empty;
    public string CurrentSnapshotId { get; set; } = string.Empty;
}
