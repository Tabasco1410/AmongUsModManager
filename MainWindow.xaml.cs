using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
<<<<<<< HEAD
using System.Threading.Tasks;
=======
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using AmongUsModManager.Models;
using AmongUsModManager.Models.Services;
using AmongUsModManager.Pages;
using AmongUsModManager.Services;
using Windows.Graphics;
using Windows.System;

namespace AmongUsModManager
{
    public sealed partial class MainWindow : Window
    {
        public Dictionary<string, string> LocalizedStrings { get; private set; } = new();
        private string _appUpdateUrl = "";
        private TrayIcon? _trayIcon;
        private bool _minimizeToTray = false;
        private bool _forceClose = false;
#if DEBUG
        private Pages.DebugConsoleWindow? _debugConsole;
#endif

        public MainWindow()
        {
            InitializeComponent();
            LogService.Info("MainWindow", "ウィンドウ初期化開始");
            SetWindowIcon();
            SetVersionInTitle();
            ApplyTheme();
            LoadLocalizedStrings();
            RestoreWindowSize();

            UpdateNotificationBadge();
            NotificationService.NotificationAdded += _ =>
                DispatcherQueue.TryEnqueue(UpdateNotificationBadge);
            NewsReadService.NewsUnreadCountChanged +=
                () => DispatcherQueue.TryEnqueue(UpdateNotificationBadge);

            this.AppWindow.Changed += AppWindow_Changed;

            var config = ConfigService.Load();
            if (config != null && !string.IsNullOrEmpty(config.GameInstallPath))
            {
                SetNavigationUI(true);
                ContentFrame.Navigate(typeof(HomePage));
            }
            else
            {
                SetNavigationUI(false);
                ContentFrame.Navigate(typeof(SetupPage));
            }

            CheckAppUpdateForPaneAsync();
            CheckEpicLoginForNotification();

<<<<<<< HEAD
            // 起動時にバックグラウンドでニュース未読数を取得してバッジに反映
            _ = PrefetchNewsUnreadCountAsync();
=======
>>>>>>> 9b70396323094b50176708b54875479518ab7e99

            InitTrayIcon();
            var cfg = ConfigService.Load();
            _minimizeToTray = cfg.MinimizeToTray;

<<<<<<< HEAD
=======

>>>>>>> 9b70396323094b50176708b54875479518ab7e99
            this.AppWindow.Closing += AppWindow_Closing;

#if DEBUG
            InitDebugConsoleShortcut();
#endif
            LogService.Info("MainWindow", "ウィンドウ初期化完了");
        }

<<<<<<< HEAD
        // 起動時にニュースをバックグラウンド取得してバッジ数を更新する
        // HomePage と同じ NewsUrl（JSON）からフェッチして未読数をキャッシュに反映する
        private const string NewsUrl = "https://amongusmodmanager.web.app/News.json";

        private async Task PrefetchNewsUnreadCountAsync()
        {
            try
            {
                using var http = new System.Net.Http.HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager-App");

                var rawList = await http.GetFromJsonAsync<List<NewsItem>>(NewsUrl);
                if (rawList == null) return;

                // NewsItem.Id が空の場合は HomePage と同じフォールバックキーを使う
                var allIds = rawList
                    .Select(n => string.IsNullOrEmpty(n.Id) ? $"{n.Title}_{n.Date}" : n.Id)
                    .ToList();

                int unread = NewsReadService.UnreadCount(allIds);
                NewsReadService.UpdateCachedUnreadCount(unread);
                LogService.Debug("MainWindow", $"起動時ニュース未読数取得: {unread}件");
            }
            catch (Exception ex)
            {
                LogService.Warn("MainWindow", $"起動時ニュース取得失敗（バッジは0のまま）: {ex.Message}");
            }
        }

=======
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
        private void SetVersionInTitle()
        {
            try
            {
                string ver = App.AppVersion ?? "";
                if (!string.IsNullOrEmpty(ver)) VersionText.Text = $"v{ver}";
                if (App.IsPreRelease) PreReleaseIcon.Visibility = Visibility.Visible;
#if DEBUG
                DebugIcon.Visibility = Visibility.Visible;
#endif
            }
            catch { }
        }

