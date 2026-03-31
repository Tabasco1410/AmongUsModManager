using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AmongUsModManager.Models.Services
{
    public static class NewsReadService
    {
        private static readonly string FolderName = "AmongUsModManager";
        private static readonly string FileName   = "news_read.aumanager";
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), FolderName);
        private static readonly string FullPath = Path.Combine(AppDataPath, FileName);

        private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

        public static HashSet<string> LoadReadIds()
        {
            if (!File.Exists(FullPath)) return new HashSet<string>();
            try
            {
                string json = File.ReadAllText(FullPath);
                var list = JsonSerializer.Deserialize<List<string>>(json, _options);
                return list != null ? new HashSet<string>(list) : new HashSet<string>();
            }
            catch { return new HashSet<string>(); }
        }

        public static void MarkRead(string id)
        {
            var ids = LoadReadIds();
            ids.Add(id);
            Save(ids);
        }

        public static void MarkAllRead(IEnumerable<string> ids)
        {
            var existing = LoadReadIds();
            foreach (var id in ids) existing.Add(id);
            Save(existing);
        }

        public static bool IsRead(string id) => LoadReadIds().Contains(id);

        // サイドバーバッジ用にニュース未読数をキャッシュする。
        // NotificationPage がロード完了時に更新し、MainWindow が参照する。
        public static int CachedNewsUnreadCount { get; private set; } = 0;
        public static event Action? NewsUnreadCountChanged;

        public static void UpdateCachedUnreadCount(int count)
        {
            if (CachedNewsUnreadCount == count) return;
            CachedNewsUnreadCount = count;
            NewsUnreadCountChanged?.Invoke();
        }

        public static int UnreadCount(IEnumerable<string> allIds)
        {
            var readIds = LoadReadIds();
            int count = 0;
            foreach (var id in allIds)
                if (!readIds.Contains(id)) count++;
            return count;
        }

        private static void Save(HashSet<string> ids)
        {
            try
            {
                if (!Directory.Exists(AppDataPath)) Directory.CreateDirectory(AppDataPath);
                File.WriteAllText(FullPath, JsonSerializer.Serialize(new List<string>(ids), _options));
            }
            catch { }
        }
    }
}
