using System;
using System.Collections.Generic;

namespace AmongUsModManager.Models
{
    public class GitHubRelease
    {
        public string tag_name { get; set; } = "";
        public string? body { get; set; }
        public DateTime? published_at { get; set; }
        public string? html_url { get; set; }
        public List<GitHubAsset> assets { get; set; } = new();
    }

    public class GitHubAsset
    {
        public string name { get; set; } = "";
        public string browser_download_url { get; set; } = "";
    }

    public class GitHubSearchResult { public List<GitHubRepoItem> items { get; set; } = new(); }
    public class GitHubRepoItem
    {
        public string name { get; set; } = "";
        public string full_name { get; set; } = "";
        public GitHubOwnerItem owner { get; set; } = new();
    }
    public class GitHubOwnerItem { public string login { get; set; } = ""; }
}
