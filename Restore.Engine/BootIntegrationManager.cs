using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Restore.Engine
{
    public class BootIntegrationManager
    {
        public BootIntegrationManager()
        {
        }

        public string EnsureBootEntry(string existingGuid)
        {
            string guid = string.IsNullOrWhiteSpace(existingGuid) ? null : existingGuid.Trim();

            if (string.IsNullOrWhiteSpace(guid))
            {
                string createOutput = RunCmd("bcdedit /copy {current} /d \"RestoreSystem VHD Mode\"");
                guid = ExtractGuid(createOutput);
                if (string.IsNullOrEmpty(guid))
                    throw new InvalidOperationException("無法建立開機項目。" + Environment.NewLine + createOutput);
            }

            ConfigureVhdEntry(guid);
            return guid;
        }

        public void ConfigureVhdEntry(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException("開機項目 GUID 不可為空。\n");

            string vhdPath = "vhd=[C:]\\RestoreSystem\\diff.vhdx";
            RunCmd("bcdedit /set " + guid + " device " + vhdPath);
            RunCmd("bcdedit /set " + guid + " osdevice " + vhdPath);
            RunCmd("bcdedit /set " + guid + " detecthal on");
        }

        public void SetDefaultBootEntry(string entryGuid)
        {
            if (string.IsNullOrWhiteSpace(entryGuid))
                return;

            RunCmd("bcdedit /default " + entryGuid);
            RunCmd("bcdedit /timeout 3");
        }

        public void SetDefaultToCurrent()
        {
            RunCmd("bcdedit /default {current}");
            RunCmd("bcdedit /timeout 3");
        }

        private string ExtractGuid(string text)
        {
            var match = Regex.Match(text ?? string.Empty, "\\{[0-9a-fA-F-]+\\}");
            return match.Success ? match.Value : null;
        }

        private string RunCmd(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c " + arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException("執行失敗：" + arguments + Environment.NewLine + error);
                return output;
            }
        }
    }
}
