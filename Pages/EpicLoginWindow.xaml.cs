using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using AmongUsModManager.Models.Services;
using AmongUsModManager.Services;

namespace AmongUsModManager
{
    public sealed partial class EpicLoginWindow : Window
    {
        private const string EpicRedirectPath = "/id/api/redirect";
        private const string ExtractCodeJs = """
            (function() {
              if (window.__AMM_EXTRACTED__) return;
              window.__AMM_EXTRACTED__ = true;
              try {
                var bodyText = document.body.innerText;
                if (!bodyText.includes("authorizationCode")) return;
                var json = JSON.parse(bodyText);
                if (json.authorizationCode) {
                  window.chrome.webview.postMessage(json.authorizationCode);
                }
              } catch (_) {}
            })();
            """;
        private readonly Action<EpicLoginResult> _onComplete;
        private int _handled   = 0;
        private int _completed = 0;

        public EpicLoginWindow(Action<EpicLoginResult> onComplete)
        {
            this.InitializeComponent();
            _onComplete = onComplete;

            this.Title  = "Login to Epic Games";
            this.Closed += EpicLoginWindow_Closed;
            _ = InitializeWebViewAsync();
        }
        private async Task InitializeWebViewAsync()
        {
            try
            {
                await LoginWebView.EnsureCoreWebView2Async();
                LoginWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                LoginWebView.Source = new Uri(EpicLoginService.GetAuthUrl());
            }
            catch (Exception ex)
            {
                LogService.Error("EpicLoginWindow", "WebView2 初期化失敗", ex);
                CompleteOnce(EpicLoginResult.Fail($"WebView2 の初期化に失敗しました: {ex.Message}"));
            }
        }
        private void LoginWebView_NavigationStarting(
            WebView2 sender,
            Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri)) return;
            LogService.Info("EpicLoginWindow", $"NavigationStarting: {uri.Host}{uri.AbsolutePath}");
        }
        private async void LoginWebView_NavigationCompleted(
            WebView2 sender,
            Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (LoadingOverlay.Visibility == Visibility.Visible)
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                LoginWebView.Visibility   = Visibility.Visible;
            }

            if (!e.IsSuccess) return;

            if (!Uri.TryCreate(LoginWebView.Source?.ToString(), UriKind.Absolute, out var uri)) return;
            if (uri.AbsolutePath == EpicRedirectPath)
            {
                LogService.Info("EpicLoginWindow", "リダイレクトページ到達。authorizationCode を抽出します。");
                try
                {
                    await LoginWebView.CoreWebView2.ExecuteScriptAsync(ExtractCodeJs);
                }
                catch (Exception ex)
                {
                    LogService.Warn("EpicLoginWindow", $"JS 実行失敗: {ex.Message}");
                }
            }
        }
        private async void CoreWebView2_WebMessageReceived(
            CoreWebView2 sender,
            CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (Interlocked.Exchange(ref _handled, 1) != 0) return;

            string rawCode = e.TryGetWebMessageAsString();
            string code = rawCode.Trim().Replace("\"", "");

            if (string.IsNullOrEmpty(code))
            {
                CompleteOnce(EpicLoginResult.Fail("authorizationCode の取得に失敗しました。"));
                DispatcherQueue.TryEnqueue(Close);
                return;
            }

            LogService.Info("EpicLoginWindow", "authorizationCode 取得成功。トークン交換を開始します。");
            var result = await EpicLoginService.LoginWithAuthCodeAsync(code);
            CompleteOnce(result);
            DispatcherQueue.TryEnqueue(Close);
        }
        private void EpicLoginWindow_Closed(object sender, WindowEventArgs args)
        {
            CompleteOnce(EpicLoginResult.Fail("キャンセルされました。"));
        }
        private void CompleteOnce(EpicLoginResult result)
        {
            if (Interlocked.Exchange(ref _completed, 1) != 0) return;
            _onComplete(result);
        }
    }
}
