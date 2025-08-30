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
        private string sourceFolderPath;
        private string zipFilePath;
        private string installFolderName;

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
                InstallProgressBar.Value = 5;
                await Task.Delay(200);

                // ★既存フォルダの確認
                if (Directory.Exists(destDir))
                {
                    var result = MessageBox.Show(
                        $"コピー先フォルダ「{installFolderName}」はすでに存在します。\n上書きしますか？",
                        "確認",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                    {
                        StatusTextBlock.Text = "インストールをキャンセルしました";
                        // ★戻るボタンを表示
                        BackButton.Visibility = Visibility.Visible;
                        return;
                    }

                    Log("既存のフォルダを削除中...");
                    Directory.Delete(destDir, true);
                    Log("削除完了");
                    InstallProgressBar.Value = 10;
                    await Task.Delay(200);
                }

                var progress = new Progress<int>(value => InstallProgressBar.Value = value);

                // ★Among Us.exeコピー（10→40%）
                StatusTextBlock.Text = "Among Us.exeをコピー中...";
                await CopyDirectoryAsync(sourceFolderPath, destDir, progress, 10, 40);
                InstallProgressBar.Value = 40;

                // 一時フォルダ作成
                string tempExtractDir = Path.Combine(Path.GetTempPath(), "AmongUsModTemp");
                if (Directory.Exists(tempExtractDir))
                    Directory.Delete(tempExtractDir, true);

                Directory.CreateDirectory(tempExtractDir);
                Log($"一時フォルダを作成: {tempExtractDir}");
                InstallProgressBar.Value = 45;
                await Task.Delay(100);

                // ZIP展開（45→60%）
                StatusTextBlock.Text = "ZIPファイルを展開中...";
                await ExtractZipAsync(zipFilePath, tempExtractDir, progress, 45, 60);
                Log("展開完了");

                // 展開ルート判定
                var extractedRootDirs = Directory.GetDirectories(tempExtractDir);
                string rootFolderToCopy = (extractedRootDirs.Length == 1) ? extractedRootDirs[0] : tempExtractDir;
                Log($"展開先のルートフォルダ: {rootFolderToCopy}");
                InstallProgressBar.Value = 65;

                // ZIP中身コピー（65→90%）
                StatusTextBlock.Text = "ZIP中身をコピー中...";
                await CopyDirectoryAsync(rootFolderToCopy, destDir, progress, 65, 90, overwrite: true);

                // BepInExコピー（90→95%）
                string? bepInExPath = FindBepInExFolder(rootFolderToCopy);
                if (bepInExPath != null)
                {
                    Log($"BepInExフォルダを発見: {bepInExPath}");
                    string targetBepInExDir = Path.Combine(destDir, "BepInEx");
                    await CopyDirectoryAsync(bepInExPath, targetBepInExDir, progress, 90, 95, overwrite: true);
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
                BackButton.Visibility = Visibility.Visible; // エラー時も戻れるようにする
            }
        }

        private string? FindBepInExFolder(string rootDir)
        {
            foreach (var dir in Directory.GetDirectories(rootDir))
            {
                if (Path.GetFileName(dir).Equals("BepInEx", StringComparison.OrdinalIgnoreCase))
                    return dir;
                string? found = FindBepInExFolder(dir);
                if (found != null) return found;
            }
            return null;
        }

        // ★ファイル単位コピー + ステップ進捗範囲
        private async Task CopyDirectoryAsync(string sourceDir, string destDir, IProgress<int> progress, int startPercent = 0, int endPercent = 100, bool overwrite = false)
        {
            var allFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            int totalFiles = allFiles.Length;
            int copiedFiles = 0;

            foreach (var filePath in allFiles)
            {
                string relativePath = Path.GetRelativePath(sourceDir, filePath);
                string targetPath = Path.Combine(destDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.Copy(filePath, targetPath, overwrite);

                copiedFiles++;
                int percent = startPercent + (int)((double)copiedFiles / totalFiles * (endPercent - startPercent));
                progress.Report(percent);

                await Task.Delay(1);
            }
        }

        // ★ZIP展開をファイル単位で進捗更新
        private async Task ExtractZipAsync(string zipFilePath, string extractDir, IProgress<int> progress, int startPercent = 0, int endPercent = 100)
        {
            using (var archive = ZipFile.OpenRead(zipFilePath))
            {
                int totalEntries = archive.Entries.Count;
                int processed = 0;

                foreach (var entry in archive.Entries)
                {
                    string destinationPath = Path.Combine(extractDir, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

                    if (!string.IsNullOrEmpty(entry.Name))
                        entry.ExtractToFile(destinationPath, overwrite: true);

                    processed++;
                    int percent = startPercent + (int)((double)processed / totalEntries * (endPercent - startPercent));
                    progress.Report(percent);

                    await Task.Delay(1);
                }
            }
        }

        private void Log(string message)
        {
            LogTextBox.AppendText($"{DateTime.Now:HH:mm:ss} {message}\n");
            LogTextBox.ScrollToEnd();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                MessageBox.Show("戻れるページがありません。", "戻れません", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ReturnHomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new MainMenuPage());
        }
    }
}
