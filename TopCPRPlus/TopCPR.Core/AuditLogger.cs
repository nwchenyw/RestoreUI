namespace TopCPR.Core;

public static class AuditLogger
{
    private static readonly object Sync = new();

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message, Exception? ex = null) => Write("ERROR", ex is null ? message : message + " | " + ex);

    private static void Write(string level, string message)
    {
        lock (Sync)
        {
            ConfigManager.EnsureFolders();
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            File.AppendAllText(AppPaths.LogPath, line + Environment.NewLine);
        }
    }
}
