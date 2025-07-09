using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Win32;
using Among_Us_ModManeger.Updates;
using Among_Us_ModManeger; // ModInfoなど参照用

namespace Among_Us_ModManeger.Pages
{
    public partial class MainMenuPage : Page
    {
        private const string NewsUrl = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/News.json";
        private const string VersionUrl = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/version.txt";

        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmongUsModManeger");
        private static readonly string LastReadNewsFile = Path.Combine(AppDataFolder, "last_read_news.txt");
        private static readonly string VanillaConfigPath = Path.Combine(AppDataFolder, "Vanilla_Config.json");
        private static readonly string AdminCheckFile = Path.Combine(AppDataFolder, "admin.txt");

        // 管理者認証済みであることを示すSHA256ハッシュ値
        private const string AdminHash = "4920dd2c464d1daacdd048f45189d1a4232402af4b9f6cff8172b489b3ba9988";

        private readonly OAuthManager _oauthManager;

        public MainMenuPage()
        {
            LogOutput.Write("MainMenuPage コンストラクタ 開始");
            InitializeComponent();

            _oauthManager = OAuthManager.Instance;

            try
            {
                Directory.CreateDirectory(AppDataFolder);
                _ = InitializeAsync();
                CheckAdminButtonVisibility();
                LoadInstalledMods();

                LogOutput.Write("MainMenuPage コンストラクタ 成功");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"MainMenuPage コンストラクタ 例外: {ex.Message}");
                throw;
            }
        }