        private void SetWindowIcon()
        {
            try
            {
<<<<<<< HEAD
                string? icoPath = ResolveIconPath();
=======
                string icoPath = ResolveIconPath();
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
                if (icoPath != null)
                {
                    this.AppWindow.SetIcon(icoPath);
                    LogService.Debug("MainWindow", $"アイコン設定: {icoPath}");
                }
                else
                {
                    LogService.Warn("MainWindow", "アイコンファイルが見つかりません");
                }
            }
            catch (Exception ex)
            {
                LogService.Warn("MainWindow", $"アイコン設定失敗: {ex.Message}");
            }
        }

<<<<<<< HEAD
=======

>>>>>>> 9b70396323094b50176708b54875479518ab7e99
        public static string? ResolveIconPath()
        {
            string base_ = AppContext.BaseDirectory;
            string[] candidates = {
<<<<<<< HEAD
                // 実行フォルダ直下
                Path.Combine(base_, "icon.ico"),
                Path.Combine(base_, "Icon.ico"),
                // Resource サブフォルダ
                Path.Combine(base_, "Resource", "icon.ico"),
                Path.Combine(base_, "Resource", "Icon.ico"),
                // Assets サブフォルダ
                Path.Combine(base_, "Assets", "icon.ico"),
                Path.Combine(base_, "Assets", "Icon.ico"),
                // 発行後の自己完結フォルダ内
                Path.Combine(base_, "..", "icon.ico"),
                Path.Combine(base_, "..", "Icon.ico"),
                Path.Combine(base_, "..", "Resource", "icon.ico"),
            };
            foreach (var p in candidates)
            {
                try { if (File.Exists(p)) return Path.GetFullPath(p); }
                catch { }
            }
=======
                Path.Combine(base_, "icon.ico"),
                Path.Combine(base_, "Icon.ico"),
                Path.Combine(base_, "Resource", "icon.ico"),
                Path.Combine(base_, "Resource", "Icon.ico"),
                Path.Combine(base_, "Assets", "icon.ico"),
                Path.Combine(base_, "Assets", "Icon.ico"),
            };
            foreach (var p in candidates)
                if (File.Exists(p)) return p;
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
            return null;
        }

        private void RestoreWindowSize()
        {
            try
            {
                var config = ConfigService.Load();
                double w = config.WindowWidth > 400 ? config.WindowWidth : 1100;
                double h = config.WindowHeight > 300 ? config.WindowHeight : 700;

                double scale = GetDpiScale();
                int physW = (int)(w * scale);
                int physH = (int)(h * scale);

                this.AppWindow.Resize(new SizeInt32(physW, physH));
                LogService.Debug("MainWindow", $"ウィンドウサイズ復元: {w}x{h} (scale={scale:F2})");
                try
                {
<<<<<<< HEAD
                    // WindowX/Y は nullable double に変更済み（NaNではなくnullで「未設定」を表す）
                    if (config.WindowX.HasValue && config.WindowY.HasValue)
                    {
                        int px = (int)(config.WindowX.Value * scale);
                        int py = (int)(config.WindowY.Value * scale);
=======
                    if (!double.IsNaN(config.WindowX) && !double.IsNaN(config.WindowY))
                    {
                        int px = (int)(config.WindowX * scale);
                        int py = (int)(config.WindowY * scale);
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
                        this.AppWindow.Move(new PointInt32(px, py));
                        LogService.Debug("MainWindow", $"ウィンドウ位置復元: {config.WindowX},{config.WindowY}");
                    }
                    if (config.IsWindowMaximized)
                    {
                        var wa = DisplayArea.Primary.WorkArea;
                        this.AppWindow.Resize(new SizeInt32(wa.Width, wa.Height));
                        this.AppWindow.Move(new PointInt32(wa.X, wa.Y));
                        LogService.Debug("MainWindow", "ウィンドウを最大化状態で復元しました");
                    }
                }
                catch (Exception ex) { LogService.Warn("MainWindow", $"ウィンドウ位置/状態復元失敗: {ex.Message}"); }
            }
            catch (Exception ex)
            {
                LogService.Warn("MainWindow", $"ウィンドウサイズ復元失敗: {ex.Message}");
            }
        }

        private System.Threading.Timer? _saveSizeTimer;

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (!args.DidSizeChange) return;

            _saveSizeTimer?.Dispose();
            _saveSizeTimer = new System.Threading.Timer(_ =>
            {
                DispatcherQueue.TryEnqueue(SaveWindowSize);
            }, null, 500, System.Threading.Timeout.Infinite);
        }

