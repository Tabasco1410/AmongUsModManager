using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Navigation;

namespace Among_Us_ModManager.Pages.Install.Zip
{
    public partial class SelectZipFile : Page
    {
        private readonly string configFolderPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmongUsModManager");
        private readonly string configFilePath;
        private readonly string vanillaConfigPath;

        private string zipPath = string.Empty;
        private string exePath = string.Empty;
        private string copyFolderName = string.Empty;

        public SelectZipFile()
        {
            InitializeComponent();
            configFilePath = Path.Combine(configFolderPath, "PutZip_Config.json");
            vanillaConfigPath = Path.Combine(configFolderPath, "Vanilla_Config.json");

            LoadVanillaExePath();
        }

        private void LoadVanillaExePath()
        {
            try
            {
                if (File.Exists(vanillaConfigPath))
                {
                    string json = File.ReadAllText(vanillaConfigPath);
                    using JsonDocument doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("ExePath", out JsonElement exeElement))
                    {
                        string path = exeElement.GetString() ?? "";
                        if (File.Exists(path))
                        {
                            exePath = path;
                            ExePathTextBox.Text = exePath;
                        }
                    }
                }
            }
            catch
            {
                // 読み込みに失敗しても無視
            }
        }

        private void SelectZipFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "ZIPファイル (*.zip)|*.zip"
            };
            if (dialog.ShowDialog() == true)
            {
                zipPath = dialog.FileName;
                ZipFilePathTextBox.Text = zipPath;
                CheckIfReady();
            }
        }

        private void SelectExeFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "実行ファイル (*.exe)|*.exe"
            };
            if (dialog.ShowDialog() == true)
            {
                exePath = dialog.FileName;
                ExePathTextBox.Text = exePath;
                CheckIfReady();
            }
        }

        private void CopyFolderNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            copyFolderName = CopyFolderNameTextBox.Text.Trim();
            CheckIfReady();
        }

        private void ZipFilePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            zipPath = ZipFilePathTextBox.Text.Trim();
            CheckIfReady();
        }

        private void ExePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            exePath = ExePathTextBox.Text.Trim();
            CheckIfReady();
        }

        private void CheckIfReady()
        {
            bool valid = File.Exists(zipPath) &&
                         File.Exists(exePath) &&
                         !string.IsNullOrWhiteSpace(copyFolderName);

            if (!string.IsNullOrWhiteSpace(copyFolderName) &&
                copyFolderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                ErrorTextBlock.Text = "無効な文字が含まれています。";
                InstallButton.IsEnabled = false;
            }
            else
            {
                ErrorTextBlock.Text = "";
                InstallButton.IsEnabled = valid;
            }
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string sourceFolder = Path.GetDirectoryName(exePath)!;
                NavigationService.Navigate(new Pages.PutZipFile.Put_Zip_File(sourceFolder, zipPath, copyFolderName));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定の保存中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                // 1つ前のページに戻る
                this.NavigationService.GoBack();
            }
            else
            {
                // 戻れない場合は MainMenuPage にフォールバック（任意）
                MessageBox.Show("前のページに戻れません。");
            }
        }


        private void CopyFolderNameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";
        }

        private void CopyFolderNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CheckIfReady();
        }
    }
}
