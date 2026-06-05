using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AmongUsModManager.Models.Services;

namespace AmongUsModManager.Pages
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
            ApplyStrings();
            LocalizationService.LanguageChanged += ApplyStrings;
            this.Unloaded += (_, _) => LocalizationService.LanguageChanged -= ApplyStrings;
        }

        private void ApplyStrings()
        {
            AboutPageTitle.Text        = LocalizationService.Get("About_Title");
            AboutVersionLabel.Text     = LocalizationService.Get("About_VersionLabel");
            AboutAppDesc.Text          = LocalizationService.Get("About_AppDesc");
            WebsiteBtn.Content         = LocalizationService.Get("About_Website");
            GitHubBtn.Content          = "GitHub";
            ChangelogBtn.Content       = LocalizationService.Get("About_Changelog");
            AboutLicenseLabel.Text     = LocalizationService.Get("About_License");
            AboutLibrariesLabel.Text   = LocalizationService.Get("About_Libraries");
            AboutDisclaimerLabel.Text  = LocalizationService.Get("About_Disclaimer");
            AboutDisclaimerText.Text   = LocalizationService.Get("About_DisclaimerText");
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
