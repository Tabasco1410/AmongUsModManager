using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using AmongUsModManager.Models;
using AmongUsModManager.Models.Services;

namespace AmongUsModManager.Pages
{
    public class NotificationNewsItem
    {
        public string   Id             { get; set; } = "";
        public string   Title          { get; set; } = "";
        public string   PreviewContent { get; set; } = "";
        public string   DateLabel      { get; set; } = "";
        public bool     IsUnread       { get; set; }
        public bool     HasImages      { get; set; }
        public bool     IsUrgent       { get; set; }
        public string   CategoryIcon   { get; set; } = "📢";
        public NewsItem OriginalItem   { get; set; } = new();

        public SolidColorBrush UnreadDotColor
            => IsUnread
               ? new SolidColorBrush(Colors.DodgerBlue)
               : new SolidColorBrush(Colors.Transparent);

        public double ContentOpacity => IsUnread ? 0.85 : 0.7;
    }

    internal class AppNotifRaw
    {
        public string Title    { get; set; } = "";
        public string Message  { get; set; } = "";
        public string TimeText { get; set; } = "";
        public bool   IsRead   { get; set; }
        public string KindIcon { get; set; } = "ℹ️";
        public Color  KindColor { get; set; } = Colors.SlateGray;
    }

    public class AppNotifDisplayItem
    {
        public string Title    { get; set; } = "";
        public string Message  { get; set; } = "";
        public string TimeText { get; set; } = "";
        public bool   IsRead   { get; set; }
        public string KindIcon { get; set; } = "ℹ️";
        public SolidColorBrush KindColor { get; set; } = new(Colors.SteelBlue);

        public SolidColorBrush UnreadDotColor
            => IsRead
               ? new SolidColorBrush(Colors.Transparent)
               : new SolidColorBrush(Colors.DodgerBlue);
    }

    public sealed partial class NotificationPage : Page
    {
        private readonly HttpClient _http = new();
        private const string NewsUrl = "https://amongusmodmanager.web.app/News.json";

        private List<NotificationNewsItem> _allNewsItems  = new();
        private List<AppNotifDisplayItem>  _appNotifItems = new();

        public NotificationPage()
        {
            this.InitializeComponent();
            _http.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager-App");
            ApplyStrings();
            LocalizationService.LanguageChanged += ApplyStrings;
            this.Unloaded += (_, _) => LocalizationService.LanguageChanged -= ApplyStrings;
            LogService.Info("NotificationPage", "ページ初期化");
            NotificationService.MarkAllRead();
            _ = LoadAllAsync();
        }

        private void ApplyStrings()
        {
            NotificationPageTitle.Text  = LocalizationService.Get("Notification_Title");
            SubtitleText.Text           = LocalizationService.Get("Common_Loading");
            RefreshBtn.Content          = LocalizationService.Get("Common_Refresh");
            MarkAllReadBtn.Content      = LocalizationService.Get("Notification_MarkAllRead");
            NewsTabItem.Header          = LocalizationService.Get("Notification_NewsTab");
            AppNotifTabLabel.Text       = LocalizationService.Get("Notification_AppNotifTab");
            NewsEmptyText.Text          = LocalizationService.Get("Notification_NoNews");
            AppNotifEmptyText.Text      = LocalizationService.Get("Notification_NoAppNotif");
            UrgentSectionLabel.Text     = LocalizationService.Get("Notification_UrgentSection");
        }

        private async Task LoadAllAsync()
        {
            LoadingRing.IsActive = true;
            await Task.WhenAll(LoadNewsAsync(), LoadAppNotificationsAsync());
            LoadingRing.IsActive = false;
        }

        private async Task LoadNewsAsync()
        {
            LogService.Info("NotificationPage", "お知らせ取得開始");
            try
            {
                var readIds = NewsReadService.LoadReadIds();
                var rawList = await _http.GetFromJsonAsync<List<NewsItem>>(NewsUrl);
                if (rawList == null)
                {
                    DispatcherQueue.TryEnqueue(() => SetNewsEmpty(LocalizationService.Get("Notification_NoNews")));
                    return;
                }

                _allNewsItems = rawList
                    .OrderByDescending(n => n.Priority)
                    .ThenByDescending(n => n.IsUrgent)
                    .Select(n =>
                    {
                        string id     = string.IsNullOrEmpty(n.Id) ? $"{n.Title}_{n.Date}" : n.Id;
                        bool isUnread = !readIds.Contains(id);

                        string dateStr = n.Date;
                        if (DateTime.TryParse(n.Date, out var dt))
                            dateStr = dt.ToString(LocalizationService.Get("Notification_DateFormat"));

                        return new NotificationNewsItem
                        {
                            Id             = id,
                            Title          = n.Title,
                            PreviewContent = n.Content?.Replace("\n", " ") ?? "",
                            DateLabel      = dateStr,
                            IsUnread       = isUnread,
                            HasImages      = n.Images?.Count > 0,
                            IsUrgent       = n.IsUrgent,
                            CategoryIcon   = CategoryToIcon(n.Category),
                            OriginalItem   = n
                        };
                    }).ToList();

                int unread = _allNewsItems.Count(i => i.IsUnread);
                LogService.Info("NotificationPage", $"お知らせ {_allNewsItems.Count} 件, 未読 {unread} 件");
                NewsReadService.UpdateCachedUnreadCount(unread);

                DispatcherQueue.TryEnqueue(() =>
                {
                    UpdateNewsViews();
                    UpdateSubtitle();
                });
            }
            catch (Exception ex)
            {
                LogService.Error("NotificationPage", "お知らせ取得失敗", ex);
                DispatcherQueue.TryEnqueue(() => SetNewsEmpty(LocalizationService.Get("Notification_LoadFailed")));
            }
        }

