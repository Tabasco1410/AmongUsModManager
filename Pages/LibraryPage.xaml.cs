using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using AmongUsModManager.Services;
using AmongUsModManager.Models;

namespace AmongUsModManager.Pages
{
    public sealed partial class LibraryPage : Page
    {
        private HttpClient _httpClient = new HttpClient();
        private List<VanillaPathInfo> _issueMods = new List<VanillaPathInfo>();

        public LibraryPage()
        {
            this.InitializeComponent();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager-App");
            LoadLibrary();
        }

        private async void LoadLibrary()
        {
            var config = ConfigService.Load();
            if (config?.VanillaPaths != null)
            {
                _issueMods.Clear();

                foreach (var mod in config.VanillaPaths)
                {
                    
                    if (string.IsNullOrEmpty(mod.CurrentVersion))
                    {
                        mod.CurrentVersion = "Unknown";
                    }

                    if (!string.IsNullOrEmpty(mod.GitHubOwner) && !string.IsNullOrEmpty(mod.GitHubRepo))
                    {
                        try
                        {
                            var latest = await _httpClient.GetFromJsonAsync<GitHubRelease>(
                                $"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases/latest");

                            
                            if (latest != null && mod.CurrentVersion != latest.tag_name)
                            {
                                _issueMods.Add(mod);
                            }
                        }
                        catch { }
                    }
                }
                LibraryGridView.ItemsSource = config.VanillaPaths;
                if (VersionIssueBar != null) VersionIssueBar.IsOpen = _issueMods.Count > 0;
            }
        }



        private async void ResolveVersionIssues_Click(object sender, RoutedEventArgs e)
        {
            var stackPanel = new StackPanel { Spacing = 15, Width = 400 };
            var choiceMap = new Dictionary<VanillaPathInfo, ComboBox>();

            foreach (var mod in _issueMods)
            {
                var modPanel = new StackPanel { Spacing = 5 };
                modPanel.Children.Add(new TextBlock { Text = mod.Name, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
                modPanel.Children.Add(new TextBlock { Text = $"現在の設定: {mod.CurrentVersion}", FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });

                var combo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, Header = "アクション" };
                combo.Items.Add("最新版に更新する");
                combo.Items.Add("タグを選択して更新する");
                combo.Items.Add("タグを選択し、そのバージョンとして設定のみ行う (更新なし)"); // 新規追加 
                combo.Items.Add("今回は何もしない");
                combo.SelectedIndex = 0;

                choiceMap.Add(mod, combo);
                modPanel.Children.Add(combo);
                stackPanel.Children.Add(modPanel);
            }

            var dialog = new ContentDialog
            {
                Title = "バージョン不一致の解決",
                Content = new ScrollViewer { Content = stackPanel, MaxHeight = 450 },
                PrimaryButtonText = "実行",
                CloseButtonText = "キャンセル",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var config = ConfigService.Load();
                foreach (var entry in choiceMap)
                {
                    var mod = entry.Key;
                    int choice = entry.Value.SelectedIndex;

                    if (choice == 0) // 最新更新
                    {
                        var latest = await _httpClient.GetFromJsonAsync<GitHubRelease>($"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases/latest");
                        await PerformUpdateWithUI(mod, latest.tag_name);
                        UpdateModVersionInConfig(config, mod.Path, latest.tag_name);
                    }
                    else if (choice == 1) // タグ選択更新
                    {
                        await ShowTagSelectionDialog(mod, true);
                    }
                    else if (choice == 2) // バージョン設定のみ
                    {
                        await ShowTagSelectionDialog(mod, false);
                    }
                }
                ConfigService.Save(config);
                LoadLibrary();
            }
        }

       
        private async Task ShowTagSelectionDialog(VanillaPathInfo mod, bool downloadUpdate)
        {
            var releases = await _httpClient.GetFromJsonAsync<List<GitHubRelease>>($"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases");
            var combo = new ComboBox { ItemsSource = releases.Select(r => r.tag_name).ToList(), HorizontalAlignment = HorizontalAlignment.Stretch, Header = "対象のバージョンを選択" };

            var dialog = new ContentDialog { Title = downloadUpdate ? "バージョン選択と更新" : "バージョン情報の修正", Content = combo, PrimaryButtonText = "確定", CloseButtonText = "キャンセル", XamlRoot = this.XamlRoot };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary && combo.SelectedItem != null)
            {
                string selectedTag = combo.SelectedItem.ToString();
                if (downloadUpdate) { await PerformUpdateWithUI(mod, selectedTag); }

                var config = ConfigService.Load();
                UpdateModVersionInConfig(config, mod.Path, selectedTag);
                ConfigService.Save(config);
            }
        }

