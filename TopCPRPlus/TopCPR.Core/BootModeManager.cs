using System.Text.RegularExpressions;

namespace TopCPR.Core;

public sealed class BootModeManager
{
    private static readonly Regex GuidRegex = new("\\{[0-9a-fA-F-]+\\}", RegexOptions.Compiled);

    public string EnsureNormalMode(string existingGuid)
    {
        return string.IsNullOrWhiteSpace(existingGuid)
            ? CreateEntry("Normal Mode")
            : existingGuid;
    }

    public string EnsureRestoreMode(string existingGuid)
    {
        var guid = string.IsNullOrWhiteSpace(existingGuid)
            ? CreateEntry("Restore Mode")
            : existingGuid;

        RunCmd($"bcdedit /set {guid} device vhd=[C:]\\TopCPR\\diff.vhdx");
        RunCmd($"bcdedit /set {guid} osdevice vhd=[C:]\\TopCPR\\diff.vhdx");
        RunCmd($"bcdedit /set {guid} detecthal on");
        return guid;
    }

    public void SetDefault(string guid)
    {
        if (string.IsNullOrWhiteSpace(guid)) return;
        RunCmd($"bcdedit /default {guid}");
    }

    public void SetDefaultCurrent() => RunCmd("bcdedit /default {current}");

    public void SetTimeout(int seconds)
    {
        if (seconds < 0) seconds = 0;
        if (seconds > 30) seconds = 30;
        RunCmd($"bcdedit /timeout {seconds}");
    }

    private string CreateEntry(string title)
    {
        var output = RunCmd($"bcdedit /copy {{current}} /d \"{title}\"");
        var match = GuidRegex.Match(output);
        if (!match.Success)
            throw new InvalidOperationException("建立開機項目失敗。\n" + output);

        return match.Value;
    }

    private static string RunCmd(string args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c " + args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var p = System.Diagnostics.Process.Start(psi) ?? throw new InvalidOperationException("無法啟動 CMD。\n");
        var output = p.StandardOutput.ReadToEnd();
        var error = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0) throw new InvalidOperationException(error);
        return output;
    }
}
