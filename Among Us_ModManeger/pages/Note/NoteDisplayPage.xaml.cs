using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Diagnostics; // Process.Startのために必要

namespace Among_Us_ModManeger
{
    /// <summary>
    /// 選択されたノート記事の内容を表示するページ
    /// </summary>
    public partial class NoteDisplayPage : Page
    {
        private NoteArticle _currentArticle;

        /// <summary>
        /// NoteDisplayPageのコンストラクタ
        /// </summary>
        /// <param name="article">表示するNoteArticleオブジェクト</param>
        public NoteDisplayPage(NoteArticle article)
        {
            InitializeComponent();
            _currentArticle = article;
            ArticleTitleTextBlock.Text = _currentArticle.Title; // タイトルを設定

            if (_currentArticle == null || string.IsNullOrEmpty(_currentArticle.ContentHtml))
            {
                // 記事コンテンツがない場合の表示
                ArticleWebBrowser.NavigateToString("<div style='font-family: Arial, sans-serif; padding: 20px; text-align: center; color: gray;'>記事の内容がありません。</div>");
                Debug.WriteLine("DEBUG: NoteDisplayPage: _currentArticle または ContentHtml が null/空です。");
                return;
            }

            try
            {
                // WebBrowserでHTMLコンテンツを表示
                ArticleWebBrowser.NavigateToString(_currentArticle.ContentHtml);
                Debug.WriteLine("DEBUG: NoteDisplayPage: HTMLコンテンツが正常に NavigateToString に渡されました。");
            }
            catch (Exception ex)
            {
                // NavigateToStringでエラーが発生した場合の処理
                string errorMessage = $"記事の表示中にエラーが発生しました: {ex.Message}";
                ArticleWebBrowser.NavigateToString($"<div style='font-family: Arial, sans-serif; padding: 20px; color: red;'>{errorMessage}</div>");
                MessageBox.Show(errorMessage, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"ERROR: NoteDisplayPage: NavigateToString で例外が発生しました: {ex.Message}");
            }

            // もし直接note.comのURLにナビゲートしたい場合は以下を使用します
            // （ただし、サイトの構造変更やWebスクレイピングの制約に注意が必要です）
            // ArticleWebBrowser.Navigate(_currentArticle.SourceUrl);
        }

        /// <summary>
        /// 「戻る」ボタンのクリックイベントハンドラ。前のページに戻ります。
        /// </summary>
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        /// <summary>
        /// 「ブラウザで開く」ボタンのクリックイベントハンドラ。
        /// 現在の記事の元のURLをデフォルトのウェブブラウザで開きます。
        /// </summary>
        private void OpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentArticle.SourceUrl))
            {
                try
                {
                    // デフォルトブラウザでURLを開く
                    // UseShellExecute = true が必要
                    Process.Start(new ProcessStartInfo(_currentArticle.SourceUrl) { UseShellExecute = true });
                    Debug.WriteLine($"DEBUG: NoteDisplayPage: URL '{_currentArticle.SourceUrl}' をブラウザで開きます。");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ブラウザで開く際にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine($"ERROR: NoteDisplayPage: ブラウザで開く際に例外が発生しました: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("開くべきURLが指定されていません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                Debug.WriteLine("DEBUG: NoteDisplayPage: SourceUrl が null/空のためブラウザで開けません。");
            }
        }
    }
}
