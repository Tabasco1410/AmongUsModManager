using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Among_Us_ModManager.Pages.Install.GitHub
{
    /// <summary>
    /// CheckVersionPage.xaml の相互作用ロジック
    /// </summary>
    public partial class CheckVersionPage : Page
    {
        private readonly string _owner;
        private readonly string _repo;

        // Settings.json の保存場所
        private readonly string SettingConfigPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "AmongUsModManager", "Settings.json");

        public CheckVersionPage(string owner, string repo)
        {
            InitializeComponent();
            _owner = owner;
            _repo = repo;

            LoadReleases();
        }

        /// <summary>
        /// GitHub Releases を取得して ListView に表示
        /// </summary>
        private async void LoadReleases()
        {
            try
            {
                var releases = await GetReleasesAsync();
                ReleaseListView.ItemsSource = releases;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"リリース情報の取得に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// GitHub API からリリース一覧を取得
        /// </summary>
        private async Task<List<GitHubRelease>> GetReleasesAsync()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager");

                string url = $"https://api.github.com/repos/{_owner}/{_repo}/releases";
                var response = await client.GetStringAsync(url);

                return JsonConvert.DeserializeObject<List<GitHubRelease>>(response) ?? new List<GitHubRelease>();

            }
        }

        /// <summary>
        /// リリース選択ボタンを押したとき
        /// </summary>
        private void SelectVersion_Click(object sender, RoutedEventArgs e)
        {
            if (ReleaseListView.SelectedItem is GitHubRelease selectedRelease)
            {
                // SourceCode.zip を除外
                var filteredAssets = new List<GitHubReleaseAsset>();
                foreach (var asset in selectedRelease.Assets ?? new List<GitHubReleaseAsset>())
                {
                    if (!asset.Name.Equals("SourceCode.zip", StringComparison.OrdinalIgnoreCase))
                        filteredAssets.Add(asset);
                }

                // ファイル一覧に表示
                FileListView.ItemsSource = filteredAssets;

                // ファイル一覧とインストールボタンを表示
                FilesPanel.Visibility = Visibility.Visible;
                InstallButton.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("リリースを選択してください。");
            }
        }

        /// <summary>
        /// インストールボタンを押したとき
        /// </summary>
        private async void InstallFile_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is not GitHubReleaseAsset selectedFile)
            {
                MessageBox.Show("インストールするファイルを選択してください。");
                return;
            }

            // .dll はインストール不可
            if (selectedFile.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(".dll ファイルはインストールできません。");
                return;
            }

            // --- プラットフォームチェック (ダウンロード前) ---
            var config = Among_Us_ModManager.Modules.SettingsConfig.Load();
            string fileName = selectedFile.Name.ToLower();
            string platform = config.Platform.ToLower();
            bool isMismatch = false;

            if (platform.Contains("steam") && fileName.Contains("epic"))
                isMismatch = true;
            else if (platform.Contains("epic") && fileName.Contains("steam"))
                isMismatch = true;

            if (isMismatch)
            {
                var result = MessageBox.Show(
                    $"選択したファイル \"{selectedFile.Name}\" は設定されたプラットフォーム ({config.Platform}) 用ではない可能性があります。\n続行しますか？",
                    "警告",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return; // ユーザーが中止した場合は何もせず return
            }

            string downloadsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AmongUsModManager", "Downloads"
            );
            Directory.CreateDirectory(downloadsDir);

            string zipFilePath = Path.Combine(downloadsDir, selectedFile.Name);

            try
            {
                // 進捗パネルを表示
                ProgressPanel.Visibility = Visibility.Visible;
                ProgressText.Text = "ダウンロード中...";
                ProgressBar.Value = 0;

                using var client = new HttpClient();
                using var response = await client.GetAsync(selectedFile.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                    {
                        double percent = (double)totalRead / totalBytes * 100;
                        ProgressBar.Value = percent;
                    }
                }

                // ダウンロード完了
                ProgressText.Text = "ダウンロード完了！";

                // 設定ファイル読み込み
                string? amongUsExePath = LoadSettingConfig();
                if (string.IsNullOrEmpty(amongUsExePath) || !File.Exists(amongUsExePath))
                {
                    MessageBox.Show("Settings.json が存在しないか Among Us.exe のパスが無効です。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    ProgressPanel.Visibility = Visibility.Collapsed;
                    return;
                }

                // ZIP名から Mod名（フォルダ名）を取得
                string rawName = Path.GetFileNameWithoutExtension(selectedFile.Name);
                string modName = rawName;

                int dashIndex = rawName.IndexOf('-');
                if (dashIndex > 0)
                    modName = rawName.Substring(0, dashIndex);

                int underscoreIndex = modName.IndexOf('_');
                if (underscoreIndex > 0)
                    modName = modName.Substring(0, underscoreIndex);

                // AmongUs.exe のディレクトリを取得
                string? sourceFolderPath = Path.GetDirectoryName(amongUsExePath);
                if (string.IsNullOrEmpty(sourceFolderPath))
                {
                    MessageBox.Show("Among Us.exe のディレクトリを取得できませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    ProgressPanel.Visibility = Visibility.Collapsed;
                    return;
                }

                // 進捗パネルを非表示にして遷移
                ProgressPanel.Visibility = Visibility.Collapsed;

                NavigationService?.Navigate(new Among_Us_ModManager.Pages.PutZipFile.Put_Zip_File(
                    sourceFolderPath,
                    zipFilePath,
                    modName
                ));
            }
            catch (Exception ex)
            {
                ProgressPanel.Visibility = Visibility.Collapsed;
                MessageBox.Show($"ダウンロードに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }






        /// <summary>
        /// Settings.json を読み込む
        /// </summary>
        private string? LoadSettingConfig()
        {
            try
            {
                if (!File.Exists(SettingConfigPath))
                    return null;

                string json = File.ReadAllText(SettingConfigPath);
                var config = JsonConvert.DeserializeObject<SettingConfig>(json);

                return config?.AmongUsExePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Settings.json の読み込みに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }


        /// <summary>
        /// 戻るボタンを押したとき
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
        }
    }

    /// <summary>
    /// GitHub Release データモデル
    /// </summary>
    public class GitHubRelease
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("assets")]
        public List<GitHubReleaseAsset> Assets { get; set; }

        // コンストラクターで初期化
        public GitHubRelease()
        {
            Name = "";
            TagName = "";
            Body = "";
            Assets = new List<GitHubReleaseAsset>();
        }
    }

    public class GitHubReleaseAsset
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("size")]
        public long SizeInBytes { get; set; }

        [JsonIgnore]
        public string Size
        {
            get
            {
                if (SizeInBytes >= 1024 * 1024)
                    return $"{SizeInBytes / (1024 * 1024)} MB";
                if (SizeInBytes >= 1024)
                    return $"{SizeInBytes / 1024} KB";
                return $"{SizeInBytes} B";
            }
        }

        [JsonProperty("browser_download_url")]
        public string DownloadUrl { get; set; }

        public GitHubReleaseAsset()
        {
            Name = "";
            DownloadUrl = "";
        }
    }

    public class SettingConfig
    {
        [JsonProperty("AmongUsExePath")]
        public string AmongUsExePath { get; set; }

        public SettingConfig()
        {
            AmongUsExePath = "";
        }
    }

}
