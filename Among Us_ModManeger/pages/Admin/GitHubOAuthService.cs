using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Among_Us_ModManeger.Auth
{
    public class GitHubOAuthService
    {
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string redirectUri;

        private static readonly HttpClient httpClient = new();

        public GitHubOAuthService()
        {
            clientId = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID") ?? throw new Exception("GITHUB_CLIENT_IDが設定されていません");
            clientSecret = Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET") ?? throw new Exception("GITHUB_CLIENT_SECRETが設定されていません");
            redirectUri = "http://localhost:57853/callback";
        }

        /// <summary>
        /// 認証用URLを生成してブラウザで開く
        /// </summary>
        public void OpenLoginPage()
        {
            string state = Guid.NewGuid().ToString(); // CSRF対策に利用推奨
            var url = $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope=read:user&state={state}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        /// <summary>
        /// codeを使ってアクセストークンを取得
        /// </summary>
        public async Task<string> ExchangeCodeForTokenAsync(string code)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri)
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");
            request.Headers.Accept.ParseAdd("application/json");
            request.Content = content;

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("access_token", out var tokenElem))
            {
                return tokenElem.GetString();
            }

            throw new Exception("アクセストークンの取得に失敗しました。レスポンス: " + json);
        }

        /// <summary>
        /// アクセストークンを使ってユーザー名を取得
        /// </summary>
        public async Task<string> GetUserNameAsync(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
            request.Headers.UserAgent.ParseAdd("AmongUsModManager");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", accessToken);

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("login", out var loginElem))
            {
                return loginElem.GetString();
            }

            throw new Exception("ユーザー名の取得に失敗しました。レスポンス: " + json);
        }

        /// <summary>
        /// GitHubログインを行い、ユーザー名を取得する一連の処理
        /// </summary>
        public async Task<string> LoginAndGetUsernameAsync()
        {
            // 認証ページをブラウザで開く
            OpenLoginPage();

            // ローカルのHTTPサーバーで認証コードを待機する（OAuthCallbackListenerを利用）
            string code = await OAuthCallbackListener.WaitForCodeAsync(redirectUri + "/");

            if (string.IsNullOrEmpty(code))
                return null;

            // 認証コードからアクセストークンを取得
            string token = await ExchangeCodeForTokenAsync(code);

            if (string.IsNullOrEmpty(token))
                return null;

            // アクセストークンでユーザー名を取得
            string username = await GetUserNameAsync(token);

            return username;
        }
    }
}
