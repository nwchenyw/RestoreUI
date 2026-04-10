using System.Text.Json;

namespace TopCPR.Core;

public static class ConfigManager
{
    public static AppConfig Load()
    {
        EnsureFolders();

        if (!File.Exists(AppPaths.ConfigPath))
        {
            var config = new AppConfig();
            Save(config);
            return config;
        }

        var json = File.ReadAllText(AppPaths.ConfigPath);
        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }

    public static void Save(AppConfig config)
    {
        EnsureFolders();
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(AppPaths.ConfigPath, json);
    }

    public static void EnsureFolders()
    {
        Directory.CreateDirectory(AppPaths.Root);
        Directory.CreateDirectory(Path.Combine(AppPaths.Root, "logs"));
        Directory.CreateDirectory(AppPaths.SnapshotRoot);
    }
}
