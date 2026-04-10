namespace RestoreSystem.Core;

public static class OperationLogger
{
    private static readonly object Sync = new();
    private static readonly string LogPath = @"C:\RestoreSystem\logs.txt";

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message) => Write("ERROR", message);

    public static void Error(string message, Exception ex) => Write("ERROR", $"{message} | {ex}");

    private static void Write(string level, string message)
    {
        lock (Sync)
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
    }
}
