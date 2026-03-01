using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AmongUsModManager.Services;

namespace AmongUsModManager.Models.Services
{
   
    public static class GitHubAuthService
    {
        
        private static HttpClient? _client;
        private static string _lastToken = "";

       
        public static HttpClient GetClient()
        {
            var config = ConfigService.Load();
            string token = config.GitHubToken?.Trim() ?? "";

            if (_client == null || token != _lastToken)
            {
                _client = BuildClient(token);
                _lastToken = token;
                LogService.Debug("GitHubAuthService",
                    string.IsNullOrEmpty(token) ? "未認証クライアント生成" : "PAT認証クライアント生成");
            }
            return _client;
        }

        private static HttpClient BuildClient(string token)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager-App");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public static async Task<(bool ok, string result)> VerifyTokenAsync(string token)
        {
            try
            {
                var client = BuildClient(token);
                var response = await client.GetAsync("https://api.github.com/user");
                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<GitHubUserResponse>();
                    return (true, user?.login ?? "（不明）");
                }
                else if ((int)response.StatusCode == 401)
                    return (false, "トークンが無効です。正しいトークンを入力してください。");
                else if ((int)response.StatusCode == 403)
                    return (false, "アクセスが拒否されました（403）。");
                else
                    return (false, $"エラー: {response.StatusCode}");
            }
            catch (System.Exception ex)
            {
                return (false, $"接続エラー: {ex.Message}");
            }
        }

        private class GitHubUserResponse
        {
            public string? login { get; set; }
        }
    }
}
