using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using AmongUsModManager.Services;
using AmongUsModManager.Models;
using AmongUsModManager.Models.Services;

namespace AmongUsModManager.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public ObservableCollection<VanillaPathInfo> VanillaPaths { get; } = new ObservableCollection<VanillaPathInfo>();
        public ObservableCollection<string> DetectedPaths { get; } = new ObservableCollection<string>();

        private string _pendingTag = "";

        public SettingsPage()
        {
            this.InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var config = ConfigService.Load();
            if (config != null)
            {
                VanillaPaths.Clear();
                if (config.VanillaPaths != null)
                {
                    foreach (var info in config.VanillaPaths)
                        VanillaPaths.Add(new VanillaPathInfo 
                        { 
                            Name = info.Name, 
                            Path = info.Path,
                            Platform = info.Platform,
                            CurrentVersion = info.CurrentVersion,
                            GitHubOwner = info.GitHubOwner,
                            GitHubRepo = info.GitHubRepo,
                            IsAutoUpdateEnabled = info.IsAutoUpdateEnabled,
                            GitHubLinkDisabled = info.GitHubLinkDisabled
                        });
                }
                ModDataPathTextBox.Text = config.ModDataPath ?? string.Empty;
                StartWithWindowsToggle.IsOn = config.StartWithWindows;
                StartMinimizedToggle.IsOn = config.StartMinimized;
                MinimizeToTrayToggle.IsOn = config.MinimizeToTray;

                // ログモード（RadioButton）
                // LogAppendMode=false → 上書き or 新ファイル は LogNewFile フラグで判断
                // 後方互換: LogAppendMode=true の場合は上書き扱いにする
                if (config.LogAppendMode)
                    LogOverwriteRadio.IsChecked = true;
                else
                    LogNewFileRadio.IsChecked = true;

                // テーマ（RadioButton）
                switch (config.Theme)
                {
                    case "Dark":  ThemeDarkRadio.IsChecked  = true; break;
                    case "Light": ThemeLightRadio.IsChecked = true; break;
                    default:      ThemeDefaultRadio.IsChecked = true; break;
                }

                // 通知設定
                NotifyModUpdateToggle.IsOn  = config.NotifyModUpdate;
                NotifyAppUpdateToggle.IsOn  = config.NotifyAppUpdate;
                NotifyNewsToggle.IsOn       = config.NotifyNews;


                string platformLabel = config.Platform switch
                {
                    "Epic"    => "Epic Games",
                    "Steam"   => "Steam",
                    "MSStore" => "Microsoft Store",
                    "Itch"    => "itch.io",
                    "Manual"  => "手動指定",
                    _         => "未設定"
                };
                CurrentPlatformText.Text = $"現在のプラットフォーム: {platformLabel}";

                bool isEpic = config.Platform == "Epic";
                EpicSettingsSection.Visibility   = isEpic ? Visibility.Visible : Visibility.Collapsed;
                EpicLaunchViaLauncherToggle.IsOn = config.EpicLaunchViaLauncher;

                if (isEpic) RefreshEpicStatus();

                LoadMainPlatformUI(config);
            }
        }

        private void VanillaPathListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int count = VanillaPathListView.SelectedItems.Count;
            SelectedFolderCountText.Text = count.ToString();
            if (count > 0)
                StatusMessage.Text = $"📋 {count}個のフォルダを選択中...";
            else
                StatusMessage.Text = "";
        }

        private void BulkPlatformSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cmb) return;
            string selectedTag = (cmb.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

            if (string.IsNullOrEmpty(selectedTag)) return;

            int selectedCount = VanillaPathListView.SelectedItems.Count;
            if (selectedCount == 0)
            {
                StatusMessage.Text = "❌ フォルダが選択されていません";
                StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 192, 0, 0));
                cmb.SelectedIndex = 0;
                return;
            }

            foreach (var item in VanillaPathListView.SelectedItems.OfType<VanillaPathInfo>())
            {
                item.Platform = selectedTag;
            }

            // 設定を即座に保存
            ExecuteSave();

            // プラットフォーム名を表示
            string platformLabel = selectedTag switch
            {
                "Steam" => "Steam",
                "Epic" => "Epic Games",
                "MSStore" => "Microsoft Store",
                "Itch" => "itch.io",
                "Manual" => "手動指定",
                _ => selectedTag
            };

            StatusMessage.Text = $"✅ {selectedCount}個のフォルダを「{platformLabel}」に設定しました";
            StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 128, 0));

            cmb.SelectedIndex = 0;
            VanillaPathListView.SelectedItems.Clear();
            SelectedFolderCountText.Text = "0";
        }

        // ComboBox での個別プラットフォーム変更時に即座に保存
        private void PlatformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cmb) return;

            // DataContextから対応する VanillaPathInfo を取得
            if (cmb.DataContext is VanillaPathInfo mod && cmb.SelectedItem is string platform)
            {
                mod.Platform = platform;

                // 即座に設定を保存
                ExecuteSave();

                // ステータスを表示
                string platformLabel = platform switch
                {
                    "Steam" => "Steam",
                    "Epic" => "Epic Games",
                    "MSStore" => "Microsoft Store",
                    "Itch" => "itch.io",
                    "Manual" => "手動指定",
                    _ => "なし"
                };

                StatusMessage.Text = $"✅ 「{mod.Name}」のプラットフォームを「{platformLabel}」に設定しました";
                StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 128, 0));
            }
        }

        private static readonly (string Tag, string Label)[] PlatformOptions =
        {
            ("Steam",   "Steam"),
            ("Epic",    "Epic Games"),
            ("MSStore", "Microsoft Store"),
            ("Itch",    "itch.io"),
        };

        private void LoadMainPlatformUI(Models.AppConfig config)
        {
            string current = !string.IsNullOrEmpty(config.Platform) ? config.Platform : "";

            MainPlatformLabel.Text = current switch
            {
                "Steam"   => "Steam",
                "Epic"    => "Epic Games",
                "MSStore" => "Microsoft Store",
                "Itch"    => "itch.io",
                _         => "未設定"
            };
            MainPlatformIcon.Glyph = current == "Epic" ? "\uE83B" : "\uE7FC";

            PlatformSwitchPanel.Children.Clear();
            PlatformSwitchHint.Visibility = Visibility.Collapsed;

            foreach (var (tag, label) in PlatformOptions)
            {
                bool isCurrent = tag == current;
                var btn = new Button
                {
                    Content = isCurrent ? $"✅ {label}（現在）" : $"{label} に変更",
                    IsEnabled = !isCurrent,
                    Tag = tag,
                    Padding = new Microsoft.UI.Xaml.Thickness(14, 8, 14, 8),
                    HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left
                };
                if (isCurrent)
                    btn.Style = (Microsoft.UI.Xaml.Style)Application.Current.Resources["AccentButtonStyle"];
                btn.Click += SwitchMainPlatform_Click;
                PlatformSwitchPanel.Children.Add(btn);
            }
        }

        private void SwitchMainPlatform_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tag) return;

            var config = ConfigService.Load();
            config.Platform     = tag;
            config.MainPlatform = tag;
            config.EpicLaunchViaLauncher = tag == "Epic";

            ConfigService.Save(config);
            LogService.Info("SettingsPage", $"メインプラットフォーム切り替え: {tag}");

            // Epic設定セクションの表示を更新
            EpicSettingsSection.Visibility = tag == "Epic" ? Visibility.Visible : Visibility.Collapsed;
            if (tag == "Epic") RefreshEpicStatus();

            LoadMainPlatformUI(config);
        }

        private void RefreshEpicStatus()
        {
            var config = ConfigService.Load();
            bool loggedIn = EpicLoginService.IsLoggedIn(config);
            LogService.Debug("SettingsPage", $"Epicログイン状態: {(loggedIn ? "ログイン済み" : "未ログイン")}");

            if (loggedIn)
            {
                EpicStatusText.Text      = $"✅ ログイン済み — {config.EpicDisplayName}（Epic Games Launcher 不要）";
                EpicStatusIcon.Glyph     = "\uE73E";
                EpicStatusIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.SeaGreen);
            }
            else
            {
                EpicStatusText.Text      = "❌ 未ログイン — アカウントページでログインしてください";
                EpicStatusIcon.Glyph     = "\uE711";
                EpicStatusIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Tomato);
            }
        }

        private void EpicOpenLauncher_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("SettingsPage", "Epic Launcher 起動");
            EpicLoginService.LaunchEpicLauncher();
        }

        private async void EpicRecheck_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("SettingsPage", "Epic ログイン状態再確認");
            await System.Threading.Tasks.Task.Delay(500);
            RefreshEpicStatus();
        }

        private void EpicLaunchViaLauncherToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var config = ConfigService.Load();
            config.EpicLaunchViaLauncher = EpicLaunchViaLauncherToggle.IsOn;
            ConfigService.Save(config);
            LogService.Info("SettingsPage", $"EpicLaunchViaLauncher: {config.EpicLaunchViaLauncher}");
        }

        public bool HasUnsavedChanges()
        {
            var config = ConfigService.Load();
            if (config == null) return false;

            if (VanillaPaths.Count != (config.VanillaPaths?.Count ?? 0)) return true;

            for (int i = 0; i < VanillaPaths.Count; i++)
            {
                if (config.VanillaPaths != null && (VanillaPaths[i].Name != config.VanillaPaths[i].Name ||
                    VanillaPaths[i].Path != config.VanillaPaths[i].Path))
                    return true;
            }

            if (ModDataPathTextBox.Text != (config.ModDataPath ?? string.Empty)) return true;

            return false;
        }

        public void ShowUnsavedWarning(string tag)
        {
            _pendingTag = tag;
            UnsavedChangesBar.IsOpen = true;
        }

        private void SaveAndExit_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSave();
            UnsavedChangesBar.IsOpen = false;
            if (App.MainWindowInstance is MainWindow mw)
                mw.NavigateToPendingPage(_pendingTag);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSave();
            StatusMessage.Text = "✅ 設定を保存しました！";
            StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 128, 0));
        }

        private void ExecuteSave()
        {
            var config = ConfigService.Load() ?? new AppConfig();
            config.VanillaPaths = VanillaPaths.ToList();
            config.ModDataPath = ModDataPathTextBox.Text;
            if (VanillaPaths.Count > 0) config.GameInstallPath = VanillaPaths[0].Path;
            config.StartWithWindows = StartWithWindowsToggle.IsOn;
            config.StartMinimized   = StartMinimizedToggle.IsOn;
            config.MinimizeToTray   = MinimizeToTrayToggle.IsOn;

            // ログモード: 上書き=false(追記OFF)、新ファイル=false+LogNewFile
            // LogAppendMode は廃止方向だが後方互換で上書き時true/新ファイル時false
            config.LogAppendMode = LogOverwriteRadio.IsChecked == true;

            // テーマ
            config.Theme = ThemeDarkRadio.IsChecked  == true ? "Dark"
                         : ThemeLightRadio.IsChecked == true ? "Light"
                         : "Default";

            // 通知設定
            config.NotifyModUpdate = NotifyModUpdateToggle.IsOn;
            config.NotifyAppUpdate = NotifyAppUpdateToggle.IsOn;
            config.NotifyNews      = NotifyNewsToggle.IsOn;

            ConfigService.Save(config);
            ApplyStartupSetting(config.StartWithWindows);

            // テーマをリアルタイム反映
            if (App.MainWindowInstance is MainWindow mw)
                mw.ReapplyTheme();

            UnsavedChangesBar.IsOpen = false;
        }

        private void ApplyStartupSetting(bool enable)
        {
            try
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(exePath)) return;

                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
                if (key == null) return;

                const string appName = "AmongUsModManager";
                if (enable)
                    key.SetValue(appName, $"\"{exePath}\"");
                else
                    key.DeleteValue(appName, throwOnMissingValue: false);
            }
            catch { }
        }

        private void StartWithWindowsToggle_Toggled(object sender, RoutedEventArgs e) { }

        private async Task<StorageFolder?> SelectFolder()
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            IntPtr hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(folderPicker, hwnd);
            try { return await folderPicker.PickSingleFolderAsync(); } catch { return null; }
        }

        private async void SelectScanTarget_Click(object sender, RoutedEventArgs e)
        {
            var folder = await SelectFolder();
            if (folder != null) ScanTargetTextBox.Text = folder.Path;
        }

        private void ClearScanTarget_Click(object sender, RoutedEventArgs e) => ScanTargetTextBox.Text = string.Empty;

        private async void ScanDrives_Click(object sender, RoutedEventArgs e)
        {
            DetectedPaths.Clear();
            AddAllButton.IsEnabled = false;
            StatusMessage.Text = "スキャン中...";
            string targetPath = ScanTargetTextBox.Text;

            var foundPaths = await Task.Run(() =>
            {
                var results = new List<string>();
                if (!string.IsNullOrEmpty(targetPath) && Directory.Exists(targetPath))
                    SearchAmongUs(targetPath, results);
                else
                {
                    var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
                    foreach (var drive in drives) SearchAmongUs(drive.RootDirectory.FullName, results);
                }
                return results;
            });

            foreach (var path in foundPaths)
            {
                if (!VanillaPaths.Any(v => v.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    DetectedPaths.Add(path);
            }
            StatusMessage.Text = $"{DetectedPaths.Count} 件見つかりました。";
            AddAllButton.IsEnabled = DetectedPaths.Count > 0;
        }

        private void SearchAmongUs(string root, List<string> results)
        {
            try
            {
                if (File.Exists(Path.Combine(root, "Among Us.exe"))) { results.Add(root); return; }
                string[] skip = { "$Recycle.Bin", "System Volume Information", "Windows", "ProgramData" };
                foreach (var dir in Directory.GetDirectories(root))
                {
                    if (skip.Any(s => dir.Contains(s))) continue;
                    SearchAmongUs(dir, results);
                }
            }
            catch { }
        }

        private void AddDetectedSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string path)
            {
                VanillaPaths.Add(new VanillaPathInfo { Name = Path.GetFileName(path), Path = path });
                DetectedPaths.Remove(path);
                AddAllButton.IsEnabled = DetectedPaths.Count > 0;
            }
        }

        private void AddAllDetected_Click(object sender, RoutedEventArgs e)
        {
            foreach (var path in DetectedPaths.ToList())
                VanillaPaths.Add(new VanillaPathInfo { Name = Path.GetFileName(path), Path = path });
            DetectedPaths.Clear();
            AddAllButton.IsEnabled = false;
        }

        private void RemovePath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is VanillaPathInfo info)
                VanillaPaths.Remove(info);
        }

        private void LogMode_Checked(object sender, RoutedEventArgs e) { }
        private void Theme_Checked(object sender, RoutedEventArgs e) { }
        private void NotifyToggle_Toggled(object sender, RoutedEventArgs e) { }

        private void MinimizeToTrayToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var config = ConfigService.Load();
            config.MinimizeToTray = MinimizeToTrayToggle.IsOn;
            ConfigService.Save(config);
            // MainWindowにトレイ設定を通知
            if (App.MainWindowInstance is MainWindow mw)
                mw.UpdateTrayBehavior(MinimizeToTrayToggle.IsOn);
        }

        private async void ChangeModPath_Click(object sender, RoutedEventArgs e)
        {
            var folder = await SelectFolder();
            if (folder != null) ModDataPathTextBox.Text = folder.Path;
        }

       
    }
}
