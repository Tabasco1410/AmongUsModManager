using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

using Among_Us_ModManeger; // ModInfo, AmongUsInstallation, AmongUsModDetector, LogOutputを参照

namespace Among_Us_ModManeger.Pages.Mod_Update
{
    public partial class Select_Updatemod : Page
    {
        private ObservableCollection<ModInfo> _modList;
        private static readonly HttpClient _httpClient = new HttpClient();
        private List<AmongUsInstallation> _amongUsInstallations;

        public Select_Updatemod(List<AmongUsInstallation> amongUsInstallations)
        {
            InitializeComponent();

            if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManeger/1.0 (Contact: your-email@example.com)");
            }

            _amongUsInstallations = amongUsInstallations;

            _modList = new ObservableCollection<ModInfo>(GetPredefinedModList());
            ModListView.ItemsSource = _modList;

            LogOutput.Write("DEBUG: Select_Updatemod: Received Among Us Installations:");
            if (_amongUsInstallations != null && _amongUsInstallations.Any())
            {
                foreach (var inst in _amongUsInstallations)
                {
                    LogOutput.Write($"  Name: {inst.Name}, Path: {inst.InstallPath}");
                }
            }
            else
            {
                LogOutput.Write("  No Among Us installations detected.");
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshModsList();
        }

        private List<ModInfo> GetPredefinedModList()
        {
            return new List<ModInfo>
            {
                new ModInfo("TownOfHost", "https://github.com/tukasa0001/TownOfHost",
                    new List<string> { "plugins/TownOfHost.dll" }),

                new ModInfo("TownOfHost_Y", "https://github.com/Yumenopai/TownOfHost_Y",
                    new List<string> { "plugins/TownOfHost_Y-[TAG_NAME].dll" }),

                new ModInfo("TownOfHost-K", "https://github.com/KYMario/TownOfHost-K",
                    new List<string> { "plugins/TownOfHost-K.dll" }),

                new ModInfo("SuperNewRoles", "https://github.com/SuperNewRoles/SuperNewRoles",
                    new List<string> { "plugins/SuperNewRoles.dll", "plugins/Agartha.dll" }),

                new ModInfo("ExtremeRoles", "https://github.com/yukieiji/ExtremeRoles",
                    new List<string> { "plugins/ExtremeRoles.dll", "plugins/ExtremeSkins.dll", "plugins/ExtremeVoiceEngine.dll" }),

                new ModInfo("Nebula on the Ship", "https://github.com/Dolly1016/Nebula",
                    new List<string> { "nebula/Nebula.dll" },
                    new List<string> { "Nebula.dll" }),

                new ModInfo("TownofHost-Enhanced", "https://github.com/EnhancedNetwork/TownofHost-Enhanced",
                    new List<string> { "plugins/TOHE.dll" })
            };
        }

        private async Task RefreshModsList()
        {
            foreach (var mod in _modList)
            {
                DetectInstalledModVersion(mod);
            }

            var fetchTasks = _modList.Select(FetchLatestModVersion).ToList();
            await Task.WhenAll(fetchTasks);
        }

        private void DetectInstalledModVersion(ModInfo mod)
        {
            mod.InstalledInstances.Clear();
            bool anyInstanceHasMod = false;

            if (_amongUsInstallations == null || !_amongUsInstallations.Any())
            {
                mod.InstalledVersion = "未検出のAmong Us";
                mod.DetectionStatus = "Among Us未検出";
                return;
            }

            foreach (var installation in _amongUsInstallations)
            {
                bool modFoundInInstance = false;
                string detectedVersion = "ファイルなし";

                foreach (var dllRelativePath in mod.DllPaths)
                {
                    string version = AmongUsModDetector.GetDllVersion(installation.InstallPath, dllRelativePath);

                    if (version != "ファイルなし" && version != "エラー" && version != "不明" && version != "バージョン情報なし")
                    {
                        detectedVersion = version;
                        modFoundInInstance = true;
                        anyInstanceHasMod = true;
                        LogOutput.Write($"DEBUG: MOD '{mod.Name}' detected in '{installation.Name}' with version: {detectedVersion}");
                        break;
                    }
                    else
                    {
                        LogOutput.Write($"DEBUG: MOD '{mod.Name}' DLL '{dllRelativePath}' not found or error in '{installation.Name}'. Result: {version}");
                    }
                }

                mod.InstalledInstances.Add(new ModInfo.InstalledModInstance
                {
                    InstallationName = installation.Name,
                    Version = detectedVersion,
                    IsInstalled = modFoundInInstance
                });
            }

            if (anyInstanceHasMod)
            {
                int count = mod.InstalledInstances.Count(i => i.IsInstalled);
                mod.InstalledVersion = count > 1 ? $"導入済み ({count}箇所)" : "導入済み";
                mod.DetectionStatus = "導入済み";
            }
            else
            {
                mod.InstalledVersion = "未導入";
                mod.DetectionStatus = "未導入";
            }
        }