        private async Task InitializeAsync()
        {
            LogOutput.Write("InitializeAsync 開始");
            try
            {
                await LoadVersionAsync();
                await CheckNewsAsync();
                await CheckUpdateButtonAsync();
                LoadGameFolders();
                LogOutput.Write("InitializeAsync 成功");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"InitializeAsync 例外: {ex.Message}");
                throw;
            }
        }

        private async Task LoadVersionAsync()
        {
            LogOutput.Write("LoadVersionAsync 開始");
            try
            {
                VersionText.Text = $"バージョン: {AppVersion.Version}";
                VersionText.ToolTip = new TextBlock
                {
                    Text = AppVersion.Notes,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 250
                };
                LogOutput.WriteVersion(AppVersion.Version, AppVersion.Notes);
                LogOutput.Write("LoadVersionAsync 成功");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LogOutput.Write($"LoadVersionAsync 例外: {ex.Message}");
                throw;
            }
        }

        private async Task CheckNewsAsync()
        {
            LogOutput.Write("CheckNewsAsync 開始");
            try
            {
                using var client = new HttpClient();
                var json = await client.GetStringAsync(NewsUrl);
                LogOutput.Write("ニュースJSON取得成功");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var newsList = JsonSerializer.Deserialize<List<NewsItem>>(json, options)?
                    .Where(n => n.Date <= DateTime.Now)
                    .OrderByDescending(n => n.Date)
                    .ToList();

                if (newsList?.Count > 0)
                {
                    var latestDate = newsList[0].Date;
                    LogOutput.Write($"最新ニュース日付: {latestDate}");

                    var lastRead = DateTime.MinValue;
                    if (File.Exists(LastReadNewsFile))
                    {
                        var lastReadStr = File.ReadAllText(LastReadNewsFile);
                        DateTime.TryParse(lastReadStr, out lastRead);
                        LogOutput.Write($"最終既読日付: {lastRead}");
                    }

                    NoticeText.Visibility = latestDate > lastRead ? Visibility.Visible : Visibility.Collapsed;
                    LogOutput.Write($"NoticeText 表示: {NoticeText.Visibility == Visibility.Visible}");
                }
                else
                {
                    NoticeText.Visibility = Visibility.Collapsed;
                    LogOutput.Write("ニュースリストなし");
                }

                NoticeBadge.Visibility = Visibility.Visible;
                LogOutput.Write("CheckNewsAsync 成功");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"CheckNewsAsync 例外: {ex.Message}");
                NoticeText.Visibility = Visibility.Collapsed;
                NoticeBadge.Visibility = Visibility.Visible;
            }
        }

        private async Task CheckUpdateButtonAsync()
        {
            LogOutput.Write("CheckUpdateButtonAsync 開始");
            try
            {
                bool isUpdateAvailable = await AppUpdater.IsUpdateAvailableAsync(AppVersion.Version, VersionUrl);
                LogOutput.Write($"アップデート有無: {isUpdateAvailable}");

                UpdateNoticeText.Visibility = isUpdateAvailable ? Visibility.Visible : Visibility.Collapsed;
                UpdateButton.Visibility = isUpdateAvailable ? Visibility.Visible : Visibility.Collapsed;

                LogOutput.Write("CheckUpdateButtonAsync 成功");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"CheckUpdateButtonAsync 例外: {ex.Message}");
                UpdateNoticeText.Visibility = Visibility.Collapsed;
                UpdateButton.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("UpdateButton_Click 開始");
            try
            {
                if (MessageBox.Show("新しいバージョンがあります。アップデートしますか？", "アップデート確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    LogOutput.Write("ユーザーがアップデート実行を承認");
                    AppUpdater.StartUpdaterAndExit();
                }
            }
            catch (Exception ex)
            {
                LogOutput.Write($"UpdateButton_Click 例外: {ex.Message}");
                throw;
            }
        }

        private void Mod_New_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("Mod_New_Click 開始");
            try
            {
                NavigationService?.Navigate(new ModSelectPage());
            }
            catch (Exception ex)
            {
                LogOutput.Write($"Mod_New_Click 例外: {ex.Message}");
                throw;
            }
        }

        private async void ModUpdateCheck_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("ModUpdateCheck_Click 開始");

            var loadingWindow = new Window
            {
                Title = "読み込み中",
                Width = 250,
                Height = 100,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Text = "MOD情報を取得中...",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 16
                },
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Owner = Window.GetWindow(this),
                ShowInTaskbar = false,
                Topmost = true
            };

            loadingWindow.Show();

            try
            {
                await Task.Yield(); // ウィンドウ描画を確実に反映
                var amongUsInstallations = await Task.Run(() => Among_Us_ModManeger.AmongUsModDetector.DetectAmongUsInstallations());

                if (!amongUsInstallations.Any())
                {
                    MessageBox.Show("Among Usのインストールフォルダが検出できませんでした。\n「Among Us.exe を選択」で設定を確認してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                NavigationService?.Navigate(new Among_Us_ModManeger.Pages.Mod_Update.Select_Updatemod(amongUsInstallations));
            }
            catch (Exception ex)
            {
                LogOutput.Write($"ModUpdateCheck_Click 例外: {ex.Message}");
                throw;
            }
            finally
            {
                loadingWindow.Close();
            }
        }

        private void Notice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.WriteAllText(LastReadNewsFile, DateTime.Now.ToString("s"));
                NoticeText.Visibility = Visibility.Collapsed;
                NavigationService?.Navigate(new News());
            }
            catch (Exception ex)
            {
                LogOutput.Write($"Notice_Click 例外: {ex.Message}");
                throw;
            }
        }

        private void Note_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService?.Navigate(new Among_Us_ModManeger.NoteSelectionPage());
            }
            catch (Exception ex)
            {
                LogOutput.Write($"Note_Click 例外: {ex.Message}");
                throw;
            }
        }

        private void SelectExeButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Among Us.exe を選択してください",
                Filter = "Among Us.exe|Among Us.exe"
            };

            if (dialog.ShowDialog() == true)
            {
                var config = new Among_Us_ModManeger.VanillaConfig { ExePath = dialog.FileName };
                try
                {
                    File.WriteAllText(VanillaConfigPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
                    MessageBox.Show($"Among Us.exeのパスを保存しました:\n{config.ExePath}", "設定完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LogOutput.Write($"SelectExeButton_Click 設定保存エラー: {ex.Message}");
                    MessageBox.Show($"設定の保存中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                LoadGameFolders();
            }
        }

        private void LoadGameFolders()
        {
            GameFolderList.Items.Clear();

            var amongUsInstallations = Among_Us_ModManeger.AmongUsModDetector.DetectAmongUsInstallations();

            if (!amongUsInstallations.Any())
            {
                GameFolderList.Items.Add(new TextBlock { Text = "Among Usフォルダが検出されませんでした。", Foreground = System.Windows.Media.Brushes.Gray, Margin = new Thickness(0, 10, 0, 0) });
                return;
            }

            foreach (var installation in amongUsInstallations)
            {
                var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
                var folderName = installation.Name;
                var exeFullPath = installation.ExePath;

                sp.Children.Add(new TextBlock
                {
                    Text = folderName,
                    Width = 250,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0)
                });

                sp.Children.Add(CreateButton("起動", 80, async () =>
                {
                    if (!File.Exists(exeFullPath))
                    {
                        MessageBox.Show("Among Us.exeが見つかりません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string appIdPath = Path.Combine(installation.InstallPath, "steam_appid.txt");
                    if (!File.Exists(appIdPath))
                    {
                        using var client = new HttpClient();
                        var appIdContent = await client.GetStringAsync("https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/steam_appid.txt");
                        File.WriteAllText(appIdPath, appIdContent);
                    }

                    Process.Start(new ProcessStartInfo(exeFullPath) { UseShellExecute = true });
                }));

                sp.Children.Add(CreateButton("フォルダを開く", 100, () =>
                {
                    if (Directory.Exists(installation.InstallPath))
                        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{installation.InstallPath}\"") { UseShellExecute = true });
                }));

                sp.Children.Add(CreateButton("名前変更", 80, () =>
                {
                    var input = Microsoft.VisualBasic.Interaction.InputBox("新しいフォルダ名を入力してください:", "名前変更", folderName);
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        var parentOfAmongUsRoot = Directory.GetParent(installation.InstallPath);
                        if (parentOfAmongUsRoot == null) return;

                        var newPath = Path.Combine(parentOfAmongUsRoot.FullName, input);
                        if (!Directory.Exists(newPath))
                        {
                            Directory.Move(installation.InstallPath, newPath);
                            LoadGameFolders();
                        }
                        else
                        {
                            MessageBox.Show("その名前のフォルダは既に存在します。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }));

                sp.Children.Add(CreateButton("削除", 80, () =>
                {
                    if (MessageBox.Show($"{folderName} を本当に削除しますか？\nこの操作は元に戻せません。", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Directory.Delete(installation.InstallPath, true);
                            LoadGameFolders();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"フォルダの削除に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }));

                GameFolderList.Items.Add(sp);
            }
        }

        private Button CreateButton(string content, double width, Action onClick)
        {
            var button = new Button { Content = content, Width = width, Margin = new Thickness(0, 0, 5, 0) };
            button.Click += (_, _) => onClick();
            return button;
        }

        public class NewsItem
        {
            public DateTime Date { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// 管理者用ファイル(admin.txt)を読み、SHA256ハッシュと比較して
        /// 問題なければ管理者ページボタンを表示する
        /// </summary>
        private void CheckAdminButtonVisibility()
        {
            if (File.Exists(AdminCheckFile))
            {
                string content = File.ReadAllText(AdminCheckFile).Trim();
                string hash = ComputeSha256Hash(content);
                if (hash == AdminHash)
                {
                    AdminPageButton.Visibility = Visibility.Visible;
                    return;
                }
            }
            AdminPageButton.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 管理者ページボタン押下時のイベント
        /// 認証がまだならOAuth認証へ遷移し、認証済みなら管理者ページへ遷移する
        /// </summary>
        private async void AdminLogin_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("AdminLogin_Click 開始");
            try
            {
                if (_oauthManager.IsLoggedIn && _oauthManager.IsAdmin)
                {
                    NavigationService?.Navigate(new AdminPanelPage());
                    LogOutput.Write($"管理者認証済み。UserName={_oauthManager.UserName}");
                    return;
                }

                // 未認証または管理者権限なしの場合、認証処理を開始
                bool loginSuccess = await _oauthManager.LoginAsync();

                if (loginSuccess)
                {
                    LogOutput.Write($"認証成功。UserName={_oauthManager.UserName}");
                    if (_oauthManager.IsAdmin)
                    {
                        NavigationService?.Navigate(new AdminPanelPage());
                    }
                    else
                    {
                        MessageBox.Show("管理者権限がありません。", "アクセス拒否", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("認証に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                LogOutput.Write($"AdminLogin_Click 例外: {ex.Message}");
                MessageBox.Show($"認証処理中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void LoadInstalledMods()
        {
            // 導入済みMOD一覧の表示をここに実装してください
            // 例: InstalledModList.Items.Clear();
            //     foreach(var mod in InstalledMods) { ... }
        }
    }
}
