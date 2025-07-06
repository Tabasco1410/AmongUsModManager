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

namespace Among_Us_ModManeger.Pages
{
    public partial class AdminPanelPage : Page
    {
        private readonly string newsPath;
        private List<NewsItem> newsList = new();

        public AdminPanelPage()
        {
            InitializeComponent();

            Debug.WriteLine("AdminPanelPage: Constructor start");

            string repoPath = Environment.GetEnvironmentVariable("GITHUB_REPO") ?? "";
            if (string.IsNullOrEmpty(repoPath))
            {
                MessageBox.Show("環境変数 GITHUB_REPO が設定されていません。");
                Debug.WriteLine("AdminPanelPage: GITHUB_REPO environment variable is empty.");
            }

            newsPath = Path.Combine(repoPath, "News.json");

            _ = LoadNewsFromGitHubAsync();

            Debug.WriteLine("AdminPanelPage: Constructor end");
        }

        private async Task LoadNewsFromGitHubAsync()
        {
            const string url = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/News.json";

            Debug.WriteLine("LoadNewsFromGitHubAsync: Start loading news from GitHub.");

            try
            {
                using HttpClient client = new();
                // キャッシュ回避
                string urlWithCacheBuster = url + "?t=" + DateTime.UtcNow.Ticks;
                string json = await client.GetStringAsync(urlWithCacheBuster);
                Debug.WriteLine("LoadNewsFromGitHubAsync: Successfully downloaded news JSON from GitHub.");

                if (!string.IsNullOrEmpty(newsPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newsPath) ?? "");
                    File.WriteAllText(newsPath, json);
                    Debug.WriteLine($"LoadNewsFromGitHubAsync: Saved news JSON to local file: {newsPath}");
                }

                LoadNews();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadNewsFromGitHubAsync: Exception caught - {ex.Message}");
                MessageBox.Show($"GitHubからニュースの取得に失敗しました。\nローカルファイルを読み込みます。\n\nエラー: {ex.Message}");
                LoadNews();
            }
        }

        private void LoadNews()
        {
            Debug.WriteLine("LoadNews: Start loading news from local file.");

            try
            {
                if (!string.IsNullOrEmpty(newsPath) && File.Exists(newsPath))
                {
                    string json = File.ReadAllText(newsPath);
                    newsList = JsonSerializer.Deserialize<List<NewsItem>>(json) ?? new List<NewsItem>();
                    Debug.WriteLine($"LoadNews: Loaded {newsList.Count} news items.");
                }
                else
                {
                    Debug.WriteLine("LoadNews: newsPath is null or file does not exist.");
                    newsList = new List<NewsItem>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadNews: Exception caught - {ex.Message}");
                MessageBox.Show($"ニュースの読み込みに失敗しました: {ex.Message}");
                newsList = new List<NewsItem>();
            }

            NewsListPanel.Items.Clear();
            foreach (var news in newsList)
            {
                NewsListPanel.Items.Add(CreateNewsEditor(news));
            }

            Debug.WriteLine("LoadNews: Finished populating UI with news items.");
        }

        private Border CreateNewsEditor(NewsItem news)
        {
            var panel = new StackPanel { Margin = new Thickness(0) };

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

            var titleLabel = new TextBlock { Text = "タイトル", FontWeight = FontWeights.SemiBold };
            var titleBox = new TextBox
            {
                Text = news.Title,
                Margin = new Thickness(0, 2, 0, 6)
            };
            titleBox.TextChanged += (s, e) => news.Title = titleBox.Text;

            var contentLabel = new TextBlock { Text = "内容", FontWeight = FontWeights.SemiBold };
            var contentBox = new TextBox
            {
                Text = news.Content,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Height = 100,
                Margin = new Thickness(0, 2, 0, 6)
            };
            contentBox.TextChanged += (s, e) => news.Content = contentBox.Text;

            var deleteButton = new Button
            {
                Content = "削除",
                Width = 60,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 4, 0, 0)
            };
            deleteButton.Click += (s, e) =>
            {
                if (MessageBox.Show("このニュースを削除しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    newsList.Remove(news);
                    Debug.WriteLine($"CreateNewsEditor: News item deleted - Title: {news.Title}");
                    SaveNewsToFile();
                    LoadNews();
                }
            };

            panel.Children.Add(dateLabel);
            panel.Children.Add(dateBox);
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
                Title = "新しいニュース",
                Content = ""
            };
            newsList.Insert(0, newNews);
            Debug.WriteLine("AddNews_Click: Added new news item.");

            SaveNewsToFile();
            LoadNews();
        }

