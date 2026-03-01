using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AmongUsModManager.Pages;

namespace AmongUsModManager.Models.Services
{
    public static class LaunchHistoryService
    {
        private static readonly string FolderName = "AmongUsModManager";
        private static readonly string FileName = "launch_history.json";
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), FolderName);
        private static readonly string FullPath = Path.Combine(AppDataPath, FileName);

        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static List<LaunchHistoryItem> Load()
        {
            if (!File.Exists(FullPath)) return new List<LaunchHistoryItem>();
            try
            {
                string json = File.ReadAllText(FullPath);
                return JsonSerializer.Deserialize<List<LaunchHistoryItem>>(json, _options)
                       ?? new List<LaunchHistoryItem>();
            }
            catch { return new List<LaunchHistoryItem>(); }
        }

        public static void Add(string modName)
        {
            var list = Load();
            list.Add(new LaunchHistoryItem { ModName = modName, LaunchedAt = DateTime.Now });
            // 最大500
            if (list.Count > 500) list.RemoveRange(0, list.Count - 500);
            Save(list);
        }

        public static void Clear()
        {
            Save(new List<LaunchHistoryItem>());
        }

        private static void Save(List<LaunchHistoryItem> list)
        {
            try
            {
                if (!Directory.Exists(AppDataPath)) Directory.CreateDirectory(AppDataPath);
                File.WriteAllText(FullPath, JsonSerializer.Serialize(list, _options));
            }
            catch { }
        }
    }
}
