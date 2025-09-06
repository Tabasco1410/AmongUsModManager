using Among_Us_ModManager.Modules;
using Among_Us_ModManager.Modules.Updates;
using Among_Us_ModManager.pages.News;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;

namespace Among_Us_ModManager.Pages
{
    public partial class MainMenuPage : Page
    {
        private SettingsConfig? config;

        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AmongUsModManager");

        private static readonly string AdminCheckFile = Path.Combine(AppDataFolder, "admin.dat");

        private const string AdminHash = "c6518372cdd213db645cc7b5e0f20612bd2e9acd1458898698acbf714bbb1bd9";

        private static readonly string NewsFile = Path.Combine(AppDataFolder, "last_read_news.dat");

        public MainMenuPage()
        {
            InitializeComponent();
            Strings.Load();
            try { Directory.CreateDirectory(AppDataFolder); } catch { }
            LoadConfig();
            ApplyLanguage();
            CheckAdminButtonVisibility();
            Loaded += async (s, e) =>
            {
                await LoadVersionAsync();
                await CheckUpdateButtonAsync();
                await CheckNewNoticeAsync();
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
            config = SettingsConfig.Load();
            if (config == null)
            {
                InstallListPanel.ItemsSource = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(config.AmongUsExePath) || !File.Exists(config.AmongUsExePath))
            {
                InstallListPanel.ItemsSource = null;
                return;
            }

            string baseFolder = Path.GetDirectoryName(config.AmongUsExePath) ?? "";
            string rootFolder = Path.GetDirectoryName(baseFolder) ?? "";
            if (string.IsNullOrEmpty(rootFolder) || !Directory.Exists(rootFolder))
                return;

            var entries = new List<InstallEntry>();
            var ignoreList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ExtremeSkins",
                "Agartha",
                "Mini.RegionInstall",
                "System.Text.Encoding.CodePages"
            };

            foreach (var folder in Directory.GetDirectories(rootFolder))
            {
                var exePath = Path.Combine(folder, "Among Us.exe");
                var pluginsFolder = Path.Combine(folder, "BepInEx", "plugins");
                string nebulaFolder = Path.Combine(folder, "BepInEx", "Nebula");

                if (Directory.Exists(pluginsFolder) && File.Exists(Path.Combine(pluginsFolder, "NebulaLoader.dll")))
                    pluginsFolder = nebulaFolder;

                if (File.Exists(exePath) && Directory.Exists(pluginsFolder))
                {
                    foreach (var dllPath in Directory.GetFiles(pluginsFolder, "*.dll"))
                    {
                        var dllName = Path.GetFileNameWithoutExtension(dllPath);
                        if (ignoreList.Contains(dllName))
                            continue;

                        string version = "";
                        try { version = FileVersionInfo.GetVersionInfo(dllPath).FileVersion ?? ""; } catch { }

                        string versionText = (dllName.Equals("Nebula", StringComparison.OrdinalIgnoreCase) ||
                                              dllName.Equals("TOHE", StringComparison.OrdinalIgnoreCase))
                            ? dllName
                            : string.IsNullOrEmpty(version) ? dllName : $"{dllName}（{version}）";

                        entries.Add(new InstallEntry { ExePath = exePath, VersionText = versionText });
                    }
                }
            }

            InstallListPanel.ItemsSource = entries;
        }

        private void ApplyLanguage()
        {
            // 言語を設定
            if (config != null && !string.IsNullOrWhiteSpace(config.Language))
                Strings.SetLanguage(config.Language);
            else
                Strings.SetLanguage("JA");

            // ヘッダー
            AppTitleText.Text = Strings.Get("AppTitle");
            VersionText.Text = Strings.Get("FetchingVersion");

            // 更新関連
            UpdateNoticeText.Text = Strings.Get("UpdateAvailable");
            UpdateButton.Content = Strings.Get("Update");

            // 右上ボタン（文字ボタンのみ）
            AdminPageButton.Content = Strings.Get("AdminPage");
            NewNoticeText.Text = Strings.Get("NewNotice");

            // DiscordやSettingsは固定画像なので触らない
            // SettingsButton.Contentは固定画像なので変更不要
            // DiscordIconは固定画像なので変更不要

            // 新規インストールボタンのテキストを変更
            InstallModButton.Content = Strings.Get("InstallMod");

            // 説明文
            InstallModDescText.Text = Strings.Get("InstallModDesc");

            // 導入済み一覧のボタン
            foreach (var item in InstallListPanel.Items)
            {
                if (item is ModItem mod)
                {
                    mod.LaunchText = Strings.Get("Launch");
                    mod.OpenFolderText = Strings.Get("OpenFolder");
                    mod.UninstallText = Strings.Get("Uninstall");
                }
            }

            InstallListPanel.Items.Refresh();
        }


        public class ModItem
        {
            public string VersionText { get; set; }
            public string ExePath { get; set; }
            public string LaunchText { get; set; }
            public string OpenFolderText { get; set; }
            public string UninstallText { get; set; }
        }

