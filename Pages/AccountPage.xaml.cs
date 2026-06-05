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
            ApplyStrings();
            LocalizationService.LanguageChanged += ApplyStrings;
            this.Unloaded += (_, _) => LocalizationService.LanguageChanged -= ApplyStrings;
        }

        private void ApplyStrings()
        {
            AccountPageTitle.Text    = LocalizationService.Get("Account_Title");
            AccountPageSubtitle.Text = LocalizationService.Get("Account_Subtitle");

            StartDeviceFlowBtn.Content    = LocalizationService.Get("Account_StartDeviceFlow");
            CancelDeviceFlowBtn.Content   = LocalizationService.Get("Account_CancelFlow");
            GitHubConnectBtn.Content      = LocalizationService.Get("Account_GitHubConnect");
            GitHubDisconnectBtn.Content   = LocalizationService.Get("Account_GitHubDisconnectBtn");
            GitHubDisconnectMainBtn.Content = LocalizationService.Get("Account_GitHubDisconnectMainBtn");

            SteamLoginBtn.Content      = LocalizationService.Get("Account_SteamLoginBtn");
            SteamCancelBtn.Content     = LocalizationService.Get("Account_CancelFlow");
            SteamDisconnectBtn.Content = LocalizationService.Get("Account_SteamDisconnectBtn");

            EpicLoginBtn.Content  = LocalizationService.Get("Account_EpicLoginBtn");
            EpicLogoutBtn.Content = LocalizationService.Get("Account_EpicLogoutBtn");

            EpicNotApplicableBar.Title   = LocalizationService.Get("Account_EpicNotApplicableTitle");
            EpicNotApplicableBar.Message = LocalizationService.Get("Account_EpicNotApplicableMsg");

            ApiRateLimitTitle.Text         = LocalizationService.Get("Account_ApiRateLimitTitle");
            ApiRateLimitRemainingLabel.Text = LocalizationService.Get("Account_ApiRateLimitRemaining");
            ApiRateLimitMaxLabel.Text       = LocalizationService.Get("Account_ApiRateLimitMax");
            CheckRateLimitBtn.Content       = LocalizationService.Get("Account_CheckRateLimit");
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
                string name = config.GitHubLoginMethod == "device"
                    ? $"@{config.GitHubUserName}"
                    : $"{config.GitHubToken[..Math.Min(4, config.GitHubToken.Length)]}****";
                SetGitHubConnected(string.Format(LocalizationService.Get("Account_ConnectedGH"), name));
            }
            else
            {
                SetGitHubDisconnected();
            }
        }

        private void SetGitHubConnected(string label)
        {
            GitHubStatusIcon.Glyph = "\uE73E";
            GitHubStatusIcon.Foreground = new SolidColorBrush(Colors.SeaGreen);
            GitHubStatusText.Text = label;
            GitHubStatusBadge.Background =
                (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBackgroundBrush"];

            GitHubLoginPanel.Visibility = Visibility.Collapsed;
            GitHubDisconnectMainBtn.Visibility = Visibility.Visible;


            DeviceCodePanel.Visibility = Visibility.Collapsed;
            StartDeviceFlowBtn.Visibility = Visibility.Visible;
            CancelDeviceFlowBtn.Visibility = Visibility.Collapsed;
            GitHubConnectBtn.Visibility = Visibility.Collapsed;
            GitHubDisconnectBtn.Visibility = Visibility.Collapsed;
        }

        private void SetGitHubDisconnected()
        {
            GitHubStatusIcon.Glyph = "\uE711";
            GitHubStatusIcon.Foreground = new SolidColorBrush(Colors.Tomato);
            GitHubStatusText.Text = LocalizationService.Get("Account_NotConnectedGH");
            GitHubStatusBadge.Background =
                (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];

            GitHubLoginPanel.Visibility = Visibility.Visible;
            GitHubDisconnectMainBtn.Visibility = Visibility.Collapsed;
            GitHubTokenBox.Password = "";
            GitHubConnectBtn.Visibility = Visibility.Visible;
            GitHubDisconnectBtn.Visibility = Visibility.Collapsed;
        }



        private async void StartDeviceFlow_Click(object sender, RoutedEventArgs e)
        {
            StartDeviceFlowBtn.IsEnabled = false;
            StartDeviceFlowBtn.Content = LocalizationService.Get("Account_Fetching");

            var codeRes = await GitHubDeviceFlowService.RequestDeviceCodeAsync();

            if (codeRes == null)
            {
                StartDeviceFlowBtn.IsEnabled = true;
                StartDeviceFlowBtn.Content = LocalizationService.Get("Account_StartDeviceFlow");
                await ShowDialog(LocalizationService.Get("Common_Error"), LocalizationService.Get("Account_DeviceCodeFailed"));
                return;
            }


            _deviceVerificationUri = codeRes.verification_uri;
            _deviceUserCode = codeRes.user_code;
            DeviceUserCodeText.Text = codeRes.user_code;
            CopyDeviceCodeBtn.Content = LocalizationService.Get("Account_CopyCode");
            DeviceCodePanel.Visibility = Visibility.Visible;
            CancelDeviceFlowBtn.Visibility = Visibility.Visible;
            StartDeviceFlowBtn.Visibility = Visibility.Collapsed;


            Process.Start(new ProcessStartInfo(codeRes.verification_uri) { UseShellExecute = true });


            _deviceFlowCts = new CancellationTokenSource();
            DeviceFlowStatusText.Text = LocalizationService.Get("Account_WaitingAuth");
            DeviceFlowProgress.IsActive = true;

            var result = await GitHubDeviceFlowService.PollForTokenAsync(
                codeRes.device_code, codeRes.interval, _deviceFlowCts.Token);

            if (result.Success)
            {

                var (ok, userName) = await GitHubAuthService.VerifyTokenAsync(result.AccessToken);
                var config = ConfigService.Load();
                config.GitHubToken = result.AccessToken;
                config.GitHubLoginMethod = "device";
                config.GitHubUserName = ok ? userName : "";
                ConfigService.Save(config);

                LogService.Info("AccountPage", $"GitHub Device Flow ログイン成功: @{config.GitHubUserName}");
                SetGitHubConnected(string.Format(LocalizationService.Get("Account_ConnectedGH"), $"@{config.GitHubUserName}"));
            }
            else
            {

                DeviceCodePanel.Visibility = Visibility.Collapsed;
                StartDeviceFlowBtn.Visibility = Visibility.Visible;
                StartDeviceFlowBtn.IsEnabled = true;
                StartDeviceFlowBtn.Content = LocalizationService.Get("Account_StartDeviceFlow");
                CancelDeviceFlowBtn.Visibility = Visibility.Collapsed;

                if (!result.Error.Contains("キャンセル"))
                    await ShowDialog(LocalizationService.Get("Account_LoginFailed"), result.Error);
            }
        }

        private void CancelDeviceFlow_Click(object sender, RoutedEventArgs e)
        {
            _deviceFlowCts?.Cancel();
            DeviceCodePanel.Visibility = Visibility.Collapsed;
            StartDeviceFlowBtn.Visibility = Visibility.Visible;
            StartDeviceFlowBtn.IsEnabled = true;
            StartDeviceFlowBtn.Content = LocalizationService.Get("Account_StartDeviceFlow");
            CancelDeviceFlowBtn.Visibility = Visibility.Collapsed;
            DeviceFlowStatusText.Text = "";
        }

        private void CopyDeviceCode_Click(object sender, RoutedEventArgs e)
        {
            var dp = new DataPackage();
            dp.SetText(_deviceUserCode);
            Clipboard.SetContent(dp);
            CopyDeviceCodeBtn.Content = LocalizationService.Get("Account_CopiedCode");
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
                await ShowDialog(LocalizationService.Get("Account_InputError"), LocalizationService.Get("Account_TokenRequired"));
                return;
            }

            GitHubConnectBtn.IsEnabled = false;
            GitHubConnectBtn.Content = LocalizationService.Get("Account_Verifying");

            var (ok, result) = await GitHubAuthService.VerifyTokenAsync(token);

            if (ok)
            {
                var config = ConfigService.Load();
                config.GitHubToken = token;
                config.GitHubLoginMethod = "pat";
                config.GitHubUserName = result;
                ConfigService.Save(config);
                LogService.Info("AccountPage", $"GitHub PAT 接続成功: @{result}");
                SetGitHubConnected(string.Format(LocalizationService.Get("Account_ConnectedGH"), $"@{result}"));
            }
            else
            {
                LogService.Warn("AccountPage", $"GitHub PAT 接続失敗: {result}");
                GitHubConnectBtn.IsEnabled = true;
                GitHubConnectBtn.Content = LocalizationService.Get("Account_GitHubConnect");
                await ShowDialog(LocalizationService.Get("Account_ConnectFailed"), result);
            }
        }

        private async void GitHubDisconnect_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ContentDialog
            {
                Title = LocalizationService.Get("Account_GitHubDisconnectTitle"),
                Content = LocalizationService.Get("Account_GitHubDisconnectContent"),
                PrimaryButtonText = LocalizationService.Get("Account_GitHubDisconnectPrimary"),
                CloseButtonText = LocalizationService.Get("Common_Cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            var config = ConfigService.Load();
            config.GitHubToken = "";
            config.GitHubLoginMethod = "";
            config.GitHubUserName = "";
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
            SteamStatusIcon.Glyph = "\uE73E";
            SteamStatusIcon.Foreground = new SolidColorBrush(Colors.SeaGreen);
            SteamStatusText.Text = string.Format(LocalizationService.Get("Account_ConnectedSteam"), userName);
            SteamStatusBadge.Background =
                (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBackgroundBrush"];

            SteamUserNameText.Text = userName;
            SteamIdText.Text = $"SteamID: {steamId}";
            SteamProfileLink.NavigateUri = new Uri($"https://steamcommunity.com/profiles/{steamId}");

            SteamProfilePanel.Visibility = Visibility.Visible;
            SteamLoginPanel.Visibility = Visibility.Collapsed;
            SteamDisconnectBtn.Visibility = Visibility.Visible;
        }

        private void SetSteamDisconnected()
        {
            SteamStatusIcon.Glyph = "\uE711";
            SteamStatusIcon.Foreground = new SolidColorBrush(Colors.Tomato);
            SteamStatusText.Text = LocalizationService.Get("Account_NotConnectedSteam");
            SteamStatusBadge.Background =
                (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];

            SteamProfilePanel.Visibility = Visibility.Collapsed;
            SteamLoginPanel.Visibility = Visibility.Visible;
            SteamDisconnectBtn.Visibility = Visibility.Collapsed;
            SteamLoginBtn.Visibility = Visibility.Visible;
            SteamCancelBtn.Visibility = Visibility.Collapsed;
            SteamWaitingPanel.Visibility = Visibility.Collapsed;
            SteamLoginBtn.IsEnabled = true;
            SteamLoginBtn.Content = LocalizationService.Get("Account_SteamLoginBtn");
        }

        private async void SteamLogin_Click(object sender, RoutedEventArgs e)
        {
            SteamLoginBtn.IsEnabled = false;
            SteamLoginBtn.Content = LocalizationService.Get("Account_SteamOpeningBrowser");
            SteamWaitingPanel.Visibility = Visibility.Visible;
            SteamCancelBtn.Visibility = Visibility.Visible;

            _steamCts = new CancellationTokenSource();
            var result = await SteamOpenIdService.AuthenticateAsync(_steamCts.Token);

            if (result.Success)
            {
                SteamStatusText.Text = LocalizationService.Get("Account_SteamFetchingName");
                string userName = await SteamOpenIdService.FetchUserNameAsync(result.SteamId);

                var config = ConfigService.Load();
                config.SteamUserId = result.SteamId;
                config.SteamUserName = userName;
                ConfigService.Save(config);

                LogService.Info("AccountPage", $"Steam OpenID ログイン成功: {userName} ({result.SteamId})");
                SetSteamConnected(userName, result.SteamId);
            }
            else
            {
                SetSteamDisconnected();
                if (!result.Error.Contains("キャンセル"))
                    await ShowDialog(LocalizationService.Get("Account_LoginFailed"), result.Error);
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
                Title = LocalizationService.Get("Account_SteamDisconnectTitle"),
                Content = LocalizationService.Get("Account_SteamDisconnectContent"),
                PrimaryButtonText = LocalizationService.Get("Account_SteamDisconnectPrimary"),
                CloseButtonText = LocalizationService.Get("Common_Cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            var config = ConfigService.Load();
            config.SteamUserId = "";
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
            EpicStatusIcon.Glyph = "\uE73E";
            EpicStatusIcon.Foreground = new SolidColorBrush(Colors.SeaGreen);
            EpicStatusText.Text = string.Format(LocalizationService.Get("Account_EpicLoggedIn"), displayName);
            EpicStatusBadge.Background =
                (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBackgroundBrush"];

            EpicDisplayNameText.Text = displayName;
            EpicAccountIdText.Text = $"Account ID: {accountId}";

            EpicProfilePanel.Visibility = Visibility.Visible;
            EpicLoginPanel.Visibility = Visibility.Collapsed;
            EpicLogoutBtn.Visibility = Visibility.Visible;
        }

        private void SetEpicDisconnected()
        {
            EpicStatusIcon.Glyph = "\uE711";
            EpicStatusIcon.Foreground = new SolidColorBrush(Colors.Tomato);
            EpicStatusText.Text = LocalizationService.Get("Account_EpicNotLoggedIn");
            EpicStatusBadge.Background =
                (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];

            EpicProfilePanel.Visibility = Visibility.Collapsed;
            EpicLoginPanel.Visibility = Visibility.Visible;
            EpicLogoutBtn.Visibility = Visibility.Collapsed;
            EpicLoginProgressPanel.Visibility = Visibility.Collapsed;
            EpicLoginBtn.Visibility = Visibility.Visible;
            EpicLoginBtn.IsEnabled = true;
            EpicLoginBtn.Content = LocalizationService.Get("Account_EpicLoginBtn");
        }

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

        private async void EpicLogout_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ContentDialog
            {
                Title = LocalizationService.Get("Account_EpicLogoutTitle"),
                Content = LocalizationService.Get("Account_EpicLogoutContent"),
                PrimaryButtonText = LocalizationService.Get("Account_EpicLogoutPrimary"),
                CloseButtonText = LocalizationService.Get("Common_Cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            EpicLoginService.Logout();
            LogService.Info("AccountPage", "Epic ログアウト");
            SetEpicDisconnected();
        }





        private async void CheckRateLimit_Click(object sender, RoutedEventArgs e)
        {
            RateLimitRemaining.Text = LocalizationService.Get("Account_RateLimitFetching");
            RateLimitMax.Text = LocalizationService.Get("Account_RateLimitFetching");
            RateLimitResetText.Text = "";

            try
            {
                var client = GitHubAuthService.GetClient();
                var response = await client.GetAsync("https://api.github.com/rate_limit");
                if (!response.IsSuccessStatusCode)
                {
                    RateLimitRemaining.Text = LocalizationService.Get("Account_RateLimitError");
                    RateLimitMax.Text = "--";
                    return;
                }

                var data = await response.Content.ReadFromJsonAsync<RateLimitResponse>();
                if (data?.rate == null) return;

                int remaining = data.rate.remaining;
                int limit = data.rate.limit;
                long reset = data.rate.reset;

                RateLimitRemaining.Text = remaining.ToString("N0");
                RateLimitMax.Text = limit.ToString("N0");
                RateLimitBar.Maximum = limit;
                RateLimitBar.Value = remaining;

                var resetTime = DateTimeOffset.FromUnixTimeSeconds(reset).ToLocalTime();
                RateLimitResetText.Text = string.Format(LocalizationService.Get("Account_RateLimitReset"), resetTime.ToString("HH:mm:ss"));

                LogService.Info("AccountPage", $"GitHub API 残り: {remaining}/{limit}, リセット: {resetTime:HH:mm}");
            }
            catch (Exception ex)
            {
                RateLimitRemaining.Text = LocalizationService.Get("Account_RateLimitError");
                RateLimitMax.Text = "--";
                LogService.Error("AccountPage", "レートリミット確認エラー", ex);
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

        private class RateLimitResponse
        {
            public RateInfo? rate { get; set; }
        }
        private class RateInfo
        {
            public int remaining { get; set; }
            public int limit { get; set; }
            public long reset { get; set; }
        }
    }
}
