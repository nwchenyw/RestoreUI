using System.Text.RegularExpressions;

namespace RestoreSystem.Core;

public sealed class BootManager
{
    private static readonly Regex GuidRegex = new("\\{[0-9a-fA-F-]+\\}", RegexOptions.Compiled);

    public string EnsureNormalModeEntry(string existingGuid)
    {
        return string.IsNullOrWhiteSpace(existingGuid)
            ? CreateEntry("Normal Mode")
            : existingGuid.Trim();
    }

    public string EnsureRestoreModeEntry(string existingGuid)
    {
        var guid = string.IsNullOrWhiteSpace(existingGuid) ? CreateEntry("Restore Mode") : existingGuid.Trim();

        CommandRunner.RunCmd($"bcdedit /set {guid} device vhd=[C:]\\RestoreSystem\\diff.vhdx");
        CommandRunner.RunCmd($"bcdedit /set {guid} osdevice vhd=[C:]\\RestoreSystem\\diff.vhdx");
        CommandRunner.RunCmd($"bcdedit /set {guid} detecthal on");

        return guid;
    }

    public void SetDefaultTo(string guid)
    {
        if (string.IsNullOrWhiteSpace(guid))
            return;

        CommandRunner.RunCmd($"bcdedit /default {guid}");
    }

    public void SetBootTimeout(int seconds)
    {
        if (seconds < 0) seconds = 0;
        if (seconds > 30) seconds = 30;
        CommandRunner.RunCmd($"bcdedit /timeout {seconds}");
    }

    public void SetDefaultCurrent()
    {
        CommandRunner.RunCmd("bcdedit /default {current}");
    }

    private string CreateEntry(string title)
    {
        var output = CommandRunner.RunCmd($"bcdedit /copy {{current}} /d \"{title}\"");
        var match = GuidRegex.Match(output ?? string.Empty);
        if (!match.Success)
            throw new InvalidOperationException("建立開機項目失敗。\n" + output);

        return match.Value;
    }
}
