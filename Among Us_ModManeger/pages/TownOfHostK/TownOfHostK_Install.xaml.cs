using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Among_Us_ModManeger.Pages.TownOfHostK
{
    public partial class TownOfHostK_Install : Page
    {
        private readonly string configFolderPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmongUsModManeger");

        private readonly string configFilePath;
        private const string ModKey = "TownOfHostK";

        public TownOfHostK_Install()
        {
            InitializeComponent();
            configFilePath = Path.Combine(configFolderPath, "Mods_Config.json");
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
                InstallButton.Visibility = Visibility.Collapsed;
                BackButton.Visibility = Visibility.Collapsed;
                IntroTextBlock.Visibility = Visibility.Collapsed;
                FinishTextBlock.Visibility = Visibility.Visible;
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
            UpdateProgress(0, "設定読み込み中...");
            // 1. 設定読み込み
            var config = LoadModConfig();
            if (config == null)
                throw new Exception("設定ファイルが見つからないか不正です。");

            if (!File.Exists(config.ExePath))
                throw new Exception($"指定されたAmongUs.exeが存在しません: {config.ExePath}");

            UpdateProgress(5, "ダウンロードURLを取得中...");

            // 2. JSONからダウンロードURLを取得
            var downloadUrl = await GetDownloadUrlAsync();
            if (string.IsNullOrEmpty(downloadUrl))
                throw new Exception("ダウンロードURLの取得に失敗しました。");

            UpdateProgress(10, "MOD ZIPファイルをダウンロード中...");

            // 3. ZIPをダウンロード
            var tempFolder = Path.Combine(Path.GetTempPath(), "AmongUsModManeger_Temp");
            Directory.CreateDirectory(tempFolder);

            var zipFilePath = Path.Combine(tempFolder, "mod.zip");
            using (var httpClient = new HttpClient())
            {
                var data = await httpClient.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(zipFilePath, data);
            }
            Log($"ZIPファイルをダウンロードしました: {zipFilePath}");

            UpdateProgress(30, "AmongUs.exeフォルダをコピー中...");

            // 4. AmongUs.exeのフォルダをコピーして名前変更
            var exeDir = Path.GetDirectoryName(config.ExePath);
            if (string.IsNullOrEmpty(exeDir) || !Directory.Exists(exeDir))
                throw new Exception("AmongUs.exeのフォルダが存在しません。");

            // 親フォルダを取得して、その直下にコピー先を作成
            var parentDirOfExeDir = Directory.GetParent(exeDir)?.FullName;
            if (string.IsNullOrEmpty(parentDirOfExeDir))
                throw new Exception("元フォルダの親フォルダが取得できません。");

            var copiedFolderPath = Path.Combine(parentDirOfExeDir, config.CopiedFolderName);

            // コピー先が既にあれば削除
            if (Directory.Exists(copiedFolderPath))
            {
                Directory.Delete(copiedFolderPath, true);
            }

            CopyDirectory(exeDir, copiedFolderPath);

            Log($"フォルダをコピーして名前を {config.CopiedFolderName} に変更しました。");

            UpdateProgress(60, "ZIPファイルを展開中...");

            // 5. ZIPを展開
            var extractPath = Path.Combine(tempFolder, "extracted");
            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);

            ZipFile.ExtractToDirectory(zipFilePath, extractPath);

            Log($"ZIPファイルを展開しました: {extractPath}");

            UpdateProgress(80, "展開したファイルをコピー中...");

            // 6. 展開ファイルをコピー（上書きで）
            // 自動で展開先の構造を調べて、1階層下にある場合は中身をコピーする

            string[] rootFolders = Directory.GetDirectories(extractPath);

            bool copied = false;

            if (rootFolders.Length == 1)
            {
                // 1つだけフォルダがある場合
                string singleRootFolder = rootFolders[0];
                // その中にBepInExやpluginsがあるかチェック
                if (Directory.Exists(Path.Combine(singleRootFolder, "BepInEx")) ||
                    Directory.Exists(Path.Combine(singleRootFolder, "plugins")) ||
                    Directory.Exists(Path.Combine(singleRootFolder, "patchers"))) // 必要に応じて他のMODフォルダ名も追加可
                {
                    CopyDirectory(singleRootFolder, copiedFolderPath);
                    copied = true;
                }
            }

            if (!copied)
            {
                // それ以外は展開先直下をそのままコピー
                CopyDirectory(extractPath, copiedFolderPath);
            }


            // 7. 一時ファイル削除
            if (File.Exists(zipFilePath))
                File.Delete(zipFilePath);

            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);

            if (Directory.Exists(tempFolder) && Directory.GetFileSystemEntries(tempFolder).Length == 0)
                Directory.Delete(tempFolder);

            UpdateProgress(100, "インストール完了！");

            // 8. コピー先フォルダをエクスプローラーで開く（追加）
            OpenFolderInExplorer(copiedFolderPath);
        }

        private void OpenFolderInExplorer(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = folderPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
                Log($"フォルダをエクスプローラーで開きました: {folderPath}");
            }
        }

        private void UpdateProgress(double percent, string message)
        {
            Dispatcher.Invoke(() =>
            {
                InstallProgressBar.Value = percent;
                StatusTextBlock.Text = message;
                Log(message);
            });
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
                LogTextBox.ScrollToEnd();
            });
        }

        private ModConfig? LoadModConfig()
        {
            try
            {
                if (!File.Exists(configFilePath))
                    return null;

                var json = File.ReadAllText(configFilePath);
                var container = JsonSerializer.Deserialize<ModConfigContainer>(json);
                if (container?.Mods != null && container.Mods.TryGetValue(ModKey, out var config))
                    return config;

                return null;
            }
            catch (Exception ex)
            {
                Log($"設定読み込みエラー: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> GetDownloadUrlAsync()
        {
            try
            {
                const string rawJsonUrl = "https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/Updates/ModUpdate.json";

                using var httpClient = new HttpClient();
                var jsonString = await httpClient.GetStringAsync(rawJsonUrl);

                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                if (root.TryGetProperty(ModKey, out var modElement))
                {
                    if (modElement.TryGetProperty("DownloadUrl", out var urlElement))
                    {
                        return urlElement.GetString();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Log($"DownloadUrl取得エラー: {ex.Message}");
                return null;
            }
        }

        private void CopyDirectory(string sourceDir, string targetDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) throw new DirectoryNotFoundException($"コピー元フォルダが見つかりません: {sourceDir}");

            Directory.CreateDirectory(targetDir);

            foreach (var file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(targetDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            foreach (var subDir in dir.GetDirectories())
            {
                string newTargetDir = Path.Combine(targetDir, subDir.Name);
                CopyDirectory(subDir.FullName, newTargetDir);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void ReturnHomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainMenuPage());
        }

        private class ModConfig
        {
            public string? ExePath { get; set; }
            public string? CopiedFolderName { get; set; }
        }

        private class ModConfigContainer
        {
            public System.Collections.Generic.Dictionary<string, ModConfig>? Mods { get; set; }
        }
    }
}
