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
using Among_Us_ModManager.Models;
using Among_Us_ModManager.Models.Updates;

namespace Among_Us_ModManager.Pages
{
    public partial class MainMenuPage : Page
    {
        private const string NewsUrl = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManager/main/News.json";
        private const string VersionUrl = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManager/main/version.txt";

        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmongUsModManager");
        private static readonly string LastReadNewsFile = Path.Combine(AppDataFolder, "last_read_news.txt");
        private static readonly string VanillaConfigPath = Path.Combine(AppDataFolder, "Vanilla_Config.json");

        private static readonly string AdminCheckFile = Path.Combine(AppDataFolder, "admin.txt");

        private const string AdminHash = "4920dd2c464d1daacdd048f45189d1a4232402af4b9f6cff8172b489b3ba9988";

        private readonly OAuthManager _oauthManager;

        public MainMenuPage()
        {
            LogOutput.Write("MainMenuPage コンストラクタ開始: UI初期化と初期処理開始");
            InitializeComponent();

            _oauthManager = OAuthManager.Instance;

            try
            {
                Directory.CreateDirectory(AppDataFolder);
                LogOutput.Write($"MainMenuPage コンストラクタ: アプリデータフォルダを作成・確認しました: {AppDataFolder}");

                _ = InitializeAsync();

                CheckAdminButtonVisibility();
                LogOutput.Write("MainMenuPage コンストラクタ: 管理者ページボタンの表示判定を実施しました。");

                LoadInstalledMods();
                LogOutput.Write("MainMenuPage コンストラクタ: 導入済みMODの一覧表示を開始しました。");

                LogOutput.Write("MainMenuPage コンストラクタ正常終了: 初期処理呼び出し完了");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"MainMenuPage コンストラクタ例外: {ex.Message}\nスタックトレース:\n{ex.StackTrace}");
                throw;
            }
      
            InitializeComponent();
            _oauthManager = OAuthManager.Instance;

            LogOutput.Write("MainMenuPage 初期化完了。");

            // 画面表示後に実行（体感高速化）
            Loaded += async (s, e) =>
            {
                try
                {
                    LogOutput.Write("MainMenuPage Loaded: 初期化処理開始");
                    await LoadVersionAsync();
                    await CheckNewsAsync();
                    await CheckUpdateButtonAsync();
                    LoadGameFolders();
                    CheckAdminButtonVisibility();
                    LoadInstalledMods();
                    LogOutput.Write("MainMenuPage Loaded: 初期化処理完了");
                }
                catch (Exception ex)
                {
                    LogOutput.Write($"MainMenuPage 初期化中にエラー: {ex.Message}");
                }
            };
        }