        private void SaveWindowSize()
        {
            try
            {
                double scale = GetDpiScale();
                var size = this.AppWindow.Size;
                double w = size.Width / scale;
                double h = size.Height / scale;

<<<<<<< HEAD
=======
                
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
                try
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    if (GetWindowRect(hwnd, out RECT r))
                    {
                        double x = r.Left / scale;
                        double y = r.Top / scale;
                        bool isMax = false;
                        try
                        {
                            var wa = DisplayArea.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd), DisplayAreaFallback.Primary);
                            isMax = ((int)size.Width) >= wa.WorkArea.Width && ((int)size.Height) >= wa.WorkArea.Height;
                        }
                        catch { }
                        ConfigService.SaveWindowBounds(x, y, w, h, isMax);
                        LogService.Debug("MainWindow", $"ウィンドウ位置/サイズ保存: {x:F0},{y:F0} {w:F0}x{h:F0} (max={isMax})");
                        return;
                    }
                }
                catch (Exception ex) { LogService.Warn("MainWindow", $"ウィンドウ位置取得失敗: {ex.Message}"); }

                ConfigService.SaveWindowSize(w, h);
                LogService.Debug("MainWindow", $"ウィンドウサイズ保存: {w:F0}x{h:F0}");
            }
            catch (Exception ex)
            {
                LogService.Warn("MainWindow", $"ウィンドウサイズ保存失敗: {ex.Message}");
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetWindowRect(System.IntPtr hWnd, out RECT lpRect);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        private double GetDpiScale()
        {
            try
            {
<<<<<<< HEAD
=======
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                var wndId = Win32Interop.GetWindowIdFromWindow(hwnd);
                var display = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
                return (double)this.AppWindow.Size.Width /
                       Math.Max(1, ((FrameworkElement)this.Content).ActualWidth);
            }
            catch
            {
                return 1.25;
            }
        }

        private void ApplyTheme()
        {
            var config = ConfigService.Load();
            var root = Content as FrameworkElement;
            if (root == null) return;
            root.RequestedTheme = config?.Theme switch
            {
<<<<<<< HEAD
                "Dark"  => ElementTheme.Dark,
                "Light" => ElementTheme.Light,
                _       => ElementTheme.Default
            };
            UpdateThemeButton(config?.Theme ?? "Default");
=======
                "Dark" => ElementTheme.Dark,
                "Light" => ElementTheme.Light,
                _ => ElementTheme.Default
            };
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
        }

        public void ReapplyTheme() => ApplyTheme();

<<<<<<< HEAD
        private void UpdateThemeButton(string theme)
        {
            try
            {
                // ボタンラベルとアイコンを現在のテーマに合わせて切替
                // "次に切り替えるテーマ" を表示する
                bool isDark = theme == "Dark"
                    || (theme != "Light" && Application.Current.RequestedTheme == ApplicationTheme.Dark);

                if (ThemeLabel != null)
                    ThemeLabel.Text = isDark ? "ライトモード" : "ダークモード";
                if (ThemeIcon != null)
                    ThemeIcon.Glyph = isDark ? "\uE706" : "\uE708"; // 太陽 / 月
            }
            catch { }
        }

        private void ThemeToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = ConfigService.Load();
            // Dark → Light → Default(System) → Dark... とサイクル
            config.Theme = config.Theme switch
            {
                "Dark"  => "Light",
                "Light" => "Default",
                _       => "Dark"
            };
            ConfigService.Save(config);
            ApplyTheme();
            LogService.Info("MainWindow", $"テーマ変更: {config.Theme}");
        }

