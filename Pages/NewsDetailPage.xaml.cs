using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AmongUsModManager.Models;

namespace AmongUsModManager.Pages
{
    public sealed partial class NewsDetailPage : Page
    {
        public NewsDetailPage() => this.InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is NewsItem item)
            {
                TitleText.Text = item.Title;
                DateText.Text = item.Date;
                ContentText.Text = item.Content; 
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => this.Frame.GoBack();
    }
}
