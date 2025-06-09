    using Among_Us_ModManeger.Pages;               // MainMenuPage‚Ì–¼‘O‹óŠÔ
    using Among_Us_ModManeger.Pages.Mod_from_zip; // SelectZipFile‚Ì–¼‘O‹óŠÔ
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
