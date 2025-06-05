using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Among_Us_ModManeger.Pages
{
    public partial class MainMenuPage : Page
    {
        public MainMenuPage()
        {
            InitializeComponent();
        }

        private void Mod_New_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ModSelectPage());
        }

        private void Notice_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("お知らせボタンが押されました。");
        }
    }
}
