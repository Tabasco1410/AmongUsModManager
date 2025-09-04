using Newtonsoft.Json;
using System;
using System.IO;

namespace Among_Us_ModManager.Modules
{
    public class SettingsConfig
    {
        [JsonProperty("AmongUsExePath")]
        public string AmongUsExePath { get; set; } = "";

        [JsonProperty("RunInBackground")]
        public bool RunInBackground { get; set; } = true; // デフォルト ON

        private static readonly string ConfigPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "AmongUsModManager", "Settings.json");

        public static SettingsConfig Load()
        {
            if (!File.Exists(ConfigPath))
                return new SettingsConfig();

            var json = File.ReadAllText(ConfigPath);
            return JsonConvert.DeserializeObject<SettingsConfig>(json) ?? new SettingsConfig();
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
        }
    }
}
