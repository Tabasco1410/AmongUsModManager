using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using AmongUsModManager.Models;
using AmongUsModManager.Models.Services;
using AmongUsModManager.Services;

namespace AmongUsModManager.Pages
{
    public class NewsDisplayItem
    {
        public string Id      { get; set; } = "";
        public string Title   { get; set; } = "";
        public string Content { get; set; } = "";
        public string Date    { get; set; } = "";
        public string Url     { get; set; } = "";
        public bool   IsRead  { get; set; }
        public NewsItem? OriginalItem { get; set; }  

        public SolidColorBrush UnreadDotColor
            => IsRead ? new SolidColorBrush(Colors.Transparent) : new SolidColorBrush(Colors.DodgerBlue);
        public double TitleOpacity   => IsRead ? 0.75 : 1.0;
        public double ContentOpacity => IsRead ? 0.65 : 0.9;
        public Visibility UnreadButtonVisibility => IsRead ? Visibility.Collapsed : Visibility.Visible;
    }

    public sealed partial class HomePage : Page
    {
        
        private HttpClient _http => GitHubAuthService.GetClient();
        private Action?    _pendingConfirmAction;
        private Queue<string> _unregisteredFolders = new();
        private ReleaseItem?  _detailTarget;

        private const string NewsUrl = "https://amongusmodmanager.web.app/News.json";
        private List<NewsDisplayItem> _newsItems = new();

        private bool _initialized = false;

        public HomePage()
        {
            this.InitializeComponent();
            LogService.Info("HomePage", "ページ初期化");

            
            _cacheStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _cacheStatusTimer.Tick += (_, _) =>
            {
                UpdateCacheStatusUI();
                UpdateReleaseInfoCacheUI();
            };
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _cacheStatusTimer.Start();

            
            if (!_initialized)
            {
                _initialized = true;
                _ = InitializeAsync();
            }
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _cacheStatusTimer.Stop();
        }

        private readonly DispatcherTimer _cacheStatusTimer;

        private async Task InitializeAsync()
        {
            LogService.Debug("HomePage", "InitializeAsync 開始");
            LoadModSelector();
            LoadLastMethod();
            await CheckAppUpdatePopupAsync();
            await LoadReleaseInfoAsync();
            await LoadNewsAsync();
            
            LogService.Debug("HomePage", "全Modリリース一覧 自動ロード開始");
            await LoadAllReleasesAsync();
            LogService.Debug("HomePage", "InitializeAsync 完了");
        }

        
        private void LoadModSelector()
        {
            var config = ConfigService.Load();
            ModSelector.Items.Clear();
            if (config?.VanillaPaths != null)
                foreach (var info in config.VanillaPaths)
                    ModSelector.Items.Add(new ComboBoxItem { Content = info.Name, Tag = info.Path });
            if (ModSelector.Items.Count > 0) ModSelector.SelectedIndex = 0;

            _unregisteredFolders.Clear();
            if (!string.IsNullOrEmpty(config?.ModDataPath) && Directory.Exists(config.ModDataPath))
            {
                foreach (var dir in Directory.GetDirectories(config.ModDataPath))
                {
                    if (!File.Exists(Path.Combine(dir, "Among Us.exe"))) continue;
                    bool registered = config.VanillaPaths?.Any(v =>
                        string.Equals(Path.GetFullPath(v.Path).TrimEnd('\\'),
                                      Path.GetFullPath(dir).TrimEnd('\\'),
                                      StringComparison.OrdinalIgnoreCase)) ?? false;
                    if (!registered) _unregisteredFolders.Enqueue(dir);
                }
            }
            ShowNextBanner();
        }

        private void ShowNextBanner()
        {
            if (_unregisteredFolders.Count == 0) { LocalInfoBar.IsOpen = false; return; }
            string folder = _unregisteredFolders.Peek();
            LocalInfoBar.Message = $"「{Path.GetFileName(folder)}」を登録しますか？";
            _pendingConfirmAction = () =>
            {
                var config = ConfigService.Load();
                config.VanillaPaths.Add(new VanillaPathInfo
                {
                    Name = Path.GetFileName(folder),
                    Path = folder,
                    CurrentVersion = "Unknown"  
                });
                ConfigService.Save(config);
                ModSelector.Items.Add(new ComboBoxItem { Content = Path.GetFileName(folder), Tag = folder });
            };
            LocalInfoBar.IsOpen = true;
        }

        private void LocalInfoBarButton_Click(object sender, RoutedEventArgs e)
        { _pendingConfirmAction?.Invoke(); _unregisteredFolders.Dequeue(); ShowNextBanner(); }

        private void LocalInfoBar_CloseButtonClick(InfoBar sender, object args)
        { _unregisteredFolders.Dequeue(); ShowNextBanner(); }

        
        private System.Diagnostics.Process? _gameProcess;

        
        private const string SteamAppId = "945360";
        private const string SteamAppIdFileName = "steam_appid.txt";

