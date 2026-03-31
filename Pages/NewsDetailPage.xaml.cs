using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AmongUsModManager.Models;
using AmongUsModManager.Services;

namespace AmongUsModManager.Pages
{
    public sealed partial class NewsDetailPage : Page
    {
        private static readonly HttpClient _http = new HttpClient();
        private const string NewsBaseUrl = "https://amongusmodmanager.web.app/News/";

        static NewsDetailPage()
        {
            _http.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager-App");
        }

        public NewsDetailPage() => this.InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is not NewsItem item) return;

            LogService.Info("NewsDetailPage", $"お知らせ詳細表示: {item.Title}");

            if (!string.IsNullOrEmpty(item.ContentFile))
            {
                TextScrollViewer.Visibility  = Visibility.Collapsed;
                WebViewContainer.Visibility  = Visibility.Visible;
                WebTitleText.Text = item.Title;
                WebDateText.Text  = FormatDate(item.Date);
                _ = LoadRichContentAsync(item.ContentFile);
            }
            else
            {
                TextScrollViewer.Visibility  = Visibility.Visible;
                WebViewContainer.Visibility  = Visibility.Collapsed;
                TitleText.Text   = item.Title;
                DateText.Text    = FormatDate(item.Date);
                ContentText.Text = item.Content;
                ContentText.Visibility = Visibility.Visible;

                if (item.Images != null && item.Images.Count > 0)
                {
                    ImageList.ItemsSource = item.Images;
                    LogService.Debug("NewsDetailPage", $"添付画像 {item.Images.Count} 件");
                }
                else
                {
                    ImageList.ItemsSource = null;
                }
            }
        }

        private async Task LoadRichContentAsync(string contentFile)
        {
            LoadingRing.IsActive = true;
            try
            {
                string url  = NewsBaseUrl + contentFile;
                string text = await _http.GetStringAsync(url);

                bool isDarkTheme = Application.Current.RequestedTheme == ApplicationTheme.Dark;
                string html;

                if (contentFile.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                {
                    html = MarkdownHelper.ToHtml(text, isDarkTheme);
                }
                else
                {
                    html = InjectBaseStyle(text, isDarkTheme);
                }

                await ContentWebView.EnsureCoreWebView2Async();
                ContentWebView.NavigateToString(html);
                LogService.Info("NewsDetailPage", $"リッチコンテンツ読み込み完了: {contentFile}");
            }
            catch (Exception ex)
            {
                LogService.Error("NewsDetailPage", $"リッチコンテンツ読み込み失敗: {contentFile}", ex);
                await ContentWebView.EnsureCoreWebView2Async();
                ContentWebView.NavigateToString(
                    "<html><body style='color:#e0e0e0;background:#1f1f1f;font-family:Segoe UI;padding:20px'>" +
                    "<p>⚠ コンテンツの読み込みに失敗しました。</p>" +
                    $"<p style='color:gray;font-size:12px'>{ex.Message}</p>" +
                    "</body></html>");
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        private static string InjectBaseStyle(string html, bool isDarkTheme)
        {
            string bgColor   = isDarkTheme ? "#1f1f1f" : "#ffffff";
            string textColor = isDarkTheme ? "#e0e0e0" : "#1a1a1a";
            string linkColor = isDarkTheme ? "#60cdff" : "#0067c0";
            string theme     = isDarkTheme ? "dark"    : "light";

            string style =
                $"<meta name=\"color-scheme\" content=\"{theme}\"/>" +
                "<style>" +
                $"body{{font-family:'Yu Gothic UI','Segoe UI',sans-serif;font-size:14px;line-height:1.7;" +
                $"background:{bgColor};color:{textColor};padding:16px;word-break:break-word;}}" +
                $"a{{color:{linkColor};}}" +
                "img{max-width:100%;border-radius:6px;margin:8px 0;}" +
                "video{max-width:100%;border-radius:6px;margin:8px 0;}" +
                "</style>";

            if (html.Contains("<head>", StringComparison.OrdinalIgnoreCase))
                return html.Replace("<head>", "<head>" + style, StringComparison.OrdinalIgnoreCase);
            if (html.Contains("<html>", StringComparison.OrdinalIgnoreCase))
                return html.Replace("<html>", "<html><head>" + style + "</head>", StringComparison.OrdinalIgnoreCase);
            return "<head>" + style + "</head>" + html;
        }

        private static string FormatDate(string raw)
        {
            if (DateTime.TryParse(raw, out var dt))
                return dt.ToString("yyyy年M月d日");
            return raw;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => this.Frame.GoBack();
    }
}
