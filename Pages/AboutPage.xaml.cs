using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AmongUsModManager.Pages
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
        }

        private void WebsiteBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://amongusmodmanager.web.app/") { UseShellExecute = true });
        }

        private void GitHubBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/Tabasco1410/AmongUsModManeger") { UseShellExecute = true });
        }

        private void ChangelogBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/Tabasco1410/AmongUsModManeger/releases") { UseShellExecute = true });
        }
    }
}
