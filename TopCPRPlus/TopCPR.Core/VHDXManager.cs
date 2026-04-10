namespace TopCPR.Core;

public sealed class VHDXManager
{
    public bool DiffExists() => File.Exists(AppPaths.DiffVhd);
    public bool BaseExists() => File.Exists(AppPaths.BaseVhd);

    public void EnsureBitLockerSafeMode()
    {
        var drive = new DriveInfo("C");
        if (!drive.IsReady)
            throw new InvalidOperationException("C 磁碟未就緒。\n");

        if (!string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("僅支援 NTFS。\n");
    }

    public bool CreateBaseIfMissing()
    {
        ConfigManager.EnsureFolders();
        if (BaseExists()) return false;
        PowerShellRunner.Run($"New-VHD -Path '{AppPaths.BaseVhd}' -SizeBytes 64GB -Dynamic");
        return true;
    }

    public void CreateDiff()
    {
        if (!BaseExists())
            throw new InvalidOperationException("缺少 base.vhdx。\n");

        if (DiffExists())
            return;

        PowerShellRunner.Run($"New-VHD -Path '{AppPaths.DiffVhd}' -ParentPath '{AppPaths.BaseVhd}' -Differencing");
    }

    public void Attach()
    {
        if (!DiffExists())
            CreateDiff();

        PowerShellRunner.Run($"Mount-VHD '{AppPaths.DiffVhd}'");
    }

    public void Detach()
    {
        if (!DiffExists()) return;
        PowerShellRunner.Run($"Dismount-VHD '{AppPaths.DiffVhd}'");
    }

    public void DiscardChanges()
    {
        if (!DiffExists()) return;
        try { Detach(); } catch { }
        PowerShellRunner.Run($"Remove-Item '{AppPaths.DiffVhd}' -Force");
    }

    public void OptimizeIfAvailable()
    {
        if (!DiffExists()) return;
        try { PowerShellRunner.Run($"Optimize-VHD -Path '{AppPaths.DiffVhd}' -Mode Full"); } catch { }
    }
}
