namespace TopCPR.Core;

public static class AppPaths
{
    public const string Root = @"C:\TopCPR";
    public static readonly string BaseVhd = Path.Combine(Root, "base.vhdx");
    public static readonly string DiffVhd = Path.Combine(Root, "diff.vhdx");
    public static readonly string SnapshotRoot = Path.Combine(Root, "snapshots");
    public static readonly string ConfigPath = Path.Combine(Root, "config.json");
    public static readonly string LogPath = Path.Combine(Root, "logs", "topcpr.log");
    public static readonly string QueuePath = Path.Combine(Root, "delete-queue.json");
}