=======
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
        private async void CheckAppUpdateForPaneAsync()
        {
            var result = await AppUpdateService.CheckAsync();
            if (result?.HasUpdate == true)
            {
                _appUpdateUrl = result.ReleaseUrl;
                AppUpdatePaneBtn.Visibility = Visibility.Visible;
                ToolTipService.SetToolTip(AppUpdatePaneBtn, $"バージョン {result.LatestTag} が利用可能です");
                LogService.Info("MainWindow", $"アプリ更新あり: {result.LatestTag}");
            }
        }

        private void CheckEpicLoginForNotification()
        {
            var config = ConfigService.Load();
            if (config?.Platform != "Epic") return;

            if (!EpicLoginService.IsLoggedIn(config))
            {
                NotificationService.Push(
                    "Epic アカウントにログインしていません",
                    "Epic版でオンライン機能を使うには、アカウントページで Epic にログインしてください。" +
                    "ログイン後は Epic Games Launcher なしで直接起動できます。",
                    NotificationKind.Warning,
                    tag: "epic_login");
                LogService.Warn("MainWindow", "Epic未ログイン通知を発行");
            }
            else
            {
                LogService.Info("MainWindow", $"Epic ログイン済み: {config.EpicDisplayName}");
            }
        }

        private void AppUpdatePaneBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_appUpdateUrl))
                Process.Start(new ProcessStartInfo(_appUpdateUrl) { UseShellExecute = true });
        }

        private void UpdateNotificationBadge()
        {
            int count = NotificationService.UnreadCount() + NewsReadService.CachedNewsUnreadCount;
            NotifSidebarBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            NotifSidebarCount.Text = count > 9 ? "9+" : count.ToString();
        }

        public void SetNavigationUI(bool isSetupComplete)
        {
            if (isSetupComplete)
            {
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                NavView.IsPaneOpen = true;
                NavView.IsPaneToggleButtonVisible = true;
                HomeItem.Visibility = Visibility.Visible;
                ModInstallItem.Visibility = Visibility.Visible;
                LibraryItem.Visibility = Visibility.Visible;
                NavSeparator.Visibility = Visibility.Visible;
                LogService.Debug("MainWindow", "NavigationUI: セットアップ完了　切り替え");
            }
            else
            {
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
                NavView.IsPaneOpen = false;
                NavView.IsPaneToggleButtonVisible = false;
                HomeItem.Visibility = Visibility.Collapsed;
                ModInstallItem.Visibility = Visibility.Collapsed;
                LibraryItem.Visibility = Visibility.Collapsed;
                NavSeparator.Visibility = Visibility.Collapsed;
                LogService.Debug("MainWindow", "NavigationUI: セットアップ中（ペイン非表示）");
            }
        }

        private void LoadLocalizedStrings()
        {
            string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var assembly = Assembly.GetExecutingAssembly();
            const string rn = "AmongUsModManager.Resources.strings.csv";
            try
            {
                using var stream = assembly.GetManifestResourceStream(rn);
                if (stream == null) return;
                using var reader = new StreamReader(stream, Encoding.UTF8);
                foreach (var line in reader.ReadToEnd().Split(Environment.NewLine).Skip(1))
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 3)
                        LocalizedStrings[parts[0].Trim()] = culture == "ja" ? parts[2].Trim() : parts[1].Trim();
                }
            }
            catch (Exception ex) { LogService.Error("MainWindow", "ローカライズ読み込みエラー", ex); }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            string tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString() ?? "";

            if (ContentFrame.Content is SetupPage)
            {
                LogService.Debug("MainWindow", $"セットアップ/チュートリアル中のため遷移ブロック: {tag}");
                DispatcherQueue.TryEnqueue(() => NavView.SelectedItem = null);
                return;
            }

            if (ContentFrame.Content is SettingsPage sp && tag != "Settings")
            {
                if (sp.HasUnsavedChanges()) { sp.ShowUnsavedWarning(tag); return; }
            }

            NavigateToPendingPage(tag);
            if (tag == "Settings" || tag == "Notification")
                DispatcherQueue.TryEnqueue(UpdateNotificationBadge);
        }

        public void NavigateToPage(string tag) => NavigateToPendingPage(tag);

        public void NavigateToPendingPage(string tag)
        {
            LogService.Debug("MainWindow", $"ページ遷移: {tag}");
            switch (tag)
            {
                case "Home": ContentFrame.Navigate(typeof(HomePage)); break;
                case "ModInstall": ContentFrame.Navigate(typeof(ModInstallPage)); break;
                case "Library": ContentFrame.Navigate(typeof(LibraryPage)); break;
                case "Settings":
                    if (ContentFrame.Content is not SettingsPage)
                        ContentFrame.Navigate(typeof(SettingsPage));
                    break;
                case "About":
                case "Information": ContentFrame.Navigate(typeof(AboutPage)); break;
                case "Contact": ContentFrame.Navigate(typeof(ContactPage)); break;
                case "ChatBot": ContentFrame.Navigate(typeof(ChatBotPage)); break;
                case "Discord":
                    Process.Start(new ProcessStartInfo("https://discord.com/invite/nFhkYmf9At")
                    { UseShellExecute = true });
                    DispatcherQueue.TryEnqueue(() => NavView.SelectedItem = null);
                    return;
                case "DataManagement": ContentFrame.Navigate(typeof(DataManagementPage)); break;
                case "Stats": ContentFrame.Navigate(typeof(StatsPage)); break;
                case "Account": ContentFrame.Navigate(typeof(AccountPage)); break;
                case "Notification": ContentFrame.Navigate(typeof(NotificationPage)); break;
                case "LogViewer": ContentFrame.Navigate(typeof(LogViewerPage)); break;
                case "Screenshot": ContentFrame.Navigate(typeof(ScreenshotPage)); break;
                case "ConflictCheck": ContentFrame.Navigate(typeof(ConflictCheckPage)); break;
                case "Friend": ContentFrame.Navigate(typeof(FriendPage)); break;
            }
        }

        public void NavigateToNewsDetail(NewsItem item)
        {
            LogService.Debug("MainWindow", $"お知らせ詳細へ: {item.Title}");
            ContentFrame.Navigate(typeof(NewsDetailPage), item);
        }

