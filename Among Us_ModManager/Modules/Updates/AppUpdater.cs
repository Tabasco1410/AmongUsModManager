using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace Among_Us_ModManager.Modules.Updates
{
    public static class AppUpdater
    {
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

            // パースできなかった場合は念のためアップデートありと判定
            return true;
        }


        public static async Task DownloadAndUpdateAsync(string version, string zipUrl, string extractPath)
        {
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"AmongUsModManager_{version}.zip");
            using var client = new HttpClient();
            var zipData = await client.GetByteArrayAsync(zipUrl);
            await File.WriteAllBytesAsync(tempZipPath, zipData);

            ZipFile.ExtractToDirectory(tempZipPath, extractPath, true);
            File.Delete(tempZipPath);
        }

        public static void StartUpdaterAndExit()
        {
            try
            {
                string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Among Us_ModManager_Updater.exe");

                if (!File.Exists(updaterPath))
                {
                    MessageBox.Show("アップデーターが見つかりませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = updaterPath,
                    UseShellExecute = true
                });

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"アップデーターの起動に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}