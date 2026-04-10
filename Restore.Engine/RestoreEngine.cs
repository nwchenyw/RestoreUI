using System;
using System.Diagnostics;
using System.IO;

namespace Restore.Engine
{
    public class RestoreEngine
    {
        private readonly string _restoreFolder;
        private readonly string _basePath;
        private readonly string _diffPath;

        public RestoreEngine(string protectDrive = "C")
        {
            _restoreFolder = protectDrive + @":\RestoreSystem";
            _basePath = Path.Combine(_restoreFolder, "base.vhdx");
            _diffPath = Path.Combine(_restoreFolder, "diff.vhdx");
        }

        public void CreateDiff()
        {
            if (!Directory.Exists(_restoreFolder))
                Directory.CreateDirectory(_restoreFolder);

            RunPowerShell("New-VHD -Path '" + _diffPath + "' -ParentPath '" + _basePath + "' -Differencing");
        }

        public void Mount()
        {
            RunPowerShell("Mount-VHD '" + _diffPath + "'");
        }

        public void Unmount()
        {
            RunPowerShell("Dismount-VHD '" + _diffPath + "'");
        }

        public void DeleteDiff()
        {
            if (File.Exists(_diffPath))
            {
                try { Unmount(); } catch { }
                RunPowerShell("Remove-Item '" + _diffPath + "' -Force");
            }
        }

        public bool DiffExists()
        {
            return File.Exists(_diffPath);
        }

        public bool BaseExists()
        {
            return File.Exists(_basePath);
        }

        private void RunPowerShell(string command)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + command + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException("PowerShell 命令失敗：" + error);
                }
            }
        }
    }
}
