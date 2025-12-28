using AmongUsModManager.Models;
using AmongUsModManager.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AmongUsModManager.Pages
{
    public class ModPreset
    {
        public string Name { get; set; }
        public string Owner { get; set; }
        public string Repository { get; set; }
    }

    public class GitHubRelease
    {
        public string tag_name { get; set; }
        public List<GitHubAsset> assets { get; set; }
    }

    public class GitHubAsset
    {
        public string name { get; set; }
        public string browser_download_url { get; set; }
    }

    public sealed partial class ModInstallPage : Page
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private List<GitHubRelease> _currentReleases;
        private ModPreset _selectedMod;

        public ModInstallPage()
        {
            this.InitializeComponent();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager-App");
            LoadModPresets();
        }

        private void LoadModPresets()
        {
            var presets = new List<ModPreset>
            {
                new ModPreset { Name = "TownOfHost", Owner = "tukasa0001", Repository = "TownOfHost" },
                new ModPreset { Name = "TownOfHost-K", Owner = "KYMario", Repository = "TownOfHost-K" },
                new ModPreset { Name = "SuperNewRoles", Owner = "SuperNewRoles", Repository = "SuperNewRoles" },
                new ModPreset { Name = "NebulaOnTheShip", Owner = "Dolly1016", Repository = "Nebula" },
                new ModPreset { Name = "ExtremeRoles", Owner = "yukieiji", Repository = "ExtremeRoles" },
                new ModPreset { Name = "TownOfHost-Enhanced", Owner = "EnhancedNetwork", Repository = "TownofHost-Enhanced" }
            };
            ModGridView.ItemsSource = presets;
        }

        private async void ModGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModGridView.SelectedItem is ModPreset selected)
            {
                _selectedMod = selected;
                await FetchGitHubData(selected.Owner, selected.Repository, selected.Name);
            }
        }

        private async void GitHubFetch_Click(object sender, RoutedEventArgs e)
        {
            string url = GitHubUrlBox.Text.Trim();
            if (string.IsNullOrEmpty(url)) return;

            try
            {
                var uri = new Uri(url);
                var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string owner = parts[0];
                    string repo = parts[1].Replace(".git", "");
                    _selectedMod = new ModPreset { Name = repo, Owner = owner, Repository = repo };
                    await FetchGitHubData(owner, repo, repo);
                }
                else
                {
                    StatusTextBlock.Text = "URLの形式が正しくありません。(https://github.com/owner/repo)";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "URL解析エラー: " + ex.Message;
            }
        }

        private async Task FetchGitHubData(string owner, string repo, string modName)
        {
            InstallDetailArea.Visibility = Visibility.Visible;
            LoadingPanel.Visibility = Visibility.Visible;
            SelectionPanel.Visibility = Visibility.Collapsed;
            SelectedModTitle.Text = modName;

            try
            {
                string url = $"https://api.github.com/repos/{owner}/{repo}/releases";
                _currentReleases = await _httpClient.GetFromJsonAsync<List<GitHubRelease>>(url);

                if (_currentReleases != null && _currentReleases.Count > 0)
                {
                    VersionCombo.ItemsSource = _currentReleases.Select(r => r.tag_name).ToList();
                    VersionCombo.SelectedIndex = 0;
                }
                else
                {
                    StatusTextBlock.Text = "リリース情報が見つかりませんでした。";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "GitHubデータ取得エラー: " + ex.Message;
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                SelectionPanel.Visibility = Visibility.Visible;
            }
        }

        private void VersionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VersionCombo.SelectedItem is string tagName && _currentReleases != null)
            {
                var release = _currentReleases.FirstOrDefault(r => r.tag_name == tagName);
                if (release != null)
                {
                    var zipAssets = release.assets
                        .Where(a => a.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        .Select(a => a.name)
                        .ToList();

                    AssetCombo.ItemsSource = zipAssets;
                    if (zipAssets.Count > 0) AssetCombo.SelectedIndex = 0;

                    InstallFolderName.Text = $"{SelectedModTitle.Text}_{tagName.Replace(".", "_")}";
                }
            }
        }

        private async void StartInstall_Click(object sender, RoutedEventArgs e)
        {
            var config = ConfigService.Load();
            if (config == null || string.IsNullOrEmpty(config.GameInstallPath) || string.IsNullOrEmpty(config.ModDataPath))
            {
                ContentDialog errorDialog = new ContentDialog { Title = "設定不足", Content = "設定画面でパスを設定してください。", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                await errorDialog.ShowAsync();
                return;
            }

            var release = _currentReleases?.FirstOrDefault(r => r.tag_name == (string)VersionCombo.SelectedItem);
            var asset = release?.assets.FirstOrDefault(a => a.name == (string)AssetCombo.SelectedItem);
            if (asset == null)
            {
                ContentDialog errorDialog = new ContentDialog { Title = "エラー", Content = "選択されたバージョンのZIPファイルが見つかりません。", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                await errorDialog.ShowAsync();
                return;
            }

            InstallProgressDialog.XamlRoot = this.XamlRoot;
            StatusTextBlock.Text = "準備中...";
            InstallProgressBar.Value = 0;
            _ = InstallProgressDialog.ShowAsync();

            try
            {
                string targetDir = Path.Combine(config.ModDataPath, InstallFolderName.Text);
                if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
                Directory.CreateDirectory(targetDir);

                StatusTextBlock.Text = "本体をコピー中...";
                await Task.Run(() => CopyDirectory(config.GameInstallPath, targetDir));
                InstallProgressBar.Value = 30;

                StatusTextBlock.Text = "ダウンロード中...";
                string tempZip = Path.Combine(Path.GetTempPath(), asset.name);
                using (var stream = await _httpClient.GetStreamAsync(asset.browser_download_url))
                using (var fileStream = new FileStream(tempZip, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
                InstallProgressBar.Value = 70;

                StatusTextBlock.Text = "展開中...";
                string extractPath = Path.Combine(Path.GetTempPath(), "AUMMExtract_" + Guid.NewGuid());
                await Task.Run(() => {
                    ZipFile.ExtractToDirectory(tempZip, extractPath);
                    var bepInExDir = Directory.GetDirectories(extractPath, "BepInEx", SearchOption.AllDirectories).FirstOrDefault();
                    if (bepInExDir != null) CopyDirectory(Directory.GetParent(bepInExDir).FullName, targetDir);
                    else CopyDirectory(extractPath, targetDir);
                    Directory.Delete(extractPath, true);
                    if (File.Exists(tempZip)) File.Delete(tempZip);
                });

                var newMod = new VanillaPathInfo
                {
                    Name = InstallFolderName.Text,
                    Path = targetDir,
                    GitHubOwner = _selectedMod?.Owner,
                    GitHubRepo = _selectedMod?.Repository,
                    CurrentVersion = release.tag_name,
                    IsAutoUpdateEnabled = true,
                    LastChecked = DateTime.Now
                };

                if (!config.VanillaPaths.Any(v => v.Path == targetDir))
                {
                    config.VanillaPaths.Add(newMod);
                    ConfigService.Save(config);
                }

                InstallProgressBar.Value = 100;
                StatusTextBlock.Text = "完了！";
                await Task.Delay(800);
                InstallProgressDialog.Hide();

                await ShowPostInstallSetup(newMod);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"エラー: {ex.Message}";
            }
        }

        private async void SelectZip_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".zip");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                ContentDialog infoDialog = new ContentDialog
                {
                    Title = "手動インストール",
                    Content = "手動ZIP選択の場合、GitHub連携ができないため自動更新は無効になります。",
                    CloseButtonText = "了解",
                    XamlRoot = this.XamlRoot
                };
                await infoDialog.ShowAsync();
                StatusTextBlock.Text = $"選択済み: {file.Name} (GitHub連携なし)";
            }
        }

        private async Task ShowPostInstallSetup(VanillaPathInfo mod)
        {
            var stack = new StackPanel { Spacing = 10 };
            var autoUpdateCheck = new CheckBox { Content = "このModの自動アップデートを有効にする", IsChecked = true };

            stack.Children.Add(new TextBlock { Text = "Modのインストールが完了しました。", TextWrapping = TextWrapping.Wrap });
            stack.Children.Add(new TextBlock { Text = $"管理タグ: {mod.CurrentVersion}", FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
            stack.Children.Add(autoUpdateCheck);

            var dialog = new ContentDialog
            {
                Title = "設定完了",
                Content = stack,
                PrimaryButtonText = "ライブラリへ",
                CloseButtonText = "閉じる",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            var config = ConfigService.Load();
            var target = config.VanillaPaths.FirstOrDefault(v => v.Path == mod.Path);
            if (target != null)
            {
                target.IsAutoUpdateEnabled = autoUpdateCheck.IsChecked ?? false;
                ConfigService.Save(config);
            }

            if (result == ContentDialogResult.Primary)
            {
                if (App.MainWindowInstance is MainWindow mw) mw.NavigateToPendingPage("Library");
            }
        }

        private void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (var f in Directory.GetFiles(source)) File.Copy(f, Path.Combine(dest, Path.GetFileName(f)), true);
            foreach (var d in Directory.GetDirectories(source)) CopyDirectory(d, Path.Combine(dest, Path.GetFileName(d)));
        }
    }
}
