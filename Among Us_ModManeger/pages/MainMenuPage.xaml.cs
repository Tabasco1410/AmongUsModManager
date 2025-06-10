using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Among_Us_ModManeger.Pages
{
    public partial class MainMenuPage : Page
    {
        private const string NewsUrl = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/News.json";
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmongUsModManeger");
        private static readonly string LastReadNewsFile = Path.Combine(AppDataFolder, "last_read_news.txt");
        private static readonly string VanillaConfigPath = Path.Combine(AppDataFolder, "Vanilla_Config.json");

        public MainMenuPage()
        {
            InitializeComponent();
            LogOutput.Write("MainMenuPage: 初期化開始");

            if (!Directory.Exists(AppDataFolder))
                Directory.CreateDirectory(AppDataFolder);

            _ = CheckNewsAsync();
            _ = LoadVersionAsync();
            LoadGameFolders();

            LogOutput.Write("MainMenuPage: 初期化完了");
        }

        private async Task CheckNewsAsync()
        {
            LogOutput.Write("CheckNewsAsync: お知らせチェック開始");
            try
            {
                using var client = new HttpClient();
                var json = await client.GetStringAsync(NewsUrl);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var newsList = JsonSerializer.Deserialize<List<NewsItem>>(json, options);

                if (newsList != null)
                    newsList = newsList.Where(n => n.Date <= DateTime.Now).OrderByDescending(n => n.Date).ToList();

                if (newsList?.Count > 0)
                {
                    var latestNewsDate = newsList[0].Date;
                    DateTime lastRead = File.Exists(LastReadNewsFile) ? DateTime.Parse(File.ReadAllText(LastReadNewsFile)) : DateTime.MinValue;

                    NoticeText.Visibility = latestNewsDate > lastRead ? Visibility.Visible : Visibility.Collapsed;
                    LogOutput.Write($"最新お知らせ日: {latestNewsDate}, 最終既読日: {lastRead}, 表示: {NoticeText.Visibility}");
                }

                NoticeBadge.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LogOutput.Write($"CheckNewsAsync: エラー発生 - {ex.Message}");
                NoticeText.Visibility = Visibility.Collapsed;
                NoticeBadge.Visibility = Visibility.Visible;
            }
            LogOutput.Write("CheckNewsAsync: お知らせチェック完了");
        }

        private void Mod_New_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("Mod_New_Click: Mod新規導入画面へ遷移");
            NavigationService?.Navigate(new ModSelectPage());
        }

        private void Notice_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("Notice_Click: お知らせ確認済みとして日時を保存");
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
            LogOutput.Write("LoadVersionAsync: バージョン情報読み込み開始");
            await Dispatcher.InvokeAsync(() =>
            {
                VersionText.Text = $"バージョン: {AppVersion.Version}";
                VersionText.ToolTip = new TextBlock
                {
                    Text = AppVersion.Notes,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 250
                };
                LogOutput.WriteVersion(AppVersion.Version, AppVersion.Notes);
            });
            LogOutput.Write("LoadVersionAsync: バージョン情報表示完了");
        }

        private void LoadGameFolders()
        {
            LogOutput.Write("LoadGameFolders: ゲームフォルダ読み込み開始");

            string amongUsPath = null;

            try
            {
                if (File.Exists(VanillaConfigPath))
                {
                    var json = File.ReadAllText(VanillaConfigPath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var config = JsonSerializer.Deserialize<VanillaConfig>(json);
                        amongUsPath = config?.ExePath;
                        LogOutput.Write($"設定ファイル読み込み成功: {amongUsPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogOutput.Write($"設定ファイル読み込み失敗: {ex.Message}");
                MessageBox.Show($"設定ファイルの読み込みに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (string.IsNullOrWhiteSpace(amongUsPath) || !File.Exists(amongUsPath))
            {
                LogOutput.Write("Among Us.exe が未設定または無効のため、ゲームフォルダの読み込みをスキップ");
                return;
            }

            var root = Directory.GetParent(amongUsPath)?.Parent;
            if (root == null || !Directory.Exists(root.FullName))
            {
                LogOutput.Write("Among Us.exe の親フォルダが存在しません");
                return;
            }

            var folders = Directory.GetDirectories(root.FullName)
                                   .Where(d => File.Exists(Path.Combine(d, "Among Us.exe")))
                                   .ToList();

            LogOutput.Write($"検出されたゲームフォルダ数: {folders.Count}");
            GameFolderList.Items.Clear();

            foreach (var folder in folders)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
                var folderName = Path.GetFileName(folder);

                panel.Children.Add(new TextBlock
                {
                    Text = folderName,
                    Width = 250,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0)
                });

                var exe = Path.Combine(folder, "Among Us.exe");

                panel.Children.Add(CreateButton("起動", 80, () =>
                {
                    if (File.Exists(exe))
                        Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
                    else
                        MessageBox.Show("実行ファイルが見つかりません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }));

                panel.Children.Add(CreateButton("フォルダを開く", 100, () =>
                {
                    if (Directory.Exists(folder))
                        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
                    else
                        MessageBox.Show("フォルダが存在しません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }));

                panel.Children.Add(CreateButton("名前変更", 80, () =>
                {
                    var input = Microsoft.VisualBasic.Interaction.InputBox("新しいフォルダ名を入力してください:", "名前変更", folderName);
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        var newPath = Path.Combine(root.FullName, input);
                        if (!Directory.Exists(newPath))
                        {
                            Directory.Move(folder, newPath);
                            LoadGameFolders();
                        }
                        else
                        {
                            MessageBox.Show("その名前のフォルダは既に存在します。");
                        }
                    }
                }));

                panel.Children.Add(CreateButton("削除", 80, () =>
                {
                    if (MessageBox.Show($"{folderName} を本当に削除しますか？", "確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        Directory.Delete(folder, true);
                        LoadGameFolders();
                    }
                }));

                GameFolderList.Items.Add(panel);
            }

            LogOutput.Write("LoadGameFolders: ゲームフォルダ読み込み完了");
        }

        private Button CreateButton(string content, double width, Action onClick)
        {
            var button = new Button { Content = content, Width = width, Margin = new Thickness(0, 0, 5, 0) };
            button.Click += (_, _) => onClick();
            return button;
        }

        private void SelectExeButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Among Us.exe を選択してください",
                Filter = "Among Us (*.exe)|Among Us.exe"
            };

            if (dialog.ShowDialog() == true)
            {
                var amongUsPath = dialog.FileName;
                var config = new VanillaConfig { ExePath = amongUsPath };
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(VanillaConfigPath, json);

                LogOutput.Write($"Among Us.exe パス選択完了: {amongUsPath}");

                LoadGameFolders();
            }
            else
            {
                LogOutput.Write("Among Us.exe の選択がキャンセルされました");
            }
        }

        private class VanillaConfig
        {
            public string ExePath { get; set; }
        }
    }
}
