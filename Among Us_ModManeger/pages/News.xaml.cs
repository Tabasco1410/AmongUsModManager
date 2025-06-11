using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

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
                var json = await client.GetStringAsync(NewsUrl);

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

                    // 最初の項目を初期選択
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
                DetailContentTextBlock.Text = selected.Content;
            }
            else
            {
                DetailDateTextBlock.Text = "";
                DetailTitleTextBlock.Text = "";
                DetailContentTextBlock.Text = "";
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
