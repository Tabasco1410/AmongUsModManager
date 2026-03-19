using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AmongUsModManager.Models.Services
{
    public static class AppUpdateService
    {        
        private const string Owner = "Tabasco1410";
        private const string Repo  = "AmongUsModManager";

        

        public static bool IsDebugBuild
        {
#if DEBUG
            get => true;
#else
            get => false;
#endif
        }

       
        public static bool IsAutoUpdateDisabled => App.IsPreRelease;

        private static readonly HttpClient _http = new();

        static AppUpdateService()
        {
            _http.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager-App");
        }

        public record UpdateResult(bool HasUpdate, string LatestTag, string ReleaseUrl, string? DownloadUrl);

        public static async Task<UpdateResult?> CheckAsync()
        {
            // プレリリース のときは自動アップデートを無効化しておきまああああす
            if (IsAutoUpdateDisabled) return null;

            try
            {
                var release = await _http.GetFromJsonAsync<GitHubReleaseInfo>(
                    $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest");
                if (release == null) return null;

                bool hasUpdate = release.tag_name != App.AppVersion
                              && release.tag_name != $"v{App.AppVersion}";

                string? downloadUrl = null;
                if (release.assets != null)
                {
                    foreach (var asset in release.assets)
                    {
                        //リリースのassetsの中の.exeを探すのでインストーラーしかおかないようにする
                        if (asset.name != null &&
                            (asset.name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))                         )
                        {
                            downloadUrl = asset.browser_download_url;
                            break;
                        }
                    }
                }

                return new UpdateResult(hasUpdate, release.tag_name, release.html_url ?? "", downloadUrl);
            }
            catch { return null; }
        }

       
        public static async Task<bool> DownloadAndApplyAsync(UpdateResult result, IProgress<int>? progress = null)
        {
            if (string.IsNullOrEmpty(result.DownloadUrl))
                return false;

           
            string fileName  = Path.GetFileName(new Uri(result.DownloadUrl).LocalPath);
            string tempPath  = Path.Combine(Path.GetTempPath(), fileName);

            try
            {
               
                using var response = await _http.GetAsync(result.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                long? total = response.Content.Headers.ContentLength;

                await using var srcStream  = await response.Content.ReadAsStreamAsync();
                await using var fileStream = File.Create(tempPath);

                byte[] buffer = new byte[81920];
                long downloaded = 0;
                int read;
                while ((read = await srcStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read));
                    downloaded += read;
                    if (total.HasValue && progress != null)
                        progress.Report((int)(downloaded * 100 / total.Value));
                }
            }
            catch { return false; }

           
            if (tempPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                return true;
            }

            
            string downloadsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string destZip = Path.Combine(downloadsFolder, Path.GetFileName(tempPath));
            try { File.Copy(tempPath, destZip, overwrite: true); } catch { }
            Process.Start(new ProcessStartInfo(downloadsFolder) { UseShellExecute = true });
            return true;
        }

        private class GitHubReleaseInfo
        {
            public string tag_name { get; set; } = "";
            public string? html_url { get; set; }
            public GitHubAsset[]? assets { get; set; }
        }

        private class GitHubAsset
        {
            public string? name { get; set; }
            public string? browser_download_url { get; set; }
        }
    }
}
