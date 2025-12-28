using AmongUsModManager.Models;
using AmongUsModManager.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AmongUsModManager.Pages
{
    public sealed partial class SetupPage : Page
    {
        public SetupPage()
        {
            this.InitializeComponent();
        }

        private void PlatformCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (PlatformCombo.SelectedItem as ComboBoxItem)?.Content.ToString();
            AutoDetectButton.IsEnabled = (selected == "Steam" || selected == "Epic Games");
        }

        private void AutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            string? path = null;
            var selected = (PlatformCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (selected == "Steam") path = AUFileDetector.GetSteamPath();
            else if (selected == "Epic Games") path = AUFileDetector.GetEpicPath();

            if (!string.IsNullOrEmpty(path))
            {
                PathTextBox.Text = path;
                ValidatePath(path);
            }
            else
            {
                StatusMessage.Text = "ゲームが見つかりませんでした。手動で選択してください。";
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            IntPtr hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                PathTextBox.Text = folder.Path;
                ValidatePath(folder.Path);
            }
        }

        private void ValidatePath(string path)
        {
            bool exists = File.Exists(Path.Combine(path, "Among Us.exe"));
            FinishButton.IsEnabled = exists;
            StatusMessage.Text = exists ? "" : "Among Us.exe が見つかりません。";
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            string vanillaPath = PathTextBox.Text;
        
            string? commonPath = Path.GetDirectoryName(vanillaPath);

            var config = new AppConfig
            {
                VanillaPaths = new List<VanillaPathInfo>
                {
                    new VanillaPathInfo { Name = "バニラ (初期設定)", Path = vanillaPath }
                },
                GameInstallPath = vanillaPath,
               
                ModDataPath = commonPath ?? vanillaPath
            };

            ConfigService.Save(config);

            if (App.MainWindowInstance is MainWindow mainWindow)
            {
                mainWindow.SetNavigationUI(true);
            }
            this.Frame.Navigate(typeof(HomePage));
        }
    }
}
