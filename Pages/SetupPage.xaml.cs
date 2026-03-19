using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AmongUsModManager.Models;
using AmongUsModManager.Models.Services;
using AmongUsModManager.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AmongUsModManager.Pages
{
    public sealed partial class SetupPage : Page
    {
        private string _selectedPlatformTag = "";
        private List<string> _detectedExistingMods = new();

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
            FinishButton.IsEnabled    = false;

            // Epic 選択時 → ログインパネル表示
            if (_selectedPlatformTag == "Epic")
            {
                EpicLoginPanel.Visibility = Visibility.Visible;
                CheckEpicLoginStatus();
            }
            else
            {
                EpicLoginPanel.Visibility = Visibility.Collapsed;
            }

            // GitHub パネルは常に表示（任意連携）
            GitHubLoginPanel.Visibility = Visibility.Visible;
            RefreshGitHubStatus();
        }

        // ─── Epic ログイン ────────────────────────────────────────────
        private void CheckEpicLoginStatus()
        {
            bool loggedIn = EpicLoginService.IsLoggedIn();
            LogService.Info("SetupPage", $"Epicログイン状態: {(loggedIn ? "ログイン済み" : "未ログイン")}");

            if (loggedIn)
            {
                var config = ConfigService.Load();
                EpicStatusText.Text      = $"✅ ログイン済み — {config.EpicDisplayName}";
                EpicStatusIcon.Glyph     = "\uE73E";
                EpicStatusIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.SeaGreen);
                EpicLoginInSetupBtn.Content = "🔄 別のアカウントでログイン";
            }
            else
            {
                EpicStatusText.Text      = "未ログイン — ログインしておくとランチャー不要で起動できます";
                EpicStatusIcon.Glyph     = "\uE711";
                EpicStatusIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Tomato);
                EpicLoginInSetupBtn.Content = "🔑 Epic Games にログイン";
            }
        }

        // セットアップ内でそのまま Epic ログインウィンドウを開く
        private void EpicLoginInSetup_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("SetupPage", "Epic ログインウィンドウを開く");
            var loginWindow = new EpicLoginWindow(result =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (result.Success)
                    {
                        CheckEpicLoginStatus();
                        LogService.Info("SetupPage", $"Epicログイン成功: {result.DisplayName}");
                    }
                    else
                    {
                        EpicStatusText.Text = $"❌ ログイン失敗: {result.Error}";
                        LogService.Warn("SetupPage", $"Epicログイン失敗: {result.Error}");
                    }
                });
            });
            loginWindow.Activate();
        }

        private void EpicLaunchLauncher_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("SetupPage", "Epic Launcher 起動ボタン");
            EpicLoginService.LaunchEpicLauncher();
            DispatcherQueue.TryEnqueue(async () =>
            {
                await Task.Delay(3000);
                if (_selectedPlatformTag == "Epic") CheckEpicLoginStatus();
            });
        }

        // ─── GitHub 連携 ──────────────────────────────────────────────
        private void RefreshGitHubStatus()
        {
            var config = ConfigService.Load();
            bool loggedIn = !string.IsNullOrEmpty(config.GitHubToken);
            if (loggedIn)
            {
                GitHubStatusText.Text = $"✅ ログイン済み — {config.GitHubUserName}";
                GitHubStatusIcon.Glyph     = "\uE73E";
                GitHubStatusIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.SeaGreen);
                GitHubLoginBtn.Content = "🔄 別のアカウントでログイン";
            }
            else
            {
                GitHubStatusText.Text = "未連携（省略可）";
                GitHubStatusIcon.Glyph     = "\uE8C8";
                GitHubStatusIcon.Foreground = null;
                GitHubLoginBtn.Content = "🐙 GitHub にログイン";
            }
        }

        private async void GitHubLoginInSetup_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("SetupPage", "GitHub デバイスフロー認証開始");
            GitHubLoginBtn.IsEnabled = false;

            try
            {
                // Step1: デバイスコード取得
                GitHubStatusText.Text = "デバイスコードを取得中…";
                var codeRes = await GitHubDeviceFlowService.RequestDeviceCodeAsync();
                if (codeRes == null)
                {
                    GitHubStatusText.Text = "❌ デバイスコードの取得に失敗しました。ネットワーク接続を確認してください。";
                    LogService.Warn("SetupPage", "GitHub デバイスコード取得失敗");
                    return;
                }

                // Step2: ブラウザを開いてユーザーにコード入力させる
                GitHubStatusText.Text = $"ブラウザで次のコードを入力してください: {codeRes.user_code}\n認証完了を待っています…";
                Process.Start(new ProcessStartInfo(codeRes.verification_uri) { UseShellExecute = true });

                // Step3: トークンポーリング
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(5));
                var result = await GitHubDeviceFlowService.PollForTokenAsync(
                    codeRes.device_code, codeRes.interval, cts.Token);

                if (result.Success)
                {
                    // Step4: トークン検証してユーザー名取得
                    var (ok, userName) = await GitHubAuthService.VerifyTokenAsync(result.AccessToken);
                    var config = ConfigService.Load();
                    config.GitHubToken       = result.AccessToken;
                    config.GitHubLoginMethod = "device";
                    config.GitHubUserName    = ok ? userName : "";
                    ConfigService.Save(config);
                    RefreshGitHubStatus();
                    LogService.Info("SetupPage", $"GitHub ログイン成功: {config.GitHubUserName}");
                }
                else
                {
                    GitHubStatusText.Text = $"❌ 連携失敗: {result.Error}";
                    LogService.Warn("SetupPage", $"GitHub ログイン失敗: {result.Error}");
                }
            }
            catch (Exception ex)
            {
                GitHubStatusText.Text = $"❌ エラー: {ex.Message}";
                LogService.Error("SetupPage", "GitHub ログイン例外", ex);
            }
            finally
            {
                GitHubLoginBtn.IsEnabled = true;
            }
        }

        // ─── フォルダ検出 ─────────────────────────────────────────────
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
                LogService.Warn("SetupPage", "自動検出失敗");
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
                // パスが確定したらインストール済みModを検出
                DetectExistingMods(path);
            }
            else
            {
                SetValidation(false, "このフォルダに Among Us.exe が見つかりません。正しいインストールフォルダを選択してください。");
                NotFoundPanel.Visibility = Visibility.Visible;
                ExistingModPanel.Visibility = Visibility.Collapsed;
            }

            FinishButton.IsEnabled = valid;
        }

        // ─── インストール済みMod 検出 ─────────────────────────────────
        private void DetectExistingMods(string gamePath)
        {
            _detectedExistingMods.Clear();

            // ゲームフォルダ内の BepInEx/plugins を走査
            string bepInExPlugins = Path.Combine(gamePath, "BepInEx", "plugins");
            if (Directory.Exists(bepInExPlugins))
            {
                var dirs = Directory.GetDirectories(bepInExPlugins);
                _detectedExistingMods.AddRange(dirs);
            }

            // ゲームフォルダの隣（ModDataPath = 親フォルダ）も走査
            string? parentPath = Path.GetDirectoryName(gamePath);
            if (parentPath != null)
            {
                string siblingPlugins = Path.Combine(parentPath, "BepInEx", "plugins");
                if (Directory.Exists(siblingPlugins) && siblingPlugins != bepInExPlugins)
                {
                    var dirs = Directory.GetDirectories(siblingPlugins);
                    _detectedExistingMods.AddRange(dirs);
                }
            }

            // 重複除去
            _detectedExistingMods = _detectedExistingMods.Distinct().ToList();

            if (_detectedExistingMods.Count > 0)
            {
                LogService.Info("SetupPage", $"インストール済みMod検出: {_detectedExistingMods.Count}件");
                var names = _detectedExistingMods
                    .Select(d => Path.GetFileName(d))
                    .Take(5)
                    .ToList();
                string preview = string.Join("、", names);
                if (_detectedExistingMods.Count > 5) preview += $" ほか{_detectedExistingMods.Count - 5}件";

                ExistingModTitle.Text = $"インストール済みModが {_detectedExistingMods.Count} 件見つかりました";
                ExistingModDesc.Text  = $"見つかったフォルダ: {preview}\n\nこれらをアプリに登録しますか？";
                ExistingModPanel.Visibility = Visibility.Visible;
                ExistingModInfoBar.IsOpen = false;
            }
            else
            {
                LogService.Debug("SetupPage", "インストール済みMod: なし");
                ExistingModPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void RegisterExistingMods_Click(object sender, RoutedEventArgs e)
        {
            int count = 0;
            var config = ConfigService.Load();
            if (config.VanillaPaths == null)
                config.VanillaPaths = new List<VanillaPathInfo>();

            foreach (var modDir in _detectedExistingMods)
            {
                string name = Path.GetFileName(modDir);
                if (config.VanillaPaths.Any(v => v.Path == modDir)) continue;

                var info = new VanillaPathInfo { Name = name, Path = modDir };

                // 名前がSupportedModsと一致すれば自動GitHub連携
                var preset = ModInstallPage.SupportedMods.FirstOrDefault(p =>
                    name.Equals(p.Name, StringComparison.OrdinalIgnoreCase)
                    || name.Contains(p.Name, StringComparison.OrdinalIgnoreCase)
                    || p.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

                if (preset != null)
                {
                    info.GitHubOwner = preset.Owner;
                    info.GitHubRepo  = preset.Repository;
                    LogService.Info("SetupPage", $"自動GitHub連携: {name} → {preset.Owner}/{preset.Repository}");
                }

                config.VanillaPaths.Add(info);
                count++;
            }
            ConfigService.Save(config);
            LogService.Info("SetupPage", $"既存Mod登録: {count}件");

            ExistingModInfoBar.Severity = InfoBarSeverity.Success;
            ExistingModInfoBar.Message  = $"{count} 件のModを登録しました。";
            ExistingModInfoBar.IsOpen   = true;
            RegisterExistingModBtn.IsEnabled = false;
        }

        private void SkipExistingMods_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("SetupPage", "既存Mod登録スキップ");
            ExistingModPanel.Visibility = Visibility.Collapsed;
        }

        // ─── その他ボタン ─────────────────────────────────────────────
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

        // ─── セットアップ完了 ─────────────────────────────────────────
        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            string gamePath    = PathTextBox.Text;
            string? parentPath = Path.GetDirectoryName(gamePath);

            LogService.Info("SetupPage", $"セットアップ完了: platform={_selectedPlatformTag}, path={gamePath}");

            // 既存設定があれば引き継ぐ（GitHubトークンなど）
            var config = ConfigService.Load();

            config.GameInstallPath       = gamePath;
            config.ModDataPath           = parentPath ?? gamePath;
            config.Platform              = _selectedPlatformTag;
            config.MainPlatform          = _selectedPlatformTag;   // メインプラットフォームとして確定
            config.EpicLaunchViaLauncher = _selectedPlatformTag == "Epic";

            // バニラパスが未設定なら追加
            if (config.VanillaPaths == null)
                config.VanillaPaths = new List<VanillaPathInfo>();
            if (!config.VanillaPaths.Any(v => v.Path == gamePath))
                config.VanillaPaths.Insert(0, new VanillaPathInfo { Name = "バニラ（Modなし）", Path = gamePath });

            ConfigService.Save(config);

            if (App.MainWindowInstance is MainWindow mw)
            {
                mw.SetNavigationUI(true);
                mw.NavigateToPage("Home");
            }
        }
    }
}