        private async Task CheckNewNoticeAsync()
        {
            try
            {
                if (!File.Exists(NewsFile))
                    File.WriteAllText(NewsFile, DateTime.MinValue.ToString("o"));

                string lastReadStr = File.ReadAllText(NewsFile).Trim();
                if (!DateTime.TryParse(lastReadStr, out DateTime lastRead))
                    lastRead = DateTime.MinValue;

                DateTime latestNewsDate = await GetLatestNewsDateAsync();
                bool hasNewNotice = latestNewsDate > lastRead;
                NewNoticeText.Visibility = hasNewNotice ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { NewNoticeText.Visibility = Visibility.Collapsed; }
        }

        private async Task<DateTime> GetLatestNewsDateAsync()
        {
            try
            {
                string newsPath = Path.Combine(AppDataFolder, "News.json");
                if (!File.Exists(newsPath))
                {
                    using var client = new HttpClient();
                    string url = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManager/main/News.json";
                    string json = await client.GetStringAsync(url);
                    File.WriteAllText(newsPath, json);
                }

                string localJson = File.ReadAllText(newsPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var newsItems = JsonSerializer.Deserialize<List<NewsItem>>(localJson, options);
                if (newsItems == null || newsItems.Count == 0)
                    return DateTime.MinValue;

                return newsItems.Max(n => n.Date);
            }
            catch { return DateTime.MinValue; }
        }

        private void Launch_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is InstallEntry entry && File.Exists(entry.ExePath))
                Process.Start(entry.ExePath);
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is InstallEntry)
                MessageBox.Show(Strings.Get("Update_NotImplemented"));
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is InstallEntry entry)
            {
                string folder = Path.GetDirectoryName(entry.ExePath) ?? "";
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
                    var result = MessageBox.Show(
                        string.Format(Strings.Get("Confirm_Uninstall"), folder),
                        Strings.Get("Confirm"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        Directory.Delete(folder, true);
                        LoadConfig();
                    }
                }
            }
        }

        private void InstallNewMod_Click(object sender, RoutedEventArgs e)
        {
            var page = new Install_TypeChoosePage();
            NavigationService?.Navigate(page);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow
            {
                Owner = Window.GetWindow(this)
            };
            settingsWindow.ShowDialog();
            config = SettingsConfig.Load();
            ApplyLanguage();
        }

        private void DiscordIcon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://discord.gg/nFhkYmf9At",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private const string VersionUrl = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManager/main/version.txt";

        private async Task LoadVersionAsync()
        {
            VersionText.Text = $"v {AppVersion.Version}";
            VersionText.ToolTip = new TextBlock
            {
                Text = AppVersion.Notes,
                TextWrapping = TextWrapping.Wrap,
                Width = 250
            };
            await Task.CompletedTask;
        }

        private async Task CheckUpdateButtonAsync()
        {
            string rawVersion = AppVersion.Version;
            bool endsWithS = rawVersion.EndsWith("s", StringComparison.OrdinalIgnoreCase);
            bool isUpdateAvailable = await AppUpdater.IsUpdateAvailableAsync(rawVersion, VersionUrl);
            UpdateNoticeText.Visibility = isUpdateAvailable ? Visibility.Visible : Visibility.Collapsed;
            UpdateButton.Visibility = (!endsWithS && isUpdateAvailable) ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Strings.Get("Update_Confirm"), Strings.Get("Update"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                await AppUpdater.StartUpdaterAndExit();
        }

        public static async Task<bool> IsUpdateAvailableAsync(string currentVersion, string versionUrl)
        {
            try
            {
                using var client = new HttpClient();
                string latestVersion = await client.GetStringAsync(versionUrl);
                return !string.Equals(currentVersion, latestVersion.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private void CheckAdminButtonVisibility()
        {
            try
            {
                if (File.Exists(AdminCheckFile))
                {
                    string content = File.ReadAllText(AdminCheckFile).Trim();
                    string hash = ComputeSha256Hash(content);
                    AdminPageButton.Visibility = (hash == AdminHash) ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                    AdminPageButton.Visibility = Visibility.Collapsed;
            }
            catch { AdminPageButton.Visibility = Visibility.Collapsed; }
        }

        private async void AdminLogin_Click(object sender, RoutedEventArgs e)
        {
            bool hasToken = await OAuthManager.Instance.InitializeAsync();
            if (!hasToken)
            {
                bool isAdmin = await OAuthManager.Instance.LoginAsync();
                if (!isAdmin)
                {
                    MessageBox.Show("管理者権限がありません。", "アクセス拒否", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else if (!OAuthManager.Instance.IsAdmin)
            {
                MessageBox.Show("管理者権限がありません。", "アクセス拒否", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AdminPageButton.Visibility == Visibility.Visible)
                NavigationService?.Navigate(new AdminPanelPage());
        }
    }

    public class InstallEntry
    {
        public string ExePath { get; set; } = "";
        public string VersionText { get; set; } = "";
    }
}
