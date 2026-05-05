using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using AmongUsModManager.Models.Services;
using AmongUsModManager.Services;
using Windows.ApplicationModel.DataTransfer;

namespace AmongUsModManager.Pages
{
    public sealed partial class AccountPage : Page
    {
        private CancellationTokenSource? _deviceFlowCts;
        private string _deviceVerificationUri = "https://github.com/login/device";
        private string _deviceUserCode = "";

        public AccountPage()
        {
            this.InitializeComponent();
            this.Loaded += AccountPage_Loaded;
        }

        private void AccountPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGitHubStatus();
            LoadSteamStatus();
            LoadEpicStatus();
        }

        
        
        

        private void LoadGitHubStatus()
        {
            var config = ConfigService.Load();
            bool hasToken = !string.IsNullOrEmpty(config.GitHubToken);

            if (hasToken)
            {
                string label = config.GitHubLoginMethod == "device"
                    ? $"接続済み: @{config.GitHubUserName}（5,000回/時間）"
                    : $"接続済み: {config.GitHubToken[..Math.Min(4, config.GitHubToken.Length)]}****（5,000回/時間）";
                SetGitHubConnected(label);
            }
            else
            {
                SetGitHubDisconnected();
            }
        }

        private void SetGitHubConnected(string label)
        {
            GitHubStatusIcon.Glyph      = "\uE73E";
            GitHubStatusIcon.Foreground = new SolidColorBrush(Colors.SeaGreen);
            GitHubStatusText.Text       = label;
            GitHubStatusBadge.Background =
                (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBackgroundBrush"];

            GitHubLoginPanel.Visibility      = Visibility.Collapsed;
            GitHubDisconnectMainBtn.Visibility = Visibility.Visible;

            
            DeviceCodePanel.Visibility     = Visibility.Collapsed;
            StartDeviceFlowBtn.Visibility  = Visibility.Visible;
            CancelDeviceFlowBtn.Visibility = Visibility.Collapsed;
            GitHubConnectBtn.Visibility    = Visibility.Collapsed;
            GitHubDisconnectBtn.Visibility = Visibility.Collapsed;
        }

        private void SetGitHubDisconnected()
        {
            GitHubStatusIcon.Glyph      = "\uE711";
            GitHubStatusIcon.Foreground = new SolidColorBrush(Colors.Tomato);
            GitHubStatusText.Text       = "未接続（レートリミット：60回/時間）";
            GitHubStatusBadge.Background =
                (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];

            GitHubLoginPanel.Visibility       = Visibility.Visible;
            GitHubDisconnectMainBtn.Visibility = Visibility.Collapsed;
            GitHubTokenBox.Password            = "";
            GitHubConnectBtn.Visibility        = Visibility.Visible;
            GitHubDisconnectBtn.Visibility     = Visibility.Collapsed;
        }

        

        private async void StartDeviceFlow_Click(object sender, RoutedEventArgs e)
        {
            StartDeviceFlowBtn.IsEnabled = false;
            StartDeviceFlowBtn.Content   = "取得中...";

            var codeRes = await GitHubDeviceFlowService.RequestDeviceCodeAsync();

            if (codeRes == null)
            {
                StartDeviceFlowBtn.IsEnabled = true;
                StartDeviceFlowBtn.Content   = "🔑 GitHubでログイン";
                await ShowDialog("エラー", "デバイスコードの取得に失敗しました。\nネットワーク接続を確認してください。");
                return;
            }

            
            _deviceVerificationUri = codeRes.verification_uri;
            _deviceUserCode        = codeRes.user_code;
            DeviceUserCodeText.Text = codeRes.user_code;
            DeviceCodePanel.Visibility    = Visibility.Visible;
            CancelDeviceFlowBtn.Visibility = Visibility.Visible;
            StartDeviceFlowBtn.Visibility  = Visibility.Collapsed;

            
            Process.Start(new ProcessStartInfo(codeRes.verification_uri) { UseShellExecute = true });

            
            _deviceFlowCts = new CancellationTokenSource();
            DeviceFlowStatusText.Text = "認証を待っています...";
            DeviceFlowProgress.IsActive = true;

            var result = await GitHubDeviceFlowService.PollForTokenAsync(
                codeRes.device_code, codeRes.interval, _deviceFlowCts.Token);

            if (result.Success)
            {
                
                var (ok, userName) = await GitHubAuthService.VerifyTokenAsync(result.AccessToken);
                var config = ConfigService.Load();
                config.GitHubToken     = result.AccessToken;
                config.GitHubLoginMethod = "device";
                config.GitHubUserName  = ok ? userName : "";
                ConfigService.Save(config);

                LogService.Info("AccountPage", $"GitHub Device Flow ログイン成功: @{config.GitHubUserName}");
                SetGitHubConnected($"接続済み: @{config.GitHubUserName}（5,000回/時間）");
            }
            else
            {
                
                DeviceCodePanel.Visibility     = Visibility.Collapsed;
                StartDeviceFlowBtn.Visibility  = Visibility.Visible;
                StartDeviceFlowBtn.IsEnabled   = true;
                StartDeviceFlowBtn.Content     = "🔑 GitHubでログイン";
                CancelDeviceFlowBtn.Visibility = Visibility.Collapsed;

                if (!result.Error.Contains("キャンセル"))
                    await ShowDialog("ログイン失敗", result.Error);
            }
        }

        private void CancelDeviceFlow_Click(object sender, RoutedEventArgs e)
        {
            _deviceFlowCts?.Cancel();
            DeviceCodePanel.Visibility     = Visibility.Collapsed;
            StartDeviceFlowBtn.Visibility  = Visibility.Visible;
            StartDeviceFlowBtn.IsEnabled   = true;
            StartDeviceFlowBtn.Content     = "🔑 GitHubでログイン";
            CancelDeviceFlowBtn.Visibility = Visibility.Collapsed;
            DeviceFlowStatusText.Text      = "";
        }

        private void CopyDeviceCode_Click(object sender, RoutedEventArgs e)
        {
            var dp = new DataPackage();
            dp.SetText(_deviceUserCode);
            Clipboard.SetContent(dp);
            CopyDeviceCodeBtn.Content = "✅ コピー済み";
        }

        private void OpenGitHubBrowser_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(_deviceVerificationUri) { UseShellExecute = true });
        }

        

        private async void GitHubConnect_Click(object sender, RoutedEventArgs e)
        {
            string token = GitHubTokenBox.Password.Trim();
            if (string.IsNullOrEmpty(token))
            {
                await ShowDialog("入力エラー", "Personal Access Token を入力してください。");
                return;
            }

            GitHubConnectBtn.IsEnabled = false;
            GitHubConnectBtn.Content   = "確認中...";

            var (ok, result) = await GitHubAuthService.VerifyTokenAsync(token);

            if (ok)
            {
                var config = ConfigService.Load();
                config.GitHubToken       = token;
                config.GitHubLoginMethod = "pat";
                config.GitHubUserName    = result;
                ConfigService.Save(config);
                LogService.Info("AccountPage", $"GitHub PAT 接続成功: @{result}");
                SetGitHubConnected($"接続済み: @{result}（5,000回/時間）");
            }
            else
            {
                LogService.Warn("AccountPage", $"GitHub PAT 接続失敗: {result}");
                GitHubConnectBtn.IsEnabled = true;
                GitHubConnectBtn.Content   = "接続";
                await ShowDialog("接続失敗", result);
            }
        }

        private async void GitHubDisconnect_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ContentDialog
            {
                Title             = "GitHub との接続を切断",
                Content           = "接続を解除してもよいですか？\nAPI 制限が 60回/時間 に戻ります。",
                PrimaryButtonText = "切断する",
                CloseButtonText   = "キャンセル",
                DefaultButton     = ContentDialogButton.Close,
                XamlRoot          = this.XamlRoot
            };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            var config = ConfigService.Load();
            config.GitHubToken       = "";
            config.GitHubLoginMethod = "";
            config.GitHubUserName    = "";
            ConfigService.Save(config);
            LogService.Info("AccountPage", "GitHub 接続を解除");
            SetGitHubDisconnected();
        }

        
        
        

        private CancellationTokenSource? _steamCts;

        private void LoadSteamStatus()
        {
            var config = ConfigService.Load();
            if (!string.IsNullOrEmpty(config.SteamUserId))
                SetSteamConnected(config.SteamUserName, config.SteamUserId);
            else
                SetSteamDisconnected();
        }

        private void SetSteamConnected(string userName, string steamId)
        {
            SteamStatusIcon.Glyph      = "\uE73E";
            SteamStatusIcon.Foreground = new SolidColorBrush(Colors.SeaGreen);
            SteamStatusText.Text       = $"接続済み: {userName}";
            SteamStatusBadge.Background =
                (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBackgroundBrush"];

            SteamUserNameText.Text = userName;
            SteamIdText.Text       = $"SteamID: {steamId}";
            SteamProfileLink.NavigateUri = new Uri($"https://steamcommunity.com/profiles/{steamId}");

            SteamProfilePanel.Visibility  = Visibility.Visible;
            SteamLoginPanel.Visibility    = Visibility.Collapsed;
            SteamDisconnectBtn.Visibility = Visibility.Visible;
        }

        private void SetSteamDisconnected()
        {
            SteamStatusIcon.Glyph      = "\uE711";
            SteamStatusIcon.Foreground = new SolidColorBrush(Colors.Tomato);
            SteamStatusText.Text       = "未接続";
            SteamStatusBadge.Background =
                (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];

            SteamProfilePanel.Visibility  = Visibility.Collapsed;
            SteamLoginPanel.Visibility    = Visibility.Visible;
            SteamDisconnectBtn.Visibility = Visibility.Collapsed;
            SteamLoginBtn.Visibility      = Visibility.Visible;
            SteamCancelBtn.Visibility     = Visibility.Collapsed;
            SteamWaitingPanel.Visibility  = Visibility.Collapsed;
            SteamLoginBtn.IsEnabled       = true;
            SteamLoginBtn.Content         = "🎮 Steamでログイン";
        }

        private async void SteamLogin_Click(object sender, RoutedEventArgs e)
        {
            SteamLoginBtn.IsEnabled      = false;
            SteamLoginBtn.Content        = "ブラウザを開いています...";
            SteamWaitingPanel.Visibility = Visibility.Visible;
            SteamCancelBtn.Visibility    = Visibility.Visible;

            _steamCts = new CancellationTokenSource();
            var result = await SteamOpenIdService.AuthenticateAsync(_steamCts.Token);

            if (result.Success)
            {
                SteamStatusText.Text = "ユーザー名を取得しています...";
                string userName = await SteamOpenIdService.FetchUserNameAsync(result.SteamId);

                var config = ConfigService.Load();
                config.SteamUserId   = result.SteamId;
                config.SteamUserName = userName;
                ConfigService.Save(config);

                LogService.Info("AccountPage", $"Steam OpenID ログイン成功: {userName} ({result.SteamId})");
                SetSteamConnected(userName, result.SteamId);
            }
            else
            {
                SetSteamDisconnected();
                if (!result.Error.Contains("キャンセル"))
                    await ShowDialog("ログイン失敗", result.Error);
            }
        }

        private void SteamCancel_Click(object sender, RoutedEventArgs e)
        {
            _steamCts?.Cancel();
            SetSteamDisconnected();
        }

        private async void SteamDisconnect_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ContentDialog
            {
                Title             = "Steam との接続を解除",
                Content           = "Steam アカウントの連携を解除してもよいですか？",
                PrimaryButtonText = "解除する",
                CloseButtonText   = "キャンセル",
                DefaultButton     = ContentDialogButton.Close,
                XamlRoot          = this.XamlRoot
            };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            var config = ConfigService.Load();
            config.SteamUserId   = "";
            config.SteamUserName = "";
            ConfigService.Save(config);
            LogService.Info("AccountPage", "Steam 接続を解除");
            SetSteamDisconnected();
        }

        
        
        



        private void LoadEpicStatus()
        {
            var config = ConfigService.Load();
            if (config.Platform != "Epic")
            {
                EpicNotApplicableBar.IsOpen = true;
                SetEpicDisconnected();
                EpicLoginBtn.IsEnabled = false;
                return;
            }

            if (EpicLoginService.IsLoggedIn(config))
                SetEpicConnected(config.EpicDisplayName, config.EpicAccountId);
            else
                SetEpicDisconnected();
        }

        private void SetEpicConnected(string displayName, string accountId)
        {
            EpicStatusIcon.Glyph      = "\uE73E";
            EpicStatusIcon.Foreground = new SolidColorBrush(Colors.SeaGreen);
            EpicStatusText.Text       = $"ログイン済み — {displayName}";
            EpicStatusBadge.Background =
                (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBackgroundBrush"];

            EpicDisplayNameText.Text = displayName;
            EpicAccountIdText.Text   = $"Account ID: {accountId}";

            EpicProfilePanel.Visibility = Visibility.Visible;
            EpicLoginPanel.Visibility   = Visibility.Collapsed;
            EpicLogoutBtn.Visibility    = Visibility.Visible;
        }

        private void SetEpicDisconnected()
        {
            EpicStatusIcon.Glyph      = "\uE711";
            EpicStatusIcon.Foreground = new SolidColorBrush(Colors.Tomato);
            EpicStatusText.Text       = "未ログイン";
            EpicStatusBadge.Background =
                (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];

            EpicProfilePanel.Visibility       = Visibility.Collapsed;
            EpicLoginPanel.Visibility         = Visibility.Visible;
            EpicLogoutBtn.Visibility          = Visibility.Collapsed;
            EpicLoginProgressPanel.Visibility = Visibility.Collapsed;
            EpicLoginBtn.Visibility           = Visibility.Visible;
            EpicLoginBtn.IsEnabled            = true;
            EpicLoginBtn.Content              = "🎮 Epic でログイン";
        }

       
        private CancellationTokenSource? _epicCts;

        private async void EpicLogin_Click(object sender, RoutedEventArgs e)
        {
           
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
                            Text = "③ \"authorizationCode\" の右の値（英数字の文字列）だけを\nコピーして次の画面に貼り付けてください。",
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
                            Text = "ブラウザの JSON に表示された\n\"authorizationCode\" の値（\" は除く）を貼り付けてください。\n見づらい場合はプリントにチェックを入れてみてください。",
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

            EpicLoginBtn.IsEnabled = false;
            EpicLoginBtn.Visibility = Visibility.Collapsed;
            EpicLoginProgressPanel.Visibility = Visibility.Visible;

            var loginResult = await EpicLoginService.LoginWithAuthCodeAsync(code);

            if (loginResult.Success)
            {
                LogService.Info("AccountPage",
                    $"Epic ログイン成功: {loginResult.DisplayName} ({loginResult.AccountId})");
                NotificationService.MarkReadByTag("epic_login");
                SetEpicConnected(loginResult.DisplayName, loginResult.AccountId);
            }
            else
            {
                SetEpicDisconnected();
                await ShowDialog("ログイン失敗", loginResult.Error);
            }
        }

        private void EpicLoginCancel_Click(object sender, RoutedEventArgs e)
        {
            _epicCts?.Cancel();
            SetEpicDisconnected();
        }

        private async void EpicLogout_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ContentDialog
            {
                Title             = "Epic Games ログアウト",
                Content           = "ログアウトすると、次回起動時に再度ログインが必要になります。",
                PrimaryButtonText = "ログアウト",
                CloseButtonText   = "キャンセル",
                DefaultButton     = ContentDialogButton.Close,
                XamlRoot          = this.XamlRoot
            };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            EpicLoginService.Logout();
            LogService.Info("AccountPage", "Epic ログアウト");
            SetEpicDisconnected();
        }

        
        
        

        private async void CheckRateLimit_Click(object sender, RoutedEventArgs e)
        {
            RateLimitRemaining.Text = "取得中...";
            RateLimitMax.Text       = "取得中...";
            RateLimitResetText.Text = "";

            try
            {
                var client   = GitHubAuthService.GetClient();
                var response = await client.GetAsync("https://api.github.com/rate_limit");
                if (!response.IsSuccessStatusCode)
                {
                    RateLimitRemaining.Text = "エラー";
                    RateLimitMax.Text       = "--";
                    return;
                }

                var data = await response.Content.ReadFromJsonAsync<RateLimitResponse>();
                if (data?.rate == null) return;

                int  remaining = data.rate.remaining;
                int  limit     = data.rate.limit;
                long reset     = data.rate.reset;

                RateLimitRemaining.Text = remaining.ToString("N0");
                RateLimitMax.Text       = limit.ToString("N0");
                RateLimitBar.Maximum    = limit;
                RateLimitBar.Value      = remaining;

                var resetTime = DateTimeOffset.FromUnixTimeSeconds(reset).ToLocalTime();
                RateLimitResetText.Text = $"リセット時刻: {resetTime:HH:mm:ss}";

                LogService.Info("AccountPage", $"GitHub API 残り: {remaining}/{limit}, リセット: {resetTime:HH:mm}");
            }
            catch (Exception ex)
            {
                RateLimitRemaining.Text = "エラー";
                RateLimitMax.Text       = "--";
                LogService.Error("AccountPage", "レートリミット確認エラー", ex);
            }
        }

        
        
        

        private async Task ShowDialog(string title, string message)
        {
            await new ContentDialog
            {
                Title           = title,
                Content         = message,
                CloseButtonText = "OK",
                XamlRoot        = this.XamlRoot
            }.ShowAsync();
        }

        private class RateLimitResponse
        {
            public RateInfo? rate { get; set; }
        }
        private class RateInfo
        {
            public int  remaining { get; set; }
            public int  limit     { get; set; }
            public long reset     { get; set; }
        }
    }
}
