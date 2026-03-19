using System;
using System.Collections.Generic;
using System.Linq;
using AmongUsModManager.Models;
using AmongUsModManager.Services;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace AmongUsModManager.Models.Services
{
    // 通知
    public static class NotificationService
    {
        // AppUserModelID: レジストリ登録とToast発行に使う一意なID
        private const string AppId = "AmongUsModManager.App";

        private static readonly List<NotificationItem> _items = new();
        public static event Action<NotificationItem>? NotificationAdded;

        // App.OnLaunched から呼ぶ
        public static void Initialize()
        {
            try
            {
                RegisterAppUserModelId();
                LogService.Debug("NotificationService", "AppUserModelID登録完了");
            }
            catch (Exception ex)
            {
                LogService.Warn("NotificationService", $"AppUserModelID登録失敗（Toast通知が出ない場合あり）: {ex.Message}");
            }
        }

        // Unpackaged アプリがトースト通知を出すには
        // HKCU\Software\Classes\AppUserModelId\{AppId} をレジストリに登録する必要がある
        private static void RegisterAppUserModelId()
        {
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (string.IsNullOrEmpty(exePath)) return;

            string regPath = $@"Software\Classes\AppUserModelId\{AppId}";
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regPath);
            if (key == null) return;

            key.SetValue("DisplayName", "AmongUsModManager");
            key.SetValue("IconUri", exePath);
            key.SetValue("IconBackgroundColor", "FF1A1A2E");
        }

        public static void Push(string title, string message,
            NotificationKind kind = NotificationKind.Info, string tag = "")
        {
            var item = new NotificationItem
            {
                Title = title,
                Message = message,
                Kind = kind,
                Tag = tag
            };
            _items.Add(item);
            LogService.Info("NotificationService", $"通知追加: [{kind}] {title}");
            NotificationAdded?.Invoke(item);

            SendToast(title, message, kind);
        }

        private static void SendToast(string title, string message, NotificationKind kind)
        {
            try
            {
                string emoji = kind switch
                {
                    NotificationKind.Update => "🔄",
                    NotificationKind.Warning => "⚠️",
                    NotificationKind.News => "📢",
                    _ => "ℹ️"
                };

                string xml = "<toast>"
                    + "<visual><binding template=\"ToastGeneric\">"
                    + "<text>" + EncodeXml(emoji + " " + title) + "</text>"
                    + "<text>" + EncodeXml(message) + "</text>"
                    + "</binding></visual>"
                    + "</toast>";

                var doc = new XmlDocument();
                doc.LoadXml(xml);

                var notifier = ToastNotificationManager.CreateToastNotifier(AppId);
                var notification = new ToastNotification(doc);
                notifier.Show(notification);
            }
            catch (Exception ex)
            {
                LogService.Warn("NotificationService", $"Toast送信失敗: {ex.Message}");
            }
        }

        private static string EncodeXml(string s)
            => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

        public static List<NotificationItem> GetAll() => _items.ToList();
        public static int UnreadCount() => _items.Count(i => !i.IsRead);
        public static void MarkAllRead() { foreach (var i in _items) i.IsRead = true; }
        public static void MarkRead(string id)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item != null) item.IsRead = true;
        }
        public static void MarkReadByTag(string tag)
        {
            foreach (var item in _items.Where(i => i.Tag == tag))
                item.IsRead = true;
        }
    }
}
