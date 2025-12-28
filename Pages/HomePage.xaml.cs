using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AmongUsModManager.Services;
using AmongUsModManager.Models;

namespace AmongUsModManager.Pages
{
    public sealed partial class HomePage : Page
    {
        private HttpClient _httpClient = new HttpClient();
        private Action _pendingConfirmAction;
        private Queue<string> _unregisteredFolders = new Queue<string>();

        public HomePage()
        {
            this.InitializeComponent();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager-App");
            InitializeHome();
        }

        private async void InitializeHome()
        {
            LoadModEnvironments();
            FetchNews();
            
            await CheckUpdatesAndAutoUpdate();
        }

        private async Task CheckUpdatesAndAutoUpdate()
        {
            var config = ConfigService.Load();
            if (config?.VanillaPaths == null) return;

            var displayList = new List<ModUpdateStatus>();
            var libPage = new LibraryPage();

            foreach (var mod in config.VanillaPaths)
            {
                var status = new ModUpdateStatus { ModName = mod.Name, ModPath = mod.Path, OriginalMod = mod };

                if (string.IsNullOrEmpty(mod.GitHubOwner) || string.IsNullOrEmpty(mod.GitHubRepo))
                {
                    status.StatusText = "未連携";
                    status.CanUpdate = false;
                }
                else
                {
                    try
                    {
                        var latestRelease = await _httpClient.GetFromJsonAsync<GitHubRelease>(
                            $"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases/latest");

                      
                        string currentTag = mod.CurrentVersion;

                        if (latestRelease != null && latestRelease.tag_name != currentTag)
                        {
                            if (mod.IsAutoUpdateEnabled)
                            {
                                status.StatusText = "自動更新中...";
                                await libPage.PerformUpdateLogic(mod, latestRelease.tag_name);

                             
                                mod.CurrentVersion = latestRelease.tag_name;
                                ConfigService.Save(config);

                                status.StatusText = "自動更新完了";
                                status.CanUpdate = false;
                            }
                            else
                            {
                                status.StatusText = $"更新あり: {latestRelease.tag_name}";
                                status.CanUpdate = true;
                            }
                        }
                        else
                        {
                            status.StatusText = "最新";
                            status.CanUpdate = false;
                        }
                    }
                    catch { status.StatusText = "チェック失敗"; }
                }
                displayList.Add(status);
            }
            UpdateListView.ItemsSource = displayList.OrderByDescending(s => s.CanUpdate).ToList();
        }

