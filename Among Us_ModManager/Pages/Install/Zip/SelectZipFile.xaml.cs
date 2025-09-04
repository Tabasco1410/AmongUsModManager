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
        private readonly string SettingConfigPath;

        private string zipPath = string.Empty;
        private string exePath = string.Empty;
        private string copyFolderName = string.Empty;

        // 通常コンストラクタ（空ページ）
        public SelectZipFile()
        {
            InitializeComponent();
            configFilePath = Path.Combine(configFolderPath, "PutZip_Config.json");
            SettingConfigPath = Path.Combine(configFolderPath, "Settings.json");

            LoadVanillaExePath();
        }

        // GitHubなどから事前に ZIP と Mod 名を渡す場合用
        // autoInstall = true の場合はページ表示時に自動でインストール開始
        public SelectZipFile(string preselectedZipPath, bool autoInstall = false) : this()
        {
            if (!string.IsNullOrEmpty(preselectedZipPath) && File.Exists(preselectedZipPath))
            {
                zipPath = preselectedZipPath;
                ZipFilePathTextBox.Text = zipPath;

                // ZIP ファイル名から Mod 名だけ抽出
                string zipFileName = Path.GetFileNameWithoutExtension(preselectedZipPath);

                // バージョンや _steam などのサフィックスを取り除く正規表現例
                // 例: TownOfHost-1.2.3_steam → TownOfHost
                var match = System.Text.RegularExpressions.Regex.Match(zipFileName, @"^[^-_]+");
                copyFolderName = match.Success ? match.Value : zipFileName;

                CopyFolderNameTextBox.Text = copyFolderName;
            }

            CheckIfReady();

            // 自動インストール
            if (autoInstall && InstallButton.IsEnabled)
            {
                InstallButton_Click(InstallButton, null);
            }
        }

        private void LoadVanillaExePath()
        {
            try
            {
                if (File.Exists(SettingConfigPath))
                {
                    string json = File.ReadAllText(SettingConfigPath);
                    using JsonDocument doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("AmongUsExePath", out JsonElement exeElement))
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
                // exePath が null または存在しない場合は警告
                if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                {
                    MessageBox.Show("Among Us の実行ファイルパスが設定されていません。",
                                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // フォルダ取得
                string sourceFolder = Path.GetDirectoryName(exePath)!;

                // NavigationService が null かどうかチェック
                if (this.NavigationService != null)
                {
                    NavigationService.Navigate(new Pages.PutZipFile.Put_Zip_File(sourceFolder, zipPath, copyFolderName));
                }
                else
                {
                    MessageBox.Show("NavigationService が null です。Frame 内で開かれているか確認してください。",
                                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定の保存中にエラーが発生しました: {ex.Message}",
                                "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
            else
            {
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