        /// <summary>
        /// GitHub APIを利用して最新リリース情報を取得する。
        /// </summary>
        private async Task FetchLatestModVersion(ModInfo mod)
        {
            try
            {
                // GitHubリポジトリURLから owner と repo を抽出
                var match = System.Text.RegularExpressions.Regex.Match(mod.GitHubUrl, @"github\.com/([^/]+)/([^/]+)$");
                if (!match.Success)
                {
                    LogOutput.Write($"WARN: GitHub URL形式不正: {mod.GitHubUrl}");
                    mod.LatestVersion = "不明 (GitHub URL不正)";
                    mod.CurrentDllDownloadUrl = null;
                    return;
                }

                string owner = match.Groups[1].Value;
                string repo = match.Groups[2].Value;

                // APIがForbiddenの場合はリダイレクトでtag取得
                string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
                LogOutput.Write($"DEBUG: Fetching latest release info from GitHub API: {apiUrl}");

                using var response = await _httpClient.GetAsync(apiUrl);
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    // リダイレクトでtag取得
                    string tagName = await GetLatestTagByRedirect(mod.GitHubUrl);
                    if (!string.IsNullOrEmpty(tagName))
                    {
                        mod.LatestVersion = tagName;
                        // DLL名置換
                        if (mod.Name == "TownOfHost_Y")
                        {
                            mod.DllPaths = mod.DllPaths.Select(p => p.Replace("[TAG_NAME]", tagName)).ToList();
                            if (mod.DllNamesForDownload != null)
                                mod.DllNamesForDownload = mod.DllNamesForDownload.Select(p => p.Replace("[TAG_NAME]", tagName)).ToList();
                        }
                        // ダウンロードURLを生成
                        var dllNames = mod.DllNamesForDownload?.Any() == true ? mod.DllNamesForDownload : mod.DllPaths.Select(p => Path.GetFileName(p));
                        // 1つ目だけセット（複数対応は必要に応じて）
                        var firstDll = dllNames.FirstOrDefault();
                        if (!string.IsNullOrEmpty(firstDll))
                        {
                            mod.CurrentDllDownloadUrl = $"https://github.com/{owner}/{repo}/releases/latest/download/{firstDll}";
                        }
                        else
                        {
                            mod.CurrentDllDownloadUrl = null;
                        }
                        return;
                    }
                    else
                    {
                        mod.LatestVersion = "取得失敗 (Forbidden)";
                        mod.CurrentDllDownloadUrl = null;
                        return;
                    }
                }
                if (!response.IsSuccessStatusCode)
                {
                    LogOutput.Write($"ERROR: GitHub APIリクエスト失敗 {mod.Name} Status: {response.StatusCode}");
                    mod.LatestVersion = $"取得失敗 ({response.StatusCode})";
                    mod.CurrentDllDownloadUrl = null;
                    return;
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("tag_name", out var tagNameElem))
                {
                    LogOutput.Write($"WARN: tag_nameがGitHub APIレスポンスにありません: {mod.Name}");
                    mod.LatestVersion = "不明 (tag_nameなし)";
                    mod.CurrentDllDownloadUrl = null;
                    return;
                }

                string tagName2 = tagNameElem.GetString();
                mod.LatestVersion = tagName2;

                if (mod.Name == "TownOfHost_Y")
                {
                    mod.DllPaths = mod.DllPaths.Select(p => p.Replace("[TAG_NAME]", tagName2)).ToList();
                    if (mod.DllNamesForDownload != null)
                        mod.DllNamesForDownload = mod.DllNamesForDownload.Select(p => p.Replace("[TAG_NAME]", tagName2)).ToList();
                }

                if (root.TryGetProperty("assets", out var assetsElem) && assetsElem.ValueKind == JsonValueKind.Array)
                {
                    string foundUrl = null;
                    foreach (var dllName in (mod.DllNamesForDownload?.Any() == true ? mod.DllNamesForDownload : mod.DllPaths.Select(p => Path.GetFileName(p))))
                    {
                        foreach (var asset in assetsElem.EnumerateArray())
                        {
                            if (asset.TryGetProperty("name", out var nameElem) && nameElem.GetString() == dllName)
                            {
                                if (asset.TryGetProperty("browser_download_url", out var urlElem))
                                {
                                    foundUrl = urlElem.GetString();
                                    LogOutput.Write($"DEBUG: Found DLL download URL for {mod.Name}: {foundUrl}");
                                    break;
                                }
                            }
                        }
                        if (foundUrl != null) break;
                    }

                    if (!string.IsNullOrEmpty(foundUrl))
                    {
                        mod.CurrentDllDownloadUrl = foundUrl;
                    }
                    else
                    {
                        mod.CurrentDllDownloadUrl = null;
                        LogOutput.Write($"WARN: No DLL download URL found for {mod.Name} with tag {tagName2}. Searched names: {(mod.DllNamesForDownload?.Any() == true ? string.Join(", ", mod.DllNamesForDownload) : string.Join(", ", mod.DllPaths.Select(p => Path.GetFileName(p))))}");
                    }
                }
                else
                {
                    mod.CurrentDllDownloadUrl = null;
                    LogOutput.Write($"WARN: assets情報がGitHub APIレスポンスにありません: {mod.Name}");
                }
            }
            catch (Exception ex)
            {
                mod.LatestVersion = $"エラー: {ex.Message}";
                mod.CurrentDllDownloadUrl = null;
                LogOutput.Write($"ERROR: MODバージョン取得エラー ({mod.Name}): {ex.Message}");
            }
        }

