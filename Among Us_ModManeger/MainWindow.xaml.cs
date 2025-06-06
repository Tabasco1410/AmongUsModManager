using System.Windows;
using System.Windows.Navigation;

namespace Among_Us_ModManeger
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // アプリ起動時に最初のページへ遷移
            MainFrame.Navigate(new Pages.MainMenuPage());
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {

        }
    }
}