        private async void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModSelector.SelectedItem is not ComboBoxItem item) return;

            string path = item.Tag?.ToString() ?? "";
            string exe  = Path.Combine(path, "Among Us.exe");
            LogService.Info("HomePage", $"ゲーム起動: {exe}");

            if (!File.Exists(exe))
            {
                LogService.Warn("HomePage", $"実行ファイルが見つかりません: {exe}");
                return;
            }

            
            var config = ConfigService.Load();
            if (config.Platform == "Steam")
            {
                string appIdPath = Path.Combine(path, SteamAppIdFileName);
                if (!File.Exists(appIdPath))
                {
                    try
                    {
                        File.WriteAllText(appIdPath, SteamAppId);
                        LogService.Info("HomePage", $"steam_appid.txt を自動生成しました: {appIdPath}");
                    }
                    catch (Exception ex)
                    {
                        LogService.Warn("HomePage", $"steam_appid.txt の生成に失敗: {ex.Message}");
                    }
                }
                else
                {
                    
                    try
                    {
                        string existing = File.ReadAllText(appIdPath).Trim();
                        if (existing != SteamAppId)
                        {
                            File.WriteAllText(appIdPath, SteamAppId);
                            LogService.Info("HomePage", $"steam_appid.txt の内容を修正しました ({existing} → {SteamAppId})");
                        }
                        else
                        {
                            LogService.Debug("HomePage", $"steam_appid.txt 確認済み: {appIdPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Warn("HomePage", $"steam_appid.txt の読み取りに失敗: {ex.Message}");
                    }
                }
            }
            

            
            LaunchBtn.IsEnabled = false;
            LaunchBtn.Content = "⏳  起動中...";

            
            string modName = item.Content?.ToString() ?? "Unknown";
            LaunchHistoryService.Add(modName);
            var launchConfig = ConfigService.Load();
            launchConfig.LastLaunchTime = DateTime.Now;
            ConfigService.Save(launchConfig);
            LogService.Info("HomePage", $"起動履歴記録: {modName}");

            try
            {
                
                if (launchConfig.Platform == "Epic")
                {
                    var epicResult = await EpicLoginService.LaunchDirectAsync(exe, path);
                    if (!epicResult.Success)
                    {
                        LogService.Warn("HomePage", $"Epic 直接起動失敗: {epicResult.Error}");
                        ResetLaunchButton();
                        await new ContentDialog
                        {
                            Title           = "起動エラー",
                            Content         = epicResult.Error,
                            CloseButtonText = "OK",
                            XamlRoot        = this.XamlRoot
                        }.ShowAsync();
                        return;
                    }
                    _gameProcess = epicResult.Process;
                    LogService.Info("HomePage", $"Epic 直接起動成功 PID={_gameProcess?.Id}");
                }
                else
                {
                    
                    _gameProcess = Process.Start(new ProcessStartInfo(exe)
                        { WorkingDirectory = path, UseShellExecute = true });
                    LogService.Info("HomePage", $"Among Us プロセス起動 PID={_gameProcess?.Id}");
                }

                
                await Task.Delay(2000);
                if (_gameProcess != null && !_gameProcess.HasExited)
                {
                    LaunchBtn.Content = "🎮  プレイ中";
                    
                    _ = WatchGameProcessAsync(_gameProcess);
                }
                else
                {
                    
                    ResetLaunchButton();
                }
            }
            catch (Exception ex)
            {
                LogService.Error("HomePage", "ゲーム起動エラー", ex);
                ResetLaunchButton();
            }
        }

        private async Task WatchGameProcessAsync(System.Diagnostics.Process proc)
        {
            try
            {
                await Task.Run(() => proc.WaitForExit());
                LogService.Info("HomePage", $"Among Us プロセス終了 ExitCode={proc.ExitCode}");
            }
            catch { }
            DispatcherQueue.TryEnqueue(ResetLaunchButton);
        }

        private void ResetLaunchButton()
        {
            LaunchBtn.IsEnabled = true;
            LaunchBtn.Content = "▶  ゲームを起動";
            _gameProcess = null;
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModSelector.SelectedItem is ComboBoxItem item)
            {
                string path = item.Tag?.ToString() ?? "";
                if (Directory.Exists(path))
                    await Windows.System.Launcher.LaunchFolderPathAsync(path);
            }
        }

        
        
        private static List<ReleaseItem>? _releaseInfoCache = null;
        private static DateTime _releaseInfoCachedAt = DateTime.MinValue;

