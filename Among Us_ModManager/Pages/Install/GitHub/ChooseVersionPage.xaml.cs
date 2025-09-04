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

                return JsonConvert.DeserializeObject<List<GitHubRelease>>(response);
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
            if (FileListView.SelectedItem is GitHubReleaseAsset selectedFile)
            {
                // .dll はインストール不可
                if (selectedFile.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(".dll ファイルはインストールできません。");
                    return;
                }

                // ダウンロード先フォルダ（AppData 下など）
                string downloadsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AmongUsModManager", "Downloads"
                );
                Directory.CreateDirectory(downloadsDir);

                string zipFilePath = Path.Combine(downloadsDir, selectedFile.Name);

                try
                {
                    MessageBox.Show("ダウンロードを開始します...", "情報", MessageBoxButton.OK, MessageBoxImage.Information);

                    using (var client = new HttpClient())
                    using (var stream = await client.GetStreamAsync(selectedFile.DownloadUrl))
                    using (var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write))
                    {
                        await stream.CopyToAsync(fileStream);
                    }

                    MessageBox.Show("ダウンロードが完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Settings.json を読み込む
                    string amongUsExePath = LoadSettingConfig();
                    if (string.IsNullOrEmpty(amongUsExePath) || !File.Exists(amongUsExePath))
                    {
                        MessageBox.Show("Settings.json が存在しないか Among Us.exe のパスが無効です。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // ZIP名から Mod名（フォルダ名）を取得
                    // ZIP名から Mod名を取得（拡張子なし）
                    string rawName = Path.GetFileNameWithoutExtension(selectedFile.Name);

                    // Mod名だけに整形
                    // 例: "TownOfHost-v1.0.0" → "TownOfHost"
                    string modName = rawName;

                    // ハイフン区切りでバージョンっぽい部分を消す
                    int dashIndex = rawName.IndexOf('-');
                    if (dashIndex > 0)
                    {
                        modName = rawName.Substring(0, dashIndex);
                    }

                    // アンダースコア区切りでも対応
                    int underscoreIndex = modName.IndexOf('_');
                    if (underscoreIndex > 0)
                    {
                        modName = modName.Substring(0, underscoreIndex);
                    }



                    string sourceFolderPath = Path.GetDirectoryName(amongUsExePath)!;

                    // 既存フォルダの処理（Yes/No/Cancel）… ←ここは今のままでOK

                    // 遷移 (Put_Zip_File に渡す)
                    NavigationService.Navigate(new Among_Us_ModManager.Pages.PutZipFile.Put_Zip_File(
                        sourceFolderPath,
                        zipFilePath,
                        modName
                    ));

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ダウンロードに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("インストールするファイルを選択してください。");
            }
        }

        /// <summary>
        /// Settings.json を読み込む
        /// </summary>
        private string LoadSettingConfig()
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
    }

    /// <summary>
    /// GitHub Release Asset（ファイル情報）データモデル
    /// </summary>
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
    }

    /// <summary>
    /// Settings.json データモデル
    /// </summary>
    public class SettingConfig
    {
        [JsonProperty("AmongUsExePath")]
        public string AmongUsExePath { get; set; }
    }
}
