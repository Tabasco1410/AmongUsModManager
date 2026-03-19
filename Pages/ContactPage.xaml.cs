using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AmongUsModManager.Pages
{
    public sealed partial class ContactPage : Page
    {
        
        private const string DiscordUrl = "https://discord.com/invite/nFhkYmf9At";

        public ContactPage()
        {
            this.InitializeComponent();
        }

        private void DiscordBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(DiscordUrl) { UseShellExecute = true });
        }
    }
}
