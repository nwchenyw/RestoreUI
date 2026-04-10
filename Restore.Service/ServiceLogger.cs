using System;
using System.IO;

namespace Restore.Service
{
    internal static class ServiceLogger
    {
        private static readonly object Sync = new object();
        private static readonly string LogDir = @"C:\RestoreSystem\Logs";
        private static readonly string LogPath = Path.Combine(LogDir, "service.log");

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string message, Exception ex = null)
        {
            Write("ERROR", message + (ex == null ? string.Empty : (" | " + ex)));
        }

        private static void Write(string level, string message)
        {
            lock (Sync)
            {
                if (!Directory.Exists(LogDir))
                    Directory.CreateDirectory(LogDir);

                string line = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}", DateTime.Now, level, message);
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
        }
    }
}
