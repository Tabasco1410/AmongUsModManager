using Among_Us_ModManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class OAuthManager
{
    private static OAuthManager _instance;
    public static OAuthManager Instance => _instance ??= new OAuthManager();

    private string ClientId = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID");
    private string ClientSecret = Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET");

    private string RedirectUri = "http://localhost:57853/callback/";

    private string tokenFilePath;

    public bool IsLoggedIn { get; private set; } = false;
    public bool IsAdmin { get; private set; } = false;
    public string UserName { get; private set; }
    private string AccessToken;

    private OAuthManager()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folder = Path.Combine(appData, "AmongUsModManager");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        tokenFilePath = Path.Combine(folder, "token.json");
    }

    public async Task<bool> InitializeAsync()
    {
        return await LoadTokenAsync();
    }

    private async Task<bool> LoadTokenAsync()
    {
        if (!File.Exists(tokenFilePath))
            return false;

        try
        {
            string json = await File.ReadAllTextAsync(tokenFilePath);
            var data = JsonSerializer.Deserialize<TokenData>(json);

            AccessToken = data.AccessToken;
            if (string.IsNullOrEmpty(AccessToken))
                return false;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AmongUsModManager");

            var userResponse = await client.GetStringAsync("https://api.github.com/user");
            using var doc = JsonDocument.Parse(userResponse);
            UserName = doc.RootElement.GetProperty("login").GetString();

            IsAdmin = UserName == "Tabasco1410";
            IsLoggedIn = true;

            LogOutput.Write($"OAuthManager: トークン読み込み成功 - UserName={UserName}, IsAdmin={IsAdmin}");
            return true;
        }
        catch (Exception ex)
        {
            LogOutput.Write($"OAuthManager: トークンの読み込み失敗 - {ex.Message}");
            return false;
        }
    }

    private void SaveToken()
    {
        if (string.IsNullOrEmpty(AccessToken))
            return;

        var data = new TokenData
        {
            AccessToken = AccessToken,
            UserName = UserName,
            IsAdmin = IsAdmin
        };

        string json = JsonSerializer.Serialize(data);
        File.WriteAllText(tokenFilePath, json);
        LogOutput.Write("OAuthManager: アクセストークンを保存しました。");
    }

    public async Task<bool> LoginAsync()
    {
        LogOutput.Write("OAuthManager.LoginAsync: 開始");

        if (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(ClientSecret))
        {
            LogOutput.Write("OAuthManager.LoginAsync: 環境変数からClient IDまたはSecretが取得できませんでした。");
            return false;
        }

        if (!string.IsNullOrEmpty(AccessToken))
        {
            LogOutput.Write("OAuthManager.LoginAsync: 既存のアクセストークンがあります。");
            IsLoggedIn = true;
            return IsAdmin;
        }

        string state = Guid.NewGuid().ToString();
        string authUrl =
            $"https://github.com/login/oauth/authorize?client_id={ClientId}&redirect_uri={Uri.EscapeDataString(RedirectUri)}&scope=read:user&state={state}";
        LogOutput.Write($"OAuthManager.LoginAsync: 認証URL生成完了: {authUrl}");

        var listener = new HttpListener();
        listener.Prefixes.Add(RedirectUri);

        try
        {
            listener.Start();
            LogOutput.Write("OAuthManager.LoginAsync: ローカルHTTPリスナー開始");

            // Chrome があれば Chrome で開く、なければ既定ブラウザ
            string chromePath = Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Google\Chrome\Application\chrome.exe");
            if (!File.Exists(chromePath))
                chromePath = Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe");

            if (File.Exists(chromePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = chromePath,
                    Arguments = authUrl,
                    UseShellExecute = true
                });
                LogOutput.Write("OAuthManager.LoginAsync: Chrome で認証ページを開きました");
            }
            else
            {
                // 規定ブラウザ
                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });
                LogOutput.Write("OAuthManager.LoginAsync: 規定ブラウザで認証ページを開きました");
            }

            var context = await listener.GetContextAsync();
            var query = context.Request.QueryString;
            string code = query["code"];
            string returnedState = query["state"];

            string responseString = "<html><body>認証完了しました。この画面を閉じてください。</body></html>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
            listener.Stop();
            LogOutput.Write("OAuthManager.LoginAsync: 認証完了ページを返しました");

            if (state != returnedState)
            {
                LogOutput.Write("OAuthManager.LoginAsync: stateの検証に失敗しました（偽造の可能性）");
                return false;
            }

            if (string.IsNullOrEmpty(code))
            {
                LogOutput.Write("OAuthManager.LoginAsync: codeが空です。認証失敗。");
                return false;
            }

            using var client = new HttpClient();
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("client_id", ClientId),
                new KeyValuePair<string,string>("client_secret", ClientSecret),
                new KeyValuePair<string,string>("code", code),
                new KeyValuePair<string,string>("redirect_uri", RedirectUri)
            });

            var tokenResponse = await client.PostAsync("https://github.com/login/oauth/access_token", tokenRequest);
            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            LogOutput.Write($"OAuthManager.LoginAsync: トークンレスポンス受信: {tokenContent}");

            var queryParams = System.Web.HttpUtility.ParseQueryString(tokenContent);
            string accessToken = queryParams["access_token"];

            if (string.IsNullOrEmpty(accessToken))
            {
                LogOutput.Write("OAuthManager.LoginAsync: アクセストークン取得に失敗しました。access_token が空です。");
                return false;
            }

            AccessToken = accessToken;
            SaveToken();

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AmongUsModManager");

            var userResponse = await client.GetStringAsync("https://api.github.com/user");
            using var doc = JsonDocument.Parse(userResponse);
            UserName = doc.RootElement.GetProperty("login").GetString();

            IsAdmin = UserName == "Tabasco1410";
            IsLoggedIn = true;

            LogOutput.Write($"OAuthManager.LoginAsync: ログイン成功 - ユーザー名: {UserName}, 管理者: {IsAdmin}");
            return IsAdmin;
        }
        catch (Exception ex)
        {
            LogOutput.Write($"OAuthManager.LoginAsync: 例外発生 - {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            try { listener.Stop(); } catch { }
            return false;
        }
    }

    public void Logout()
    {
        AccessToken = null;
        IsLoggedIn = false;
        IsAdmin = false;
        UserName = null;

        if (File.Exists(tokenFilePath))
        {
            File.Delete(tokenFilePath);
            LogOutput.Write("OAuthManager: トークンファイルを削除しました。");
        }
    }

    private class TokenData
    {
        public string AccessToken { get; set; }
        public string UserName { get; set; }
        public bool IsAdmin { get; set; }
    }
}
