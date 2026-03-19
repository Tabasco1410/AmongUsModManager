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
            LoadStats();
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
                Title = "履歴のクリア",
                Content = "起動履歴をすべて削除しますか？",
                PrimaryButtonText = "削除",
                CloseButtonText = "キャンセル",
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
