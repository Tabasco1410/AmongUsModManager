using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Among_Us_ModManeger
{
    /// <summary>
    /// ノート記事の情報を保持するデータ構造
    /// </summary>
    public class NoteArticle
    {
        public string Title { get; set; } // 記事のタイトル
        public string ContentHtml { get; set; } // 記事のHTMLコンテンツ（WebBrowser表示用）
        public string SourceUrl { get; set; } // 元のnote.comのURL
    }

    /// <summary>
    /// ユーザーに読む記事を選択させるページ
    /// </summary>
    public partial class NoteSelectionPage : Page
    {
        private ObservableCollection<NoteArticle> _noteArticles;

        public NoteSelectionPage()
        {
            InitializeComponent();

            // 模擬的なノート記事データを読み込む
            _noteArticles = new ObservableCollection<NoteArticle>(GetMockNoteArticles());
            NoteListBox.ItemsSource = _noteArticles;
        }

        /// <summary>
        /// 模擬的なノート記事データを作成します。
        /// 実際には、note.comのAPI（もしあれば）や、手動で変換したローカルファイルから読み込むことを検討してください。
        /// Webスクレイピングは推奨されません。
        /// </summary>
        /// <returns>ノート記事のリスト</returns>
        private List<NoteArticle> GetMockNoteArticles()
        {
            return new List<NoteArticle>
            {
                new NoteArticle
                {
                    // タイトルを正しいものに修正
                    Title = "Among Us/アモングアスの「MOD」ってなに？使用する際の注意点や導入方法まで解説",
                    ContentHtml = "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"></head><body>" +
                                  "<div style='font-family: Arial, sans-serif; padding: 15px;'>" +
                                  "<h2 style='color: #333;'>Among Us/アモングアスの「MOD」ってなに？使用する際の注意点や導入方法まで解説</h2>" +
                                  "<p>この記事では、Among UsのMODとは何か、その使用における注意点や具体的な導入方法までを詳しく解説しています。</p>" +
                                  "<p>MODを安全に、そして楽しく利用するための基本的な情報が詰まっています。</p>" +
                                  "<p style='margin-top: 20px;'>詳細は<a href='https://note.com/tabasuko_1410/n/n5b7422a1061b' style='color: #007bff; text-decoration: none;'>元の記事（note.com）</a>を参照してください。</p>" +
                                  "</div></body></html>",
                    SourceUrl = "https://note.com/tabasuko_1410/n/n5b7422a1061b"
                },
                new NoteArticle
                {
                    // タイトルを正しいものに修正
                    Title = "Among UsのModの便利な入れ方",
                    ContentHtml = "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"></head><body>" +
                                  "<div style='font-family: Arial, sans-serif; padding: 15px;'>" +
                                  "<h2 style='color: #333;'>Among UsのModの便利な入れ方</h2>" +
                                  "<p>この記事では、Among UsのModを効率的かつ便利に導入するためのヒントと手順を紹介します。</p>" +
                                  "<p>Modの管理をより簡単にするための情報が役立つでしょう。</p>" +
                                  "<p style='margin-top: 20px;'>詳細は<a href='https://note.com/tabasuko_1410/n/nc45036ed4122' style='color: #007bff; text-decoration: none;'>元の記事（note.com）</a>を参照してください。</p>" +
                                  "</div></body></html>",
                    SourceUrl = "https://note.com/tabasuko_1410/n/nc45036ed4122"
                },
                new NoteArticle
                {
                    // タイトルを正しいものに修正
                    Title = "Among Us PC版　トラブルシューティング",
                    ContentHtml = "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"></head><body>" +
                                  "<div style='font-family: Arial, sans-serif; padding: 15px;'>" +
                                  "<h2 style='color: #333;'>Among Us PC版　トラブルシューティング</h2>" +
                                  "<p>Among UsのPC版で発生しうる様々なトラブルやエラーに対する解決策を解説しています。</p>" +
                                  "<p>ゲームが起動しない、動作が不安定などの問題に直面した際に役立つ情報が満載です。</p>" +
                                  "<p style='margin-top: 20px;'>詳細は<a href='https://note.com/tabasuko_1410/n/n6850483312ff' style='color: #007bff; text-decoration: none;'>元の記事（note.com）</a>を参照してください。</p>" +
                                  "</div></body></html>",
                    SourceUrl = "https://note.com/tabasuko_1410/n/n6850483312ff"
                }
            };
        }

        /// <summary>
        /// リストボックスで記事が選択されたときのイベントハンドラ。
        /// 選択された記事のコンテンツを表示するページへナビゲートします。
        /// </summary>
        private void NoteListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NoteListBox.SelectedItem is NoteArticle selectedArticle)
            {
                NavigationService?.Navigate(new NoteDisplayPage(selectedArticle));
            }
        }

        /// <summary>
        /// 「戻る」ボタンのクリックイベントハンドラ。前のページに戻ります。
        /// </summary>
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }
    }
}
