using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmongUsModManager.Models;
using AmongUsModManager.Models.Services;
using AmongUsModManager.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AmongUsModManager.Pages
{
    public sealed partial class SetupPage : Page
    {
        private string _selectedPlatformTag = "";
        private List<string> _detectedExistingMods = new();

        private string _githubDeviceVerificationUri = "https://github.com/login/device";
        private string _githubDeviceUserCode = "";
        private CancellationTokenSource? _githubDeviceFlowCts;

        private const string DiscordSupportUrl = "https://discord.gg/nFhkYmf9At";

        private static readonly Dictionary<string, string> PlatformHints = new()
        {
            ["Steam"] = "Steam の「ライブラリ」→「Among Us」を右クリック →「管理」→「ローカルファイルを閲覧」でフォルダを確認できます。",
            ["Epic"] = "Epic Games Launcher の「ライブラリ」→「Among Us」の「…」→「管理」→「インストール先」でフォルダを確認できます。",
            ["MSStore"] = "Microsoft Store 版は C:\\Program Files\\WindowsApps フォルダにインストールされますが、アクセス制限があるため自動検出を使用してください。",
            ["Itch"] = "itch.io アプリの「Among Us」→「…」→「フォルダを開く」でフォルダを確認できます。",
            ["Manual"] = "「参照...」ボタンから「Among Us.exe」が入っているフォルダを直接選んでください。",
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
                PlatformHint.Text = hint;
                PlatformHint.Visibility = Visibility.Visible;
            }
            else
            {
                PlatformHint.Visibility = Visibility.Collapsed;
            }

            AutoDetectButton.IsEnabled = _selectedPlatformTag != "Manual";
            PathTextBox.Text = "";
            SetValidation(false, "");
            NotFoundPanel.Visibility = Visibility.Collapsed;
            FinishButton.IsEnabled = false;

            EpicLoginPanel.Visibility = _selectedPlatformTag == "Epic"
                ? Visibility.Visible : Visibility.Collapsed;
            if (_selectedPlatformTag == "Epic") CheckEpicLoginStatus();

            GitHubLoginPanel.Visibility = Visibility.Visible;
            RefreshGitHubStatus();
        }

        private void CheckEpicLoginStatus()
        {
            bool loggedIn = EpicLoginService.IsLoggedIn();
            LogService.Info("SetupPage", $"Epicログイン状態: {(loggedIn ? "ログイン済み" : "未ログイン")}");

            if (loggedIn)
            {
                var config = ConfigService.Load();
                EpicStatusText.Text = $"✅ ログイン済み — {config.EpicDisplayName}";
                EpicStatusIcon.Glyph = "\uE73E";
                EpicStatusIcon.Foreground = new SolidColorBrush(Colors.SeaGreen);
                EpicLoginInSetupBtn.Content = "🔄 別のアカウントでログイン";
            }
            else
            {
                EpicStatusText.Text = "未ログイン — ログインしておくとランチャー不要で起動できます";
                EpicStatusIcon.Glyph = "\uE711";
                EpicStatusIcon.Foreground = new SolidColorBrush(Colors.Tomato);
                EpicLoginInSetupBtn.Content = "🔑 Epic Games にログイン";
            }
        }

        private async void EpicLoginInSetup_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("SetupPage", "Epic ログイン開始");

            var guideDlg = new ContentDialog
            {
                Title = "Epic ログイン — 手順",
                Content = new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "① 「ブラウザを開く」を押してEpicにログインしてください。",
                            TextWrapping = TextWrapping.Wrap,
                        },
                        new TextBlock
                        {
                            Text = "② ログイン完了後、ブラウザに以下のような JSON が表示されます：",
                            TextWrapping = TextWrapping.Wrap,
                        },
                        new Border
                        {
                            Background   = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 30, 30)),
                            CornerRadius = new CornerRadius(6),
                            Padding      = new Thickness(12, 8, 12, 8),
                            Child = new TextBlock
                            {
                                FontFamily   = new FontFamily("Consolas, Courier New"),
                                FontSize     = 12,
                                Foreground   = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 200, 200)),
                                Text         = "{\n  \"redirectUrl\": \"...\",\n  \"authorizationCode\": \"ここをコピー\",\n  \"sid\": null\n}",
                                TextWrapping = TextWrapping.Wrap,
                            }
                        },
                        new TextBlock
                        {
                            Text = "③ \"authorizationCode\" の値（英数字の文字列）だけをコピーして次の画面に貼り付けてください。",
                            TextWrapping = TextWrapping.Wrap,
                        },
                    }
                },
                PrimaryButtonText = "ブラウザを開く",
                CloseButtonText = "キャンセル",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };

            if (await guideDlg.ShowAsync() != ContentDialogResult.Primary) return;

            EpicLoginService.OpenBrowserForLogin();

            var inputBox = new TextBox
            {
                PlaceholderText = "例: a1b2c3d4e5f6a1b2c3d4e5f6...",
                MinWidth = 360,
            };

            var codeDlg = new ContentDialog
            {
                Title = "authorizationCode を貼り付け",
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "ブラウザの JSON に表示された\n\"authorizationCode\" の値（\" は除く）を貼り付けてください。",
                            TextWrapping = TextWrapping.Wrap,
                        },
                        inputBox,
                    }
                },
                PrimaryButtonText = "ログイン",
                CloseButtonText = "キャンセル",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };

            if (await codeDlg.ShowAsync() != ContentDialogResult.Primary) return;

            string code = inputBox.Text.Trim().Trim('"');
            if (string.IsNullOrEmpty(code))
            {
                await ShowDialog("入力エラー", "認可コードを入力してください。");
                return;
            }

            EpicLoginInSetupBtn.IsEnabled = false;
            EpicStatusText.Text = "ログイン処理中...";

            var loginResult = await EpicLoginService.LoginWithAuthCodeAsync(code);

            EpicLoginInSetupBtn.IsEnabled = true;

            if (loginResult.Success)
            {
                LogService.Info("SetupPage", $"Epic ログイン成功: {loginResult.DisplayName}");
                CheckEpicLoginStatus();
            }
            else
            {
                EpicStatusText.Text = $"❌ ログイン失敗: {loginResult.Error}";
                LogService.Warn("SetupPage", $"Epic ログイン失敗: {loginResult.Error}");
            }
        }

        private void RefreshGitHubStatus()
        {
            var config = ConfigService.Load();
            bool loggedIn = !string.IsNullOrEmpty(config.GitHubToken);
            if (loggedIn)
            {
                GitHubStatusText.Text = $"✅ ログイン済み — {config.GitHubUserName}";
                GitHubStatusIcon.Glyph = "\uE73E";
                GitHubStatusIcon.Foreground = new SolidColorBrush(Colors.SeaGreen);
                GitHubLoginBtn.Content = "🔄 別のアカウントでログイン";
            }
            else
            {
                GitHubStatusText.Text = "未連携（省略可）";
                GitHubStatusIcon.Glyph = "\uE8C8";
                GitHubStatusIcon.Foreground = null;
                GitHubLoginBtn.Content = "🐙 GitHub にログイン";
            }
        }

        private async void GitHubLoginInSetup_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("SetupPage", "GitHub デバイスフロー認証開始");
            GitHubLoginBtn.IsEnabled = false;
            GitHubCancelBtn.Visibility = Visibility.Visible;

            var codeRes = await GitHubDeviceFlowService.RequestDeviceCodeAsync();
            if (codeRes == null)
            {
                GitHubStatusText.Text = "❌ デバイスコードの取得に失敗しました。ネットワーク接続を確認してください。";
                GitHubLoginBtn.IsEnabled = true;
                GitHubCancelBtn.Visibility = Visibility.Collapsed;
                LogService.Warn("SetupPage", "GitHub デバイスコード取得失敗");
                return;
            }

            _githubDeviceVerificationUri = codeRes.verification_uri;
            _githubDeviceUserCode = codeRes.user_code;
            GitHubDeviceUserCodeText.Text = codeRes.user_code;
            GitHubCopyCodeBtn.Content = "📋 コピー";
            GitHubDeviceCodePanel.Visibility = Visibility.Visible;
            GitHubDeviceFlowStatusText.Text = "認証を待っています...";
            GitHubDeviceFlowProgress.IsActive = true;

            Process.Start(new ProcessStartInfo(codeRes.verification_uri) { UseShellExecute = true });

            _githubDeviceFlowCts = new CancellationTokenSource();
            var result = await GitHubDeviceFlowService.PollForTokenAsync(
                codeRes.device_code, codeRes.interval, _githubDeviceFlowCts.Token);

            GitHubDeviceCodePanel.Visibility = Visibility.Collapsed;
            GitHubCancelBtn.Visibility = Visibility.Collapsed;
            GitHubLoginBtn.IsEnabled = true;

            if (result.Success)
            {
                var (ok, userName) = await GitHubAuthService.VerifyTokenAsync(result.AccessToken);
                var config = ConfigService.Load();
                config.GitHubToken = result.AccessToken;
                config.GitHubLoginMethod = "device";
                config.GitHubUserName = ok ? userName : "";
                ConfigService.Save(config);
                RefreshGitHubStatus();
                LogService.Info("SetupPage", $"GitHub ログイン成功: {config.GitHubUserName}");
            }
            else
            {
                if (!result.Error.Contains("キャンセル"))
                {
                    GitHubStatusText.Text = $"❌ 連携失敗: {result.Error}";
                    LogService.Warn("SetupPage", $"GitHub ログイン失敗: {result.Error}");
                }
            }
        }

        private void GitHubCancelFlow_Click(object sender, RoutedEventArgs e)
        {
            _githubDeviceFlowCts?.Cancel();
            GitHubDeviceCodePanel.Visibility = Visibility.Collapsed;
            GitHubCancelBtn.Visibility = Visibility.Collapsed;
            GitHubLoginBtn.IsEnabled = true;
            GitHubDeviceFlowStatusText.Text = "";
        }

        private void GitHubCopyCode_Click(object sender, RoutedEventArgs e)
        {
            var dp = new DataPackage();
            dp.SetText(_githubDeviceUserCode);
            Clipboard.SetContent(dp);
            GitHubCopyCodeBtn.Content = "✅ コピー済み";
        }

        private void OpenGitHubBrowser_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(_githubDeviceVerificationUri) { UseShellExecute = true });
        }

        private void AutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("SetupPage", $"自動検出開始: {_selectedPlatformTag}");
            NotFoundPanel.Visibility = Visibility.Collapsed;

            string? path = _selectedPlatformTag switch
            {
                "Steam" => AUFileDetector.GetSteamPath(),
                "Epic" => AUFileDetector.GetEpicPath(),
                "MSStore" => AUFileDetector.GetMicrosoftStorePath(),
                "Itch" => AUFileDetector.GetItchPath(),
                _ => null,
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

        private void DetectExistingMods(string gamePath)
        {
            _detectedExistingMods.Clear();

            string bepInExPlugins = Path.Combine(gamePath, "BepInEx", "plugins");
            if (Directory.Exists(bepInExPlugins))
                _detectedExistingMods.AddRange(Directory.GetDirectories(bepInExPlugins));

            string? parentPath = Path.GetDirectoryName(gamePath);
            if (parentPath != null)
            {
                string siblingPlugins = Path.Combine(parentPath, "BepInEx", "plugins");
                if (Directory.Exists(siblingPlugins) && siblingPlugins != bepInExPlugins)
                    _detectedExistingMods.AddRange(Directory.GetDirectories(siblingPlugins));
            }

            _detectedExistingMods = _detectedExistingMods.Distinct().ToList();

            if (_detectedExistingMods.Count > 0)
            {
                LogService.Info("SetupPage", $"インストール済みMod検出: {_detectedExistingMods.Count}件");
                var names = _detectedExistingMods.Select(d => Path.GetFileName(d)).Take(5).ToList();
                string preview = string.Join("、", names);
                if (_detectedExistingMods.Count > 5) preview += $" ほか{_detectedExistingMods.Count - 5}件";

                ExistingModTitle.Text = $"インストール済みModが {_detectedExistingMods.Count} 件見つかりました";
                ExistingModDesc.Text = $"見つかったフォルダ: {preview}\n\nこれらをアプリに登録しますか？";
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
            config.VanillaPaths ??= new List<VanillaPathInfo>();

            foreach (var modDir in _detectedExistingMods)
            {
                string name = Path.GetFileName(modDir);
                if (config.VanillaPaths.Any(v => v.Path == modDir)) continue;

                var info = new VanillaPathInfo { Name = name, Path = modDir };

                var preset = ModInstallPage.SupportedMods.FirstOrDefault(p =>
                    name.Equals(p.Name, StringComparison.OrdinalIgnoreCase)
                    || name.Contains(p.Name, StringComparison.OrdinalIgnoreCase)
                    || p.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

                if (preset != null)
                {
                    info.GitHubOwner = preset.Owner;
                    info.GitHubRepo = preset.Repository;
                    LogService.Info("SetupPage", $"自動GitHub連携: {name} → {preset.Owner}/{preset.Repository}");
                }

                config.VanillaPaths.Add(info);
                count++;
            }
            ConfigService.Save(config);
            LogService.Info("SetupPage", $"既存Mod登録: {count}件");

            ExistingModInfoBar.Severity = InfoBarSeverity.Success;
            ExistingModInfoBar.Message = $"{count} 件のModを登録しました。";
            ExistingModInfoBar.IsOpen = true;
            RegisterExistingModBtn.IsEnabled = false;
        }

        private void SkipExistingMods_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("SetupPage", "既存Mod登録スキップ");
            ExistingModPanel.Visibility = Visibility.Collapsed;
        }

        private void SetValidation(bool success, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                ValidateInfoBar.IsOpen = false;
                return;
            }
            ValidateInfoBar.Severity = success ? InfoBarSeverity.Success : InfoBarSeverity.Error;
            ValidateInfoBar.Message = message;
            ValidateInfoBar.IsOpen = true;
        }

        private void OpenSteamStore_Click(object sender, RoutedEventArgs e)
            => Process.Start(new ProcessStartInfo(
                "https://store.steampowered.com/app/945360/Among_Us/")
            { UseShellExecute = true });

        private void OpenEpicStore_Click(object sender, RoutedEventArgs e)
            => Process.Start(new ProcessStartInfo(
                "https://store.epicgames.com/ja/p/among-us")
            { UseShellExecute = true });

        private void OpenItchStore_Click(object sender, RoutedEventArgs e)
            => Process.Start(new ProcessStartInfo(
                "https://innersloth.itch.io/among-us")
            { UseShellExecute = true });

        private void OpenDiscordSupport_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("SetupPage", "Discord サポートリンクを開く");
            Process.Start(new ProcessStartInfo(DiscordSupportUrl) { UseShellExecute = true });
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            string gamePath = PathTextBox.Text;
            string? parentPath = Path.GetDirectoryName(gamePath);

            LogService.Info("SetupPage", $"セットアップ完了: platform={_selectedPlatformTag}, path={gamePath}");

            var config = ConfigService.Load();
            config.GameInstallPath = gamePath;
            config.ModDataPath = parentPath ?? gamePath;
            config.Platform = _selectedPlatformTag;
            config.MainPlatform = _selectedPlatformTag;
            config.EpicLaunchViaLauncher = _selectedPlatformTag == "Epic";

            config.VanillaPaths ??= new List<VanillaPathInfo>();
            if (!config.VanillaPaths.Any(v => v.Path == gamePath))
                config.VanillaPaths.Insert(0, new VanillaPathInfo { Name = "バニラ（Modなし）", Path = gamePath });

            ConfigService.Save(config);

            if (App.MainWindowInstance is MainWindow mw)
            {
                mw.SetNavigationUI(true);
                mw.NavigateToPage("Home");
            }
        }

        private async Task ShowDialog(string title, string message)
        {
            await new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }
    }
}
