using AmongUsModManager.Models.Services;
using Microsoft.UI.Xaml.Controls;

namespace AmongUsModManager.Pages
{
    public sealed partial class ScreenshotPage : Page
    {
        public ScreenshotPage()
        {
            this.InitializeComponent();
            ApplyStrings();
            LocalizationService.LanguageChanged += ApplyStrings;
            this.Unloaded += (_, _) => LocalizationService.LanguageChanged -= ApplyStrings;
        }

        private void ApplyStrings()
        {
            ScreenshotPageTitle.Text    = LocalizationService.Get("Screenshot_Title");
            ScreenshotPageSubtitle.Text = LocalizationService.Get("Common_UnderConstructionPage");
        }
    }
}
