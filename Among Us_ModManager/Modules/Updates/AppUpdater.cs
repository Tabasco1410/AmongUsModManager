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

        public static async Task StartUpdaterAndExit()
        {
            try
            {
                string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Among Us_ModManager_Updater.exe");

                // Updater が存在しない場合
                if (!File.Exists(updaterPath))
                {
                    var result = MessageBox.Show(
                        "アップデーターが見つかりませんでした。\nダウンロードしますか？",
                        "確認",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        bool downloadSuccess = await DownloadUpdaterAsync(updaterPath);
                        if (!downloadSuccess)
                        {
                            MessageBox.Show("アップデーターのダウンロードに失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        // ユーザーがダウンロードを拒否した場合は終了
                        return;
                    }
                }

                // Updater を起動（ダウンロード後も同じ）
                var psi = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    UseShellExecute = true,
                    Verb = "runas" // 管理者権限が必要な場合
                };

                Process.Start(psi);

                // アプリ終了（Updater 起動後に必ず行う）
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"アップデーターの起動に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private static async Task<bool> DownloadUpdaterAsync(string savePath)
        {
            try
            {
                const string updaterUrl = "https://github.com/Tabasco1410/AmongUsModManager/releases/download/1.3.3/Among.Us_ModManager_Updater.exe";

                using var client = new HttpClient();
                var data = await client.GetByteArrayAsync(updaterUrl);

                await File.WriteAllBytesAsync(savePath, data);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ダウンロード中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

    }
}