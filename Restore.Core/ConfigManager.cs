using System;
using System.IO;
using System.Web.Script.Serialization;

namespace Restore.Core
{
    public static class ConfigManager
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "RestoreSystem");

        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

        public static RestoreConfig Load()
        {
            if (!File.Exists(ConfigPath))
                return new RestoreConfig();

            string json = File.ReadAllText(ConfigPath);
            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<RestoreConfig>(json);
        }

        public static void Save(RestoreConfig config)
        {
            if (!Directory.Exists(ConfigDir))
                Directory.CreateDirectory(ConfigDir);

            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(config);
            File.WriteAllText(ConfigPath, json);
        }
    }
}
