using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AmongUsModManager.Models;
using AmongUsModManager.Models.Services;

namespace AmongUsModManager.Pages
{
    public class LaunchHistoryItem
    {
        public string ModName { get; set; } = "";
        public DateTime LaunchedAt { get; set; }
        public string LaunchedAtText => LaunchedAt.ToString("yyyy/MM/dd HH:mm");
    }

    public sealed partial class StatsPage : Page
    {
        public StatsPage()
        {
            this.InitializeComponent();
            ApplyStrings();
            LocalizationService.LanguageChanged += ApplyStrings;
            this.Unloaded += (_, _) => LocalizationService.LanguageChanged -= ApplyStrings;
            LoadStats();
        }

        private void ApplyStrings()
        {
            StatsPageTitle.Text            = LocalizationService.Get("Stats_Title");
            StatsSubtitle.Text             = LocalizationService.Get("Stats_Subtitle");
            LaunchSectionLabel.Text        = LocalizationService.Get("Stats_LaunchSection");
            TotalLaunchLabel.Text          = LocalizationService.Get("Stats_TotalLaunch");
            MonthlyLaunchLabel.Text        = LocalizationService.Get("Stats_MonthlyLaunch");
            RegisteredModLabel.Text        = LocalizationService.Get("Stats_RegisteredMods");
            LastLaunchLabel.Text           = LocalizationService.Get("Stats_LastLaunch");
            HistorySectionLabel.Text       = LocalizationService.Get("Stats_HistorySection");
            HistoryDescLabel.Text          = LocalizationService.Get("Stats_HistoryDesc");
            ClearHistoryBtn.Content        = LocalizationService.Get("Stats_ClearHistory");
            HistoryEmptyText.Text          = LocalizationService.Get("Stats_HistoryEmpty");
            AchievementsSectionLabel.Text  = LocalizationService.Get("Stats_AchievementsSection");
            AchievementsUnderDevLabel.Text = LocalizationService.Get("Common_UnderConstruction");
            AchievementsPendingText.Text   = LocalizationService.Get("Stats_AchievementsPending");
        }

        private void LoadStats()
        {
            var config = ConfigService.Load();

            RegisteredModCount.Text = config.VanillaPaths.Count.ToString();

            if (config.LastLaunchTime.HasValue)
                LastLaunchText.Text = config.LastLaunchTime.Value.ToString("MM/dd HH:mm");
            else
                LastLaunchText.Text = "--";

            var history = LaunchHistoryService.Load();
            TotalLaunchCount.Text = history.Count.ToString();
            MonthlyLaunchCount.Text = history
                .Count(h => h.LaunchedAt.Year == DateTime.Now.Year &&
                            h.LaunchedAt.Month == DateTime.Now.Month)
                .ToString();

            LaunchHistoryView.ItemsSource = history
                .OrderByDescending(h => h.LaunchedAt)
                .Take(50)
                .ToList();
            HistoryEmptyText.Visibility = history.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = LocalizationService.Get("Stats_ClearConfirmTitle"),
                Content = LocalizationService.Get("Stats_ClearConfirmContent"),
                PrimaryButtonText = LocalizationService.Get("Common_Delete"),
                CloseButtonText = LocalizationService.Get("Common_Cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                LaunchHistoryService.Clear();
                LoadStats();
            }
        }
    }
}
