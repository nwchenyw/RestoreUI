namespace RestoreSystem.Core;

public sealed class RestoreConfig
{
    public bool Enabled { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string AuthTokenHash { get; set; } = string.Empty;
    public string ProtectDrive { get; set; } = "C";
    public string DataDrive { get; set; } = "D";
    public string BootEntryGuid { get; set; } = string.Empty;
    public string NormalBootEntryGuid { get; set; } = string.Empty;
    public string CurrentSnapshotId { get; set; } = string.Empty;
    public bool AdminModeEnabled { get; set; }
    public int SessionTimeoutMinutes { get; set; } = 10;
    public int BootTimeoutSeconds { get; set; } = 5;
}
