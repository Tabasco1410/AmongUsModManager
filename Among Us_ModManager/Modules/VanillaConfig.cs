using System.IO;
using System.Text.Json;

namespace Among_Us_ModManager.Modules
{
    public class VanillaConfig
    {
        public string AmongUsExePath { get; set; } = "";

        private static readonly string ConfigPath =
            Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                         "AmongUsModManager", "Vanilla_Config.json");

        public static VanillaConfig Load()
        {
            if (!File.Exists(ConfigPath))
                return new VanillaConfig();

            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<VanillaConfig>(json) ?? new VanillaConfig();
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
    }
}
