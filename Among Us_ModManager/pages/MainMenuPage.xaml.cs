using Among_Us_ModManager.Modules;
using Among_Us_ModManager.Modules.Updates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Among_Us_ModManager.Pages
{
    public partial class MainMenuPage : Page
    {
        private Among_Us_ModManager.Modules.VanillaConfig? config;

        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AmongUsModManager");

        private static readonly string AdminCheckFile = Path.Combine(AppDataFolder, "admin.txt");

        private const string AdminHash = "c6518372cdd213db645cc7b5e0f20612bd2e9acd1458898698acbf714bbb1bd9";

        public MainMenuPage()
        {
            InitializeComponent();

            try
            {
                Directory.CreateDirectory(AppDataFolder);
                LogOutput.Write("AppDataフォルダの作成または確認完了");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"AppDataフォルダ作成失敗: {ex.Message}");
            }

            LoadConfig();
            CheckAdminButtonVisibility();

            // ページがロードされたときにバージョン表示とアップデートチェック
            Loaded += async (s, e) =>
            {
                await LoadVersionAsync();
                await CheckUpdateButtonAsync();
            };
        }


        private static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        private void LoadConfig()
        {
            LogOutput.Write("LoadConfig 開始");

            config = Among_Us_ModManager.Modules.VanillaConfig.Load();
            if (config == null)
            {
                LogOutput.Write("VanillaConfig がロードできませんでした");
                InstallListPanel.ItemsSource = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(config.AmongUsExePath) || !File.Exists(config.AmongUsExePath))
            {
                LogOutput.Write("AmongUsExePath が存在しません");
                InstallListPanel.ItemsSource = null;
                return;
            }

            string baseFolder = Path.GetDirectoryName(config.AmongUsExePath) ?? "";
            string rootFolder = Path.GetDirectoryName(baseFolder) ?? "";
            if (string.IsNullOrEmpty(rootFolder) || !Directory.Exists(rootFolder))
            {
                LogOutput.Write("rootFolder が存在しません");
                return;
            }

            var entries = new List<InstallEntry>();

            foreach (var folder in Directory.GetDirectories(rootFolder))
            {
                var exePath = Path.Combine(folder, "Among Us.exe");
                var pluginsFolder = Path.Combine(folder, "BepInEx", "plugins");
                string nebulaFolder = Path.Combine(folder, "BepInEx", "Nebula");
                if (Directory.Exists(pluginsFolder) && File.Exists(Path.Combine(pluginsFolder, "NebulaLoader.dll")))
                {
                    pluginsFolder = nebulaFolder;
                }

                if (File.Exists(exePath) && Directory.Exists(pluginsFolder))
                {
                    foreach (var dllPath in Directory.GetFiles(pluginsFolder, "*.dll"))
                    {
                        var dllName = Path.GetFileNameWithoutExtension(dllPath);
                        string version = "";
                        try
                        {
                            version = FileVersionInfo.GetVersionInfo(dllPath).FileVersion ?? "";
                        }
                        catch { }

                        entries.Add(new InstallEntry
                        {
                            ExePath = exePath,
                            VersionText = string.IsNullOrEmpty(version) ? dllName : $"{dllName}（{version}）"
                        });

                        LogOutput.Write($"Mod検出: {dllName}, バージョン: {version}");
                    }
                }
            }

            InstallListPanel.ItemsSource = entries;
            LogOutput.Write("LoadConfig 完了");
        }

        #region 標準ボタン処理


        private void Launch_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is InstallEntry entry && File.Exists(entry.ExePath))
            {
                LogOutput.Write($"起動: {entry.ExePath}");
                Process.Start(entry.ExePath);
            }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is InstallEntry entry)
            {
                LogOutput.Write($"アップデート要求: {entry.ExePath}");
                MessageBox.Show($"{entry.ExePath} をアップデートします（仮）");
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is InstallEntry entry)
            {
                string folder = Path.GetDirectoryName(entry.ExePath) ?? "";
                LogOutput.Write($"フォルダ開く: {folder}");
                if (Directory.Exists(folder))
                    Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true, Verb = "open" });
            }
        }

        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is InstallEntry entry)
            {
                string folder = Path.GetDirectoryName(entry.ExePath) ?? "";
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                    LogOutput.Write($"アンインストール: {folder}");
                }
                LoadConfig();
            }
        }

        private void InstallNewMod_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("新規Modインストール画面へ遷移");
            var page = new Install_TypeChoosePage();
            NavigationService?.Navigate(page);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("設定ボタンクリック");
            MessageBox.Show("設定画面を開く処理をここに書きます。");
        }

        private void DiscordIcon_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("Discordボタンクリック");
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://discord.gg/tKHPHTHDXw",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LogOutput.Write($"Discordを開けませんでした: {ex.Message}");
            }
        }
        #endregion

        private const string VersionUrl = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManager/main/version.txt";

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

        public static async Task<bool> IsUpdateAvailableAsync(string currentVersion, string versionUrl)
        {
            try
            {
                using var client = new HttpClient();
                string latestVersion = await client.GetStringAsync(versionUrl);
                return !string.Equals(currentVersion, latestVersion.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                LogOutput.Write($"IsUpdateAvailableAsync 例外: {ex.Message}");
                return false;
            }
        }


        #region 管理者ページ
        private void CheckAdminButtonVisibility()
        {
            try
            {
                if (File.Exists(AdminCheckFile))
                {
                    string content = File.ReadAllText(AdminCheckFile).Trim();
                    string hash = ComputeSha256Hash(content);

                    LogOutput.Write($"管理者チェック: admin.txt の内容 = \"{content}\"");
                    LogOutput.Write($"管理者チェック: 計算されたハッシュ = {hash}");

                    AdminPageButton.Visibility = (hash == AdminHash)
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                    LogOutput.Write($"管理者チェック結果: {AdminPageButton.Visibility}");
                }
                else
                {
                    AdminPageButton.Visibility = Visibility.Collapsed;
                    LogOutput.Write("管理者チェック: admin.txt が存在しないため Collapsed");
                }
            }
            catch (Exception ex)
            {
                AdminPageButton.Visibility = Visibility.Collapsed;
                LogOutput.Write($"管理者チェック: 例外発生 {ex}");
            }
        }


        private void AdminLogin_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("管理者ページボタンクリック");
            if (AdminPageButton.Visibility == Visibility.Visible)
                NavigationService?.Navigate(new AdminPanelPage());
        }
        #endregion

        #region お知らせ
        private void Notice_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("お知らせボタンクリック");
            NavigationService?.Navigate(new News());
        }
        #endregion
    }

    public class InstallEntry
    {
        public string ExePath { get; set; } = "";
        public string VersionText { get; set; } = "";
    }
}
