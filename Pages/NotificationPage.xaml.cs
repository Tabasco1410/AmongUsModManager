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
using AmongUsModManager.Services;
using AmongUsModManager.Models.Services;

namespace AmongUsModManager.Pages
{
    public class NotificationNewsItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string PreviewContent { get; set; } = "";
        public string Date { get; set; } = "";
        public bool IsUnread { get; set; }
        public bool HasImages { get; set; }
        public NewsItem OriginalItem { get; set; } = new();

        public SolidColorBrush UnreadDotColor
            => IsUnread
               ? new SolidColorBrush(Colors.DodgerBlue)
               : new SolidColorBrush(Colors.Transparent);

        public double TitleOpacity => IsUnread ? 1.0 : 0.75;
        public double ContentOpacity => IsUnread ? 0.85 : 0.7;
    }

  
    internal class AppNotifRaw
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string TimeText { get; set; } = "";
        public bool IsRead { get; set; }
        public string KindIcon { get; set; } = "ℹ️";
        public Color KindColor { get; set; } = Colors.SlateGray;
    }

    public class AppNotifDisplayItem
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string TimeText { get; set; } = "";
        public bool IsRead { get; set; }

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

        private List<NotificationNewsItem> _newsItems = new();
        private List<AppNotifDisplayItem> _appNotifItems = new();

        public NotificationPage()
        {
            this.InitializeComponent();
            _http.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager-App");
            LogService.Info("NotificationPage", "ページ初期化");
            NotificationService.MarkAllRead();
            _ = LoadAllAsync();
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
            LogService.Debug("NotificationPage", $"取得URL: {NewsUrl}");
            try
            {
                var readIds = NewsReadService.LoadReadIds();
                LogService.Debug("NotificationPage", $"既読ID数: {readIds.Count}");

                LogService.Trace("NotificationPage", "HTTPリクエスト送信");
                var rawList = await _http.GetFromJsonAsync<List<NewsItem>>(NewsUrl);
                if (rawList == null)
                {
                    LogService.Warn("NotificationPage", "お知らせリストがnull");
                    DispatcherQueue.TryEnqueue(() => SetNewsEmpty("お知らせはありません"));
                    return;
                }
                LogService.Debug("NotificationPage", $"取得件数: {rawList.Count}");

                _newsItems = rawList.Select(n =>
                {
                    string id = string.IsNullOrEmpty(n.Id) ? $"{n.Title}_{n.Date}" : n.Id;
                    bool isUnread = !readIds.Contains(id);

                    string dateStr = n.Date;
                    if (DateTime.TryParse(n.Date, out var dt))
                        dateStr = dt.ToString("yyyy年M月d日");

                    LogService.Trace("NotificationPage",
                        $"  item: id={id}, title={n.Title}, isUnread={isUnread}, hasImages={n.Images?.Count > 0}");

                    return new NotificationNewsItem
                    {
                        Id = id,
                        Title = n.Title,
                        PreviewContent = n.Content?.Replace("\n", " ") ?? "",
                        Date = dateStr,
                        IsUnread = isUnread,
                        HasImages = n.Images?.Count > 0,
                        OriginalItem = n
                    };
                }).ToList();

                int unread = _newsItems.Count(i => i.IsUnread);
                LogService.Info("NotificationPage", $"お知らせ {_newsItems.Count} 件, 未読 {unread} 件");
                NewsReadService.UpdateCachedUnreadCount(unread);

                DispatcherQueue.TryEnqueue(() =>
                {
                    LogService.Debug("NotificationPage", "UIスレッドでNewsListView更新");
                    NewsListView.ItemsSource = _newsItems;
                    NewsEmptyText.Visibility = _newsItems.Count == 0
                        ? Visibility.Visible : Visibility.Collapsed;
                    UpdateSubtitle();
                });
            }
            catch (Exception ex)
            {
                LogService.Error("NotificationPage", "お知らせ取得失敗", ex);
                DispatcherQueue.TryEnqueue(() => SetNewsEmpty("お知らせの取得に失敗しました"));
            }
        }


        private async Task LoadAppNotificationsAsync()
        {
          
            var rawItems = await Task.Run(() =>
            {
                return NotificationService.GetAll()
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new AppNotifRaw
                    {
                        Title = i.Title,
                        Message = i.Message,
                        TimeText = FormatRelativeTime(i.CreatedAt),
                        IsRead = i.IsRead,
                        KindIcon = KindToIcon(i.Kind),
                        KindColor = KindToColor(i.Kind),   
                    })
                    .ToList();
            });

            DispatcherQueue.TryEnqueue(() =>
            {
                _appNotifItems = rawItems.Select(r => new AppNotifDisplayItem
                {
                    Title = r.Title,
                    Message = r.Message,
                    TimeText = r.TimeText,
                    IsRead = r.IsRead,
                    KindIcon = r.KindIcon,
                    KindColor = new SolidColorBrush(r.KindColor),  
                }).ToList();

                int unread = _appNotifItems.Count(i => !i.IsRead);
                AppNotifListView.ItemsSource = _appNotifItems;
                AppNotifEmptyText.Visibility = _appNotifItems.Count == 0
                    ? Visibility.Visible : Visibility.Collapsed;
                AppNotifBadge.Visibility = unread > 0 ? Visibility.Visible : Visibility.Collapsed;
                AppNotifCount.Text = unread > 9 ? "9+" : unread.ToString();
            });
        }


        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("NotificationPage", "手動更新");
            await LoadAllAsync();
        }

        private void MarkAllNewsRead_Click(object sender, RoutedEventArgs e)
        {
            if (_newsItems.Count == 0) return;
            NewsReadService.MarkAllRead(_newsItems.Select(n => n.Id));
            foreach (var n in _newsItems) n.IsUnread = false;
            RefreshNewsList();
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
            if (sender is Button btn && btn.Tag is NotificationNewsItem item)
            {
                MarkAsRead(item);
                NavigateToDetail(item.OriginalItem);
            }
        }

        private void MarkReadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is NotificationNewsItem item)
            {
                MarkAsRead(item);
                LogService.Info("NotificationPage", $"既読: {item.Title}");
            }
        }

      

        private void MarkAsRead(NotificationNewsItem item)
        {
            if (!item.IsUnread) return;
            NewsReadService.MarkRead(item.Id);
            item.IsUnread = false;
            RefreshNewsList();
            UpdateSubtitle();
            int unread = _newsItems.Count(i => i.IsUnread);
            NewsReadService.UpdateCachedUnreadCount(unread);
        }

        private void NavigateToDetail(NewsItem newsItem)
        {
            if (App.MainWindowInstance is MainWindow mw)
                mw.NavigateToNewsDetail(newsItem);
        }

        private void RefreshNewsList()
        {
            NewsListView.ItemsSource = null;
            NewsListView.ItemsSource = _newsItems;
        }

        private void SetNewsEmpty(string msg)
        {
            NewsListView.ItemsSource = null;
            NewsEmptyText.Text = msg;
            NewsEmptyText.Visibility = Visibility.Visible;
            SubtitleText.Text = msg;
        }

        private void UpdateSubtitle()
        {
            int total = _newsItems.Count;
            int unread = _newsItems.Count(i => i.IsUnread);
            SubtitleText.Text = unread > 0
                ? $"{total} 件のお知らせ・未読 {unread} 件"
                : $"{total} 件のお知らせ（すべて既読）";
        }

        private static string FormatRelativeTime(DateTime dt)
        {
            var diff = DateTime.Now - dt;
            if (diff.TotalMinutes < 1) return "たった今";
            if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes} 分前";
            if (diff.TotalDays < 1) return $"{(int)diff.TotalHours} 時間前";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} 日前";
            return dt.ToString("yyyy/MM/dd");
        }

        private static string KindToIcon(NotificationKind kind) => kind switch
        {
            NotificationKind.Update => "🔄",
            NotificationKind.Warning => "⚠️",
            NotificationKind.News => "📢",
            _ => "ℹ️"
        };

        
        private static Color KindToColor(NotificationKind kind) => kind switch
        {
            NotificationKind.Update => Colors.SeaGreen,
            NotificationKind.Warning => Colors.DarkOrange,
            NotificationKind.News => Colors.SteelBlue,
            _ => Colors.SlateGray
        };
    }
}