        private void UpdateModVersionInConfig(AppConfig config, string path, string version)
        {
            var target = config.VanillaPaths.FirstOrDefault(v => v.Path == path);
            if (target != null) target.CurrentVersion = version;
        }

        private async Task ShowTagSelectionDialog(VanillaPathInfo mod)
        {
            try
            {
                var releases = await _httpClient.GetFromJsonAsync<List<GitHubRelease>>(
                    $"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases");

                var combo = new ComboBox
                {
                    ItemsSource = releases.Select(r => r.tag_name).ToList(),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Header = "インストールするバージョンを選択"
                };

                var dialog = new ContentDialog
                {
                    Title = "バージョン選択",
                    Content = combo,
                    PrimaryButtonText = "インストール",
                    CloseButtonText = "戻る",
                    XamlRoot = this.XamlRoot
                };

              
                if (await dialog.ShowAsync() == ContentDialogResult.Primary && combo.SelectedItem != null)
                {
                    string selectedTag = combo.SelectedItem.ToString();
                    await PerformUpdateWithUI(mod, selectedTag);

                
                    var config = ConfigService.Load();
                    var target = config.VanillaPaths.FirstOrDefault(v => v.Path == mod.Path);
                    if (target != null)
                    {
                        target.CurrentVersion = selectedTag;
                        ConfigService.Save(config);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"タグ取得エラー: {ex.Message}");
            }
        }

        private async Task PerformUpdateWithUI(VanillaPathInfo mod, string tag)
        {
            UpdateProgressDialog.XamlRoot = this.XamlRoot;
            UpdateStatusText.Text = "ファイルをダウンロード中...";
            UpdateProgressBar.IsIndeterminate = true;

            var showTask = UpdateProgressDialog.ShowAsync();
            try
            {
                await PerformUpdateLogic(mod, tag);
                UpdateStatusText.Text = "完了しました。";
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = $"エラー: {ex.Message}";
                await Task.Delay(2000);
            }
            finally
            {
                UpdateProgressDialog.Hide();
            }
        }

        public async Task PerformUpdateLogic(VanillaPathInfo mod, string specificTag = null)
        {
            string url = specificTag == null
                ? $"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases/latest"
                : $"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases/tags/{specificTag}";

            var release = await _httpClient.GetFromJsonAsync<GitHubRelease>(url);
            var dllAsset = release?.assets.FirstOrDefault(a => a.name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

            if (dllAsset == null) throw new Exception("有効なDLLファイルが見つかりませんでした。");

            string pluginsPath = Path.Combine(mod.Path, "BepInEx", "plugins");
            bool isNebula = Directory.Exists(pluginsPath) && Directory.GetFiles(pluginsPath, "NebulaLoader.dll").Any();
            string targetDir = isNebula ? Path.Combine(mod.Path, "nebula") : pluginsPath;

            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            byte[] data = await _httpClient.GetByteArrayAsync(dllAsset.browser_download_url);
            File.WriteAllBytes(Path.Combine(targetDir, dllAsset.name), data);
        }

        private async void UpdateMod_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuFlyoutItem mfi && mfi.Tag is VanillaPathInfo mod)) return;
            if (string.IsNullOrEmpty(mod.GitHubOwner)) return;

            await PerformUpdateWithUI(mod, null);
            LoadLibrary();
        }

