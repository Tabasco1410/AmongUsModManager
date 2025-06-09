using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Among_Us_ModManeger.Pages.PutZipFile
{
    public partial class Put_Zip_File : Page
    {
        private readonly string configFolderPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmongUsModManeger");
        private readonly string configFilePath;

        public Put_Zip_File()
        {
            InitializeComponent();
            configFilePath = Path.Combine(configFolderPath, "Mods_Config.json");

            Loaded += Put_Zip_File_Loaded;
        }

        private async void Put_Zip_File_Loaded(object sender, RoutedEventArgs e)
        {
            InstallProgressBar.Value = 0;
            StatusTextBlock.Text = "インストールを開始します...";
            LogTextBox.Clear();

            try
            {
                await Task.Delay(300); // 見た目の準備のためちょっと待つ
                await InstallZipAsync();
                StatusTextBlock.Text = "インストールが完了しました。";
                FinishTextBlock.Visibility = Visibility.Visible;
                IntroTextBlock.Visibility = Visibility.Collapsed;
                ReturnHomeButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "エラーが発生しました。";
                Log($"エラー詳細: {ex.Message}");
            }
        }

        private async Task InstallZipAsync()
        {
            UpdateProgress(5, "設定読み込み中...");

            var config = LoadModConfig();
            if (config == null)
                throw new Exception("設定ファイルが見つかりません。");

            if (!File.Exists(config.ZipPath))
                throw new Exception("指定されたZIPファイルが存在しません。");

            if (string.IsNullOrEmpty(config.ExtractTo))
                throw new Exception("展開先のパスが指定されていません。");

            UpdateProgress(20, "ZIPを展開中...");

            string tempExtract = Path.Combine(Path.GetTempPath(), "PutZip_Temp");
            if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true);

            await ExtractZipAsync(config.ZipPath, tempExtract);

            UpdateProgress(60, "ファイルをコピー中...");

            if (Directory.Exists(config.ExtractTo))
                Directory.Delete(config.ExtractTo, true);

            await CopyDirectoryAsync(tempExtract, config.ExtractTo);

            UpdateProgress(90, "処理中...");

            await Task.Run(() => Directory.Delete(tempExtract, true));

            UpdateProgress(100, "完了しました！");
        }

        private async Task ExtractZipAsync(string zipPath, string extractPath)
        {
            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(zipPath, extractPath);
            });
        }

        private async Task CopyDirectoryAsync(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            Log($"ディレクトリ作成: {destination}");

            var files = Directory.GetFiles(source);
            int totalFiles = files.Length;
            int copiedFiles = 0;

            foreach (var file in files)
            {
                string destFile = Path.Combine(destination, Path.GetFileName(file));
                await Task.Run(() => File.Copy(file, destFile, true));
                copiedFiles++;
                Log($"コピー: {file} → {destFile}");

                double progress = 60 + 30 * copiedFiles / totalFiles;
                UpdateProgress(progress, $"ファイルをコピー中... {copiedFiles}/{totalFiles}");
            }

            foreach (var dir in Directory.GetDirectories(source))
            {
                string destDir = Path.Combine(destination, Path.GetFileName(dir));
                await CopyDirectoryAsync(dir, destDir);
            }
        }

        private void UpdateProgress(double percent, string message)
        {
            Dispatcher.InvokeAsync(() =>
            {
                InstallProgressBar.Value = percent;
                StatusTextBlock.Text = message;
                Log(message);
            });
        }

        private void Log(string message)
        {
            Dispatcher.InvokeAsync(() =>
            {
                LogTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
                LogTextBox.ScrollToEnd();
            });
        }

        private ModZipConfig? LoadModConfig()
        {
            try
            {
                var configPath = Path.Combine(configFolderPath, "PutZip_Config.json");
                if (!File.Exists(configPath)) return null;

                var json = File.ReadAllText(configPath);
                return System.Text.Json.JsonSerializer.Deserialize<ModZipConfig>(json);
            }
            catch
            {
                return null;
            }
        }

        private void ReturnHomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainMenuPage());
        }

        private class ModZipConfig
        {
            public string? ZipPath { get; set; }
            public string? ExtractTo { get; set; }
        }
    }
}
