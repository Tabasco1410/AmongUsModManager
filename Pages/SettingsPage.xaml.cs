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
using AmongUsModManager.Models.Services;
using AmongUsModManager.Models;

namespace AmongUsModManager.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public ObservableCollection<VanillaPathInfo> VanillaPaths { get; } = new ObservableCollection<VanillaPathInfo>();
        public ObservableCollection<string> DetectedPaths { get; } = new ObservableCollection<string>();

        private string _pendingTag = "";
        private bool _languageComboBoxReady = false;
        private bool _hasChanges = false;
        private bool _bulkSelectorReady = false;
        private int _pendingLanguageId = -1;
        private int _savedLanguageId = -1;

        public SettingsPage()
        {
            try
            {
                LogService.Info("SettingsPage", "コンストラクタ開始");
                this.InitializeComponent();
                LogService.Info("SettingsPage", "InitializeComponent完了");
                PopulateLanguageComboBox();
                LogService.Info("SettingsPage", "PopulateLanguageComboBox完了");
                LoadCurrentSettings();
                LogService.Info("SettingsPage", "LoadCurrentSettings完了");
                ApplyStrings();
                LogService.Info("SettingsPage", "ApplyStrings完了");
                LocalizationService.LanguageChanged += ApplyStrings;
                this.Unloaded += (_, _) => LocalizationService.LanguageChanged -= ApplyStrings;
                LogService.Info("SettingsPage", "コンストラクタ完了");
            }
            catch (Exception ex)
            {
                LogService.Error("SettingsPage", $"コンストラクタで例外が発生しました (HResult=0x{ex.HResult:X8})", ex);
                throw;
            }
        }

        private void ApplyStrings()
        {
            SettingsPageTitle.Text     = LocalizationService.Get("Settings_Title");
            UnsavedChangesBar.Title   = LocalizationService.Get("Settings_UnsavedTitle");
            UnsavedChangesBar.Message = LocalizationService.Get("Settings_UnsavedMessage");
            SaveAndExitButton.Content = LocalizationService.Get("Settings_SaveAndMove");

            TabAppearanceLabel.Text = LocalizationService.Get("Settings_TabAppearance");
            TabGameLabel.Text       = LocalizationService.Get("Settings_TabGame");
            TabSystemLabel.Text     = LocalizationService.Get("Settings_TabSystem");
            TabNotifLabel.Text      = LocalizationService.Get("Settings_TabNotif");
            LangPendingBar.Message  = LocalizationService.Get("Settings_LangPendingNotice");

            GameFolderSectionTitle.Text    = LocalizationService.Get("Settings_GameFolder");
            GameFolderSectionDesc.Text     = LocalizationService.Get("Settings_GameFolderDesc");
            SelectedFolderCountLabel.Text  = LocalizationService.Get("Settings_SelectedCount");
            BulkChangeLabel.Text           = LocalizationService.Get("Settings_BulkChange");
            PlatformSelectPlaceholder.Content = LocalizationService.Get("Settings_SelectPlatform");
            ManualPlatformItem.Content     = LocalizationService.Get("Settings_Manual");
            AutoDetectExpander.Header      = LocalizationService.Get("Settings_AutoDetect");
            AutoDetectDesc.Text            = LocalizationService.Get("Settings_AutoDetectDesc");
            ScanTargetTextBox.PlaceholderText = LocalizationService.Get("Settings_SelectScanFolder");
            SelectScanTargetBtn.Content    = LocalizationService.Get("Settings_SelectLocation");
            StartScanBtn.Content           = LocalizationService.Get("Settings_StartScan");
            ClearPathBtn.Content           = LocalizationService.Get("Settings_ClearPath");
            AddAllButton.Content           = LocalizationService.Get("Settings_AddAll");

            MainPlatformSectionTitle.Text  = LocalizationService.Get("Settings_MainPlatform");
            MainPlatformSectionDesc.Text   = LocalizationService.Get("Settings_MainPlatformDesc");
            CurrentMainPlatformLabel.Text  = LocalizationService.Get("Settings_CurrentMainPlatform");
            PlatformSwitchHint.Text        = LocalizationService.Get("Settings_MultipleFoldersHint");

            ModFolderSectionTitle.Text     = LocalizationService.Get("Settings_ModFolder");
            ModFolderSectionDesc.Text      = LocalizationService.Get("Settings_ModFolderDesc");
            ChangeModPathBtn.Content       = LocalizationService.Get("Settings_Change");

            LaunchSectionTitle.Text        = LocalizationService.Get("Settings_LaunchResidency");
            StartWithWindowsToggle.Header  = LocalizationService.Get("Settings_StartWithWindows");
            StartMinimizedToggle.Header    = LocalizationService.Get("Settings_StartMinimized");
            MinimizeToTrayToggle.Header    = LocalizationService.Get("Settings_MinimizeToTray");
            TrayHintText.Text              = LocalizationService.Get("Settings_TrayHint");
            AlwaysRunAsAdminToggle.Header  = LocalizationService.Get("Settings_AlwaysAdmin");
            AlwaysAdminHintText.Text       = LocalizationService.Get("Settings_AlwaysAdminHint");

            EpicSectionTitle.Text          = LocalizationService.Get("Settings_EpicSettings");
            EpicOpenLauncherBtn.Content    = LocalizationService.Get("Settings_OpenEpicLauncher");
            EpicRecheckBtn.Content         = LocalizationService.Get("Settings_RecheckStatus");
            EpicLaunchViaLauncherToggle.Header = LocalizationService.Get("Settings_EpicLaunchViaLauncher");
            EpicLauncherDesc.Text          = LocalizationService.Get("Settings_EpicLauncherDesc");

            LogSectionTitle.Text           = LocalizationService.Get("Settings_Log");
            LogSectionDesc.Text            = LocalizationService.Get("Settings_LogDesc");
            LogModeLabel.Text              = LocalizationService.Get("Settings_LogMode");
            LogOverwriteRadio.Content      = LocalizationService.Get("Settings_LogOverwrite");
            LogNewFileRadio.Content        = LocalizationService.Get("Settings_LogNewFile");

            NotifSectionTitle.Text         = LocalizationService.Get("Settings_Notification");
            NotifSectionDesc.Text          = LocalizationService.Get("Settings_NotificationDesc");
            NotifyModUpdateToggle.Header   = LocalizationService.Get("Settings_NotifyModUpdate");
            NotifyAppUpdateToggle.Header   = LocalizationService.Get("Settings_NotifyAppUpdate");
            NotifyNewsToggle.Header        = LocalizationService.Get("Settings_NotifyNews");

            AppearanceSectionTitle.Text    = LocalizationService.Get("Settings_Appearance");
            ThemeLabel.Text                = LocalizationService.Get("Settings_Theme");
            ThemeDefaultRadio.Content      = LocalizationService.Get("Settings_ThemeDefault");
            ThemeDarkRadio.Content         = LocalizationService.Get("Settings_ThemeDark");
            ThemeLightRadio.Content        = LocalizationService.Get("Settings_ThemeLight");
            LanguageSectionLabel.Text      = LocalizationService.Get("Settings_Language");

            SaveButton.Content = LocalizationService.Get("Settings_Save");
        }

        private void SetHasChanges(bool value)
        {
            _hasChanges = value;
            if (SaveBar != null)
                SaveBar.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TabNav_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppearanceContent == null) return;

            int idx = TabNavList.SelectedIndex;
            AppearanceContent.Visibility = idx == 0 ? Visibility.Visible : Visibility.Collapsed;
            GameContent.Visibility       = idx == 1 ? Visibility.Visible : Visibility.Collapsed;
            SystemContent.Visibility     = idx == 2 ? Visibility.Visible : Visibility.Collapsed;
            NotifContent.Visibility      = idx == 3 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PopulateLanguageComboBox()
        {
            _languageComboBoxReady = false;
            LanguageComboBox.Items.Clear();
            foreach (var id in LocalizationService.AvailableLanguageIds)
            {
                LanguageComboBox.Items.Add(new ComboBoxItem
                {
                    Content = LocalizationService.GetLanguageName(id),
                    Tag = id
                });
            }
            _languageComboBoxReady = true;
        }

        private void SelectCurrentLanguageInComboBox(int langId)
        {
            _languageComboBoxReady = false;
            foreach (var item in LanguageComboBox.Items)
            {
                if (item is ComboBoxItem cbi && cbi.Tag is int id && id == langId)
                {
                    LanguageComboBox.SelectedItem = cbi;
                    break;
                }
            }
            _languageComboBoxReady = true;
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_languageComboBoxReady) return;
            if (LanguageComboBox.SelectedItem is ComboBoxItem cbi && cbi.Tag is int langId)
            {
                _pendingLanguageId = langId;
                LangPendingBar.IsOpen = langId != _savedLanguageId;
                SetHasChanges(true);
            }
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
                StartMinimizedToggle.IsEnabled = config.StartWithWindows;
                MinimizeToTrayToggle.IsOn = config.MinimizeToTray;
                TrayHintText.Visibility = config.MinimizeToTray ? Visibility.Visible : Visibility.Collapsed;

                if (config.LogAppendMode)
                    LogOverwriteRadio.IsChecked = true;
                else
                    LogNewFileRadio.IsChecked = true;

                switch (config.Theme)
                {
                    case "Dark":  ThemeDarkRadio.IsChecked  = true; break;
                    case "Light": ThemeLightRadio.IsChecked = true; break;
                    default:      ThemeDefaultRadio.IsChecked = true; break;
                }

                NotifyModUpdateToggle.IsOn  = config.NotifyModUpdate;
                NotifyAppUpdateToggle.IsOn  = config.NotifyAppUpdate;
                NotifyNewsToggle.IsOn       = config.NotifyNews;

                SelectCurrentLanguageInComboBox(config.Language);
                _pendingLanguageId = config.Language;
                _savedLanguageId   = config.Language;

                string platformLabel = config.Platform switch
                {
                    "Epic"    => "Epic Games",
                    "Steam"   => "Steam",
                    "MSStore" => "Microsoft Store",
                    "Itch"    => "itch.io",
                    "Manual"  => LocalizationService.Get("Settings_Manual"),
                    _         => LocalizationService.Get("Settings_NotSet")
                };
                CurrentPlatformText.Text = $"{LocalizationService.Get("Settings_CurrentPlatform")} {platformLabel}";

                bool isEpic = config.Platform == "Epic";
                EpicSettingsSection.Visibility   = isEpic ? Visibility.Visible : Visibility.Collapsed;
                EpicLaunchViaLauncherToggle.IsOn = config.EpicLaunchViaLauncher;

                if (isEpic) RefreshEpicStatus();

                AlwaysRunAsAdminToggle.IsOn = config.AlwaysRunAsAdmin;

                _bulkSelectorReady = true;
                LoadMainPlatformUI(config);
            }
        }

        private void VanillaPathListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int count = VanillaPathListView.SelectedItems.Count;
            SelectedFolderCountText.Text = count.ToString();
            if (count > 0)
                StatusMessage.Text = $"📋 {count}{LocalizationService.Get("Settings_SelectedCount")}...";
            else
                StatusMessage.Text = "";
        }

        private void BulkPlatformSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_bulkSelectorReady) return;
            if (sender is not ComboBox cmb) return;
            string selectedTag = (cmb.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

            if (string.IsNullOrEmpty(selectedTag)) return;

            int selectedCount = VanillaPathListView.SelectedItems.Count;
            if (selectedCount == 0)
            {
                StatusMessage.Text = "❌ " + LocalizationService.Get("Settings_SelectPlatform");
                StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 192, 0, 0));
                cmb.SelectedIndex = 0;
                return;
            }

            foreach (var item in VanillaPathListView.SelectedItems.OfType<VanillaPathInfo>())
            {
                item.Platform = selectedTag;
            }

            ExecuteSave();

            string platformLabel = selectedTag switch
            {
                "Steam" => "Steam",
                "Epic" => "Epic Games",
                "MSStore" => "Microsoft Store",
                "Itch" => "itch.io",
                "Manual" => LocalizationService.Get("Settings_Manual"),
                _ => selectedTag
            };

            StatusMessage.Text = $"✅ {selectedCount}{LocalizationService.Get("Settings_SelectedCount")}「{platformLabel}」";
            StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 128, 0));

            cmb.SelectedIndex = 0;
            VanillaPathListView.SelectedItems.Clear();
            SelectedFolderCountText.Text = "0";
        }

        private void PlatformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cmb) return;

            string platform = (cmb.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            if (cmb.DataContext is VanillaPathInfo mod)
            {
                mod.Platform = platform;

                ExecuteSave();

                string platformLabel = platform switch
                {
                    "Steam" => "Steam",
                    "Epic" => "Epic Games",
                    "MSStore" => "Microsoft Store",
                    "Itch" => "itch.io",
                    "Manual" => LocalizationService.Get("Settings_Manual"),
                    _ => LocalizationService.Get("Settings_NotSet")
                };

                StatusMessage.Text = $"✅ 「{mod.Name}」→「{platformLabel}」";
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
                _         => LocalizationService.Get("Settings_NotSet")
            };
            MainPlatformIcon.Glyph = current == "Epic" ? "" : "";

            PlatformSwitchPanel.Children.Clear();
            PlatformSwitchHint.Visibility = Visibility.Collapsed;

            foreach (var (tag, label) in PlatformOptions)
            {
                bool isCurrent = tag == current;
                var btn = new Button
                {
                    Content = isCurrent
                        ? string.Format(LocalizationService.Get("Settings_CurrentPlatformBtn"), label)
                        : string.Format(LocalizationService.Get("Settings_SwitchPlatformBtn"), label),
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
                EpicStatusText.Text      = "✅ " + string.Format(LocalizationService.Get("Account_EpicLoggedIn"), config.EpicDisplayName) + " " + LocalizationService.Get("Settings_EpicNoLauncherNeeded");
                EpicStatusIcon.Glyph     = "";
                EpicStatusIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.SeaGreen);
            }
            else
            {
                EpicStatusText.Text      = "❌ " + LocalizationService.Get("Account_EpicNotLoggedIn");
                EpicStatusIcon.Glyph     = "";
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

        private void AlwaysRunAsAdminToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var config = ConfigService.Load();
            config.AlwaysRunAsAdmin = AlwaysRunAsAdminToggle.IsOn;
            ConfigService.Save(config);
            LogService.Info("SettingsPage", $"AlwaysRunAsAdmin: {config.AlwaysRunAsAdmin}");
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

            return _hasChanges;
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
            StatusMessage.Text = LocalizationService.Get("Settings_Saved");
            StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 128, 0));
            SetHasChanges(false);
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

            config.LogAppendMode = LogOverwriteRadio.IsChecked == true;

            config.Theme = ThemeDarkRadio.IsChecked  == true ? "Dark"
                         : ThemeLightRadio.IsChecked == true ? "Light"
                         : "Default";

            config.NotifyModUpdate = NotifyModUpdateToggle.IsOn;
            config.NotifyAppUpdate = NotifyAppUpdateToggle.IsOn;
            config.NotifyNews      = NotifyNewsToggle.IsOn;

            if (_pendingLanguageId >= 0)
            {
                config.Language  = _pendingLanguageId;
                _savedLanguageId = _pendingLanguageId;
                LocalizationService.SetLanguage(_pendingLanguageId);
                LangPendingBar.IsOpen = false;
            }

            ConfigService.Save(config);
            ApplyStartupSetting(config.StartWithWindows);

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

        private void StartWithWindowsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            StartMinimizedToggle.IsEnabled = StartWithWindowsToggle.IsOn;
            SetHasChanges(true);
        }

        private void StartMinimizedToggle_Toggled(object sender, RoutedEventArgs e)
        {
            SetHasChanges(true);
        }

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
            StatusMessage.Text = LocalizationService.Get("Common_Loading");
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
            StatusMessage.Text = $"{DetectedPaths.Count} " + LocalizationService.Get("Common_Success");
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
                SetHasChanges(true);
            }
        }

        private void AddAllDetected_Click(object sender, RoutedEventArgs e)
        {
            foreach (var path in DetectedPaths.ToList())
                VanillaPaths.Add(new VanillaPathInfo { Name = Path.GetFileName(path), Path = path });
            DetectedPaths.Clear();
            AddAllButton.IsEnabled = false;
            SetHasChanges(true);
        }

        private void RemovePath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is VanillaPathInfo info)
            {
                VanillaPaths.Remove(info);
                SetHasChanges(true);
            }
        }

        private void LogMode_Checked(object sender, RoutedEventArgs e)
        {
            SetHasChanges(true);
        }

        private void Theme_Checked(object sender, RoutedEventArgs e)
        {
            SetHasChanges(true);
        }

        private void NotifyToggle_Toggled(object sender, RoutedEventArgs e)
        {
            SetHasChanges(true);
        }

        private void MinimizeToTrayToggle_Toggled(object sender, RoutedEventArgs e)
        {
            TrayHintText.Visibility = MinimizeToTrayToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
            var config = ConfigService.Load();
            config.MinimizeToTray = MinimizeToTrayToggle.IsOn;
            ConfigService.Save(config);
            if (App.MainWindowInstance is MainWindow mw)
                mw.UpdateTrayBehavior(MinimizeToTrayToggle.IsOn);
        }

        private async void ChangeModPath_Click(object sender, RoutedEventArgs e)
        {
            var folder = await SelectFolder();
            if (folder != null)
            {
                ModDataPathTextBox.Text = folder.Path;
                SetHasChanges(true);
            }
        }
    }
}
