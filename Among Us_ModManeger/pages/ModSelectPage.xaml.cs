using Among_Us_ModManeger.Pages.TownOfHostK;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

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
            // ここは正しいページクラス名を使う
            NavigationService?.Navigate(new Among_Us_ModManeger.Pages.TownOfHostK.TownOfHostK());
        }

        private void OpenUrlInput_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("ここでGitHubのURL入力画面に進みます（未実装）。");
        }

        private void NotImplemented_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("まだ未実装です。", "未実装", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
