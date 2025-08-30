using System.Windows;
using System.Windows.Controls;
using Among_Us_ModManager.Pages.Install.Zip;

namespace Among_Us_ModManager.Pages
{
    /// <summary>
    /// Install_TypeChoosePage.xaml の相互作用ロジック
    /// </summary>
    public partial class Install_TypeChoosePage : Page
    {
        public Install_TypeChoosePage()
        {
            InitializeComponent();
        }

        // TownOfHost-Fun をインストールするボタン
        private void InstallDefault_Click(object sender, RoutedEventArgs e)
        {
            // ChooseVersionPage に遷移
            var chooseVersionPage = new Install.GitHub.ChooseVersionPage();
            this.NavigationService?.Navigate(chooseVersionPage);
        }

        // Modの.zipファイルからインストールするボタン
        private void InstallFromZip_Click(object sender, RoutedEventArgs e)
        {
            // SelectZipFile ページに遷移
            var selectZipPage = new Install.Zip.SelectZipFile();
            if (this.NavigationService != null)
                this.NavigationService.Navigate(selectZipPage);
            else
                MessageBox.Show("ページ遷移できません。NavigationWindow または Frame 内に配置されている必要があります。");
        }

        // 左下の戻るボタン
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
            else
                MessageBox.Show("前のページに戻れません。");
        }
    }
}
