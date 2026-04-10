using System.Text.Json;

namespace RestoreSystem.Core;

public sealed class SnapshotManager
{
    private readonly VhdManager _vhdManager;
    private readonly string _snapshotRoot;
    private readonly string _metadataPath;

    public SnapshotManager(VhdManager vhdManager)
    {
        _vhdManager = vhdManager;
        _snapshotRoot = Path.Combine(ConfigManager.GetRootPath(), "Snapshots");
        _metadataPath = Path.Combine(_snapshotRoot, "snapshots.json");
    }

    public SnapshotInfo CreateSnapshot()
    {
        EnsureFolders();

        if (!_vhdManager.BaseExists())
            throw new InvalidOperationException("缺少 base.vhdx，無法建立快照。\n");

        var snapshots = LoadMetadata();
        var parent = snapshots.Count > 0 ? snapshots[^1].DiffPath : _vhdManager.BasePath;
        var id = DateTime.Now.ToString("yyyyMMddHHmmss");
        var folder = Path.Combine(_snapshotRoot, id);
        var diffPath = Path.Combine(folder, "diff.vhdx");

        Directory.CreateDirectory(folder);
        CommandRunner.RunPowerShell($"New-VHD -Path '{diffPath}' -ParentPath '{parent}' -Differencing");

        var info = new SnapshotInfo
        {
            Id = id,
            CreatedAt = DateTime.Now,
            DiffPath = diffPath,
            ParentPath = parent
        };

        snapshots.Add(info);
        SaveMetadata(snapshots);
        return info;
    }

    public IReadOnlyList<SnapshotInfo> ListSnapshots()
    {
        EnsureFolders();
        return LoadMetadata().OrderByDescending(x => x.CreatedAt).ToList();
    }

    public SnapshotInfo RestoreSnapshot(string id)
    {
        var snapshots = LoadMetadata();
        var snapshot = snapshots.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
        if (snapshot is null)
            throw new InvalidOperationException("找不到指定快照。\n");

        if (!File.Exists(snapshot.DiffPath))
            throw new InvalidOperationException("快照檔遺失：" + snapshot.DiffPath);

        _vhdManager.DeleteDiff();
        File.Copy(snapshot.DiffPath, _vhdManager.DiffPath, true);
        _vhdManager.Mount();
        return snapshot;
    }

    public void DeleteSnapshot(string id, DeferredDeletionQueueManager queueManager)
    {
        var snapshots = LoadMetadata();
        var snapshot = snapshots.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
        if (snapshot is null)
            return;

        bool hasChildren = snapshots.Any(s => string.Equals(s.ParentPath, snapshot.DiffPath, StringComparison.OrdinalIgnoreCase));
        if (hasChildren)
            throw new InvalidOperationException("此快照為其他快照的父層，無法刪除。\n");

        var folder = Path.GetDirectoryName(snapshot.DiffPath);
        if (!string.IsNullOrWhiteSpace(folder))
            queueManager.Enqueue(folder);

        snapshots.Remove(snapshot);
        SaveMetadata(snapshots);
    }

    private void EnsureFolders()
    {
        _vhdManager.EnsureFolder();
        if (!Directory.Exists(_snapshotRoot))
            Directory.CreateDirectory(_snapshotRoot);
    }

    private List<SnapshotInfo> LoadMetadata()
    {
        if (!File.Exists(_metadataPath))
            return new List<SnapshotInfo>();

        var json = File.ReadAllText(_metadataPath);
        return JsonSerializer.Deserialize<List<SnapshotInfo>>(json) ?? new List<SnapshotInfo>();
    }

    private void SaveMetadata(List<SnapshotInfo> snapshots)
    {
        var json = JsonSerializer.Serialize(snapshots, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_metadataPath, json);
    }
}
