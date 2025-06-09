using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Among_Us_ModManeger.Pages.Mod_from_zip
{
    public partial class SelectZipFile : Page
    {
        private readonly string copyFolderNamePlaceholder = "Modの名前を推奨";

        public SelectZipFile()
        {
            InitializeComponent();

            CopyFolderNameTextBox.Text = copyFolderNamePlaceholder;
            CopyFolderNameTextBox.Foreground = System.Windows.Media.Brushes.Gray;

            CheckInputValidity();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new MainMenuPage());
        }

        private void SelectZipFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "ZIPファイル (*.zip)|*.zip",
                Title = "MODのZIPファイルを選択してください"
            };

            if (dlg.ShowDialog() == true)
            {
                ZipFilePathTextBox.Text = dlg.FileName;
            }
        }

        private void SelectExeFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Among Us 実行ファイル (Among Us.exe)|Among Us.exe",
                Title = "Among Us.exe を選択してください"
            };

            if (dlg.ShowDialog() == true)
            {
                ExePathTextBox.Text = dlg.FileName;
            }
        }

        private void CopyFolderNameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (CopyFolderNameTextBox.Text == copyFolderNamePlaceholder)
            {
                CopyFolderNameTextBox.Text = "";
                CopyFolderNameTextBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void CopyFolderNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CopyFolderNameTextBox.Text))
            {
                CopyFolderNameTextBox.Text = copyFolderNamePlaceholder;
                CopyFolderNameTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
            CheckInputValidity();
        }

        private void CopyFolderNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckInputValidity();
        }

        private void ZipFilePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckInputValidity();
        }

        private void ExePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckInputValidity();
        }

        private void CheckInputValidity()
        {
            ErrorTextBlock.Text = "";

            bool zipSelected = !string.IsNullOrWhiteSpace(ZipFilePathTextBox.Text);
            bool exeSelected = !string.IsNullOrWhiteSpace(ExePathTextBox.Text);

            string folderName = CopyFolderNameTextBox.Text;
            bool folderNameValid = true;

            if (folderName == copyFolderNamePlaceholder || string.IsNullOrWhiteSpace(folderName))
            {
                folderNameValid = false;
                ErrorTextBlock.Text = "コピー後のフォルダ名を入力してください。";
            }
            else
            {
                string invalidCharsPattern = @"[<>:""/\\|?*]";
                if (Regex.IsMatch(folderName, invalidCharsPattern))
                {
                    folderNameValid = false;
                    ErrorTextBlock.Text = "フォルダ名に使用できない文字が含まれています。<>:\"/\\|?* は使えません。";
                }
            }

            InstallButton.IsEnabled = zipSelected && exeSelected && folderNameValid;
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            // Put_Zip_File ページのインスタンスを作成
            var putZipFilePage = new Among_Us_ModManeger.Pages.PutZipFile.Put_Zip_File();

            SavePutZipConfig();

            // ページ遷移
            NavigationService?.Navigate(putZipFilePage);
        }

        private void SavePutZipConfig()
        {
            try
            {
                var configFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmongUsModManeger");
                if (!Directory.Exists(configFolderPath))
                    Directory.CreateDirectory(configFolderPath);

                var configPath = Path.Combine(configFolderPath, "PutZip_Config.json");

                string exeDir = Path.GetDirectoryName(ExePathTextBox.Text) ?? "";

                // 親フォルダを取得（親がなければ exeDir のまま）
                string parentDir = Directory.GetParent(exeDir)?.FullName ?? exeDir;

                var config = new
                {
                    ZipPath = ZipFilePathTextBox.Text,
                    ExtractTo = Path.Combine(parentDir, CopyFolderNameTextBox.Text)
                };

                var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定保存時にエラーが発生しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
