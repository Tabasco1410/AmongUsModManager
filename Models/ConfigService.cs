using System;
using System.IO;
using System.Text.Json;
using AmongUsModManager.Models;

namespace AmongUsModManager.Services
{
    public class ConfigService
    {
        private static readonly string FolderName = "AmongUsModManager";
        private static readonly string FileName = "settings.aumanager";

        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            FolderName);

        private static readonly string FullPath = Path.Combine(AppDataPath, FileName);

      
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
        };

        public static void Save(AppConfig config)
        {
            try
            {
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                }

                string json = JsonSerializer.Serialize(config, _options);
                File.WriteAllText(FullPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
            }
        }

        public static AppConfig Load()
        {
            if (!File.Exists(FullPath)) return new AppConfig();

            try
            {
                string json = File.ReadAllText(FullPath);
                return JsonSerializer.Deserialize<AppConfig>(json, _options) ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }
    }
}
