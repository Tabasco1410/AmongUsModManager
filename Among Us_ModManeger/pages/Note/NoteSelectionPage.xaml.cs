using Among_Us_ModManeger.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Among_Us_ModManeger
{
    public partial class NoteSelectionPage : Page
    {
        private static readonly HttpClient Client = new();

        public NoteSelectionPage()
        {
            InitializeComponent();
            _ = LoadNotesAsync();
        }

        private async Task LoadNotesAsync()
        {
            try
            {
                const string jsonUrl = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/Note/NoteItem/NoteList.json\r\n";
                var json = await Client.GetStringAsync(jsonUrl);
                var notes = JsonSerializer.Deserialize<List<NoteItem>>(json);
                NoteListBox.ItemsSource = notes;
            }
            catch (Exception ex)
            {
                // エラーハンドリング（必要に応じてUI表示など）
                System.Diagnostics.Debug.WriteLine("Note list load failed: " + ex.Message);
            }
        }

        private void NoteListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NoteListBox.SelectedItem is NoteItem note)
            {
                NavigationService.Navigate(new NoteDisplayPage(note));
            }
        }

        private void Back_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
    }
}