        private async void ReleaseInfoForceRefresh_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("HomePage", "インストール済みリリース情報 手動強制更新");
            _releaseInfoCache = null;
            _releaseInfoCachedAt = DateTime.MinValue;
            ReleaseInfoRefreshBtn.IsEnabled = false;
            await LoadReleaseInfoAsync();
            ReleaseInfoRefreshBtn.IsEnabled = true;
        }

        private async Task LoadReleaseInfoAsync()
        {
            UpdateLoadingRing.IsActive = true;
            LogService.Info("HomePage", "リリース情報取得開始");

            
            if (_releaseInfoCache != null && DateTime.Now - _releaseInfoCachedAt < CacheTtl)
            {
                LogService.Debug("HomePage", "インストール済みリリース情報: キャッシュを使用");
                var sorted0 = _releaseInfoCache.OrderByDescending(i => i.CanUpdate).ThenByDescending(i => i.CanInstall).ToList();
                ReleaseListView.ItemsSource = sorted0;
                ReleaseEmptyText.Visibility = sorted0.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                UpdateLoadingRing.IsActive  = false;
                UpdateReleaseInfoCacheUI();
                return;
            }

            var config = ConfigService.Load();
            var items  = new List<ReleaseItem>();

            if (config?.VanillaPaths != null)
            {
                foreach (var mod in config.VanillaPaths)
                {
                    if (string.IsNullOrEmpty(mod.GitHubOwner) || string.IsNullOrEmpty(mod.GitHubRepo))
                    {
                        items.Add(MakeReleaseItem(mod, "未連携", Colors.Gray, Colors.Gray, false, false));
                        continue;
                    }
                    try
                    {
                        var release = await _http.GetFromJsonAsync<GitHubRelease>(
                            $"https://api.github.com/repos/{mod.GitHubOwner}/{mod.GitHubRepo}/releases/latest");
                        if (release == null) continue;

                        bool notInstalled = string.IsNullOrEmpty(mod.CurrentVersion);
                        bool hasUpdate    = !notInstalled && mod.CurrentVersion != "Unknown"
                                           && release.tag_name != mod.CurrentVersion;
                        LogService.Debug("HomePage", $"{mod.Name}: current={mod.CurrentVersion}, latest={release.tag_name}, hasUpdate={hasUpdate}");

                        bool isUnknownVersion = mod.CurrentVersion == "Unknown";

                        var ri = new ReleaseItem
                        {
                            ModName = mod.Name, LatestTag = release.tag_name,
                            CurrentVersion = notInstalled ? "未インストール" : (isUnknownVersion ? "バージョン不明" : (mod.CurrentVersion ?? "不明")),
                            PublishedAt = release.published_at?.ToString("yyyy/MM/dd") ?? "",
                            ReleaseBody = release.body ?? "", DownloadUrl = release.assets?.FirstOrDefault()?.browser_download_url ?? "",
                            OriginalMod = mod, CanUpdate = hasUpdate && !notInstalled, CanInstall = notInstalled
                        };
                        if      (notInstalled)     { ri.BadgeText = "未インストール"; ri.BadgeColor = new SolidColorBrush(Colors.SteelBlue);   ri.StatusColor = new SolidColorBrush(Colors.SteelBlue); }
                        else if (isUnknownVersion) { ri.BadgeText = "バージョン不明"; ri.BadgeColor = new SolidColorBrush(Colors.SlateGray);    ri.StatusColor = new SolidColorBrush(Colors.SlateGray); }
                        else if (hasUpdate)        { ri.BadgeText = "更新あり";       ri.BadgeColor = new SolidColorBrush(Colors.DarkOrange);   ri.StatusColor = new SolidColorBrush(Colors.Orange); }
                        else                   { ri.BadgeText = "最新";           ri.BadgeColor = new SolidColorBrush(Colors.SeaGreen);   ri.StatusColor = new SolidColorBrush(Colors.SeaGreen); }
                        items.Add(ri);
                    }
                    catch (Exception ex)
                    {
                        LogService.Error("HomePage", $"{mod.Name} リリース取得エラー", ex);
                        if (IsRateLimitError(ex))
                        {
                            ShowRateLimitBanner();
                            items.Add(MakeReleaseItem(mod, "制限中", Colors.Orange, Colors.Orange, false, false));
                        }
                        else
                        {
                            items.Add(MakeReleaseItem(mod, "エラー", Colors.Tomato, Colors.Red, false, false));
                        }
                    }
                }
            }

            var sorted = items.OrderByDescending(i => i.CanUpdate).ThenByDescending(i => i.CanInstall).ToList();

            
            _releaseInfoCache   = sorted;
            _releaseInfoCachedAt = DateTime.Now;

            ReleaseListView.ItemsSource = sorted;
            ReleaseEmptyText.Visibility = sorted.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateLoadingRing.IsActive  = false;
            UpdateReleaseInfoCacheUI();
            LogService.Info("HomePage", $"リリース情報取得完了: {sorted.Count} 件");
        }

        private void UpdateReleaseInfoCacheUI()
        {
            if (_releaseInfoCachedAt == DateTime.MinValue)
            {
                ReleaseLastUpdatedText.Text  = "最終確認: --";
                ReleaseCacheStatusText.Text  = "";
                return;
            }
            ReleaseLastUpdatedText.Text = "最終確認: " + _releaseInfoCachedAt.ToString("HH:mm:ss");
            if (CacheTtl == TimeSpan.Zero)
            {
                ReleaseCacheStatusText.Text = "（GitHub連携中・毎回更新）";
                return;
            }
            var elapsed = DateTime.Now - _releaseInfoCachedAt;
            if (elapsed >= CacheTtl)
                ReleaseCacheStatusText.Text = "（更新可能）";
            else
            {
                int rem = (int)(CacheTtl - elapsed).TotalSeconds;
                ReleaseCacheStatusText.Text = rem >= 60
                    ? $"（次の更新まで {rem / 60}分{rem % 60}秒）"
                    : $"（次の更新まで {rem}秒）";
            }
        }

        private ReleaseItem MakeReleaseItem(VanillaPathInfo mod, string badge,
            Windows.UI.Color badgeColor, Windows.UI.Color statusColor, bool canUpdate, bool canInstall)
            => new ReleaseItem { ModName = mod.Name, LatestTag = badge, CurrentVersion = mod.CurrentVersion ?? "不明",
                OriginalMod = mod, CanUpdate = canUpdate, CanInstall = canInstall, BadgeText = badge,
                BadgeColor = new SolidColorBrush(badgeColor), StatusColor = new SolidColorBrush(statusColor) };

        private static bool IsRateLimitError(Exception ex)
        {
            
            if (ex is System.Net.Http.HttpRequestException hre)
            {
                string msg = hre.Message ?? "";
                return msg.Contains("403") || msg.Contains("rate limit", StringComparison.OrdinalIgnoreCase);
            }
            return ex.Message.Contains("403") || ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase);
        }

        private void ShowRateLimitBanner()
        {
            
            if (RateLimitBar.IsOpen) return;
            RateLimitBar.IsOpen = true;
            LogService.Warn("HomePage", "GitHub API レートリミット到達 → バナー表示");
        }

        private void RecheckUpdates_Click(object sender, RoutedEventArgs e) => _ = LoadReleaseInfoAsync();

        private void ReleaseItem_Click(object sender, ItemClickEventArgs e)
        { if (e.ClickedItem is ReleaseItem item) _ = ShowDetailAsync(item); }

        private void ReleaseDetail_Click(object sender, RoutedEventArgs e)
        { if (sender is Button btn && btn.Tag is ReleaseItem item) _ = ShowDetailAsync(item); }

        private async Task ShowDetailAsync(ReleaseItem item)
        {
            _detailTarget = item;
            DetailModName.Text = item.ModName;
            DetailTag.Text     = item.LatestTag;
            DetailDate.Text    = item.PublishedAt;
            
            bool isDark = ActualTheme == Microsoft.UI.Xaml.ElementTheme.Dark;
            string mdHtml = MarkdownHelper.ToHtml(item.ReleaseBody, isDark);
            await DetailBodyWebView.EnsureCoreWebView2Async();
            DetailBodyWebView.NavigateToString(mdHtml);
            ReleaseDetailDialog.PrimaryButtonText   = item.CanUpdate  ? "更新する" : "";
            ReleaseDetailDialog.SecondaryButtonText = item.CanInstall ? "インストール" : "ファイルをDL";
            ReleaseDetailDialog.XamlRoot = this.XamlRoot;

            var result = await ReleaseDetailDialog.ShowAsync();
            if (result == ContentDialogResult.Primary && item.CanUpdate)
                await PerformUpdateAsync(item);
            else if (result == ContentDialogResult.Secondary)
            {
                if (item.CanInstall) NavigateToInstall();
                else if (!string.IsNullOrEmpty(item.DownloadUrl))
                    Process.Start(new ProcessStartInfo(item.DownloadUrl) { UseShellExecute = true });
            }
        }

        private async void QuickUpdate_Click(object sender, RoutedEventArgs e)
        { if (sender is Button btn && btn.Tag is ReleaseItem item) await PerformUpdateAsync(item); }

        private async Task PerformUpdateAsync(ReleaseItem item)
        {
            UpdateProgressDialog.Title = $"{item.ModName} をアップデート中";
            UpdateStatusText.Text      = "準備中...";
            UpdateProgressBar.IsIndeterminate = true;
            UpdateProgressDialog.XamlRoot = this.XamlRoot;
            _ = UpdateProgressDialog.ShowAsync();
            LogService.Info("HomePage", $"アップデート開始: {item.ModName} → {item.LatestTag}");
            try
            {
                var libPage = new LibraryPage();
                await libPage.PerformUpdateLogic(item.OriginalMod, item.LatestTag);
                var config = ConfigService.Load();
                var target = config.VanillaPaths.FirstOrDefault(v => v.Path == item.OriginalMod.Path);
                if (target != null) { target.CurrentVersion = item.LatestTag; ConfigService.Save(config); }
                NotificationService.Push($"{item.ModName} を更新しました", $"{item.CurrentVersion} → {item.LatestTag}", NotificationKind.Update, "Library");
                LogService.Info("HomePage", $"アップデート完了: {item.ModName}");
                UpdateStatusText.Text = "完了しました！";
                await Task.Delay(800);
            }
            catch (Exception ex)
            {
                LogService.Error("HomePage", $"アップデートエラー: {item.ModName}", ex);
                UpdateStatusText.Text = $"エラー: {ex.Message}";
                await Task.Delay(2000);
            }
            finally { UpdateProgressDialog.Hide(); await LoadReleaseInfoAsync(); }
        }

        private void QuickInstall_Click(object sender, RoutedEventArgs e) => NavigateToInstall();
        private void NavigateToInstall()
        { if (App.MainWindowInstance is MainWindow mw) mw.NavigateToPendingPage("ModInstall"); }

        
        private List<AllReleaseItem> _allReleaseRaw = new();
        
        private static List<AllReleaseItem>? _allReleaseCache = null;
        private static DateTime _allReleaseCachedAt = DateTime.MinValue;

        
        private static TimeSpan? _cacheTtl;
        private static TimeSpan CacheTtl
        {
            get
            {
                if (_cacheTtl.HasValue) return _cacheTtl.Value;
                var config = ConfigService.Load();
                _cacheTtl = !string.IsNullOrEmpty(config.GitHubToken)
                    ? TimeSpan.Zero
                    : TimeSpan.FromMinutes(5);
                return _cacheTtl.Value;
            }
        }
        public static void InvalidateCacheTtl() => _cacheTtl = null;

        
        private async void LoadAllReleases_Click(object sender, RoutedEventArgs e)
            => await LoadAllReleasesAsync();

        
        private async void AllReleaseForceRefresh_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("HomePage", "全Modリリース一覧 手動強制更新");
            
            _allReleaseCache = null;
            _allReleaseCachedAt = DateTime.MinValue;
            AllReleaseRefreshBtn.IsEnabled = false;
            await LoadAllReleasesAsync();
            AllReleaseRefreshBtn.IsEnabled = true;
        }

        private async Task LoadAllReleasesAsync()
        {
            AllReleaseLoadingRing.IsActive = true;
            AllReleaseEmptyText.Text = "読み込み中...";
            AllReleaseEmptyText.Visibility = Visibility.Visible;

            
            if (_allReleaseCache != null && DateTime.Now - _allReleaseCachedAt < CacheTtl)
            {
                LogService.Debug("HomePage", "全Modリリース一覧: キャッシュを使用");
                _allReleaseRaw = new List<AllReleaseItem>(_allReleaseCache);
                RebuildModFilterFromCache();
                AllReleaseLoadingRing.IsActive = false;
                ApplyAllReleaseFilter();
                UpdateCacheStatusUI();
                return;
            }

            _allReleaseRaw.Clear();
            AllReleaseModFilter.Items.Clear();
            AllReleaseModFilter.Items.Add(new ComboBoxItem { Content = "すべて", Tag = "all" });
            LogService.Info("HomePage", "全Modリリース一覧取得開始");
            LogService.Debug("HomePage", $"対象Mod数: {ModInstallPage.SupportedMods.Count}");

            int successCount = 0;
            int errorCount = 0;
            
            foreach (var mod in ModInstallPage.SupportedMods)
            {
                LogService.Debug("HomePage", $"リリース取得中: {mod.Name} ({mod.Owner}/{mod.Repository})");
                try
                {
                    var url = $"https://api.github.com/repos/{mod.Owner}/{mod.Repository}/releases?per_page=50";
                    LogService.Trace("HomePage", $"  → GET {url}");
                    var releases = await _http.GetFromJsonAsync<List<GitHubRelease>>(url);
                    if (releases == null || releases.Count == 0)
                    {
                        LogService.Warn("HomePage", $"  → リリースなし: {mod.Name}");
                        errorCount++;
                        continue;
                    }
                    LogService.Debug("HomePage", $"  → 取得成功: {releases.Count}件");

<<<<<<< HEAD
=======
                    bool addedToFilter = false;
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
                    foreach (var release in releases)
                    {
                        _allReleaseRaw.Add(new AllReleaseItem
                        {
                            ModName = mod.Name, TagName = release.tag_name,
                            PublishedAt = release.published_at?.ToString("yyyy/MM/dd") ?? "",
                            PublishedAtDate = release.published_at,
                            GitHubRepo = $"{mod.Owner}/{mod.Repository}",
                            ModType = mod.IsReactor ? "Reactor" : "Mod",
                            ReleaseBody = release.body ?? "",
                            ReleaseUrl = release.html_url ?? $"https://github.com/{mod.Owner}/{mod.Repository}/releases",
                            IsLatest = release == releases[0],
                            StatusColor = release == releases[0]
                                ? new SolidColorBrush(Colors.SeaGreen)
                                : new SolidColorBrush(Colors.Gray),
                            TypeColor = mod.IsReactor
                                ? new SolidColorBrush(Colors.MediumPurple)
                                : new SolidColorBrush(Colors.SteelBlue)
                        });
                    }
                    AllReleaseModFilter.Items.Add(new ComboBoxItem { Content = mod.Name, Tag = mod.Name });
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    LogService.Error("HomePage", $"全リリース取得エラー: {mod.Name}", ex);
                    if (IsRateLimitError(ex)) ShowRateLimitBanner();
                }
            }

            
            _allReleaseCache = new List<AllReleaseItem>(_allReleaseRaw);
            _allReleaseCachedAt = DateTime.Now;

            AllReleaseLoadingRing.IsActive = false;
            ApplyAllReleaseFilter();
            UpdateCacheStatusUI();
            LogService.Info("HomePage", $"全Modリリース一覧取得完了: 成功={successCount} 件, エラー={errorCount} 件, 合計={_allReleaseRaw.Count} 件");
        }

        
        private void RebuildModFilterFromCache()
        {
            AllReleaseModFilter.Items.Clear();
            AllReleaseModFilter.Items.Add(new ComboBoxItem { Content = "すべて", Tag = "all" });
            foreach (var item in _allReleaseRaw)
                AllReleaseModFilter.Items.Add(new ComboBoxItem { Content = item.ModName, Tag = item.ModName });
        }

        
        private void UpdateCacheStatusUI()
        {
            if (_allReleaseCachedAt == DateTime.MinValue)
            {
                AllReleaseLastUpdatedText.Text = "最終確認: --";
                AllReleaseCacheStatusText.Text = "";
                return;
            }

            AllReleaseLastUpdatedText.Text = "最終確認: " + _allReleaseCachedAt.ToString("HH:mm:ss");

            if (CacheTtl == TimeSpan.Zero)
            {
                AllReleaseCacheStatusText.Text = "（GitHub連携中・毎回更新）";
                return;
            }

            var elapsed = DateTime.Now - _allReleaseCachedAt;
            if (elapsed >= CacheTtl)
            {
                AllReleaseCacheStatusText.Text = "（更新可能）";
            }
            else
            {
                int remaining = (int)(CacheTtl - elapsed).TotalSeconds;
                if (remaining >= 60)
                    AllReleaseCacheStatusText.Text = $"（次の更新まで {remaining / 60}分{remaining % 60}秒）";
                else
                    AllReleaseCacheStatusText.Text = $"（次の更新まで {remaining}秒）";
            }
        }

        
        private void AllReleaseFilter_Changed(object sender, RoutedEventArgs e)
        { if (_allReleaseRaw.Count > 0) ApplyAllReleaseFilter(); }
        private void AllReleaseFilter_Changed(object sender, SelectionChangedEventArgs e)
        { if (_allReleaseRaw.Count > 0) ApplyAllReleaseFilter(); }

        private void ApplyAllReleaseFilter()
        {
            var filtered = _allReleaseRaw.AsEnumerable();

            
            if (AllReleaseModFilter.SelectedItem is ComboBoxItem modItem &&
                modItem.Tag?.ToString() is string modTag && modTag != "all" && modTag.Length > 0)
                filtered = filtered.Where(r => r.ModName == modTag);

            
            if (AllReleaseLatestOnly.IsChecked == true)
                filtered = filtered.Where(r => r.IsLatest);

            
            if (AllReleasePeriodFilter.SelectedItem is ComboBoxItem periodItem)
            {
                var cutoff = periodItem.Tag?.ToString() switch
                {
                    "today"   => DateTime.Now.Date,
                    "week"    => DateTime.Now.AddDays(-7),
                    "month"   => DateTime.Now.AddDays(-30),
                    "3months" => DateTime.Now.AddMonths(-3),
                    "6months" => DateTime.Now.AddMonths(-6),
                    "1year"   => DateTime.Now.AddYears(-1),
                    _         => DateTime.MinValue
                };
                if (cutoff > DateTime.MinValue) filtered = filtered.Where(r => r.PublishedAtDate >= cutoff);
            }

            
            filtered = AllReleaseSortFilter.SelectedItem is ComboBoxItem sortItem
                ? sortItem.Tag?.ToString() switch
                {
                    "oldest" => filtered.OrderBy(r => r.PublishedAtDate),
                    "name"   => filtered.OrderBy(r => r.ModName).ThenByDescending(r => r.PublishedAtDate),
                    _        => filtered.OrderByDescending(r => r.PublishedAtDate)
                }
                : filtered.OrderByDescending(r => r.PublishedAtDate);

            var all = filtered.ToList();
            int total = all.Count;

            
            int pageSize = AllReleasePageSizeFilter.SelectedItem is ComboBoxItem psItem
                && psItem.Tag?.ToString() is string ps && ps != "all" && int.TryParse(ps, out int n) ? n : int.MaxValue;
            var result = all.Take(pageSize).ToList();

            AllReleaseListView.ItemsSource = result;
            AllReleaseTotalText.Text = total == result.Count
                ? $"全{total}件"
                : $"{result.Count}件表示 / 全{total}件";
            AllReleaseEmptyText.Visibility = result.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            AllReleaseEmptyText.Text = result.Count == 0 ? "該当するリリースが見つかりませんでした" : "";
        }

        private async void AllReleaseItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is AllReleaseItem item)
                await ShowReleaseDetailDialogAsync(item);
        }

        private async void AllReleaseDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AllReleaseItem item)
                await ShowReleaseDetailDialogAsync(item);
        }

        
        
        private async Task ShowReleaseDetailDialogAsync(AllReleaseItem item)
        {
            
            var webView = new Microsoft.UI.Xaml.Controls.WebView2
            {
                Height = 320,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var panel = new StackPanel { Spacing = 8, Width = 480 };
            panel.Children.Add(new TextBlock { Text = $"{item.ModName}  {item.TagName}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontSize = 15 });
            panel.Children.Add(new TextBlock { Text = $"公開日: {item.PublishedAt}  |  {item.GitHubRepo}", FontSize = 12, Opacity = 0.7 });
            panel.Children.Add(new Border { Height = 1, Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray), Opacity = 0.3 });
            panel.Children.Add(webView);

            var dialog = new ContentDialog
            {
                Title = "リリース詳細",
                Content = panel,
                PrimaryButtonText = "GitHubで開く",
                CloseButtonText = "閉じる",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            
            await webView.EnsureCoreWebView2Async();
            bool isDark = ActualTheme == Microsoft.UI.Xaml.ElementTheme.Dark;
            webView.NavigateToString(MarkdownHelper.ToHtml(item.ReleaseBody, isDark));

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
                Process.Start(new ProcessStartInfo(item.ReleaseUrl) { UseShellExecute = true });
        }

        
        private async Task LoadNewsAsync()
        {
            NewsLoadingRing.IsActive = true;
            LogService.Info("HomePage", "お知らせ取得開始");
            try
            {
                var readIds = NewsReadService.LoadReadIds();
                var rawList = await _http.GetFromJsonAsync<List<NewsItem>>(NewsUrl);
                if (rawList == null) { SetNewsEmpty("お知らせはありません"); return; }

                _newsItems = rawList.Select(n =>
                {
                    string id = string.IsNullOrEmpty(n.Id) ? $"{n.Title}_{n.Date}" : n.Id;
                    return new NewsDisplayItem
                    {
                        Id = id, Title = n.Title, Content = n.Content, Date = n.Date, Url = n.Url,
                        IsRead = readIds.Contains(id), OriginalItem = n
                    };
                }).ToList();

                NewsListView.ItemsSource    = _newsItems;
                NewsEmptyText.Visibility    = _newsItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                LogService.Info("HomePage", $"お知らせ取得完了: {_newsItems.Count} 件, 未読: {_newsItems.Count(n => !n.IsRead)} 件");
            }
            catch (Exception ex)
            {
                LogService.Error("HomePage", "お知らせ取得失敗", ex);
                SetNewsEmpty("お知らせの取得に失敗しました");
            }
            finally { NewsLoadingRing.IsActive = false; }
        }

        private void SetNewsEmpty(string message)
        {
            NewsListView.ItemsSource = null;
            NewsEmptyText.Text       = message;
            NewsEmptyText.Visibility = Visibility.Visible;
        }

        
        
        
        
        
        private void NewsItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not NewsDisplayItem item) return;

            
            NewsReadService.MarkRead(item.Id);
            item.IsRead = true;
            RefreshNewsList();
            LogService.Info("HomePage", $"お知らせクリック: {item.Title}");

            
            if (item.OriginalItem != null && App.MainWindowInstance is MainWindow mw)
                mw.NavigateToNewsDetail(item.OriginalItem);
        }

        
        private void MarkReadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is NewsDisplayItem item)
            {
                NewsReadService.MarkRead(item.Id);
                item.IsRead = true;
                RefreshNewsList();
                LogService.Info("HomePage", $"既読: {item.Title}");
            }
        }

        private void MarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            if (_newsItems.Count == 0) return;
            NewsReadService.MarkAllRead(_newsItems.Select(n => n.Id));
            foreach (var n in _newsItems) n.IsRead = true;
            RefreshNewsList();
            LogService.Info("HomePage", "すべて既読にしました");
        }

        private void RefreshNewsList()
        {
            NewsListView.ItemsSource = null;
            NewsListView.ItemsSource = _newsItems;
        }

        
        private async Task CheckAppUpdatePopupAsync()
        {
            var config = ConfigService.Load();
            if (!config.NotifyAppUpdate) return;
            var result = await AppUpdateService.CheckAsync();
            if (result?.HasUpdate != true) return;
            LogService.Info("HomePage", $"アプリ更新あり: {result.LatestTag}");
            AppUpdateBody.Text = $"バージョン {result.LatestTag} が利用可能です。\n現在のバージョン: {App.AppVersion}";
            AppUpdateDialog.XamlRoot = this.XamlRoot;
            var answer = await AppUpdateDialog.ShowAsync();

            if (answer == ContentDialogResult.Primary)
            {
                
                if (!string.IsNullOrEmpty(result.DownloadUrl))
                    await DownloadAppUpdateAsync(result);
                else
                    Process.Start(new ProcessStartInfo(result.ReleaseUrl) { UseShellExecute = true });
            }
            else if (answer == ContentDialogResult.Secondary)
            { config.NotifyAppUpdate = false; ConfigService.Save(config); }
        }

        
        private async Task DownloadAppUpdateAsync(AppUpdateService.UpdateResult result)
        {
            var progressBar = new ProgressBar { IsIndeterminate = false, Value = 0, Minimum = 0, Maximum = 100, Width = 320 };
            var statusText  = new TextBlock { Text = "ダウンロード中... 0%" };
            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(new TextBlock { Text = $"v{result.LatestTag} をダウンロード中" });
            panel.Children.Add(progressBar);
            panel.Children.Add(statusText);

            var dlDialog = new ContentDialog
            {
                Title = "アップデートをダウンロード中",
                Content = panel,
                CloseButtonText = "",
                XamlRoot = this.XamlRoot
            };
            _ = dlDialog.ShowAsync();

            var progress = new Progress<int>(pct =>
            {
                progressBar.Value = pct;
                statusText.Text = $"ダウンロード中... {pct}%";
            });

            bool ok = await AppUpdateService.DownloadAndApplyAsync(result, progress);
            dlDialog.Hide();

            if (ok)
            {
                await new ContentDialog
                {
                    Title = "ダウンロード完了",
                    Content = "インストーラーを起動します。このアプリは自動で終了されます。終了しない場合は手動で終了してください。",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
                Application.Current.Exit();
            }
            else
            {
                Process.Start(new ProcessStartInfo(result.ReleaseUrl) { UseShellExecute = true });
            }
        }

        
        private void LoadLastMethod() { LastMethodText.Text = "前回の方法を適用"; LastMethodBtn.IsEnabled = false; }

        private async void QuickInstallFromFile_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".zip");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            if (file != null && App.MainWindowInstance is MainWindow mw)
                mw.NavigateToPendingPage("ModInstall");
        }

        private async void QuickInstallFromCode_Click(object sender, RoutedEventArgs e)
        {
            ShareCodeBox.Text = "";
            ShareCodeDialog.XamlRoot = this.XamlRoot;
            var result = await ShareCodeDialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(ShareCodeBox.Text))
            {
                var data = ShareCodeService.Decode(ShareCodeBox.Text.Trim());
                if (data != null)
                    await new ContentDialog { Title = "共有コード情報",
                        Content = $"Mod: {data.ModName}\nバージョン: {data.Version}\nプラットフォーム: {data.Platform}",
                        PrimaryButtonText = "インストールへ", CloseButtonText = "キャンセル", XamlRoot = this.XamlRoot }.ShowAsync();
                if (App.MainWindowInstance is MainWindow mw) mw.NavigateToPendingPage("ModInstall");
            }
        }

        private void QuickInstallLastMethod_Click(object sender, RoutedEventArgs e) { }
    }

    
    
    
    public class AllReleaseItem
    {
        public string ModName   { get; set; } = "";
        public string TagName   { get; set; } = "";
        public string PublishedAt { get; set; } = "";
        public string GitHubRepo  { get; set; } = "";
        public string ModType     { get; set; } = "Mod";
        public string ReleaseBody { get; set; } = "";
        public string ReleaseUrl  { get; set; } = "";
        public DateTime? PublishedAtDate { get; set; }
        public bool IsLatest { get; set; } = false;  
        public string LatestBadge => IsLatest ? "最新" : "";
        public SolidColorBrush StatusColor { get; set; } = new(Colors.Gray);
        public SolidColorBrush TypeColor   { get; set; } = new(Colors.SteelBlue);
    }

    public class ReleaseItem
    {
        public string ModName        { get; set; } = "";
        public string LatestTag      { get; set; } = "";
        public string CurrentVersion { get; set; } = "";
        public string PublishedAt    { get; set; } = "";
        public string ReleaseBody    { get; set; } = "";
        public string DownloadUrl    { get; set; } = "";
        public bool   CanUpdate  { get; set; }
        public bool   CanInstall { get; set; }
        public string BadgeText  { get; set; } = "";
        public SolidColorBrush BadgeColor  { get; set; } = new(Colors.Gray);
        public SolidColorBrush StatusColor { get; set; } = new(Colors.Gray);
        public VanillaPathInfo OriginalMod { get; set; } = new();
    }
}
