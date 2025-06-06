using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Among_Us_ModManeger.Pages.TownOfHostK;  

namespace Among_Us_ModManeger.Pages
{
    public partial class ModSelectPage : Page
    {
        public ModSelectPage()
        {
            InitializeComponent();
        }

        private void BackToMain_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainMenuPage());
        }

        private void TownOfHostK_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Among_Us_ModManeger.Pages.TownOfHostK.TownOfHostK());
        }

        private void OpenUrlInput_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("ここでGitHubのURL入力画面に進みます（未実装）。");
        }
    }
}
