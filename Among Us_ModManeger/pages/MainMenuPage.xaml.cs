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
using Among_Us_ModManeger; // ModInfoを参照

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

        private const string AdminHash = "4920dd2c464d1daacdd048f45189d1a4232402af4b9f6cff8172b489b3ba9988";

        public MainMenuPage()
        {
            LogOutput.Write("MainMenuPage コンストラクタ 開始");
            InitializeComponent();

            try
            {
                Directory.CreateDirectory(AppDataFolder);
                _ = InitializeAsync();
                CheckAdminButtonVisibility();

                // 導入済みMOD一覧の表示
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
                Debug.WriteLine($"ERROR: CheckNewsAsync failed: {ex.Message}");
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
                Debug.WriteLine($"ERROR: CheckUpdateButtonAsync failed: {ex.Message}");
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
                else
                {
                    LogOutput.Write("ユーザーがアップデート実行を拒否");
                }
            }
            catch (Exception ex)
            {
                LogOutput.Write($"UpdateButton_Click 例外: {ex.Message}");
                throw;
            }
            LogOutput.Write("UpdateButton_Click 終了");
        }

        private void Mod_New_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("Mod_New_Click 開始");
            try
            {
                NavigationService?.Navigate(new ModSelectPage());
                LogOutput.Write("Mod_New_Click 成功");
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
                LogOutput.Write($"検出されたAmong Usインストール数: {amongUsInstallations.Count()}");

                if (!amongUsInstallations.Any())
                {
                    LogOutput.Write("Among Usインストールフォルダが見つからなかった");
                    MessageBox.Show("Among Usのインストールフォルダが検出できませんでした。\n「Among Us.exe を選択」で設定を確認してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                NavigationService?.Navigate(new Among_Us_ModManeger.Pages.Mod_Update.Select_Updatemod(amongUsInstallations));
                LogOutput.Write("ModUpdateCheck_Click ナビゲーション成功");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"ModUpdateCheck_Click 例外: {ex.Message}");
                throw;
            }
            finally
            {
                loadingWindow.Close();
                LogOutput.Write("ModUpdateCheck_Click ローディングウィンドウ閉じる");
            }
        }

        private void Notice_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("Notice_Click 開始");
            try
            {
                File.WriteAllText(LastReadNewsFile, DateTime.Now.ToString("s"));
                NoticeText.Visibility = Visibility.Collapsed;
                NavigationService?.Navigate(new News());
                LogOutput.Write("Notice_Click 成功");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"Notice_Click 例外: {ex.Message}");
                throw;
            }
        }

        private void Note_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("Note_Click 開始");
            try
            {
                NavigationService?.Navigate(new Among_Us_ModManeger.NoteSelectionPage());
                LogOutput.Write("Note_Click 成功");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"Note_Click 例外: {ex.Message}");
                throw;
            }
        }

        private void SelectExeButton_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("SelectExeButton_Click 開始");
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
                    LogOutput.Write($"Among Us.exeパス保存成功: {config.ExePath}");
                }
                catch (Exception ex)
                {
                    LogOutput.Write($"SelectExeButton_Click 設定保存エラー: {ex.Message}");
                    MessageBox.Show($"設定の保存中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                LoadGameFolders();
            }
            else
            {
                LogOutput.Write("SelectExeButton_Click ユーザーがファイル選択をキャンセル");
            }
        }

        private void LoadGameFolders()
        {
            LogOutput.Write("LoadGameFolders 開始");
            GameFolderList.Items.Clear();

            try
            {
                var amongUsInstallations = Among_Us_ModManeger.AmongUsModDetector.DetectAmongUsInstallations();
                LogOutput.Write($"検出されたAmong Usインストール数: {amongUsInstallations.Count()}");

                if (!amongUsInstallations.Any())
                {
                    GameFolderList.Items.Add(new TextBlock { Text = "Among Usフォルダが検出されませんでした。", Foreground = System.Windows.Media.Brushes.Gray, Margin = new Thickness(0, 10, 0, 0) });
                    LogOutput.Write("Among Usフォルダが検出されなかったため処理終了");
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
                        LogOutput.Write($"起動ボタン押下: {folderName}");
                        try
                        {
                            if (!File.Exists(exeFullPath))
                            {
                                LogOutput.Write($"Among Us.exeが存在しません: {exeFullPath}");
                                MessageBox.Show("Among Us.exeが見つかりません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            string appIdPath = Path.Combine(installation.InstallPath, "steam_appid.txt");
                            if (!File.Exists(appIdPath))
                            {
                                using var client = new HttpClient();
                                var appIdContent = await client.GetStringAsync("https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/steam_appid.txt");
                                File.WriteAllText(appIdPath, appIdContent);
                                LogOutput.Write("steam_appid.txt を書き込みました");
                            }

                            Process.Start(new ProcessStartInfo(exeFullPath) { UseShellExecute = true });
                            LogOutput.Write("Among Us.exe の起動に成功");
                        }
                        catch (Exception ex)
                        {
                            LogOutput.Write($"Among Us起動失敗: {ex.Message}");
                            MessageBox.Show($"起動に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }));

                    sp.Children.Add(CreateButton("フォルダを開く", 100, () =>
                    {
                        LogOutput.Write($"フォルダを開くボタン押下: {folderName}");
                        if (Directory.Exists(installation.InstallPath))
                            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{installation.InstallPath}\"") { UseShellExecute = true });
                    }));

                    sp.Children.Add(CreateButton("名前変更", 80, () =>
                    {
                        LogOutput.Write($"名前変更ボタン押下: {folderName}");
                        var input = Microsoft.VisualBasic.Interaction.InputBox("新しいフォルダ名を入力してください:", "名前変更", folderName);
                        if (!string.IsNullOrWhiteSpace(input))
                        {
                            var parentOfAmongUsRoot = Directory.GetParent(installation.InstallPath);
                            if (parentOfAmongUsRoot == null)
                            {
                                LogOutput.Write("名前変更時に親ディレクトリが見つからなかった");
                                MessageBox.Show("親ディレクトリが見つかりません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            var newPath = Path.Combine(parentOfAmongUsRoot.FullName, input);
                            if (!Directory.Exists(newPath))
                            {
                                Directory.Move(installation.InstallPath, newPath);
                                LogOutput.Write($"フォルダ名を {folderName} から {input} に変更しました");
                                LoadGameFolders();
                            }
                            else
                            {
                                LogOutput.Write("名前変更しようとした名前が既に存在していた");
                                MessageBox.Show("その名前のフォルダは既に存在します。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        else
                        {
                            LogOutput.Write("名前変更入力がキャンセルまたは空白");
                        }
                    }));

                    sp.Children.Add(CreateButton("削除", 80, () =>
                    {
                        LogOutput.Write($"削除ボタン押下: {folderName}");
                        if (MessageBox.Show($"{folderName} を本当に削除しますか？\nこの操作は元に戻せません。", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            try
                            {
                                Directory.Delete(installation.InstallPath, true);
                                LogOutput.Write($"{folderName} のフォルダを削除しました");
                                LoadGameFolders();
                            }
                            catch (Exception ex)
                            {
                                LogOutput.Write($"フォルダ削除失敗: {ex.Message}");
                                MessageBox.Show($"フォルダの削除に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            LogOutput.Write("削除操作がユーザーによりキャンセルされた");
                        }
                    }));

                    GameFolderList.Items.Add(sp);
                }

                LogOutput.Write("LoadGameFolders 成功");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"LoadGameFolders 例外: {ex.Message}");
                MessageBox.Show($"ゲームフォルダの読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"ERROR: LoadGameFolders failed: {ex.Message}");
            }
        }

        private Button CreateButton(string content, double width, Action onClick)
        {
            // ボタン作成時のログは冗長になりやすいため割愛（必要ならここにも追加可）
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
            LogOutput.Write("ComputeSha256Hash 開始");
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                string hash = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
                LogOutput.Write("ComputeSha256Hash 成功");
                return hash;
            }
        }

        private void CheckAdminButtonVisibility()
        {
            LogOutput.Write("CheckAdminButtonVisibility 開始");
            try
            {
                if (File.Exists(AdminCheckFile))
                {
                    string content = File.ReadAllText(AdminCheckFile).Trim();
                    string hash = ComputeSha256Hash(content);
                    if (hash == AdminHash)
                    {
                        AdminPageButton.Visibility = Visibility.Visible;
                        LogOutput.Write("AdminPageButton を表示");
                        return;
                    }
                }
                AdminPageButton.Visibility = Visibility.Collapsed;
                LogOutput.Write("AdminPageButton を非表示");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"CheckAdminButtonVisibility 例外: {ex.Message}");
                Debug.WriteLine("DEBUG: AdminCheckFile read error or AdminHash mismatch.");
                AdminPageButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void AdminLogin_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("AdminLogin_Click 開始");
            try
            {
                if (File.Exists(AdminCheckFile))
                {
                    string content = File.ReadAllText(AdminCheckFile).Trim();
                    string hash = ComputeSha256Hash(content);
                    if (hash == AdminHash)
                    {
                        NavigationService?.Navigate(new AdminPanelPage());
                        LogOutput.Write("AdminPanelPageへナビゲート成功");
                        return;
                    }
                }

                MessageBox.Show("管理者権限がありません。", "アクセス拒否", MessageBoxButton.OK, MessageBoxImage.Warning);
                LogOutput.Write("管理者権限なしのメッセージ表示");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"AdminLogin_Click 例外: {ex.Message}");
                MessageBox.Show($"管理者ページの読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"ERROR: AdminLogin_Click failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 導入済みMOD一覧をListBoxに表示する
        /// </summary>
        private void LoadInstalledMods()
        {
            LogOutput.Write("LoadInstalledMods 開始");
            try
            {
                var mods = GetInstalledMods();
                InstalledModsListBox.ItemsSource = mods;
                LogOutput.Write($"LoadInstalledMods 成功: {mods.Count}個のMODを表示");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"LoadInstalledMods 例外: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 導入済みMODリストを取得する（仮実装）
        /// </summary>
        private List<ModInfo> GetInstalledMods()
        {
            LogOutput.Write("GetInstalledMods 開始");
            var mods = new List<ModInfo>();
            try
            {
                var amongUsInstallations = Among_Us_ModManeger.AmongUsModDetector.DetectAmongUsInstallations();
                LogOutput.Write($"検出されたAmong Usインストール数: {amongUsInstallations.Count()}");

                foreach (var installation in amongUsInstallations)
                {
                    var pluginsPath = Path.Combine(installation.InstallPath, "BepInEx", "plugins");
                    if (!Directory.Exists(pluginsPath))
                    {
                        LogOutput.Write($"pluginsフォルダが存在しません: {pluginsPath}");
                        continue;
                    }

                    foreach (var dll in Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories))
                    {
                        var modName = Path.GetFileNameWithoutExtension(dll);
                        if (mods.Any(m => m.Name == modName)) continue;

                        mods.Add(new ModInfo(
                            modName,
                            "", // GitHubUrlは不明な場合空欄
                            new List<string> { Path.GetRelativePath(installation.InstallPath, dll) }
                        )
                        {
                            DetectionStatus = "導入済み"
                        });
                        LogOutput.Write($"MOD検出: {modName}");
                    }
                }
                LogOutput.Write("GetInstalledMods 成功");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"GetInstalledMods 例外: {ex.Message}");
                throw;
            }
            return mods;
        }
    }
}