<<<<<<< HEAD
#if DEBUG
        private void InitDebugConsoleShortcut()
        {
            if (this.Content is FrameworkElement fe)
            {
                // WinUI 3 では Window.Current.CoreWindow が null になるため
                // InputKeyboardSource.GetKeyStateForCurrentThread を使う
=======


#if DEBUG
        private void InitDebugConsoleShortcut()
        {
           
            if (this.Content is FrameworkElement fe)
            {
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
                fe.KeyDown += (s, e) =>
                {
                    try
                    {
<<<<<<< HEAD
                        if (e.Key == VirtualKey.L)
                        {
                            var ctrlState  = Microsoft.UI.Input.InputKeyboardSource
                                .GetKeyStateForCurrentThread(VirtualKey.Control);
                            var shiftState = Microsoft.UI.Input.InputKeyboardSource
                                .GetKeyStateForCurrentThread(VirtualKey.Shift);

                            bool ctrl  = (ctrlState  & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
                            bool shift = (shiftState & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;

                            if (ctrl && shift)
                            {
                                ToggleDebugConsole();
                                e.Handled = true;
                            }
=======
                        if (e.Key == VirtualKey.L &&
                            (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0 &&
                            (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0)
                        {
                            ToggleDebugConsole();
                            e.Handled = true;
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
                        }
                    }
                    catch { }
                };
                LogService.Debug("MainWindow", "Debug Console キーハンドラ登録 (Ctrl+Shift+L)");
            }
        }

        private void ToggleDebugConsole()
        {
            if (_debugConsole == null)
            {
                _debugConsole = new Pages.DebugConsoleWindow();
                _debugConsole.Closed += (_, _) => _debugConsole = null;
                _debugConsole.Activate();
                LogService.Debug("MainWindow", "Debug Console を開きました");
            }
            else
            {
<<<<<<< HEAD
=======
                // 既に開いている場合は最前面に持ってくるだけ（閉じるには×ボタン）。
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
                _debugConsole.Activate();
            }
        }
#endif

        private void InitTrayIcon()
        {
            _trayIcon = new TrayIcon();
            _trayIcon.ShowRequested += () => ShowWindow();
            _trayIcon.ExitRequested += () =>
            {
                _forceClose = true;
                DispatcherQueue.TryEnqueue(() => this.Close());
            };
        }

        private void ShowWindow()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                this.AppWindow.Show();
                _trayIcon?.Hide();
            });
        }

        public void UpdateTrayBehavior(bool minimizeToTray)
        {
            _minimizeToTray = minimizeToTray;
            if (!minimizeToTray)
                _trayIcon?.Hide();
        }

        private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            if (_minimizeToTray && !_forceClose)
            {
                args.Cancel = true;
                this.AppWindow.Hide();
                _trayIcon?.Show("AmongUsModManager");
                _trayIcon?.ShowBalloon("AmongUsModManager",
                    "バックグラウンドで動作中です。トレイアイコンから開けます。");
            }
            else
            {
                _trayIcon?.Dispose();
            }
        }
    }
}
