using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using AmongUsModManager.Models;
using AmongUsModManager.Services;
using AmongUsModManager.Models.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AmongUsModManager.Pages
{
    public sealed partial class SetupPage : Page
    {
        private string _selectedPlatformTag = "";

        private const string DiscordSupportUrl = "https://discord.gg/nFhkYmf9At";

        private static readonly Dictionary<string, string> PlatformHints = new()
        {
            ["Steam"]   = "Steam の「ライブラリ」→「Among Us」を右クリック →「管理」→「ローカルファイルを閲覧」でフォルダを確認できます。",
            ["Epic"]    = "Epic Games Launcher の「ライブラリ」→「Among Us」の「…」→「管理」→「インストール先」でフォルダを確認できます。",
            ["MSStore"] = "Microsoft Store 版は C:\\Program Files\\WindowsApps フォルダにインストールされますが、アクセス制限があるため自動検出を使用してください。",
            ["Itch"]    = "itch.io アプリの「Among Us」→「…」→「フォルダを開く」でフォルダを確認できます。",
            ["Manual"]  = "「参照...」ボタンから「Among Us.exe」が入っているフォルダを直接選んでください。",
        };

        public SetupPage()
        {
            this.InitializeComponent();
            LogService.Info("SetupPage", "セットアップページ初期化");
        }

        private void PlatformCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlatformCombo.SelectedItem is not ComboBoxItem item) return;
            _selectedPlatformTag = item.Tag?.ToString() ?? "";

            LogService.Info("SetupPage", $"プラットフォーム選択: {_selectedPlatformTag}");

            if (PlatformHints.TryGetValue(_selectedPlatformTag, out var hint))
            {
                PlatformHint.Text       = hint;
                PlatformHint.Visibility = Visibility.Visible;
            }
            else
            {
                PlatformHint.Visibility = Visibility.Collapsed;
            }

            AutoDetectButton.IsEnabled = _selectedPlatformTag != "Manual";

            PathTextBox.Text = "";
            SetValidation(false, "");
            NotFoundPanel.Visibility  = Visibility.Collapsed;
            EpicLoginPanel.Visibility = Visibility.Collapsed;
            FinishButton.IsEnabled    = false;

            if (_selectedPlatformTag == "Epic")
                CheckEpicLoginStatus();
        }

        private void CheckEpicLoginStatus()
        {
            var config = ConfigService.Load();
            bool loggedIn = EpicLoginService.IsLoggedIn(config);
            LogService.Info("SetupPage", $"Epicログイン状態: {(loggedIn ? "ログイン済み" : "未ログイン")}");

            EpicLoginPanel.Visibility = Visibility.Visible;

            if (loggedIn)
            {
                EpicStatusText.Text      = $"✅ ログイン済み — {config.EpicDisplayName}（Epic Games Launcher 不要で起動できます）";
                EpicStatusIcon.Glyph     = "\uE73E";
                EpicStatusIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.SeaGreen);
                EpicLaunchLauncherBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                EpicStatusText.Text      = "❌ 未ログイン — セットアップ後にアカウントページでログインしてください";
                EpicStatusIcon.Glyph     = "\uE711";
                EpicStatusIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Tomato);
                EpicLaunchLauncherBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void EpicLaunchLauncher_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("SetupPage", "Epic Launcher 起動ボタン");
            EpicLoginService.LaunchEpicLauncher();
            DispatcherQueue.TryEnqueue(async () =>
            {
                await System.Threading.Tasks.Task.Delay(3000);
                if (_selectedPlatformTag == "Epic") CheckEpicLoginStatus();
            });
        }

        private void AutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("SetupPage", $"自動検出開始: {_selectedPlatformTag}");
            NotFoundPanel.Visibility = Visibility.Collapsed;

            string? path = _selectedPlatformTag switch
            {
                "Steam"   => AUFileDetector.GetSteamPath(),
                "Epic"    => AUFileDetector.GetEpicPath(),
                "MSStore" => AUFileDetector.GetMicrosoftStorePath(),
                "Itch"    => AUFileDetector.GetItchPath(),
                _         => null,
            };

            if (path != null)
            {
                LogService.Info("SetupPage", $"自動検出成功: {path}");
                PathTextBox.Text = path;
                ValidatePath(path);
            }
            else
            {
                LogService.Warn("SetupPage", "自動検出失敗: Among Usが見つかりませんでした");
                SetValidation(false, "Among Us が見つかりませんでした。「参照...」で手動指定するか、下の案内からインストールしてください。");
                NotFoundPanel.Visibility = Visibility.Visible;
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Debug("SetupPage", "フォルダ参照ダイアログを開く");
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            IntPtr hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                LogService.Info("SetupPage", $"フォルダ選択: {folder.Path}");
                PathTextBox.Text = folder.Path;
                ValidatePath(folder.Path);
            }
        }

        private void ValidatePath(string path)
        {
            bool valid = AUFileDetector.IsValidPath(path);
            LogService.Debug("SetupPage", $"パス検証: {path} → {(valid ? "OK" : "NG")}");

            if (valid)
            {
                SetValidation(true, $"✔  Among Us.exe を確認しました：{path}");
                NotFoundPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                SetValidation(false, "このフォルダに Among Us.exe が見つかりません。正しいインストールフォルダを選択してください。");
                NotFoundPanel.Visibility = Visibility.Visible;
            }

            FinishButton.IsEnabled = valid;
        }

        private void SetValidation(bool success, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                ValidateInfoBar.IsOpen = false;
                return;
            }
            ValidateInfoBar.Severity = success ? InfoBarSeverity.Success : InfoBarSeverity.Error;
            ValidateInfoBar.Message  = message;
            ValidateInfoBar.IsOpen   = true;
        }

        private void OpenSteamStore_Click(object sender, RoutedEventArgs e)
            => Process.Start(new ProcessStartInfo(
                "https://store.steampowered.com/app/945360/Among_Us/") { UseShellExecute = true });

        private void OpenEpicStore_Click(object sender, RoutedEventArgs e)
            => Process.Start(new ProcessStartInfo(
                "https://store.epicgames.com/ja/p/among-us") { UseShellExecute = true });

        private void OpenItchStore_Click(object sender, RoutedEventArgs e)
            => Process.Start(new ProcessStartInfo(
                "https://innersloth.itch.io/among-us") { UseShellExecute = true });

        private void OpenDiscordSupport_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("SetupPage", "Discord サポートリンクを開く");
            Process.Start(new ProcessStartInfo(DiscordSupportUrl) { UseShellExecute = true });
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            string vanillaPath = PathTextBox.Text;
            string? commonPath = Path.GetDirectoryName(vanillaPath);

            LogService.Info("SetupPage", $"セットアップ完了: platform={_selectedPlatformTag}, path={vanillaPath}");

            var config = new AppConfig
            {
                VanillaPaths = new List<VanillaPathInfo>
                {
                    new VanillaPathInfo { Name = "バニラ（Modなし）", Path = vanillaPath }
                },
                GameInstallPath       = vanillaPath,
                ModDataPath           = commonPath ?? vanillaPath,
                Platform              = _selectedPlatformTag,
                EpicLaunchViaLauncher = _selectedPlatformTag == "Epic",
            };

            ConfigService.Save(config);

            if (App.MainWindowInstance is MainWindow mw)
            {
                mw.SetNavigationUI(true);
                mw.NavigateToPage("Home");
            }
        }
    }
}
