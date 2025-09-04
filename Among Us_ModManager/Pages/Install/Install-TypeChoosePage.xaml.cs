using System.Windows;
using System.Windows.Controls;
using Among_Us_ModManager.Pages.Install.Zip;
using Among_Us_ModManager.Pages.Install.GitHub;

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

        // TownOfHost
        private void InstallTownOfHost_Click(object sender, RoutedEventArgs e)
        {
            NavigateToGitHub("tukasa0001", "TownOfHost");
        }

        // TownOfHost-K
        private void InstallTownOfHostK_Click(object sender, RoutedEventArgs e)
        {
            NavigateToGitHub("KYMario", "TownOfHost-K");
        }

        // SuperNewRoles
        private void InstallSuperNewRoles_Click(object sender, RoutedEventArgs e)
        {
            NavigateToGitHub("SuperNewRoles", "SuperNewRoles");
        }

        // Nebula on the Ship
        private void InstallNebula_Click(object sender, RoutedEventArgs e)
        {
            NavigateToGitHub("Dolly1016", "Nebula");
        }

        // ExtremeRoles
        private void InstallExtremeRoles_Click(object sender, RoutedEventArgs e)
        {
            NavigateToGitHub("yukieiji", "ExtremeRoles");
        }

        // TownOfHost-Fun
        private void InstallTownOfHostFun_Click(object sender, RoutedEventArgs e)
        {
            NavigateToGitHub("ToritenKabosu", "TownOfHost-Fun");
        }

        // 共通 GitHub 遷移
        private void NavigateToGitHub(string owner, string repo)
        {
            var page = new CheckVersionPage(owner, repo);
            this.NavigationService?.Navigate(page);
        }

        // Modの.zipファイルからインストールするボタン
        private void InstallFromZip_Click(object sender, RoutedEventArgs e)
        {
            var selectZipPage = new SelectZipFile();
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
