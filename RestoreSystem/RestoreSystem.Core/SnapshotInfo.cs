namespace RestoreSystem.Core;

public sealed class SnapshotInfo
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string DiffPath { get; set; } = string.Empty;
    public string ParentPath { get; set; } = string.Empty;
}
