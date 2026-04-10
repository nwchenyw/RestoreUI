using System.Text.Json;

namespace RestoreSystem.Core;

public static class ConfigManager
{
    private static readonly string RootPath = @"C:\RestoreSystem";
    private static readonly string ConfigPath = Path.Combine(RootPath, "config.json");

    public static string GetRootPath() => RootPath;

    public static RestoreConfig Load()
    {
        if (!Directory.Exists(RootPath))
            Directory.CreateDirectory(RootPath);

        if (!File.Exists(ConfigPath))
        {
            var created = new RestoreConfig();
            Save(created);
            return created;
        }

        var json = File.ReadAllText(ConfigPath);
        var config = JsonSerializer.Deserialize<RestoreConfig>(json);
        return config ?? new RestoreConfig();
    }

    public static void Save(RestoreConfig config)
    {
        if (!Directory.Exists(RootPath))
            Directory.CreateDirectory(RootPath);

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}
