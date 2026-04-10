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
            {
                var created = new RestoreConfig();
                Save(created);
                return created;
            }

            string json = File.ReadAllText(ConfigPath);
            var serializer = new JavaScriptSerializer();
            var config = serializer.Deserialize<RestoreConfig>(json);
            if (config == null)
                return new RestoreConfig();

            bool changed = ApplyCompatibilityDefaults(config);
            if (changed)
                Save(config);

            return config;
        }

        public static void Save(RestoreConfig config)
        {
            if (!Directory.Exists(ConfigDir))
                Directory.CreateDirectory(ConfigDir);

            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(config);
            File.WriteAllText(ConfigPath, json);
        }

        private static bool ApplyCompatibilityDefaults(RestoreConfig config)
        {
            bool changed = false;
            bool legacyMissingVmFields = string.IsNullOrWhiteSpace(config.VmRestorePath);

            if (string.IsNullOrWhiteSpace(config.ProtectDrive))
            {
                config.ProtectDrive = "C";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(config.DataDrive))
            {
                config.DataDrive = "D";
                changed = true;
            }

            if (legacyMissingVmFields)
            {
                config.VmRestorePath = @"C:\RestoreSystem\VMProtected";
                changed = true;
            }

            // 只在舊版設定檔缺少 VM 欄位時，預設啟用自動偵測。
            if (legacyMissingVmFields && !config.AutoDetectVm && !config.VmSafeMode)
            {
                config.AutoDetectVm = true;
                changed = true;
            }

            return changed;
        }
    }
}