        private void SaveNews_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("SaveNews_Click: Saving news to file.");
            SaveNewsToFile(showDialog: true);
        }

        private void SaveNewsToFile(bool showDialog = false)
        {
            Debug.WriteLine("SaveNewsToFile: Start saving news JSON to file.");

            try
            {
                if (string.IsNullOrEmpty(newsPath))
                {
                    MessageBox.Show("保存先のパスが設定されていません。");
                    Debug.WriteLine("SaveNewsToFile: newsPath is null or empty.");
                    return;
                }

                string json = JsonSerializer.Serialize(newsList, new JsonSerializerOptions { WriteIndented = true });
                Directory.CreateDirectory(Path.GetDirectoryName(newsPath) ?? string.Empty);
                File.WriteAllText(newsPath, json);

                Debug.WriteLine($"SaveNewsToFile: Successfully saved news to {newsPath}.");

                if (showDialog)
                    MessageBox.Show("News.json を保存しました。");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveNewsToFile: Exception caught - {ex.Message}");
                MessageBox.Show("保存エラー: " + ex.Message);
            }
        }

        private void PushNews_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("PushNews_Click: Starting git push process.");

            try
            {
                var commitTitle = Microsoft.VisualBasic.Interaction.InputBox("コミットタイトルを入力してください：", "Git Commit", "お知らせの更新");
                if (string.IsNullOrWhiteSpace(commitTitle))
                {
                    MessageBox.Show("コミットタイトルが空です。");
                    Debug.WriteLine("PushNews_Click: Commit title was empty.");
                    return;
                }

                var commitDescription = Microsoft.VisualBasic.Interaction.InputBox("コミットの説明を入力してください（任意）：", "Git Commit 説明", "");

                string repoDir = Path.GetDirectoryName(newsPath) ?? "";
                if (string.IsNullOrEmpty(repoDir))
                {
                    MessageBox.Show("Gitリポジトリのパスが不正です。");
                    Debug.WriteLine("PushNews_Click: repoDir is null or empty.");
                    return;
                }

                RunGitCommand($"add \"{newsPath}\"", repoDir);
                string commitMsg = commitTitle + (!string.IsNullOrWhiteSpace(commitDescription) ? "\n\n" + commitDescription : "");
                RunGitCommand($"commit -m \"{commitMsg.Replace("\"", "\\\"")}\"", repoDir);
                RunGitCommand("push", repoDir);

                MessageBox.Show("Git push を実行しました。");
                Debug.WriteLine("PushNews_Click: Git push completed successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PushNews_Click: Exception caught - {ex.Message}");
                MessageBox.Show("Git Push に失敗しました: " + ex.Message);
            }
        }

        private static void RunGitCommand(string arguments, string workingDirectory)
        {
            Debug.WriteLine($"RunGitCommand: Running git command: git {arguments} in {workingDirectory}");

            var processInfo = new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    Debug.WriteLine("Git output:\n" + output);
                    MessageBox.Show("Git output:\n" + output);
                }
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.WriteLine("Git error:\n" + error);
                    MessageBox.Show("Git error:\n" + error);
                }
            }
            else
            {
                Debug.WriteLine("RunGitCommand: Git process start failed.");
                MessageBox.Show("Git コマンドの起動に失敗しました。");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("BackButton_Click: Back button pressed.");

            if (this.NavigationService?.CanGoBack == true)
            {
                this.NavigationService.GoBack();
                Debug.WriteLine("BackButton_Click: Navigated back.");
            }
            else
            {
                MessageBox.Show("戻れるページがありません。");
                Debug.WriteLine("BackButton_Click: No page to go back to.");
            }
        }

        public class NewsItem
        {
            [JsonPropertyName("date")]
            public DateTime Date { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; } = "";

            [JsonPropertyName("content")]
            public string Content { get; set; } = "";
        }
    }
}