        private void AutoUpdateToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleMenuFlyoutItem item && item.Tag is VanillaPathInfo mod)
            {
                var config = ConfigService.Load();
                var target = config.VanillaPaths.FirstOrDefault(v => v.Path == mod.Path);
                if (target != null)
                {
                    target.IsAutoUpdateEnabled = item.IsChecked;
                    ConfigService.Save(config);
                }
            }
        }

        private async void LinkGitHub_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuFlyoutItem mfi && mfi.Tag is VanillaPathInfo mod)) return;

            var input = new TextBox { Header = "GitHubリポジトリURLまたはキーワード", Text = !string.IsNullOrEmpty(mod.GitHubOwner) ? $"https://github.com/{mod.GitHubOwner}/{mod.GitHubRepo}" : mod.Name };
            var dialog = new ContentDialog { Title = "GitHub連携", Content = input, PrimaryButtonText = "検索・連携", CloseButtonText = "キャンセル", XamlRoot = this.XamlRoot };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                string searchText = input.Text.Trim();
                if (string.IsNullOrEmpty(searchText)) return;

                // URLかキーワードか判定
                if (Uri.TryCreate(searchText, UriKind.Absolute, out var uri) && uri.Host == "github.com")
                {
                    var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2) { ApplyGitHubLink(mod.Path, parts[0], parts[1]); }
                }
                else
                {
                    // キーワード検索を実行し候補を表示
                    await ShowGitHubSearchCandidates(mod, searchText);
                }
            }
        }

        private async Task ShowGitHubSearchCandidates(VanillaPathInfo mod, string keyword)
        {
            try
            {
                string query = $"https://api.github.com/search/repositories?q={Uri.EscapeDataString(keyword)}";
                var result = await _httpClient.GetFromJsonAsync<GitHubSearchResult>(query);

                if (result?.items == null || result.items.Count == 0) return;

                var listView = new ListView
                {
                    ItemsSource = result.items.Take(5).ToList(),
                    DisplayMemberPath = "full_name",
                    SelectionMode = ListViewSelectionMode.Single
                };

                var candidateDialog = new ContentDialog
                {
                    Title = "リポジトリ候補の選択",
                    Content = listView,
                    PrimaryButtonText = "選択したリポジトリで連携",
                    CloseButtonText = "キャンセル",
                    XamlRoot = this.XamlRoot
                };

                if (await candidateDialog.ShowAsync() == ContentDialogResult.Primary && listView.SelectedItem is GitHubRepoItem selected)
                {
                    ApplyGitHubLink(mod.Path, selected.owner.login, selected.name);
                }
            }
            catch { /* 検索エラー処理 */ }
        }

        private void ApplyGitHubLink(string modPath, string owner, string repo)
        {
            var config = ConfigService.Load();
            var target = config.VanillaPaths.FirstOrDefault(v => v.Path == modPath);
            if (target != null)
            {
                target.GitHubOwner = owner;
                target.GitHubRepo = repo;
                ConfigService.Save(config);
                LoadLibrary();
            }
        }

        private async Task TryAutoLinkGitHub(VanillaPathInfo mod)
        {
            try
            {
                string query = $"https://api.github.com/search/repositories?q={mod.Name}+in:name";
                var result = await _httpClient.GetFromJsonAsync<GitHubSearchResult>(query);
                if (result?.items != null && result.items.Count > 0)
                {
                    mod.GitHubOwner = result.items[0].owner.login;
                    mod.GitHubRepo = result.items[0].name;
                }
            }
            catch { }
        }

        private void GridModeBtn_Click(object sender, RoutedEventArgs e)
        {
            var style = new Style(typeof(GridViewItem));
            style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            LibraryGridView.ItemContainerStyle = style;
        }

        private void ListModeBtn_Click(object sender, RoutedEventArgs e)
        {
            var style = new Style(typeof(GridViewItem));
            style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            style.Setters.Add(new Setter(MinWidthProperty, 600.0));
            LibraryGridView.ItemContainerStyle = style;
        }

        private void LibraryGridView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            var config = ConfigService.Load();
            config.VanillaPaths = LibraryGridView.Items.Cast<VanillaPathInfo>().ToList();
            ConfigService.Save(config);
        }

        private void LaunchMod_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string path)
            {
                string exe = Path.Combine(path, "Among Us.exe");
                if (File.Exists(exe)) Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = path, UseShellExecute = true });
            }
        }

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string path) await Windows.System.Launcher.LaunchFolderPathAsync(path);
        }

        private void RenameMod_Click(object sender, RoutedEventArgs e)
        {
           //あとで実装
        }

        private async void DeleteMod_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuFlyoutItem item && item.Tag is VanillaPathInfo mod)) return;

            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "MODの削除確認",
                Content = $"「{mod.Name}」をどのように削除しますか？\n\n" +
                          "・登録解除：アプリのリストから消します（ファイルは残ります）\n" +
                          "・完全削除：フォルダごと物理的に削除します",
                PrimaryButtonText = "登録解除のみ",
                SecondaryButtonText = "フォルダごと削除",
                CloseButtonText = "キャンセル",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            ContentDialogResult result = await deleteDialog.ShowAsync();
            if (result == ContentDialogResult.None) return;

            var config = ConfigService.Load();

            if (result == ContentDialogResult.Secondary)
            {
                try
                {
                    if (Directory.Exists(mod.Path)) Directory.Delete(mod.Path, true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"削除エラー: {ex.Message}");
                }
            }

            config.VanillaPaths.RemoveAll(v => v.Path == mod.Path);
            ConfigService.Save(config);
            LoadLibrary();
        }
    }

 

    public class GitHubSearchResult { public List<GitHubRepoItem> items { get; set; } }
    public class GitHubRepoItem
    {
        public string name { get; set; }
        public string full_name { get; set; } // 追加: "owner/repo" 形式
        public GitHubOwnerItem owner { get; set; }
    }
    public class GitHubOwnerItem { public string login { get; set; } }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, string language) => value is Visibility v && v == Visibility.Visible;
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) => !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, string language) => null;
    }
}
