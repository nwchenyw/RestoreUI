using System.Text.Json;

namespace TopCPR.Core;

public sealed class DeferredDeletionQueue
{
    public void Enqueue(string path)
    {
        var list = Load().ToList();
        if (!list.Contains(path, StringComparer.OrdinalIgnoreCase))
            list.Add(path);

        Save(list);
    }

    public void ProcessOnBoot()
    {
        var pending = Load().ToList();
        var remaining = new List<string>();

        foreach (var path in pending)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
                else if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                remaining.Add(path);
            }
        }

        Save(remaining);
    }

    private IReadOnlyList<string> Load()
    {
        if (!File.Exists(AppPaths.QueuePath)) return Array.Empty<string>();
        var json = File.ReadAllText(AppPaths.QueuePath);
        return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }

    private void Save(List<string> list)
    {
        ConfigManager.EnsureFolders();
        var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(AppPaths.QueuePath, json);
    }
}
