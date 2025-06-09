using Among_Us_ModManeger.Pages;               // MainMenuPageの名前空間
using Among_Us_ModManeger.Pages.Mod_from_zip; // SelectZipFileの名前空間
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
            NavigationService?.Navigate(new MainMenuPage());
        }

        private void ZipInstall_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SelectZipFile());
        }
    }
}
