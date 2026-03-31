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
using Microsoft.UI.Xaml.Media.Imaging;
using AmongUsModManager.Services;
using AmongUsModManager.Models;
using AmongUsModManager.Models.Services;

namespace AmongUsModManager.Pages
{
    public sealed partial class LibraryPage : Page
    {
        private static readonly HttpClient _http = new HttpClient();
        private List<VanillaPathInfo> _issueMods = new();
        private static List<VanillaPathInfo>? _lastLoadedMods;
        private static DateTime _lastLoadedAt = DateTime.MinValue;

        public LibraryPage()
        {
            this.InitializeComponent();
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "AmongUsModManager-App");
            LogService.Info("LibraryPage", "ページ初期化");
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _ = LoadLibraryAsync();
        }

        private async Task LoadLibraryAsync(bool forceVersionCheck = false)
        {
            // キャッシュを必ず無効化してから読む（タグ設定・解除直後でも最新を反映）
            ConfigService.InvalidateCache();
            var config = ConfigService.Load();
            var mods = config.VanillaPaths ?? new List<VanillaPathInfo>();

            // CurrentVersion が null/空のものだけ "Unknown" にする
            // ※ 保存済みのバージョン文字列を "Unknown" で上書きしない
            foreach (var m in mods)
                if (m.CurrentVersion == null) m.CurrentVersion = "";

            // GitHubOwner未設定 かつ 手動解除していない Mod のみ自動連携
            bool autoLinked = false;
            foreach (var mod in mods)
            {
                if (!string.IsNullOrEmpty(mod.GitHubOwner)) continue;
                if (mod.GitHubLinkDisabled) continue;  // 解除済みは再連携しない
                var match = FindPresetByName(mod.Name);
                if (match != null)
                {
                    mod.GitHubOwner = match.Owner;
                    mod.GitHubRepo = match.Repository;
                    autoLinked = true;
                    LogService.Info("LibraryPage", $"自動GitHub連携: {mod.Name} → {match.Owner}/{match.Repository}");
                }
            }
            if (autoLinked)
            {
                config.VanillaPaths = mods;
                ConfigService.Save(config);
            }

            LibraryGridView.ItemsSource = null;
            LibraryGridView.ItemsSource = mods;

            _ = LoadImagesAfterLayoutAsync(mods);

            // forceVersionCheck か、前回から5分経過か、件数変化か、強制リセット時
            bool shouldCheck = forceVersionCheck
                || _lastLoadedMods == null
                || mods.Count != _lastLoadedMods.Count
                || (DateTime.Now - _lastLoadedAt).TotalMinutes > 5;

            if (shouldCheck)
            {
                _issueMods.Clear();
                LoadingBar.Visibility = Visibility.Visible;
                VersionIssueBar.IsOpen = false;

                foreach (var mod in mods)
                {
                    if (string.IsNullOrEmpty(mod.GitHubOwner)) continue;
                    try
                    {
                        string apiUrl = "https://api.github.com/repos/" + mod.GitHubOwner + "/" + mod.GitHubRepo + "/releases/latest";
                        var rel = await _http.GetFromJsonAsync<GitHubRelease>(apiUrl);
                        if (rel?.tag_name != null && rel.tag_name != mod.CurrentVersion)
                            _issueMods.Add(mod);
                    }
                    catch { }
                }

                // キャッシュは件数ではなくコピーで保持する（参照共有を避ける）
                _lastLoadedMods = mods.Select(m => new VanillaPathInfo
                {
                    Name = m.Name,
                    Path = m.Path,
                    GitHubOwner = m.GitHubOwner,
                    GitHubRepo = m.GitHubRepo,
                    CurrentVersion = m.CurrentVersion
                }).ToList();
                _lastLoadedAt = DateTime.Now;
                LoadingBar.Visibility = Visibility.Collapsed;

                // GitHub連携済みで CurrentVersion が空または "Unknown" のMod → バージョン未設定警告
                UpdateWarningBar(mods);
            }
            else
            {
                // shouldCheck が false の場合でも、警告状態は更新する
                UpdateWarningBar(mods);
            }
        }

