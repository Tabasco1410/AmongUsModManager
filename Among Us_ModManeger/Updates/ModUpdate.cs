using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class VersionFetcher
{
    public class ModUpdateData
    {
        public string GitHubUrl { get; set; }
    }

    public static async Task<string> GetRedirectedVersionAsync(string url)
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler);
        using var response = await client.GetAsync(url);

        if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
        {
            string location = response.Headers.Location?.ToString();
            if (!string.IsNullOrEmpty(location))
            {
                int index = location.LastIndexOf("/tag/");
                if (index != -1)
                {
                    return location.Substring(index + "/tag/".Length);
                }
            }
        }

        return null;
    }

    public static async Task RunAsync()
    {
        string jsonPath = "Updates/ModUpdate.json"; // 必要に応じてパスを調整
        if (!File.Exists(jsonPath))
        {
            Console.WriteLine("ModUpdate.json が見つかりません。");
            return;
        }

        string jsonText = await File.ReadAllTextAsync(jsonPath);
        var data = JsonSerializer.Deserialize<ModUpdateData>(jsonText);

        if (data?.GitHubUrl == null)
        {
            Console.WriteLine("GitHubUrl が見つかりません。");
            return;
        }

        string version = await GetRedirectedVersionAsync(data.GitHubUrl);
        if (version != null)
        {
            Console.WriteLine($"取得したバージョン: {version}");
        }
        else
        {
            Console.WriteLine("バージョンの取得に失敗しました。");
        }
    }
}