        private async void HomeUpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ModUpdateStatus status)
            {
                btn.IsEnabled = false;

              
                UpdateProgressDialog.XamlRoot = this.XamlRoot;
                UpdateStatusText.Text = $"{status.ModName} をアップデート中...";
                UpdateProgressBar.IsIndeterminate = true;
                var dialogTask = UpdateProgressDialog.ShowAsync();

                try
                {
                    var libPage = new LibraryPage();
                    await libPage.PerformUpdateLogic(status.OriginalMod);

                    UpdateStatusText.Text = "アップデートが完了しました。";
                    await Task.Delay(1000); // 完了を見せるための待機
                }
                catch (Exception ex)
                {
                    UpdateStatusText.Text = $"エラー: {ex.Message}";
                    await Task.Delay(2000);
                }
                finally
                {
                    UpdateProgressDialog.Hide();
                    btn.IsEnabled = true;
                    await CheckUpdatesAndAutoUpdate(); 
                }
            }
        }

        private string GetVersionFromDll(string folderPath)
        {
            try
            {
                // NebulaontheShipだけちょっと特殊なんだよね
                string pluginsPath = Path.Combine(folderPath, "BepInEx", "plugins");
                bool isNebula = Directory.Exists(pluginsPath) &&
                               Directory.GetFiles(pluginsPath, "NebulaLoader.dll", SearchOption.AllDirectories).Any();

                string searchPath = isNebula ? Path.Combine(folderPath, "nebula") : pluginsPath;

                if (Directory.Exists(searchPath))
                {
                    var dlls = Directory.GetFiles(searchPath, "*.dll", SearchOption.AllDirectories);
                    foreach (var dll in dlls)
                    {
                        if (Path.GetFileName(dll).Equals("GameAssembly.dll", StringComparison.OrdinalIgnoreCase)) continue;
                        var info = FileVersionInfo.GetVersionInfo(dll);
                        if (!string.IsNullOrEmpty(info.FileVersion)) return info.FileVersion;
                    }
                }
            }
            catch { }
            return "N/A";
        }

        private void LoadModEnvironments()
        {
            var config = ConfigService.Load();
            if (config == null) return;

            ModSelector.Items.Clear();
            if (config.VanillaPaths != null)
            {
                foreach (var info in config.VanillaPaths)
                    ModSelector.Items.Add(new ComboBoxItem { Content = info.Name, Tag = info.Path });
            }

            _unregisteredFolders.Clear();
            if (!string.IsNullOrEmpty(config.ModDataPath) && Directory.Exists(config.ModDataPath))
            {
                foreach (var dir in Directory.GetDirectories(config.ModDataPath))
                {
                    if (!File.Exists(Path.Combine(dir, "Among Us.exe"))) continue;
                    bool registered = config.VanillaPaths?.Any(v =>
                        string.Equals(Path.GetFullPath(v.Path).TrimEnd('\\'), Path.GetFullPath(dir).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) ?? false;
                    if (!registered) _unregisteredFolders.Enqueue(dir);
                }
            }
            ShowNextBanner();
            if (ModSelector.Items.Count > 0) ModSelector.SelectedIndex = 0;
        }

        private void ShowNextBanner()
        {
            if (_unregisteredFolders.Count == 0) { LocalInfoBar.IsOpen = false; return; }
            string folderPath = _unregisteredFolders.Peek();
            LocalInfoBar.Message = $"「{Path.GetFileName(folderPath)}」を登録しますか？";
            _pendingConfirmAction = () => {
                var config = ConfigService.Load();
                config.VanillaPaths.Add(new VanillaPathInfo { Name = Path.GetFileName(folderPath), Path = folderPath });
                ConfigService.Save(config);
                ModSelector.Items.Add(new ComboBoxItem { Content = Path.GetFileName(folderPath), Tag = folderPath });
            };
            LocalInfoBar.IsOpen = true;
        }

        private void LocalInfoBarButton_Click(object sender, RoutedEventArgs e) { _pendingConfirmAction?.Invoke(); _unregisteredFolders.Dequeue(); ShowNextBanner(); }
        private void LocalInfoBar_CloseButtonClick(InfoBar sender, object args) { _unregisteredFolders.Dequeue(); ShowNextBanner(); }

        private async void FetchNews()
        {
            try
            {
                string json = await _httpClient.GetStringAsync("https://amongusmodmanager.web.app/News.json");
                NewsListView.ItemsSource = JsonSerializer.Deserialize<List<NewsItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { }
        }

        private void NewsListView_ItemClick(object sender, ItemClickEventArgs e) { if (e.ClickedItem is NewsItem selected) this.Frame.Navigate(typeof(NewsDetailPage), selected); }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModSelector.SelectedItem is ComboBoxItem item)
            {
                string path = item.Tag?.ToString();
                if (File.Exists(Path.Combine(path, "Among Us.exe")))
                    Process.Start(new ProcessStartInfo(Path.Combine(path, "Among Us.exe")) { WorkingDirectory = path, UseShellExecute = true });
            }
        }
    }

    public class ModUpdateStatus
    {
        public string ModName { get; set; }
        public string ModPath { get; set; }
        public string StatusText { get; set; }
        public bool CanUpdate { get; set; }
        public VanillaPathInfo OriginalMod { get; set; }
    }
}
