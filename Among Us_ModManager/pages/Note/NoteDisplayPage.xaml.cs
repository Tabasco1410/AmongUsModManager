using Among_Us_ModManager.Models;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Among_Us_ModManager
{
    public partial class NoteDisplayPage : Page
    {
        private readonly NoteItem _note;

        public NoteDisplayPage(NoteItem note)
        {
            InitializeComponent();
            _note = note;
            ArticleTitleTextBlock.Text = note.Title;
            ArticleBrowser.Address = note.Url;
        }

        private void OpenInBrowser_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(_note.Url) { UseShellExecute = true });
        }

        private void Back_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
    }
}