        private async Task InitializeAsync()
        {
            LogOutput.Write("InitializeAsync 開始: バージョン取得、ニュース確認、アップデート判定、ゲームフォルダ読み込みを順に実行します。");
            try
            {
                await LoadVersionAsync();
                LogOutput.Write("InitializeAsync: バージョン情報読み込み成功");

                await CheckNewsAsync();
                LogOutput.Write("InitializeAsync: ニュースチェックと通知表示更新成功");

                await CheckUpdateButtonAsync();
                LogOutput.Write("InitializeAsync: アップデート有無チェックとUI制御成功");

                LoadGameFolders();
                LogOutput.Write("InitializeAsync: Among Usインストールフォルダ検出とUI反映成功");

                LogOutput.Write("InitializeAsync 正常終了: 初期処理すべて完了");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"InitializeAsync 例外: {ex.Message}\nスタックトレース:\n{ex.StackTrace}");
                throw;
            }
        }

        private async Task LoadVersionAsync()
        {
            LogOutput.Write("LoadVersionAsync 開始: アプリバージョン情報をUIにセット");
            try
            {
                VersionText.Text = $"バージョン: {AppVersion.Version}";
                VersionText.ToolTip = new TextBlock
                {
                    Text = AppVersion.Notes,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 250
                };
                LogOutput.Write($"LoadVersionAsync 成功: バージョン表示更新 Version={AppVersion.Version}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LogOutput.Write($"LoadVersionAsync 例外: {ex.Message}\nスタックトレース:\n{ex.StackTrace}");
                throw;
            }
        }

        private async Task CheckNewsAsync()
        {
            LogOutput.Write("CheckNewsAsync 開始: GitHubからNews.jsonを取得し最新ニュースと最終既読日を比較します。");
            try
            {
                using var client = new HttpClient();
                var json = await client.GetStringAsync(NewsUrl);
                LogOutput.Write("CheckNewsAsync: News.json取得成功");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var noteList = JsonSerializer.Deserialize<List<NoteItem>>(json, options);

                // Dateが文字列なのでDateTimeに変換してフィルター＆ソート
                var filteredOrderedNotes = noteList?
                    .Where(n => DateTime.TryParse(n.Date, out var d) && d <= DateTime.Now)
                    .OrderByDescending(n => DateTime.Parse(n.Date))
                    .ToList();

                if (filteredOrderedNotes?.Count > 0)
                {
                    var latestDate = DateTime.Parse(filteredOrderedNotes[0].Date);
                    LogOutput.Write($"CheckNewsAsync: 最新ニュース日付 {latestDate} を取得");

                    var lastRead = DateTime.MinValue;
                    if (File.Exists(LastReadNewsFile))
                    {
                        var lastReadStr = File.ReadAllText(LastReadNewsFile);
                        DateTime.TryParse(lastReadStr, out lastRead);
                        LogOutput.Write($"CheckNewsAsync: ユーザーの最終既読日時 {lastRead} をファイルから読み込み");
                    }
                    else
                    {
                        LogOutput.Write("CheckNewsAsync: 最終既読日時ファイルが存在しません");
                    }

                    bool shouldShowNotice = latestDate > lastRead;
                    NoticeText.Visibility = shouldShowNotice ? Visibility.Visible : Visibility.Collapsed;
                    LogOutput.Write($"CheckNewsAsync: 通知テキスト表示判定結果: {shouldShowNotice}");
                }
                else
                {
                    NoticeText.Visibility = Visibility.Collapsed;
                    LogOutput.Write("CheckNewsAsync: ニュースリストが空または不正。通知テキストを非表示に設定");
                }

                NoticeBadge.Visibility = Visibility.Visible;
                LogOutput.Write("CheckNewsAsync 正常終了: 通知バッジを表示");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"CheckNewsAsync 例外: ニュース取得・処理中に問題発生。例外詳細: {ex.Message}\nスタックトレース:\n{ex.StackTrace}");
                NoticeText.Visibility = Visibility.Collapsed;
                NoticeBadge.Visibility = Visibility.Visible;
                LogOutput.Write("CheckNewsAsync: 例外発生時は通知テキスト非表示、通知バッジ表示に設定");
            }
        }


        private async Task CheckUpdateButtonAsync()
        {
            LogOutput.Write("CheckUpdateButtonAsync 開始: GitHubのversion.txtから最新バージョン取得し現在バージョンと比較。アップデート通知UIを制御。");
            try
            {
                string rawVersion = AppVersion.Version;
                bool endsWithS = rawVersion.EndsWith("s", StringComparison.OrdinalIgnoreCase);

                bool isUpdateAvailable = await AppUpdater.IsUpdateAvailableAsync(rawVersion, VersionUrl);

                UpdateNoticeText.Visibility = isUpdateAvailable ? Visibility.Visible : Visibility.Collapsed;
                UpdateButton.Visibility = (!endsWithS && isUpdateAvailable) ? Visibility.Visible : Visibility.Collapsed;

                LogOutput.Write($"CheckUpdateButtonAsync: 現在バージョン={rawVersion}、アップデート有無={isUpdateAvailable}、末尾s判定={endsWithS}");
                LogOutput.Write("CheckUpdateButtonAsync 正常終了: アップデートUIを設定");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"CheckUpdateButtonAsync 例外: アップデートチェック失敗。例外詳細: {ex.Message}\nスタックトレース:\n{ex.StackTrace}");
                UpdateNoticeText.Visibility = Visibility.Collapsed;
                UpdateButton.Visibility = Visibility.Collapsed;
                LogOutput.Write("CheckUpdateButtonAsync: 例外時はアップデート通知とボタンを非表示に設定");
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("UpdateButton_Click 開始: ユーザーがアップデートボタンをクリック。更新確認ダイアログ表示。");
            try
            {
                if (MessageBox.Show("新しいバージョンがあります。アップデートしますか？", "アップデート確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    LogOutput.Write("UpdateButton_Click: ユーザーがアップデートを承認。Updater.exeを起動してアプリ終了。");
                    AppUpdater.StartUpdaterAndExit();
                }
                else
                {
                    LogOutput.Write("UpdateButton_Click: ユーザーがアップデートを拒否。処理終了。");
                }
            }
            catch (Exception ex)
            {
                LogOutput.Write($"UpdateButton_Click 例外: アップデートボタンクリック処理で例外。詳細: {ex.Message}\nスタックトレース:\n{ex.StackTrace}");
                throw;
            }
        }

        private void Notice_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("Notice_Click 開始: 最新ニュース既読日時を保存し、通知テキストを非表示にしてニュースページへ遷移。");
            try
            {
                File.WriteAllText(LastReadNewsFile, DateTime.Now.ToString("s"));
                LogOutput.Write($"Notice_Click: {LastReadNewsFile} に最終既読日時を保存");

                NoticeText.Visibility = Visibility.Collapsed;
                LogOutput.Write("Notice_Click: 通知テキストのVisibilityをCollapsedに設定");

                NavigationService?.Navigate(new News());
                LogOutput.Write("Notice_Click: ニュースページへの遷移開始");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"Notice_Click 例外: 通知クリック処理で例外。詳細: {ex.Message}\nスタックトレース:\n{ex.StackTrace}");
                throw;
            }
        }

        private void SelectExeButton_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("SelectExeButton_Click 開始: Among Us.exeのファイル選択ダイアログをユーザーに表示。");
            var dialog = new OpenFileDialog
            {
                Title = "Among Us.exe を選択してください",
                Filter = "Among Us.exe|Among Us.exe"
            };

            if (dialog.ShowDialog() == true)
            {
                LogOutput.Write($"SelectExeButton_Click: ファイル選択結果: {dialog.FileName}");
                var config = new Among_Us_ModManager.VanillaConfig { ExePath = dialog.FileName };
                try
                {
                    File.WriteAllText(VanillaConfigPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
                    LogOutput.Write($"SelectExeButton_Click: {VanillaConfigPath} にexeパスを保存完了");

                    MessageBox.Show($"Among Us.exeのパスを保存しました:\n{config.ExePath}", "設定完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LogOutput.Write($"SelectExeButton_Click 設定保存時例外: {ex.Message}\nスタックトレース:\n{ex.StackTrace}");
                    MessageBox.Show($"設定保存中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                LoadGameFolders();
            }
            else
            {
                LogOutput.Write("SelectExeButton_Click: ユーザーがファイル選択ダイアログをキャンセル");
            }
        }

        private void LoadGameFolders()
        {
            GameFolderList.Items.Clear();

            var amongUsInstallations = Among_Us_ModManager.AmongUsModDetector.DetectAmongUsInstallations();

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
                        var appIdContent = await client.GetStringAsync("https://raw.githubusercontent.com/Tabasco1410/AmongUsModManager/main/steam_appid.txt");
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
            var btn = new Button
            {
                Content = content,
                Width = width,
                Margin = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(5, 2, 5, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void CheckAdminButtonVisibility()
        {
            LogOutput.Write("CheckAdminButtonVisibility 開始: 管理者権限チェック処理開始");
            try
            {
                if (File.Exists(AdminCheckFile))
                {
                    var content = File.ReadAllText(AdminCheckFile).Trim();
                    var hash = ComputeSha256Hash(content);
                    if (hash == AdminHash)
                    {
                        AdminPageButton.Visibility = Visibility.Visible;
                        LogOutput.Write("CheckAdminButtonVisibility: 管理者認証成功。管理者ボタンを表示");
                        return;
                    }
                }
                AdminPageButton.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                AdminPageButton.Visibility = Visibility.Collapsed;
                LogOutput.Write($"CheckAdminButtonVisibility 例外: {ex.Message}\nスタックトレース:\n{ex.StackTrace}");
            }
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        private void LoadInstalledMods()
        {
            // 今後MOD一覧表示の実装予定
            // LogOutput.Write("LoadInstalledMods 開始: 導入済みMOD情報をロードし、UI表示に反映します。");
        }

        private void DiscordIcon_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://discord.gg/nFhkYmf9At",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Discordへのリンクを開けませんでした: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


     

        private async void AdminLogin_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("AdminLogin_Click 開始: 管理者ログイン処理を開始");

            try
            {
                bool isLoggedIn = await _oauthManager.LoginAsync();

                if (isLoggedIn)
                {
                    LogOutput.Write("AdminLogin_Click: OAuth認証成功。管理者ページへ遷移");
                    NavigationService?.Navigate(new AdminPanelPage());
                }
                else
                {
                    LogOutput.Write("AdminLogin_Click: OAuth認証失敗またはキャンセル");
                    MessageBox.Show("管理者ログインに失敗しました。", "認証エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LogOutput.Write($"AdminLogin_Click 例外: {ex.Message}\nスタックトレース:\n{ex.StackTrace}");
                MessageBox.Show($"管理者ログイン中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Mod_New_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("Mod_New_Click 開始: MOD選択ページへ遷移");
            NavigationService?.Navigate(new ModSelectPage());
        }

        private void Note_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("Note_Click 開始: Note選択ページへ遷移");
            NavigationService?.Navigate(new Among_Us_ModManager.NoteSelectionPage());
        }

    }
}