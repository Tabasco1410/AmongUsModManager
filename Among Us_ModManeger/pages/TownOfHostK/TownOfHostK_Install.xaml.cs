using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Among_Us_ModManeger.Pages.TownOfHostK
{
    public partial class TownOfHostK_Install : Page
    {
        public TownOfHostK_Install()
        {
            InitializeComponent();
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            InstallProgressBar.Value = 0;
            StatusTextBlock.Text = "インストールを開始します...";
            LogTextBox.Clear();
            ((Button)sender).IsEnabled = false;

            try
            {
                await InstallProcessAsync();
                StatusTextBlock.Text = "インストールが完了しました。";
                Log("すべての処理が正常に終了しました。");
                ReturnHomeButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "エラーが発生しました。";
                Log($"エラー詳細: {ex.Message}");
            }
            finally
            {
                ((Button)sender).IsEnabled = true;
            }
        }

        private async Task InstallProcessAsync()
        {
            const int totalSteps = 200;
            const double stepSize = 0.5;

            for (int i = 0; i <= totalSteps; i++)
            {
                double percent = i * stepSize;
                string message = $"ステップ{i}/{totalSteps} 実行中...";
                UpdateProgress(percent, message);
                await Task.Delay(10);
            }
        }

        private void UpdateProgress(double percent, string message)
        {
            InstallProgressBar.Value = percent;
            StatusTextBlock.Text = message;
            Log(message);
        }

        private void Log(string message)
        {
            LogTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
            LogTextBox.ScrollToEnd();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void ReturnHomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainMenuPage());
        }
    }
}
