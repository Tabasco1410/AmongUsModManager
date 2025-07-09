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
            // ページ読み込み完了時に次ページへ遷移させている、ボタン出すときはまた変える
            this.Loaded += ModSelectPage_Loaded;
        }

        private void ModSelectPage_Loaded(object sender, RoutedEventArgs e)
        {
            var nextPage = new Mod_from_zip.SelectZipFile();
            this.NavigationService?.Navigate(nextPage);
        }

        private void BackToMain_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}
