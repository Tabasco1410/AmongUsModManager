using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Among_Us_ModManeger.Pages
{
    public partial class MainMenuPage : Page
    {
        private const string NewsUrl = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/News.json";
        private const string LastReadNewsFile = "last_read_news.txt"; // 実行フォルダに保存

        public MainMenuPage()
        {
            InitializeComponent();
            _ = CheckNewsAsync();
        }

        private async Task CheckNewsAsync()
        {
            try
            {
                using HttpClient client = new();
                var json = await client.GetStringAsync(NewsUrl);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var newsList = JsonSerializer.Deserialize<List<NewsItem>>(json, options);

                if (newsList != null)
                {
                    var now = DateTime.Now;

                    // 未来日時のニュースを除外
                    newsList = newsList.Where(n => n.Date <= now).OrderByDescending(n => n.Date).ToList();
                }

                if (newsList?.Count > 0)
                {
                    var latestNewsDate = newsList[0].Date;

                    DateTime lastRead = DateTime.MinValue;
                    if (File.Exists(LastReadNewsFile))
                    {
                        string stored = File.ReadAllText(LastReadNewsFile);
                        DateTime.TryParse(stored, out lastRead);
                    }

                    if (latestNewsDate > lastRead)
                    {
                        // お知らせテキストとバッジを表示
                        NoticeText.Visibility = Visibility.Visible;
                        NoticeBadge.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        // 既読ならお知らせ非表示、バッジは表示
                        NoticeText.Visibility = Visibility.Collapsed;
                        NoticeBadge.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    // 有効なニュースなし（未来のみ、または空）
                    NoticeText.Visibility = Visibility.Collapsed;
                    NoticeBadge.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                // エラー時は通知テキスト非表示、バッジ表示
                NoticeText.Visibility = Visibility.Collapsed;
                NoticeBadge.Visibility = Visibility.Visible;
            }
        }

        private void Mod_New_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ModSelectPage());
        }

        private void Notice_Click(object sender, RoutedEventArgs e)
        {
            // お知らせ画面へ遷移する前に「既読」として日時保存
            File.WriteAllText(LastReadNewsFile, DateTime.Now.ToString("s"));
            NoticeText.Visibility = Visibility.Collapsed;

            NavigationService?.Navigate(new News());
        }

        public class NewsItem
        {
            public DateTime Date { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
        }
    }
}