        private void UpdateNewsViews()
        {
            var urgent  = _allNewsItems.Where(n => n.IsUrgent).ToList();
            var regular = _allNewsItems.Where(n => !n.IsUrgent).ToList();

            UrgentSection.Visibility       = urgent.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            UrgentNewsListView.ItemsSource  = urgent;
            NewsListView.ItemsSource        = regular;
            NewsEmptyText.Visibility        = regular.Count == 0 && urgent.Count == 0
                                             ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task LoadAppNotificationsAsync()
        {
            var rawItems = await Task.Run(() =>
                NotificationService.GetAll()
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new AppNotifRaw
                    {
                        Title     = i.Title,
                        Message   = i.Message,
                        TimeText  = FormatRelativeTime(i.CreatedAt),
                        IsRead    = i.IsRead,
                        KindIcon  = KindToIcon(i.Kind),
                        KindColor = KindToColor(i.Kind),
                    }).ToList());

            DispatcherQueue.TryEnqueue(() =>
            {
                _appNotifItems = rawItems.Select(r => new AppNotifDisplayItem
                {
                    Title     = r.Title,
                    Message   = r.Message,
                    TimeText  = r.TimeText,
                    IsRead    = r.IsRead,
                    KindIcon  = r.KindIcon,
                    KindColor = new SolidColorBrush(r.KindColor),
                }).ToList();

                int unread = _appNotifItems.Count(i => !i.IsRead);
                AppNotifListView.ItemsSource = _appNotifItems;
                AppNotifEmptyText.Visibility = _appNotifItems.Count == 0
                    ? Visibility.Visible : Visibility.Collapsed;
                AppNotifBadge.Visibility  = unread > 0 ? Visibility.Visible : Visibility.Collapsed;
                AppNotifCount.Text        = unread > 9 ? "9+" : unread.ToString();
            });
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAllAsync();

        private void MarkAllNewsRead_Click(object sender, RoutedEventArgs e)
        {
            if (_allNewsItems.Count == 0) return;
            NewsReadService.MarkAllRead(_allNewsItems.Select(n => n.Id));
            foreach (var n in _allNewsItems) n.IsUnread = false;
            UpdateNewsViews();
            UpdateSubtitle();
            LogService.Info("NotificationPage", "すべて既読にしました");
        }

        private void NewsItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not NotificationNewsItem item) return;
            MarkAsRead(item);
            NavigateToDetail(item.OriginalItem);
        }

        private void ViewDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: NotificationNewsItem item })
            {
                MarkAsRead(item);
                NavigateToDetail(item.OriginalItem);
            }
        }

        private void MarkReadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: NotificationNewsItem item })
                MarkAsRead(item);
        }

        private void MarkAsRead(NotificationNewsItem item)
        {
            if (!item.IsUnread) return;
            NewsReadService.MarkRead(item.Id);
            item.IsUnread = false;
            UpdateNewsViews();
            UpdateSubtitle();
            int unread = _allNewsItems.Count(i => i.IsUnread);
            NewsReadService.UpdateCachedUnreadCount(unread);
        }

        private void NavigateToDetail(NewsItem newsItem)
        {
            if (App.MainWindowInstance is MainWindow mw)
                mw.NavigateToNewsDetail(newsItem);
        }

        private void SetNewsEmpty(string msg)
        {
            NewsListView.ItemsSource       = null;
            UrgentNewsListView.ItemsSource = null;
            UrgentSection.Visibility       = Visibility.Collapsed;
            NewsEmptyText.Text             = msg;
            NewsEmptyText.Visibility       = Visibility.Visible;
            SubtitleText.Text              = msg;
        }

        private void UpdateSubtitle()
        {
            int total  = _allNewsItems.Count;
            int unread = _allNewsItems.Count(i => i.IsUnread);
            SubtitleText.Text = unread > 0
                ? string.Format(LocalizationService.Get("Notification_SubtitleUnread"), total, unread)
                : string.Format(LocalizationService.Get("Notification_SubtitleAllRead"), total);
        }

        private static string CategoryToIcon(string category) => category switch
        {
            "update"      => "🔄",
            "important"   => "⚠️",
            "warning"     => "⚠️",
            "maintenance" => "🔧",
            _             => "📢"
        };

        private static string FormatRelativeTime(DateTime dt)
        {
            var diff = DateTime.Now - dt;
            if (diff.TotalMinutes < 1) return LocalizationService.Get("Notification_JustNow");
            if (diff.TotalHours   < 1) return string.Format(LocalizationService.Get("Notification_MinutesAgo"), (int)diff.TotalMinutes);
            if (diff.TotalDays    < 1) return string.Format(LocalizationService.Get("Notification_HoursAgo"),   (int)diff.TotalHours);
            if (diff.TotalDays    < 7) return string.Format(LocalizationService.Get("Notification_DaysAgo"),    (int)diff.TotalDays);
            return dt.ToString("yyyy/MM/dd");
        }

        private static string KindToIcon(NotificationKind kind) => kind switch
        {
            NotificationKind.Update  => "🔄",
            NotificationKind.Warning => "⚠️",
            NotificationKind.News    => "📢",
            _                        => "ℹ️"
        };

        private static Color KindToColor(NotificationKind kind) => kind switch
        {
            NotificationKind.Update  => Colors.SeaGreen,
            NotificationKind.Warning => Colors.DarkOrange,
            NotificationKind.News    => Colors.SteelBlue,
            _                        => Colors.SlateGray
        };
    }
}
