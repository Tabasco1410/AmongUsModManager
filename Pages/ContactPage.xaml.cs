using System.Diagnostics;
using AmongUsModManager.Models.Services;
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
            ApplyStrings();
            LocalizationService.LanguageChanged += ApplyStrings;
            this.Unloaded += (_, _) => LocalizationService.LanguageChanged -= ApplyStrings;
        }

        private void ApplyStrings()
        {
            ContactPageTitle.Text              = LocalizationService.Get("Contact_Title");
            ContactUnderConstructionLabel.Text = LocalizationService.Get("Common_UnderConstruction");
            ContactUnderConstructionDesc.Text  = LocalizationService.Get("Contact_UnderConstructionDesc");
            ContactDiscordLabel.Text           = LocalizationService.Get("Contact_DiscordLabel");
            ContactDiscordSubLabel.Text        = LocalizationService.Get("Contact_DiscordSubLabel");
            DiscordBtn.Content                 = LocalizationService.Get("Contact_OpenDiscord");
        }

        private void DiscordBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(DiscordUrl) { UseShellExecute = true });
        }
    }
}
