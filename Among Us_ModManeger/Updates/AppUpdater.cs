using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace Among_Us_ModManeger.Updates
{
    public static class AppUpdater
    {
        public static async Task<bool> IsUpdateAvailableAsync(string localVersion, string versionUrl)
        {
            using var client = new HttpClient();
            var onlineVersion = await client.GetStringAsync(versionUrl);
            return onlineVersion.Trim() != localVersion;
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
                string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Among Us_ModManeger_Updater.exe");

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
