using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Among_Us_ModManeger.Pages
{
    public partial class MainMenuPage : Page
    {
        private const string NewsUrl = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/News.json";

        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AmongUsModManeger");

        private static readonly string LastReadNewsFile = Path.Combine(AppDataFolder, "last_read_news.txt");

        public MainMenuPage()
        {
            InitializeComponent();

            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }

            _ = CheckNewsAsync();
            _ = LoadVersionAsync();
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
                        NoticeText.Visibility = Visibility.Visible;
                        NoticeBadge.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        NoticeText.Visibility = Visibility.Collapsed;
                        NoticeBadge.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    NoticeText.Visibility = Visibility.Collapsed;
                    NoticeBadge.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

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

        private async Task LoadVersionAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                VersionText.Text = $" {AppVersion.Version}";

                // 改行を反映する TextBlock を ToolTip に設定
                var tooltip = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Width = 250
                };

                var lines = AppVersion.Notes.Split('\n');
                foreach (var line in lines)
                {
                    tooltip.Inlines.Add(new Run(line));
                    tooltip.Inlines.Add(new LineBreak());
                }

                VersionText.ToolTip = tooltip;
            });
        }
    }
}
