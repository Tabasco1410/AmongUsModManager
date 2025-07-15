using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

using Among_Us_ModManager; // ModInfo を参照

namespace Among_Us_ModManager.Pages.Mod_Update
{
    public partial class ModVersionSelectionPage : Page, INotifyPropertyChanged
    {
        private ModInfo _selectedMod;
        private string _amongUsInstallPath;
        private string _dllDisplayPath;

        public ModInfo SelectedMod
        {
            get => _selectedMod;
            set
            {
                if (_selectedMod != value)
                {
                    _selectedMod = value;
                    OnPropertyChanged();
                    UpdateDllDisplayPath(); // DLL 表示パス更新
                }
            }
        }

        public string DllDisplayPath
        {
            get => _dllDisplayPath;
            set
            {
                if (_dllDisplayPath != value)
                {
                    _dllDisplayPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public ModVersionSelectionPage(ModInfo selectedMod, string dllDirectory)
        {
            InitializeComponent();
            SelectedMod = selectedMod;
            // ここで表示したいdllのフルパスをセット
            if (selectedMod.DllNamesForDownload != null && selectedMod.DllNamesForDownload.Count > 0)
            {
                DllDisplayPath = System.IO.Path.Combine(dllDirectory, selectedMod.DllNamesForDownload[0]);
            }
            else
            {
                DllDisplayPath = "(dllファイル名未設定)";
            }
            DataContext = this;
        }

        /// <summary>
        /// DLL の表示用パスを更新
        /// </summary>
        private void UpdateDllDisplayPath()
        {
            if (!string.IsNullOrEmpty(_amongUsInstallPath) && SelectedMod != null && SelectedMod.DllPaths != null && SelectedMod.DllPaths.Any())
            {
                DllDisplayPath = Path.Combine(_amongUsInstallPath, SelectedMod.DllPaths.First());
            }
            else
            {
                DllDisplayPath = "パス不明";
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        private async void ExecuteUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMod == null || SelectedMod.DllNamesForDownload == null || SelectedMod.DllNamesForDownload.Count == 0)
            {
                UpdateStatusTextBlock.Text = "エラー: アップデート情報が不足しています。";
                UpdateStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show("アップデート情報が不足しています。前のページに戻ってやり直してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            UpdateStatusTextBlock.Text = "アップデートを開始します...";
            UpdateStatusTextBlock.Foreground = System.Windows.Media.Brushes.Blue;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager/1.0 (https://github.com/あなたのGitHubユーザー名)");

                    for (int i = 0; i < SelectedMod.DllNamesForDownload.Count; i++)
                    {
                        string dllName = SelectedMod.DllNamesForDownload[i];
                        string owner = "";
                        string repo = "";
                        if (!string.IsNullOrEmpty(SelectedMod.GitHubUrl))
                        {
                            var parts = SelectedMod.GitHubUrl.TrimEnd('/').Split('/');
                            if (parts.Length >= 2)
                            {
                                owner = parts[parts.Length - 2];
                                repo = parts[parts.Length - 1];
                            }
                        }
                        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                            throw new Exception("GitHubリポジトリ情報が不正です");

                        // APIを使わず、リダイレクトURLで最新DLLを取得
                        string downloadUrl = $"https://github.com/{owner}/{repo}/releases/latest/download/{dllName}";
                        byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl);

                        // Nebula on the Shipだけ特別なパスに保存
                        string targetRelativePath;
                        if (repo.Equals("Nebula", StringComparison.OrdinalIgnoreCase))
                        {
                            targetRelativePath = Path.Combine("nebula", dllName); // BepInEx/nebula/xxx.dll
                        }
                        else
                        {
                            targetRelativePath = SelectedMod.DllPaths.Count > i ? SelectedMod.DllPaths[i] : Path.Combine("plugins", dllName);
                        }

                        string targetFilePath = Path.Combine(_amongUsInstallPath, targetRelativePath);
                        string targetDirectory = Path.GetDirectoryName(targetFilePath);

                        if (!Directory.Exists(targetDirectory))
                            Directory.CreateDirectory(targetDirectory);

                        // バックアップ処理
                        if (File.Exists(targetFilePath))
                        {
                            string backupFilePath = targetFilePath + ".bak";
                            if (File.Exists(backupFilePath))
                                File.Delete(backupFilePath);

                            File.Move(targetFilePath, backupFilePath);
                        }

                        await File.WriteAllBytesAsync(targetFilePath, fileBytes);
                        UpdateStatusTextBlock.Text = $"{dllName} を更新しました...";
                    }

                    UpdateStatusTextBlock.Text = $"{SelectedMod.Name} のアップデートが完了しました！";
                    UpdateStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                    MessageBox.Show($"{SelectedMod.Name} のアップデートが正常に完了しました。", "アップデート完了", MessageBoxButton.OK, MessageBoxImage.Information);

                    if (SelectedMod.IsAutoUpdateEnabled)
                    {
                        MessageBox.Show($"{SelectedMod.Name} は自動アップデートが有効です。", "自動アップデート", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                UpdateStatusTextBlock.Text = $"ネットワークエラー: {httpEx.Message}";
                UpdateStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"ネットワークエラー: {httpEx.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException ioEx)
            {
                UpdateStatusTextBlock.Text = $"ファイル操作エラー: {ioEx.Message}";
                UpdateStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"ファイル操作エラー: {ioEx.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                UpdateStatusTextBlock.Text = $"予期せぬエラー: {ex.Message}";
                UpdateStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // DLL名リストをAPIで取得するメソッド例
        private async Task<List<string>> FetchLatestDllNamesFromGitHubRelease(string gitHubUrl)
        {
            var dllNames = new List<string>();
            if (string.IsNullOrEmpty(gitHubUrl)) return dllNames;

            // 例: https://github.com/owner/repo
            var parts = gitHubUrl.TrimEnd('/').Split('/');
            if (parts.Length < 2) return dllNames;
            string owner = parts[parts.Length - 2];
            string repo = parts[parts.Length - 1];

            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AmongUsModManager", "1.0"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                var response = await client.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode) return dllNames;

                var json = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.TryGetProperty("assets", out var assets))
                    {
                        foreach (var asset in assets.EnumerateArray())
                        {
                            if (asset.TryGetProperty("name", out var nameProp) && nameProp.GetString().EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                            {
                                dllNames.Add(nameProp.GetString());
                            }
                        }
                    }
                }
            }
            return dllNames;
        }

        // 例: コンストラクタや初期化時にDLL名を取得して表示
        public async void InitializeDllNames()
        {
            if (SelectedMod != null && !string.IsNullOrEmpty(SelectedMod.GitHubUrl))
            {
                var dllNames = await FetchLatestDllNamesFromGitHubRelease(SelectedMod.GitHubUrl);
                SelectedMod.DllNamesForDownload = dllNames;
                // 必要に応じてUI更新
                if (dllNames.Count > 0)
                    DllDisplayPath = dllNames[0];
                else
                    DllDisplayPath = "(dllファイル名未設定)";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
