using Among_Us_ModManager.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Among_Us_ModManager.pages.Note;

namespace Among_Us_ModManager
{
    public partial class NoteSelectionPage : Page
    {
        private static readonly HttpClient Client = new();

        public NoteSelectionPage()
        {
            InitializeComponent();
            _ = LoadNotesAsync();   // 非同期ロード開始
        }

        /// <summary>
        /// NoteItem の一覧を GitHub から取得して ListBox にバインド
        /// </summary>
        private async Task LoadNotesAsync()
        {
            try
            {
                const string jsonUrl =
                    "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManager/main/Among%20Us_ModManager/pages/Note/NoteItem/NoteItem.json";

                var json = await Client.GetStringAsync(jsonUrl);
                var notes = JsonSerializer.Deserialize<List<NoteItem>>(json);
                NoteListBox.ItemsSource = notes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Note list load failed: " + ex.Message);
                MessageBox.Show("ノート一覧の取得に失敗しました。", "エラー",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ListBox でノートを選択したとき
        /// </summary>
        private async void NoteListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NoteListBox.SelectedItem is not NoteItem note) return;

            // 1) ポップアップを表示
            var popup = new LoadingPopup
            {
                Owner = Window.GetWindow(this)      // 親ウィンドウを指定
            };
            popup.Show();

            try
            {
                // 2) UI スレッドに描画を完了させる
                await Task.Yield();   // ← これで popup が描画される

                // 3) 必要なら重い前処理をここで実行（ファイル IO など）
                //    await SomeHeavyWorkAsync();

                // 4) ページ遷移
                NavigationService.Navigate(new NoteDisplayPage(note));
            }
            finally
            {
                // 5) 終わったら閉じる
                popup.Close();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
    }
}