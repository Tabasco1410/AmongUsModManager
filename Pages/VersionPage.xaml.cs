using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AmongUsModManager.Models.Services;

namespace AmongUsModManager.Pages
{
    public sealed partial class VersionPage : Page
    {
        public VersionPage()
        {
            this.InitializeComponent();
            this.Loaded += (s, e) => _ = LoadVersionsAsync();
        }

        public static Visibility BoolToVisibility(bool value)
            => value ? Visibility.Visible : Visibility.Collapsed;

        private async System.Threading.Tasks.Task LoadVersionsAsync()
        {
            LoadingRing.Visibility = Visibility.Visible;
            try
            {
                var history = await AppUpdateService.GetVersionHistoryAsync();
                VersionListView.ItemsSource = history;
            }
            finally
            {
                LoadingRing.Visibility = Visibility.Collapsed;
            }
        }

        private static string MarkdownToPlainText(string markdown)
        {
            var text = markdown;
            // 見出し (#, ##, ###)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"^#{1,6}\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            // 太字・斜体 (**bold**, *italic*, __bold__, _italic_)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\*{1,2}(.+?)\*{1,2}", "$1");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"_{1,2}(.+?)_{1,2}", "$1");
            // インラインコード (`code`)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"`(.+?)`", "$1");
            // コードブロック (```...```)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"```[\s\S]*?```", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            // リンク ([text](url))
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\[(.+?)\]\(.+?\)", "$1");
            // 箇条書き記号 (-, *, +)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"^[-*+]\s+", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            // 番号リスト (1. 2.)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"^\d+\.\s+", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            // 水平線 (---, ***)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"^[-*]{3,}\s*$", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            // 引用 (>)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"^>\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            return text.Trim();
        }

        private async void ReleaseNotes_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UpdateResult result)
            {
                string raw = string.IsNullOrWhiteSpace(result.ReleaseNotes)
                    ? "リリースノートはありません。"
                    : result.ReleaseNotes;

                string plain;
                try { plain = MarkdownToPlainText(raw); }
                catch { plain = raw; }

                var textBlock = new TextBlock
                {
                    Text = plain,
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe UI"),
                    FontSize = 13,
                    LineHeight = 22
                };

                var scrollViewer = new ScrollViewer
                {
                    Content = textBlock,
                    MaxHeight = 450,
                    HorizontalScrollMode = ScrollMode.Disabled
                };

                ContentDialog dialog = new ContentDialog
                {
                    Title = $"{result.LatestTag} リリースノート",
                    Content = scrollViewer,
                    CloseButtonText = "閉じる",
                    SecondaryButtonText = "GitHubで開く",
                    XamlRoot = this.XamlRoot
                };

                var dialogResult = await dialog.ShowAsync();
                if (dialogResult == ContentDialogResult.Secondary && !string.IsNullOrEmpty(result.ReleaseUrl))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(result.ReleaseUrl)
                    {
                        UseShellExecute = true
                    });
                }
            }
        }

        private async void VersionApply_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UpdateResult result)
            {
                btn.IsEnabled = false;

                ContentDialog dialog = new ContentDialog
                {
                    Title = "バージョンの適用",
                    Content = $"{result.LatestTag} をダウンロードして適用します。完了後にアプリは再起動します。よろしいですか？",
                    PrimaryButtonText = "はい",
                    CloseButtonText = "キャンセル",
                    XamlRoot = this.XamlRoot
                };

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    bool success = await AppUpdateService.DownloadAndApplyAsync(result);
                    if (!success)
                    {
                        btn.IsEnabled = true;
                    }
                }
                else
                {
                    btn.IsEnabled = true;
                }
            }
        }
    }
}
