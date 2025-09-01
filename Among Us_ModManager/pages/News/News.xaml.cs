using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Markdig;

namespace Among_Us_ModManager.Pages
{
    public partial class News : Page
    {
        private const string NewsUrl = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManager/main/News.json";
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AmongUsModManager");
        private static readonly string NewsFile = Path.Combine(AppDataFolder, "last_read_news.txt");

        public News()
        {
            InitializeComponent();

            Loaded += News_Loaded;

            _ = LoadNewsAsync();
        }

        private void News_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(AppDataFolder);

                // ニュース閲覧日時を更新
                File.WriteAllText(NewsFile, DateTime.Now.ToString("o"));

                // MainMenuPage 側で表示していた「新着通知」をここで非表示にする場合は
                // 例: NewNoticeText.Visibility = Visibility.Collapsed;
            }
            catch { }
        }

        private async Task LoadNewsAsync()
        {
            try
            {
                using HttpClient client = new();
                string urlWithCacheBuster = NewsUrl + "?t=" + DateTime.UtcNow.Ticks;
                var json = await client.GetStringAsync(urlWithCacheBuster);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var newsList = JsonSerializer.Deserialize<List<NewsItem>>(json, options);

                if (newsList != null)
                {
                    newsList.Sort((a, b) => b.Date.CompareTo(a.Date));
                    NewsListBox.ItemsSource = newsList;

                    if (newsList.Count > 0)
                        NewsListBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("お知らせの取得に失敗しました。\n" + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NewsListBox.SelectedItem is NewsItem selected)
            {
                DetailDateTextBlock.Text = selected.Date.ToString("yyyy/MM/dd");
                DetailTitleTextBlock.Text = selected.Title;

                string html = Markdown.ToHtml(selected.Content);
                string fullHtml = @$"
<html>
<head>
<meta charset='UTF-8'>
<style>
body {{ font-family: 'Segoe UI', sans-serif; padding: 10px; }}
h1,h2,h3 {{ color: #333; }}
p {{ margin: 0 0 10px; }}
</style>
</head>
<body>{html}</body>
</html>";
                DetailWebBrowser.NavigateToString(fullHtml);
            }
            else
            {
                DetailDateTextBlock.Text = "";
                DetailTitleTextBlock.Text = "";
                DetailWebBrowser.NavigateToString("<html><body></body></html>");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        public class NewsItem
        {
            public DateTime Date { get; set; }
            public string Title { get; set; } = "";
            public string Content { get; set; } = "";
        }
    }
}
