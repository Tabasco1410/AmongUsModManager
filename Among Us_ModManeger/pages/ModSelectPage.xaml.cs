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
            // ハイフンは使えないのでクラス名はTownOfHostK
            NavigationService?.Navigate(new TownOfHostK());
        }

        private void OpenUrlInput_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("ここでGitHubのURL入力画面に進みます（未実装）。");
            // NavigationService.Navigate(new UrlInputPage()); ← 実装予定ならここを使う
        }
    }
}
