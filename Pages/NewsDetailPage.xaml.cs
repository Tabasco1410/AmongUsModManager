using System;
using System.Net.Http;
using System.Text.RegularExpressions;
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

            TitleText.Text = item.Title;
            DateText.Text = FormatDate(item.Date);

            if (!string.IsNullOrEmpty(item.ContentFile))
            {
                // ContentFile がある場合はHTTP取得してプレーンテキスト表示
                ContentText.Text = string.Empty;
                ContentText.Visibility = Visibility.Visible;
                ImageList.ItemsSource = null;
                _ = LoadPlainTextAsync(item.ContentFile);
            }
            else
            {
                // 通常テキスト表示
                ContentText.Text = item.Content ?? string.Empty;
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

        private async Task LoadPlainTextAsync(string contentFile)
        {
            LoadingRing.IsActive = true;

            try
            {
                string url = NewsBaseUrl + contentFile;
                string text = await _http.GetStringAsync(url);

                // Markdown・HTMLタグを除去してプレーンテキスト化
                string plain = StripMarkdownAndHtml(text);

                ContentText.Text = plain;
                LogService.Info("NewsDetailPage", $"コンテンツ読み込み完了: {contentFile}");
            }
            catch (Exception ex)
            {
                LogService.Error("NewsDetailPage", $"コンテンツ読み込み失敗: {contentFile}", ex);
                ContentText.Text = $"⚠ コンテンツの読み込みに失敗しました。\n{ex.Message}";
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        /// <summary>
        /// Markdown記法とHTMLタグを除去してプレーンテキストを返す。
        /// </summary>
        private static string StripMarkdownAndHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            string s = input;

            // HTMLタグを除去
            s = Regex.Replace(s, "<[^>]+>", string.Empty);
            // HTML エンティティ（簡易）
            s = s.Replace("&amp;", "&")
                 .Replace("&lt;", "<")
                 .Replace("&gt;", ">")
                 .Replace("&quot;", "\"")
                 .Replace("&#39;", "'")
                 .Replace("&nbsp;", " ");

            // 見出し記号 (# ## ###...)
            s = Regex.Replace(s, @"^#{1,6}\s+", string.Empty, RegexOptions.Multiline);
            // 太字・斜体 (**text**, *text*, __text__, _text_)
            s = Regex.Replace(s, @"\*{1,3}(.+?)\*{1,3}", "$1");
            s = Regex.Replace(s, @"_{1,3}(.+?)_{1,3}", "$1");
            // インラインコード (`code`)
            s = Regex.Replace(s, @"`(.+?)`", "$1");
            // コードブロック (```...```)
            s = Regex.Replace(s, @"```[\s\S]*?```", string.Empty);
            // 画像 ![alt](url) → alt テキストのみ
            s = Regex.Replace(s, @"!\[([^\]]*)\]\([^\)]*\)", "$1");
            // リンク [text](url) → text のみ
            s = Regex.Replace(s, @"\[([^\]]*)\]\([^\)]*\)", "$1");
            // 水平線 (--- / ***)
            s = Regex.Replace(s, @"^[-\*]{3,}\s*$", string.Empty, RegexOptions.Multiline);
            // 引用記号 (> text)
            s = Regex.Replace(s, @"^>\s?", string.Empty, RegexOptions.Multiline);
            // リスト記号 (- item / * item / 1. item)
            s = Regex.Replace(s, @"^[\*\-\+]\s+", string.Empty, RegexOptions.Multiline);
            s = Regex.Replace(s, @"^\d+\.\s+", string.Empty, RegexOptions.Multiline);

            // 連続する空行を最大2行に圧縮
            s = Regex.Replace(s, @"\n{3,}", "\n\n");

            return s.Trim();
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
