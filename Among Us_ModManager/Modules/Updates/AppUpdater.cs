using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Among_Us_ModManager.Modules.Updates
{
    public static class AppUpdater
    {
        private const string GitHubOwner = "Tabasco1410";
        private const string GitHubRepo = "AmongUsModManager";
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 現在のバージョンと比較して更新があるか確認
        /// </summary>
        public static async Task<bool> IsUpdateAvailableAsync(string currentVersion, string versionUrl)
        {
            using var client = new HttpClient();
            string latestVersionRaw = await client.GetStringAsync(versionUrl);
            string latestVersion = latestVersionRaw.Trim();

            string currentNormalized = currentVersion.Trim().TrimEnd('s', 'S');
            string latestNormalized = latestVersion.Trim().TrimEnd('s', 'S');

            LogOutput.Write($"現在のバージョン: {currentVersion}（比較用: {currentNormalized}）");
            LogOutput.Write($"最新のバージョン: {latestVersion}（比較用: {latestNormalized}）");

            if (Version.TryParse(currentNormalized, out var currentVer) &&
                Version.TryParse(latestNormalized, out var latestVer))
            {
                return currentVer < latestVer;
            }

            return true; // パースできなかった場合はアップデートあり
        }

        /// <summary>
        /// ZIPをダウンロードして展開する
        /// </summary>
        public static async Task DownloadAndUpdateAsync(string version, string zipUrl, string extractPath)
        {
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"AmongUsModManager_{version}.zip");
            using var client = new HttpClient();
            var zipData = await client.GetByteArrayAsync(zipUrl);
            await File.WriteAllBytesAsync(tempZipPath, zipData);

            ZipFile.ExtractToDirectory(tempZipPath, extractPath, true);
            File.Delete(tempZipPath);
        }

        /// <summary>
        /// Updaterを最新版に更新して起動
        /// </summary>
        public static async Task StartUpdaterAndExit()
        {
            try
            {
                string updaterExePath = Path.Combine(BaseDir, "Among Us_ModManager_Updater.exe");

                // UpdaterのZIPがReleaseにあるか確認してダウンロード
                string? updaterZipUrl = await GetLatestUpdaterZipUrlAsync();
                if (updaterZipUrl != null)
                {
                    await DownloadAndUpdateAsync("Updater", updaterZipUrl, BaseDir);
                }

                // ZIPが無くてもexeがなければ過去Releaseから取得
                if (!File.Exists(updaterExePath))
                {
                    updaterZipUrl = await GetLatestUpdaterZipUrlFromAllReleasesAsync();
                    if (updaterZipUrl != null)
                        await DownloadAndUpdateAsync("Updater", updaterZipUrl, BaseDir);
                }

                if (!File.Exists(updaterExePath))
                {
                    MessageBox.Show("Updaterを取得できませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Updater起動
                var psi = new ProcessStartInfo
                {
                    FileName = updaterExePath,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Updaterの起動に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 最新ReleaseにUpdater ZIPがある場合のURLを取得
        /// </summary>
        private static async Task<string?> GetLatestUpdaterZipUrlAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AmongUsModManager");

                string latestUrl = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
                string latestJson = await client.GetStringAsync(latestUrl);

                using var doc = JsonDocument.Parse(latestJson);
                foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString() ?? "";
                    if (name.Equals("Among Us_ModManager_Updater.zip", StringComparison.OrdinalIgnoreCase))
                        return asset.GetProperty("browser_download_url").GetString();
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 過去Releaseも含めてUpdater ZIPのURLを取得
        /// </summary>
        private static async Task<string?> GetLatestUpdaterZipUrlFromAllReleasesAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AmongUsModManager");

                string allUrl = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases";
                string allJson = await client.GetStringAsync(allUrl);

                using var doc = JsonDocument.Parse(allJson);
                foreach (var release in doc.RootElement.EnumerateArray())
                {
                    foreach (var asset in release.GetProperty("assets").EnumerateArray())
                    {
                        string name = asset.GetProperty("name").GetString() ?? "";
                        if (name.Equals("Among Us_ModManager_Updater.zip", StringComparison.OrdinalIgnoreCase))
                            return asset.GetProperty("browser_download_url").GetString();
                    }
                }
            }
            catch { }

            return null;
        }
    }
}
