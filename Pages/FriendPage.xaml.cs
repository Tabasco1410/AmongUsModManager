using AmongUsModManager.Models.Services;
using Microsoft.UI.Xaml.Controls;

namespace AmongUsModManager.Pages
{
    public sealed partial class FriendPage : Page
    {
        public FriendPage()
        {
            this.InitializeComponent();
            ApplyStrings();
            LocalizationService.LanguageChanged += ApplyStrings;
            this.Unloaded += (_, _) => LocalizationService.LanguageChanged -= ApplyStrings;
        }

        private void ApplyStrings()
        {
            FriendPageTitle.Text    = LocalizationService.Get("Friend_Title");
            FriendPageSubtitle.Text = LocalizationService.Get("Common_UnderConstructionPage");
        }
    }
}
