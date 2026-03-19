using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmongUsModManager.Models;
using AmongUsModManager.Services;

namespace AmongUsModManager.Models.Services
{
    public class ConfigService
    {
        private static readonly string FolderName = "AmongUsModManager";
        private static readonly string FileName = "settings.aumanager";
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), FolderName);
        private static readonly string FullPath = Path.Combine(AppDataPath, FileName);

        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            // NaN / Infinity を文字列として書き出す（旧設定ファイルの互換性のため残す）
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(
                System.Text.Unicode.UnicodeRanges.All)
        };

        private static AppConfig? _cache;
        private static DateTime _cacheTime = DateTime.MinValue;
        private static readonly object _lock = new object();

        public static void InvalidateCache()
        {
            lock (_lock) { _cache = null; }
        }

        public static void Save(AppConfig config)
        {
            try
            {
                Directory.CreateDirectory(AppDataPath);
                string json = JsonSerializer.Serialize(config, _options);
                File.WriteAllText(FullPath, json);
                lock (_lock)
                {
                    _cache = config;
                    _cacheTime = DateTime.Now;
                }
                LogService.Debug("ConfigService", "設定を保存しました");
            }
            catch (Exception ex)
            {
                LogService.Error("ConfigService", "設定の保存に失敗しました", ex);
            }
        }

        public static void SaveWindowSize(double width, double height)
        {
            try
            {
                var config = Load();
                config.WindowWidth  = width;
                config.WindowHeight = height;
                Directory.CreateDirectory(AppDataPath);
                string json = JsonSerializer.Serialize(config, _options);
                File.WriteAllText(FullPath, json);
                lock (_lock) { _cache = config; _cacheTime = DateTime.Now; }
            }
            catch { /* 無視 */ }
        }

        public static void SaveWindowBounds(double x, double y, double width, double height, bool isMaximized)
        {
            try
            {
                var config = Load();
                config.WindowX = x;
                config.WindowY = y;
                config.WindowWidth = width;
                config.WindowHeight = height;
                config.IsWindowMaximized = isMaximized;
                Directory.CreateDirectory(AppDataPath);
                string json = JsonSerializer.Serialize(config, _options);
                File.WriteAllText(FullPath, json);
                lock (_lock) { _cache = config; _cacheTime = DateTime.Now; }
            }
            catch { /* 無視 */ }
        }

        public static AppConfig Load()
        {
            lock (_lock)
            {
                if (_cache != null && (DateTime.Now - _cacheTime).TotalSeconds < 30)
                    return _cache;
            }

            if (!File.Exists(FullPath))
            {
                LogService.Debug("ConfigService", "設定ファイルが存在しないため新規作成します");
                var fresh = new AppConfig();
                lock (_lock) { _cache = fresh; _cacheTime = DateTime.Now; }
                return fresh;
            }
            try
            {
                string json = File.ReadAllText(FullPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, _options) ?? new AppConfig();
                LogService.Debug("ConfigService", "設定を読み込みました");
                lock (_lock) { _cache = config; _cacheTime = DateTime.Now; }
                return config;
            }
            catch (Exception ex)
            {
                LogService.Error("ConfigService", "設定ファイルの読み込みに失敗しました", ex);
                return new AppConfig();
            }
        }
    }
}
