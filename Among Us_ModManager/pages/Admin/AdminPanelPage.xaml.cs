using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Among_Us_ModManager.Pages
{
    public partial class AdminPanelPage : Page
    {
        private readonly string newsPath;
        private List<NewsItem> newsList = new();

        public AdminPanelPage()
        {
            InitializeComponent();

            string repoPath = Environment.GetEnvironmentVariable("GITHUB_REPO") ?? "";
            if (string.IsNullOrEmpty(repoPath))
            {
                MessageBox.Show("環境変数 GITHUB_REPO が設定されていません。");
            }

            newsPath = Path.Combine(repoPath, "News.json");

            _ = LoadNewsFromGitHubAsync();
        }

        private async Task LoadNewsFromGitHubAsync()
        {
            const string url = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManager/main/News.json";

            try
            {
                using HttpClient client = new();
                string json = await client.GetStringAsync(url + "?t=" + DateTime.UtcNow.Ticks);

                if (!string.IsNullOrEmpty(newsPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newsPath) ?? "");
                    File.WriteAllText(newsPath, json);
                }

                LoadNews();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"GitHubからニュース取得失敗: {ex.Message}\nローカルを読み込みます。");
                LoadNews();
            }
        }

        private void LoadNews()
        {
            try
            {
                if (!string.IsNullOrEmpty(newsPath) && File.Exists(newsPath))
                {
                    string json = File.ReadAllText(newsPath);
                    newsList = JsonSerializer.Deserialize<List<NewsItem>>(json) ?? new List<NewsItem>();
                }
                else
                {
                    newsList = new List<NewsItem>();
                }
            }
            catch
            {
                newsList = new List<NewsItem>();
            }

            NewsListPanel.Items.Clear();
            foreach (var news in newsList)
            {
                NewsListPanel.Items.Add(CreateNewsEditor(news));
            }
        }

        private Border CreateNewsEditor(NewsItem news)
        {
            var panel = new StackPanel { Margin = new Thickness(0) };

            // 言語切替
            var langLabel = new TextBlock { Text = "言語", FontWeight = FontWeights.SemiBold };
            var langBox = new ComboBox
            {
                ItemsSource = new List<string> { "JA", "EN" },
                SelectedIndex = 0,
                Width = 60,
                Margin = new Thickness(0, 2, 0, 6)
            };

            // タイトルと内容
            var titleLabel = new TextBlock { Text = "タイトル", FontWeight = FontWeights.SemiBold };
            var titleBox = new TextBox { Margin = new Thickness(0, 2, 0, 6) };
            var contentLabel = new TextBlock { Text = "内容", FontWeight = FontWeights.SemiBold };
            var contentBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Height = 100,
                Margin = new Thickness(0, 2, 0, 6)
            };

            void UpdateTextBoxes()
            {
                string lang = langBox.SelectedItem as string ?? "JA";
                titleBox.Text = news.Title.ContainsKey(lang) ? news.Title[lang] : "";
                contentBox.Text = news.Content.ContainsKey(lang) ? news.Content[lang] : "";
            }

            langBox.SelectionChanged += (s, e) => UpdateTextBoxes();

            titleBox.TextChanged += (s, e) =>
            {
                string lang = langBox.SelectedItem as string ?? "JA";
                news.Title[lang] = titleBox.Text;
            };

            contentBox.TextChanged += (s, e) =>
            {
                string lang = langBox.SelectedItem as string ?? "JA";
                news.Content[lang] = contentBox.Text;
            };

            UpdateTextBoxes();

            // 日付
            var dateLabel = new TextBlock { Text = "日付 (yyyy-MM-dd HH:mm:ss)", FontWeight = FontWeights.SemiBold };
            var dateBox = new TextBox
            {
                Text = news.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                Margin = new Thickness(0, 2, 0, 6),
                Width = 180
            };
            dateBox.TextChanged += (s, e) =>
            {
                if (DateTime.TryParse(dateBox.Text, out var dt))
                    news.Date = dt;
            };

            // 削除
            var deleteButton = new Button
            {
                Content = "削除",
                Width = 60,
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            deleteButton.Click += (s, e) =>
            {
                if (MessageBox.Show("このニュースを削除しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    newsList.Remove(news);
                    SaveNewsToFile();
                    LoadNews();
                }
            };

            panel.Children.Add(dateLabel);
            panel.Children.Add(dateBox);
            panel.Children.Add(langLabel);
            panel.Children.Add(langBox);
            panel.Children.Add(titleLabel);
            panel.Children.Add(titleBox);
            panel.Children.Add(contentLabel);
            panel.Children.Add(contentBox);
            panel.Children.Add(deleteButton);

            return new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 10, 0, 10),
                Background = System.Windows.Media.Brushes.AliceBlue,
                Child = panel
            };
        }

        private void AddNews_Click(object sender, RoutedEventArgs e)
        {
            var newNews = new NewsItem
            {
                Date = DateTime.Now,
                Title = new Dictionary<string, string> { { "JA", "新しいニュース" }, { "EN", "New News" } },
                Content = new Dictionary<string, string> { { "JA", "" }, { "EN", "" } }
            };
            newsList.Insert(0, newNews);
            SaveNewsToFile();
            LoadNews();
        }

        private void SaveNews_Click(object sender, RoutedEventArgs e)
        {
            SaveNewsToFile(true);
        }

        private void SaveNewsToFile(bool showDialog = false)
        {
            try
            {
                if (string.IsNullOrEmpty(newsPath)) return;

                string json = JsonSerializer.Serialize(newsList, new JsonSerializerOptions { WriteIndented = true });
                Directory.CreateDirectory(Path.GetDirectoryName(newsPath) ?? string.Empty);
                File.WriteAllText(newsPath, json);

                if (showDialog)
                    MessageBox.Show("News.json を保存しました。");
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存エラー: " + ex.Message);
            }
        }

        private void PushNews_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(newsPath)) return;
            string repoDir = Path.GetDirectoryName(newsPath) ?? "";
            RunGitCommand($"add \"{newsPath}\"", repoDir);
            RunGitCommand("commit -m \"Update news\"", repoDir);
            RunGitCommand("push", repoDir);
            MessageBox.Show("Git push を実行しました。");
        }

        private static void RunGitCommand(string arguments, string workingDirectory)
        {
            var psi = new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (!string.IsNullOrEmpty(output)) Debug.WriteLine(output);
                if (!string.IsNullOrEmpty(error)) Debug.WriteLine(error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService?.CanGoBack == true)
                this.NavigationService.GoBack();
        }

        public class NewsItem
        {
            [JsonPropertyName("date")]
            public DateTime Date { get; set; }

            [JsonPropertyName("title")]
            public Dictionary<string, string> Title { get; set; } = new();

            [JsonPropertyName("content")]
            public Dictionary<string, string> Content { get; set; } = new();
        }
    }
}
