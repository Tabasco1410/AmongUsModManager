using System;
using System.Collections.Generic;
using System.Linq;
using AmongUsModManager.Models;
using AmongUsModManager.Services;

namespace AmongUsModManager.Models.Services
    //通知
{
    public static class NotificationService
    {
        private static readonly List<NotificationItem> _items = new();
        public static event Action<NotificationItem>? NotificationAdded;

        public static void Push(string title, string message,
            NotificationKind kind = NotificationKind.Info, string tag = "")
        {
            var item = new NotificationItem
            {
                Title = title, Message = message, Kind = kind, Tag = tag
            };
            _items.Add(item);
            LogService.Info("NotificationService", $"通知追加: [{kind}] {title}");
            NotificationAdded?.Invoke(item);
        }

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
