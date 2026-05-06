using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace AmongUsModManager.Models.Services
{
    public record UpdateResult(
        bool HasUpdate,
        string LatestTag,
        string ReleaseUrl,
        string? DownloadUrl,
        bool IsPreRelease,
        bool IsLatest,
        string? ReleaseNotes);

    public static class AppUpdateService
    {
        private const string Owner = "Tabasco1410";
        private const string Repo = "AmongUsModManager";

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

        public static async Task<UpdateResult?> CheckAsync()
        {
            if (IsAutoUpdateDisabled) return null;

            try
            {
                var release = await _http.GetFromJsonAsync<GitHubReleaseInfo>(
                    $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest");
                if (release == null) return null;

                bool hasUpdate = release.tag_name != App.AppVersion
                              && release.tag_name != $"v{App.AppVersion}";

                string? downloadUrl = FindSetupExeAsset(release.assets);

                return new UpdateResult(
                    hasUpdate,
                    release.tag_name,
                    release.html_url ?? "",
                    downloadUrl,
                    release.prerelease,
                    true,
                    release.body);
            }
            catch { return null; }
        }

        public static async Task<List<UpdateResult>> GetVersionHistoryAsync()
        {
            var results = new List<UpdateResult>();
            try
            {
                var releases = await _http.GetFromJsonAsync<List<GitHubReleaseInfo>>(
                    $"https://api.github.com/repos/{Owner}/{Repo}/releases?per_page=30");
                if (releases == null) return results;

                bool latestMarked = false;
                foreach (var release in releases)
                {
                    if (string.IsNullOrEmpty(release.tag_name)) continue;

                    var tag = release.tag_name.TrimStart('v');
                    if (!IsVersionAtLeast(tag, "1.4.1")) continue;

                    bool isCurrent = release.tag_name == App.AppVersion
                                  || release.tag_name == $"v{App.AppVersion}";

                    bool isLatest = false;
                    if (!latestMarked && !release.prerelease)
                    {
                        isLatest = true;
                        latestMarked = true;
                    }

                    string? downloadUrl = FindSetupExeAsset(release.assets);
                    results.Add(new UpdateResult(
                        !isCurrent,
                        release.tag_name,
                        release.html_url ?? "",
                        downloadUrl,
                        release.prerelease,
                        isLatest,
                        release.body));
                }
            }
            catch { }
            return results;
        }

        private static bool IsVersionAtLeast(string tag, string minimum)
        {
            if (Version.TryParse(tag, out var v) && Version.TryParse(minimum, out var min))
                return v >= min;
            return false;
        }

        private static string? FindSetupExeAsset(GitHubAsset[]? assets)
        {
            if (assets == null) return null;
            string? fallback = null;
            foreach (var asset in assets)
            {
                if (asset.name == null) continue;
                if (asset.name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    if (asset.name.Contains("Setup", StringComparison.OrdinalIgnoreCase))
                        return asset.browser_download_url;
                    fallback ??= asset.browser_download_url;
                }
            }
            return fallback;
        }

        public static async Task<bool> DownloadAndApplyAsync(UpdateResult result, IProgress<int>? progress = null)
        {
            if (string.IsNullOrEmpty(result.DownloadUrl))
                return false;

            string fileName = Path.GetFileName(new Uri(result.DownloadUrl).LocalPath);
            string tempPath = Path.Combine(Path.GetTempPath(), fileName);

            try
            {
                using var response = await _http.GetAsync(result.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                long? total = response.Content.Headers.ContentLength;

                await using var srcStream = await response.Content.ReadAsStreamAsync();
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
                string currentDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
                string installArgs = $"/SILENT /SUPPRESSMSGBOXES /UPDATE \"/DIR={currentDir}\"";

                Process.Start(new ProcessStartInfo(tempPath)
                {
                    Arguments = installArgs,
                    UseShellExecute = true,
                });

                Application.Current.Exit();
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
            public string? body { get; set; }
            public bool prerelease { get; set; }
            public GitHubAsset[]? assets { get; set; }
        }

        private class GitHubAsset
        {
            public string? name { get; set; }
            public string? browser_download_url { get; set; }
        }
    }
}
