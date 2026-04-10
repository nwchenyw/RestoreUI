using System.Text.Json;

namespace RestoreSystem.Core;

public sealed class DeferredDeletionQueueManager
{
    private readonly string _queuePath;

    public DeferredDeletionQueueManager()
    {
        _queuePath = Path.Combine(ConfigManager.GetRootPath(), "deferred-delete-queue.json");
    }

    public void Enqueue(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var queue = Load();
        if (!queue.Contains(path, StringComparer.OrdinalIgnoreCase))
            queue.Add(path);

        Save(queue);
    }

    public IReadOnlyList<string> LoadQueue() => Load();

    public void ProcessQueue()
    {
        var queue = Load();
        if (queue.Count == 0)
            return;

        var remaining = new List<string>();
        foreach (var path in queue)
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

    private List<string> Load()
    {
        if (!File.Exists(_queuePath))
            return new List<string>();

        var json = File.ReadAllText(_queuePath);
        return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }

    private void Save(List<string> queue)
    {
        var json = JsonSerializer.Serialize(queue, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_queuePath, json);
    }
}