        private async Task<string> GetLatestTagByRedirect(string gitHubUrl)
        {
            // 例: https://github.com/owner/repo
            var match = System.Text.RegularExpressions.Regex.Match(gitHubUrl, @"github\.com/([^/]+)/([^/]+)$");
            if (!match.Success) return null;
            string owner = match.Groups[1].Value;
            string repo = match.Groups[2].Value;

            // /releases/latest へHEADリクエスト
            var latestUrl = $"https://github.com/{owner}/{repo}/releases/latest";
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManeger/1.0 (Contact: your-email@example.com)");

            using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, latestUrl));
            if (response.StatusCode == System.Net.HttpStatusCode.Found || response.StatusCode == System.Net.HttpStatusCode.Redirect)
            {
                var location = response.Headers.Location?.ToString();
                // 例: .../releases/tag/v1.2.3
                var tagMatch = System.Text.RegularExpressions.Regex.Match(location ?? "", @"/tag/([^/?#]+)");
                if (tagMatch.Success)
                    return tagMatch.Groups[1].Value;
            }
            return null;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        private void ExecuteUpdate_Click(object sender, RoutedEventArgs e)
        {
            var selectedMod = ModListView.SelectedItem as ModInfo;
            if (selectedMod == null)
            {
                MessageBox.Show("アップデートするMODを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!selectedMod.IsUpdateAvailable)
            {
                MessageBox.Show($"{selectedMod.Name} には現在アップデートがありません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrEmpty(selectedMod.CurrentDllDownloadUrl))
            {
                MessageBox.Show($"{selectedMod.Name} のダウンロードURLが見つかりません。手動でアップデートしてください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var targetInstallation = _amongUsInstallations?.FirstOrDefault();
            if (targetInstallation == null)
            {
                MessageBox.Show("Among Usのインストールパスが検出できませんでした。先に「Among Us.exe を選択」で設定してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                LogOutput.Write("ERROR: No Among Us installation found for update execution.");
                return;
            }

            var updatePage = new ModVersionSelectionPage(selectedMod, targetInstallation.InstallPath);
            NavigationService.Navigate(updatePage);
        }

        private void ManualFolderSelect_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("「手動で選択」機能は現在開発中です。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
