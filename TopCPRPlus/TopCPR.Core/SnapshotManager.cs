using System.Text.Json;

namespace TopCPR.Core;

public sealed class SnapshotManager
{
    private readonly VHDXManager _vhd;
    private readonly DeferredDeletionQueue _queue;
    private readonly string _metaPath = Path.Combine(AppPaths.SnapshotRoot, "snapshots.json");

    public SnapshotManager(VHDXManager vhd, DeferredDeletionQueue queue)
    {
        _vhd = vhd;
        _queue = queue;
    }

    public SnapshotInfo CreateSnapshot()
    {
        ConfigManager.EnsureFolders();
        Directory.CreateDirectory(AppPaths.SnapshotRoot);

        var snapshots = LoadSnapshots();
        var parent = snapshots.Count > 0 ? snapshots[^1].DiffPath : AppPaths.BaseVhd;
        var id = DateTime.Now.ToString("yyyyMMddHHmmss");
        var folder = Path.Combine(AppPaths.SnapshotRoot, id);
        var diff = Path.Combine(folder, "diff.vhdx");

        Directory.CreateDirectory(folder);
        PowerShellRunner.Run($"New-VHD -Path '{diff}' -ParentPath '{parent}' -Differencing");

        var item = new SnapshotInfo { Id = id, CreatedAt = DateTime.Now, DiffPath = diff, ParentPath = parent };
        snapshots.Add(item);
        SaveSnapshots(snapshots);
        return item;
    }

    public IReadOnlyList<SnapshotInfo> ListSnapshots() => LoadSnapshots().OrderByDescending(x => x.CreatedAt).ToList();

    public SnapshotInfo RestoreSnapshot(string id)
    {
        var item = LoadSnapshots().FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidOperationException("快照不存在。\n");

        _vhd.DiscardChanges();
        File.Copy(item.DiffPath, AppPaths.DiffVhd, true);
        _vhd.Attach();
        return item;
    }

    public void DeleteSnapshot(string id)
    {
        var list = LoadSnapshots();
        var item = list.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (item is null) return;

        if (list.Any(x => x.ParentPath.Equals(item.DiffPath, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("此快照仍被子鏈依賴，無法刪除。\n");

        var folder = Path.GetDirectoryName(item.DiffPath);
        if (!string.IsNullOrWhiteSpace(folder))
            _queue.Enqueue(folder);

        list.Remove(item);
        SaveSnapshots(list);
    }

    private List<SnapshotInfo> LoadSnapshots()
    {
        if (!File.Exists(_metaPath)) return new List<SnapshotInfo>();
        var json = File.ReadAllText(_metaPath);
        return JsonSerializer.Deserialize<List<SnapshotInfo>>(json) ?? new List<SnapshotInfo>();
    }

    private void SaveSnapshots(List<SnapshotInfo> list)
    {
        var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_metaPath, json);
    }
}
