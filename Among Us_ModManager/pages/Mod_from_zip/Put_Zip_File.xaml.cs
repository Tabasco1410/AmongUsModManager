using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Among_Us_ModManager.Pages;

namespace Among_Us_ModManager.Pages.PutZipFile
{
    public partial class Put_Zip_File : Page
    {
        private string sourceFolderPath;  // Among Us.exeが入っているフォルダのパス
        private string zipFilePath;       // 展開するZIPファイルのパス
        private string installFolderName; // コピー先のフォルダ名

        public Put_Zip_File(string sourceFolderPath, string zipFilePath, string installFolderName)
        {
            InitializeComponent();

            this.sourceFolderPath = sourceFolderPath;
            this.zipFilePath = zipFilePath;
            this.installFolderName = installFolderName;

            Loaded += Put_Zip_File_Loaded;
        }

        private async void Put_Zip_File_Loaded(object sender, RoutedEventArgs e)
        {
            await InstallModAsync();
        }

        private async Task InstallModAsync()
        {
            try
            {
                StatusTextBlock.Text = "準備中...";
                LogTextBox.Text = "";

                string appBaseDir = Path.GetDirectoryName(sourceFolderPath.TrimEnd(Path.DirectorySeparatorChar));
                string destDir = Path.Combine(appBaseDir, installFolderName);

                Log($"コピー先パスを設定: {destDir}");
                await Task.Delay(300);

                if (Directory.Exists(destDir))
                {
                    Log("既存のフォルダを削除中...");
                    Directory.Delete(destDir, true);
                    Log("削除完了");
                    await Task.Delay(300);
                }

                Log("Among Us.exeが入っているフォルダをコピーしています...");
                CopyDirectory(sourceFolderPath, destDir);
                Log("コピー完了");
                await Task.Delay(300);

                string tempExtractDir = Path.Combine(Path.GetTempPath(), "AmongUsModTemp");
                if (Directory.Exists(tempExtractDir))
                {
                    Log("一時フォルダを削除中...");
                    Directory.Delete(tempExtractDir, true);
                    Log("削除完了");
                }
                Directory.CreateDirectory(tempExtractDir);
                Log($"一時フォルダを作成: {tempExtractDir}");
                await Task.Delay(300);

                Log("ZIPファイルを展開中...");
                // ZIPファイルを展開
                ZipFile.ExtractToDirectory(zipFilePath, tempExtractDir);
                Log("展開完了");

                // 展開直後のルートフォルダを判定
                var extractedRootDirs = Directory.GetDirectories(tempExtractDir);

                string rootFolderToCopy;
                if (extractedRootDirs.Length == 1)
                {
                    rootFolderToCopy = extractedRootDirs[0];
                    Log($"展開先のルートフォルダ: {rootFolderToCopy}");
                }
                else
                {
                    rootFolderToCopy = tempExtractDir;
                    Log("展開先に複数フォルダやファイルがあります。");
                }

                // コピー先に中身を上書きコピー
                CopyDirectory(rootFolderToCopy, destDir, overwrite: true);
                Log("上書きコピー完了");

                // BepInExフォルダ検出＆コピー
                string? bepInExPath = FindBepInExFolder(rootFolderToCopy);
                if (bepInExPath != null)
                {
                    Log($"BepInExフォルダを発見: {bepInExPath}");
                    string targetBepInExDir = Path.Combine(destDir, "BepInEx");
                    CopyDirectory(bepInExPath, targetBepInExDir, overwrite: true);
                    Log("BepInExのコピー完了");
                }
                else
                {
                    Log("BepInExフォルダはZIPに含まれていません。");
                }

                Directory.Delete(tempExtractDir, true);
                Log("一時フォルダを削除しました");

                InstallProgressBar.Value = 100;
                StatusTextBlock.Text = "インストール完了";
                FinishTextBlock.Visibility = Visibility.Visible;
                ReturnHomeButton.Visibility = Visibility.Visible;
                Log("インストールが完了しました。");
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "エラー発生";
                Log($"エラー: {ex.Message}");
            }
        }

        private string? FindBepInExFolder(string rootDir)
        {
            foreach (var dir in Directory.GetDirectories(rootDir))
            {
                if (Path.GetFileName(dir).Equals("BepInEx", StringComparison.OrdinalIgnoreCase))
                {
                    return dir;
                }
                else
                {
                    string? found = FindBepInExFolder(dir);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        private void CopyDirectory(string sourceDir, string destDir, bool overwrite = false)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"ソースディレクトリが見つかりません: {sourceDir}");

            Directory.CreateDirectory(destDir);

            foreach (var file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, overwrite);
            }

            foreach (var subDir in dir.GetDirectories())
            {
                string newDestDir = Path.Combine(destDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestDir, overwrite);
            }
        }

        private void Log(string message)
        {
            LogTextBox.AppendText($"{DateTime.Now:HH:mm:ss} {message}\n");
            LogTextBox.ScrollToEnd();
        }

        private void ReturnHomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new MainMenuPage());
        }

    }
}
