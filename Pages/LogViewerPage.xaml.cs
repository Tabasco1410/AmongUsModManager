using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AmongUsModManager.Services;

namespace AmongUsModManager.Pages
{
    public sealed partial class LogViewerPage : Page
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AmongUsModManager", "LogOutput.log");

        public LogViewerPage()
        {
            this.InitializeComponent();
            LoadLog();
        }

        private void LoadLog()
        {
            if (!File.Exists(LogPath))
            {
                LogText.Text = "ログファイルがまだ存在しません。";
                return;
            }
            try
            {
                LogText.Text = File.ReadAllText(LogPath, System.Text.Encoding.UTF8);
                
                LogScrollViewer.UpdateLayout();
                LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
            }
            catch (Exception ex)
            {
                LogText.Text = $"ログ読み込みエラー: {ex.Message}";
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadLog();

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string folder = Path.GetDirectoryName(LogPath) ?? "";
            if (Directory.Exists(folder))
                await Windows.System.Launcher.LaunchFolderPathAsync(folder);
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "ログをクリア",
                Content = "LogOutput.log の内容を削除しますか？",
                PrimaryButtonText = "削除",
                CloseButtonText = "キャンセル",
                XamlRoot = this.XamlRoot
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                try { File.WriteAllText(LogPath, "", System.Text.Encoding.UTF8); LoadLog(); }
                catch (Exception ex) { LogText.Text = $"クリアエラー: {ex.Message}"; }
            }
        }
    }
}
