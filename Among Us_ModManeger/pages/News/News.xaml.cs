using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Markdig;

namespace Among_Us_ModManeger.Pages
{
    public partial class News : Page
    {
        private const string NewsUrl = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/News.json";

        public News()
        {
            InitializeComponent();
            _ = LoadNewsAsync();
        }

        private async Task LoadNewsAsync()
        {
            try
            {
                using HttpClient client = new();
                // キャッシュ回避用クエリパラメータ付加
                string urlWithCacheBuster = NewsUrl + "?t=" + DateTime.UtcNow.Ticks;
                var json = await client.GetStringAsync(urlWithCacheBuster);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var newsList = JsonSerializer.Deserialize<List<NewsItem>>(json, options);

                if (newsList != null)
                {
                    // 日付降順に並べ替え
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

                // Markdown → HTML に変換
                string html = Markdown.ToHtml(selected.Content);

                string fullHtml = @$"
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', sans-serif; padding: 10px; }}
        h1, h2, h3 {{ color: #333; }}
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
