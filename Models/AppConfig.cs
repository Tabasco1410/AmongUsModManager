using System;
using System.Collections.Generic;

namespace AmongUsModManager.Models
{
    public class VanillaPathInfo
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string? GitHubOwner { get; set; }
        public string? GitHubRepo { get; set; }
        public string? CurrentVersion { get; set; }
        public bool IsAutoUpdateEnabled { get; set; }
        public DateTime LastChecked { get; set; }
        // true のとき自動GitHub連携を行わない（ユーザーが意図的に解除した場合）
        public bool GitHubLinkDisabled { get; set; } = false;
        // フォルダが属するプラットフォーム (Steam / Epic / MSStore / Itch / Manual / "")
        public string? Platform { get; set; } = "";
        // x:Bind 用の表示ラベル
        public string PlatformLabel => Platform switch
        {
            "Steam" => "Steam",
            "Epic" => "Epic",
            "MSStore" => "MS Store",
            "Itch" => "itch.io",
            "Manual" => "手動",
            _ => "その他"
        };
    }

    public class AppConfig
    {
        public string GameInstallPath { get; set; } = "";
        public List<VanillaPathInfo> VanillaPaths { get; set; } = new List<VanillaPathInfo>();
        public string ModDataPath { get; set; } = "";

        public string Platform { get; set; } = "";
        public bool EpicLaunchViaLauncher { get; set; } = true;

        public string EpicAccountId { get; set; } = "";
        public string EpicDisplayName { get; set; } = "";
        public bool CheckUpdateOnStartup { get; set; } = true;

        public bool StartWithWindows { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public DateTime? LastLaunchTime { get; set; }

        public string Theme { get; set; } = "Default";

        public bool NotifyModUpdate { get; set; } = true;
        public bool NotifyAppUpdate { get; set; } = true;
        public bool NotifyNews { get; set; } = true;

        public bool WatchScreenshots { get; set; } = true;
        public string ScreenshotSavePath { get; set; } = "";

        public bool SaveGameLogs { get; set; } = true;
        public bool RecordMatchStats { get; set; } = true;

        // ログ: false=起動ごとに新ファイル, true=追記
        public bool LogAppendMode { get; set; } = false;

        public bool MinimizeToTray { get; set; } = false;

        public string ClaudeApiKey { get; set; } = "";

        public string GitHubToken { get; set; } = "";
        public string GitHubLoginMethod { get; set; } = "";
        public string GitHubUserName { get; set; } = "";

        public string SteamApiKey { get; set; } = "";
        public string SteamUserId { get; set; } = "";
        public string SteamUserName { get; set; } = "";

        public string MainPlatform { get; set; } = "";

        public string? LibraryViewMode { get; set; }
        public double WindowWidth { get; set; } = 1100;
        public double WindowHeight { get; set; } = 700;

        // NaN を JSON にシリアライズするとエラーになるため nullable double に変更
        public double? WindowX { get; set; } = null;
        public double? WindowY { get; set; } = null;
        public bool IsWindowMaximized { get; set; } = false;

        public List<FriendEntry> Friends { get; set; } = new List<FriendEntry>();
        public List<BanEntry> BanList { get; set; } = new List<BanEntry>();
    }

    public class NotificationItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Tag { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        public NotificationKind Kind { get; set; } = NotificationKind.Info;
    }

    public enum NotificationKind { Info, Update, Warning, News }

    public class FriendEntry
    {
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public string Memo { get; set; } = "";
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }

    public class BanEntry
    {
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTime BannedAt { get; set; } = DateTime.Now;
    }

    public class MatchRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime PlayedAt { get; set; } = DateTime.Now;
        public string ModName { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsImpostor { get; set; }
        public bool IsWin { get; set; }
        public int Kills { get; set; }
        public string Map { get; set; } = "";
    }
}