        private void UpdateWarningBar(List<VanillaPathInfo> mods)
        {
            // GitHub連携済みで CurrentVersion が空または "Unknown" のMod → バージョン未設定警告
            var unknownMods = mods.Where(m =>
                (string.IsNullOrEmpty(m.CurrentVersion) || string.Equals(m.CurrentVersion, "Unknown", StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrEmpty(m.GitHubOwner)).ToList();

            if (unknownMods.Count > 0)
            {
                VersionIssueBar.Title = "バージョンが登録されていません";
                VersionIssueBar.Message = $"「{unknownMods[0].Name}」などのModにバージョンが登録されていません。";
                VersionIssueActionBtn.Content = "タグを設定する";
                VersionIssueActionBtn.Click -= ResolveVersionIssues_Click;
                VersionIssueActionBtn.Click += (s, e) => _ = ShowTagPickerAsync(unknownMods[0], downloadFile: false);
                VersionIssueBar.IsOpen = true;
            }
            else
            {
                VersionIssueBar.IsOpen = _issueMods.Count > 0;
                VersionIssueActionBtn.Content = "確認して解決する";
            }
        }

        private async Task LoadImagesAfterLayoutAsync(List<VanillaPathInfo> mods)
        {
            await Task.Delay(400);

            foreach (var mod in mods)
            {
                if (string.IsNullOrEmpty(mod.GitHubOwner) || string.IsNullOrEmpty(mod.GitHubRepo))
                    continue;
                try
                {
                    var container = LibraryGridView.ContainerFromItem(mod) as GridViewItem;
                    if (container == null) continue;

                    var img = FindChild<Image>(container, "ModBannerImage");
                    if (img == null) continue;

                    string url = "https://opengraph.githubassets.com/1/" + mod.GitHubOwner + "/" + mod.GitHubRepo;
                    var bmp = new BitmapImage();
                    bmp.UriSource = new Uri(url);
                    img.Source = bmp;
                }
                catch { }
            }
        }

        private static T? FindChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            int n = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < n; i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T fe && fe.Name == name) return fe;
                var found = FindChild<T>(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private async void ResolveVersionIssues_Click(object sender, RoutedEventArgs e)
        {
            var panel = new StackPanel { Spacing = 16, Width = 420 };
            var choiceMap = new Dictionary<VanillaPathInfo, ComboBox>();

            foreach (var mod in _issueMods)
            {
                var sp = new StackPanel { Spacing = 4 };
                sp.Children.Add(new TextBlock
                {
                    Text = mod.Name,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 14
                });
                sp.Children.Add(new TextBlock
                {
                    Text = "現在のタグ設定: " + mod.CurrentVersion,
                    FontSize = 12,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                });

                var combo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, Header = "対応方法" };
                combo.Items.Add("最新版にアップデートする（ファイルも更新）");
                combo.Items.Add("タグを手動で選んでアップデートする");
                combo.Items.Add("タグ設定だけ変更する（ファイルはそのまま）");
                combo.Items.Add("今回はスキップ");
                combo.SelectedIndex = 0;
                choiceMap[mod] = combo;
                sp.Children.Add(combo);
                panel.Children.Add(sp);
                panel.Children.Add(new MenuFlyoutSeparator());
            }

            var dialog = new ContentDialog
            {
                Title = "バージョン不一致の解決",
                Content = new ScrollViewer { Content = panel, MaxHeight = 460 },
                PrimaryButtonText = "実行",
                CloseButtonText = "キャンセル",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            var config = ConfigService.Load();
            foreach (var (mod, combo) in choiceMap)
            {
                switch (combo.SelectedIndex)
                {
                    case 0:
                        {
                            string apiUrl = "https://api.github.com/repos/" + mod.GitHubOwner + "/" + mod.GitHubRepo + "/releases/latest";
                            var rel = await _http.GetFromJsonAsync<GitHubRelease>(apiUrl);
                            if (rel?.tag_name != null)
                            {
                                await PerformUpdateWithUI(mod, rel.tag_name);
                                UpdateVersionInConfig(config, mod.Path, rel.tag_name);
                            }
                            break;
                        }
                    case 1:
                        await ShowTagPickerAsync(mod, downloadFile: true);
                        break;
                    case 2:
                        await ShowTagPickerAsync(mod, downloadFile: false);
                        break;
                }
            }

            ConfigService.Save(config);
            _lastLoadedAt = DateTime.MinValue;
            await LoadLibraryAsync();
        }

        private async Task ShowTagPickerAsync(VanillaPathInfo mod, bool downloadFile)
        {
            GitHubRelease? latest;
            List<GitHubRelease>? releases;
            try
            {
                string latestUrl = "https://api.github.com/repos/" + mod.GitHubOwner + "/" + mod.GitHubRepo + "/releases/latest";
                string listUrl = "https://api.github.com/repos/" + mod.GitHubOwner + "/" + mod.GitHubRepo + "/releases?per_page=50";
                var t1 = _http.GetFromJsonAsync<GitHubRelease>(latestUrl);
                var t2 = _http.GetFromJsonAsync<List<GitHubRelease>>(listUrl);
                latest = await t1;
                releases = await t2;
            }
            catch (Exception ex)
            {
                await ShowError("リリース取得エラー", ex.Message);
                return;
            }

            if (releases == null || releases.Count == 0) return;

            var tagOptions = new List<string>();
            if (latest?.tag_name != null)
                tagOptions.Add("最新版 (" + latest.tag_name + ")");
            tagOptions.AddRange(releases.Select(r => r.tag_name ?? ""));

            var combo = new ComboBox
            {
                ItemsSource = tagOptions,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Header = downloadFile ? "インストールするバージョンを選択" : "バージョン設定値を選択（ファイルはダウンロードしません）",
                SelectedIndex = 0
            };

            var infoText = new TextBlock
            {
                Text = "現在のタグ設定: " + (string.IsNullOrEmpty(mod.CurrentVersion) ? "未設定" : mod.CurrentVersion),
                FontSize = 12,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                Margin = new Thickness(0, 0, 0, 4)
            };

            var content = new StackPanel { Spacing = 8, Width = 380 };
            content.Children.Add(infoText);
            content.Children.Add(combo);

            string dlgTitle = downloadFile ? mod.Name + " をアップデート" : mod.Name + " バージョン設定";
            var dlg = new ContentDialog
            {
                Title = dlgTitle,
                Content = content,
                PrimaryButtonText = downloadFile ? "ダウンロードして更新" : "この設定を保存",
                CloseButtonText = "キャンセル",
                XamlRoot = this.XamlRoot
            };

            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;
            if (combo.SelectedItem is not string selected) return;

            string tag = selected.StartsWith("最新版") && latest?.tag_name != null
                ? latest.tag_name : selected;

            if (downloadFile)
                await PerformUpdateWithUI(mod, tag);

            var config = ConfigService.Load();
            UpdateVersionInConfig(config, mod.Path, tag);
            ConfigService.Save(config);

            _lastLoadedAt = DateTime.MinValue;
            await LoadLibraryAsync();
        }

        private async Task PerformUpdateWithUI(VanillaPathInfo mod, string? tag)
        {
            UpdateProgressDialog.XamlRoot = this.XamlRoot;
            UpdateStatusText.Text = "ダウンロード中...";
            UpdateProgressBar.IsIndeterminate = true;
            _ = UpdateProgressDialog.ShowAsync();
            try
            {
                await PerformUpdateLogic(mod, tag);
                UpdateStatusText.Text = "完了しました";
                await Task.Delay(800);
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = "エラー: " + ex.Message;
                await Task.Delay(2000);
            }
            finally { UpdateProgressDialog.Hide(); }
        }

        public async Task PerformUpdateLogic(VanillaPathInfo mod, string? specificTag = null)
        {
            LogService.Info("LibraryPage", "アップデート: " + mod.Name);
            string url = specificTag == null
                ? "https://api.github.com/repos/" + mod.GitHubOwner + "/" + mod.GitHubRepo + "/releases/latest"
                : "https://api.github.com/repos/" + mod.GitHubOwner + "/" + mod.GitHubRepo + "/releases/tags/" + specificTag;

            var release = await _http.GetFromJsonAsync<GitHubRelease>(url);
            var dll = release?.assets.FirstOrDefault(a => a.name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
            if (dll == null) throw new Exception("DLLファイルが見つかりませんでした。");

            string plugins = Path.Combine(mod.Path, "BepInEx", "plugins");
            bool isNebula = Directory.Exists(plugins) && Directory.GetFiles(plugins, "NebulaLoader.dll").Any();
            string dest = isNebula ? Path.Combine(mod.Path, "nebula") : plugins;
            if (!Directory.Exists(dest)) Directory.CreateDirectory(dest);

            var data = await _http.GetByteArrayAsync(dll.browser_download_url);
            File.WriteAllBytes(Path.Combine(dest, dll.name), data);
        }

        private void UpdateVersionInConfig(AppConfig config, string path, string version)
        {
            var t = config.VanillaPaths?.FirstOrDefault(v => v.Path == path);
            if (t != null) t.CurrentVersion = version;
        }

        private async void UpdateMod_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem { Tag: VanillaPathInfo mod }) return;
            if (string.IsNullOrEmpty(mod.GitHubOwner)) return;
            await PerformUpdateWithUI(mod, tag: null);
            var config = ConfigService.Load();
            string apiUrl = "https://api.github.com/repos/" + mod.GitHubOwner + "/" + mod.GitHubRepo + "/releases/latest";
            var rel = await _http.GetFromJsonAsync<GitHubRelease>(apiUrl);
            if (rel?.tag_name != null) UpdateVersionInConfig(config, mod.Path, rel.tag_name);
            ConfigService.Save(config);
            _lastLoadedAt = DateTime.MinValue;
            await LoadLibraryAsync();
        }

        private async void SetVersionTag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem { Tag: VanillaPathInfo mod }) return;
            if (string.IsNullOrEmpty(mod.GitHubOwner)) return;
            await ShowTagPickerAsync(mod, downloadFile: false);
        }

        private void AutoUpdateToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleMenuFlyoutItem { Tag: VanillaPathInfo mod }) return;
            var config = ConfigService.Load();
            var t = config.VanillaPaths?.FirstOrDefault(v => v.Path == mod.Path);
            if (t != null) { t.IsAutoUpdateEnabled = mod.IsAutoUpdateEnabled; ConfigService.Save(config); }
        }

        private async void DeleteMod_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem { Tag: VanillaPathInfo mod }) return;

            var dlg = new ContentDialog
            {
                Title = "MODの削除確認",
                Content = mod.Name + " をどのように削除しますか？\n\n登録解除：アプリのリストから消します（ファイルは残ります）\n完全削除：フォルダごと物理的に削除します",
                PrimaryButtonText = "登録解除のみ",
                SecondaryButtonText = "フォルダごと削除",
                CloseButtonText = "キャンセル",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.None) return;

            var config = ConfigService.Load();
            if (result == ContentDialogResult.Secondary)
            {
                try { if (Directory.Exists(mod.Path)) Directory.Delete(mod.Path, true); }
                catch (Exception ex) { Debug.WriteLine("削除エラー: " + ex.Message); }
            }
            config.VanillaPaths?.RemoveAll(v => v.Path == mod.Path);
            ConfigService.Save(config);
            _lastLoadedAt = DateTime.MinValue;
            await LoadLibraryAsync();
        }

        private void RenameMod_Click(object sender, RoutedEventArgs e) { }

        // SupportedMods から名前が最もよく一致するものを返す
        // 優先順位: 完全一致 > 正規化後の完全一致 > 部分一致（ただし部分一致は大きな一致のみ）
        private static ModPreset? FindPresetByName(string modName)
        {
            if (string.IsNullOrEmpty(modName)) return null;

            // 1. 完全一致を探す（大文字小文字区別なし）
            var exactMatch = ModInstallPage.SupportedMods.FirstOrDefault(p =>
                modName.Equals(p.Name, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null) return exactMatch;

            // 2. 正規化後の完全一致を探す（スペースとハイフンを統一して比較）
            var normalizedInput = NormalizeName(modName);
            var normalizedMatch = ModInstallPage.SupportedMods.FirstOrDefault(p =>
                normalizedInput.Equals(NormalizeName(p.Name), StringComparison.OrdinalIgnoreCase));
            if (normalizedMatch != null) return normalizedMatch;

            // 3. 部分一致を探す（modName が p.Name を完全に含む、または p.Name が modName を完全に含む）
            // ただし、短い方が長い方の80%以上の長さを持つ場合のみ（偶然のマッチを避ける）
            var partialMatches = ModInstallPage.SupportedMods
                .Where(p =>
                {
                    int modLen = modName.Length;
                    int pLen = p.Name.Length;
                    int minLen = Math.Min(modLen, pLen);
                    int maxLen = Math.Max(modLen, pLen);

                    // マッチの最小長が最大長の70%以上でないと partial match にしない
                    if (minLen < maxLen * 0.7) return false;

                    return modName.Contains(p.Name, StringComparison.OrdinalIgnoreCase)
                        || p.Name.Contains(modName, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            // 一番名前が短いものを返す（より具体的な一致）
            return partialMatches.OrderBy(p => p.Name.Length).FirstOrDefault();
        }

        // スペース、ハイフン、アンダースコアを統一
        private static string NormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            // スペース、ハイフン、アンダースコアをすべて削除してから比較
            return System.Text.RegularExpressions.Regex.Replace(name, @"[\s\-_]", "");
        }

        private async void UnlinkGitHub_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem { Tag: VanillaPathInfo mod }) return;

            var dlg = new ContentDialog
            {
                Title = "GitHub連携を解除",
                Content = $"「{mod.Name}」のGitHub連携を解除しますか？\n\n" +
                          "解除すると自動アップデートチェックが無効になります。\n" +
                          "Modのファイルは削除されません。",
                PrimaryButtonText = "解除する",
                CloseButtonText = "キャンセル",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            var config = ConfigService.Load();
            var target = config.VanillaPaths?.FirstOrDefault(v => v.Path == mod.Path);
            if (target != null)
            {
                target.GitHubOwner = null;
                target.GitHubRepo = null;
                target.IsAutoUpdateEnabled = false;
                target.GitHubLinkDisabled = true;  // 自動再連携を防ぐ
                ConfigService.Save(config);
                LogService.Info("LibraryPage", $"GitHub連携解除: {mod.Name}");
            }

            _lastLoadedAt = DateTime.MinValue;
            await LoadLibraryAsync();
        }

        private async void LinkGitHub_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem { Tag: VanillaPathInfo mod }) return;

            // ── Step 1: SupportedMods 全リスト表示（名前一致があれば自動選択） ──
            var allPresets = ModInstallPage.SupportedMods;
            var autoMatch = FindPresetByName(mod.Name);

            // ListView に表示するデータクラス
            var listItems = allPresets.Select((p, i) => new
            {
                Index = i,
                Preset = p,
                IsMatch = p == autoMatch,
                LabelText = p.Name,
                SubText = $"github.com/{p.Owner}/{p.Repository}"
            }).ToList();

            var list = new ListView
            {
                SelectionMode = ListViewSelectionMode.Single,
                MaxHeight = 360,
                Margin = new Thickness(0, 4, 0, 0)
            };

            foreach (var item in listItems)
            {
                var sp = new StackPanel { Margin = new Thickness(0, 3, 0, 3) };
                var nameTb = new TextBlock
                {
                    Text = item.IsMatch ? $"✅ {item.LabelText}  （名前一致）" : item.LabelText,
                    FontWeight = item.IsMatch
                        ? Microsoft.UI.Text.FontWeights.Bold
                        : Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = item.IsMatch
                        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.SeaGreen)
                        : null
                };
                var subTb = new TextBlock
                {
                    Text = item.SubText,
                    FontSize = 11,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                };
                sp.Children.Add(nameTb);
                sp.Children.Add(subTb);
                list.Items.Add(sp);
            }

            // 名前一致があればそれを初期選択、なければ先頭
            list.SelectedIndex = autoMatch != null
                ? listItems.FindIndex(i => i.IsMatch)
                : 0;

            // 手動入力用テキストボックス（URL直打ち or キーワード検索）
            var manualBox = new TextBox
            {
                Header = "または URL・キーワードで直接指定（リストにない場合）",
                PlaceholderText = "例: TownOfHost  または  https://github.com/xxx/yyy",
                Margin = new Thickness(0, 12, 0, 0)
            };
            if (!string.IsNullOrEmpty(mod.GitHubOwner))
                manualBox.Text = $"https://github.com/{mod.GitHubOwner}/{mod.GitHubRepo}";

            var panel = new StackPanel { Spacing = 0 };
            panel.Children.Add(new TextBlock
            {
                Text = "リストから選択するか、下のテキストボックスに入力してください。",
                FontSize = 12,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                Margin = new Thickness(0, 0, 0, 8)
            });
            panel.Children.Add(list);
            panel.Children.Add(manualBox);

            var dlg = new ContentDialog
            {
                Title = $"「{mod.Name}」のGitHubリポジトリを選択",
                Content = new ScrollViewer { Content = panel, MaxHeight = 520 },
                PrimaryButtonText = "連携する",
                SecondaryButtonText = "GitHubで検索...",
                CloseButtonText = "キャンセル",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dlg.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // テキストボックスに入力があればそちらを優先
                string manual = manualBox.Text.Trim();
                if (!string.IsNullOrEmpty(manual))
                {
                    if (Uri.TryCreate(manual, UriKind.Absolute, out var uri) && uri.Host == "github.com")
                    {
                        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            await ApplyGitHubLink(mod.Path, parts[0], parts[1]);
                            return;
                        }
                    }
                    // キーワードとして GitHub 検索にフォールスルー
                    await ShowGitHubSearchCandidatesAsync(mod, manual);
                    return;
                }

                // リストから選択
                int idx = list.SelectedIndex;
                if (idx >= 0 && idx < allPresets.Count)
                {
                    var chosen = allPresets[idx];
                    await ApplyGitHubLink(mod.Path, chosen.Owner, chosen.Repository);
                }
                return;
            }

            if (result == ContentDialogResult.Secondary)
            {
                // "GitHubで検索..." → キーワード検索ダイアログ
                string keyword = string.IsNullOrEmpty(manualBox.Text) ? mod.Name : manualBox.Text.Trim();
                await ShowGitHubSearchCandidatesAsync(mod, keyword);
            }
        }


        private async Task ShowGitHubSearchCandidatesAsync(VanillaPathInfo mod, string keyword)
        {
            try
            {
                var client = GitHubAuthService.GetClient();
                string q = Uri.EscapeDataString(keyword + " topic:among-us");
                var result = await client.GetFromJsonAsync<GitHubSearchResult>(
                    "https://api.github.com/search/repositories?q=" + q + "&sort=stars&order=desc&per_page=30");

                if (result?.items == null || result.items.Count == 0)
                {
                    string q2 = Uri.EscapeDataString(keyword + " among-us in:name,description");
                    result = await client.GetFromJsonAsync<GitHubSearchResult>(
                        "https://api.github.com/search/repositories?q=" + q2 + "&sort=stars&order=desc&per_page=30");
                }

                if (result?.items == null || result.items.Count == 0)
                {
                    await ShowError("見つかりませんでした", keyword + " に一致するリポジトリが見つかりませんでした。");
                    return;
                }

                var list = new ListView { SelectionMode = ListViewSelectionMode.Single, MaxHeight = 380 };
                list.ItemsSource = result.items;
                list.DisplayMemberPath = "full_name";

                var dlg = new ContentDialog
                {
                    Title = "検索結果（" + result.items.Count + "件）",
                    Content = list,
                    PrimaryButtonText = "このリポジトリと連携",
                    CloseButtonText = "キャンセル",
                    XamlRoot = this.XamlRoot
                };

                if (await dlg.ShowAsync() == ContentDialogResult.Primary
                    && list.SelectedItem is GitHubRepoItem sel
                    && sel.owner?.login != null
                    && sel.name != null)
                    await ApplyGitHubLink(mod.Path, sel.owner.login, sel.name);
            }
            catch (Exception ex) { await ShowError("検索エラー", ex.Message); }
        }

        private async Task ApplyGitHubLink(string modPath, string owner, string repo)
        {
            var config = ConfigService.Load();
            var t = config.VanillaPaths?.FirstOrDefault(v => v.Path == modPath);
            if (t != null)
            {
                t.GitHubOwner = owner;
                t.GitHubRepo = repo;
                t.GitHubLinkDisabled = false;  // 手動連携時は解除フラグをリセット
                ConfigService.Save(config);
            }
            _lastLoadedAt = DateTime.MinValue;
            await LoadLibraryAsync();
        }

        private async void GenerateShareCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem { Tag: VanillaPathInfo mod }) return;
            try
            {
                var config = ConfigService.Load();
                string platform = !string.IsNullOrEmpty(config.MainPlatform) ? config.MainPlatform : config.Platform;
                string shareCode = ShareCodeService.Generate(mod, platform,
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                 "AmongUsModManager", "SharedCodes"));

                var copyBtn = new Button { Content = "コピー", Padding = new Thickness(16, 8, 16, 8), HorizontalAlignment = HorizontalAlignment.Left };
                copyBtn.Click += (_, _) =>
                {
                    var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
                    dp.SetText(shareCode);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
                    copyBtn.Content = "コピーしました";
                };

                var content = new StackPanel { Spacing = 10 };
                content.Children.Add(new TextBlock { Text = "共有コードを生成しました。", TextWrapping = TextWrapping.Wrap });
                content.Children.Add(new TextBox { Text = shareCode, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"), FontSize = 13, IsReadOnly = true, TextWrapping = TextWrapping.Wrap });
                content.Children.Add(copyBtn);

                await new ContentDialog { Title = "共有コード", Content = content, CloseButtonText = "閉じる", XamlRoot = this.XamlRoot }.ShowAsync();
            }
            catch (Exception ex) { await ShowError("エラー", "共有コードの生成に失敗しました: " + ex.Message); }
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
            if (sender is Button { Tag: string path })
            {
                string exe = Path.Combine(path, "Among Us.exe");
                if (File.Exists(exe))
                    Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = path, UseShellExecute = true });
            }
        }

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: string path })
                await Windows.System.Launcher.LaunchFolderPathAsync(path);
        }

        private async Task ShowError(string title, string msg)
        {
            await new ContentDialog { Title = title, Content = msg, CloseButtonText = "OK", XamlRoot = this.XamlRoot }.ShowAsync();
        }
    }

    public class GitHubSearchResult
    {
        public List<GitHubRepoItem>? items { get; set; }
        public int total_count { get; set; }
    }

    public class GitHubRepoItem
    {
        public string? name { get; set; }
        public string? full_name { get; set; }
        public string? description { get; set; }
        public int stargazers_count { get; set; }
        public GitHubOwnerItem? owner { get; set; }
    }

    public class GitHubOwnerItem
    {
        public string? login { get; set; }
    }
}
