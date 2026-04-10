namespace RestoreSystem.Core;

public sealed class VhdManager
{
    private readonly string _basePath;
    private readonly string _diffPath;
    private readonly string _rootPath;

    public VhdManager()
    {
        _rootPath = ConfigManager.GetRootPath();
        _basePath = Path.Combine(_rootPath, "base.vhdx");
        _diffPath = Path.Combine(_rootPath, "diff.vhdx");
    }

    public string BasePath => _basePath;
    public string DiffPath => _diffPath;

    public void EnsureFolder()
    {
        if (!Directory.Exists(_rootPath))
            Directory.CreateDirectory(_rootPath);
    }

    public bool Exists() => File.Exists(_diffPath);

    public bool BaseExists() => File.Exists(_basePath);

    public void EnsureBitLockerCompatibleVolume()
    {
        var drive = new DriveInfo("C");
        if (!drive.IsReady)
            throw new InvalidOperationException("系統磁碟尚未就緒。\n");

        if (!string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("僅支援 NTFS 掛載磁碟。\n");
    }

    public bool CreateBaseIfMissing(int sizeGb = 64)
    {
        EnsureFolder();
        if (BaseExists())
            return false;

        CommandRunner.RunPowerShell($"New-VHD -Path '{_basePath}' -SizeBytes {sizeGb}GB -Dynamic");
        return true;
    }

    public void CreateDiff()
    {
        EnsureFolder();
        if (!BaseExists())
            throw new InvalidOperationException($"缺少 base.vhdx：{_basePath}");

        if (Exists())
            return;

        CommandRunner.RunPowerShell($"New-VHD -Path '{_diffPath}' -ParentPath '{_basePath}' -Differencing");
    }

    public void Mount()
    {
        if (!Exists())
            CreateDiff();

        CommandRunner.RunPowerShell($"Mount-VHD '{_diffPath}'");
    }

    public void Unmount()
    {
        if (!Exists())
            return;

        CommandRunner.RunPowerShell($"Dismount-VHD '{_diffPath}'");
    }

    public void DeleteDiff()
    {
        if (!Exists())
            return;

        try { Unmount(); } catch { }
        CommandRunner.RunPowerShell($"Remove-Item '{_diffPath}' -Force");
    }
}
