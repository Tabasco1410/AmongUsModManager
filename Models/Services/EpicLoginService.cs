using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AmongUsModManager.Models;
using AmongUsModManager.Services;
using Windows.Security.Credentials;

namespace AmongUsModManager.Models.Services
{
    public class EpicSession
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = "";
        [JsonPropertyName("account_id")] public string AccountId { get; set; } = "";
        [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
    }

    internal static class EpicKeyring
    {
        private const string Resource = "AmongUsModManager-EpicSession";
        private const string Username = "epic_session";

        public static void Save(EpicSession session)
        {
            try
            {
                string json = JsonSerializer.Serialize(session);
                var vault = new PasswordVault();
                try { vault.Remove(vault.Retrieve(Resource, Username)); } catch { }
                vault.Add(new PasswordCredential(Resource, Username, json));
                LogService.Debug("EpicKeyring", "キーリング保存成功。");
            }
            catch (Exception ex)
            {
                LogService.Warn("EpicKeyring", $"キーリング保存失敗: {ex.Message}");
            }
        }

        public static EpicSession? Load()
        {
            try
            {
                var vault = new PasswordVault();
                var cred = vault.Retrieve(Resource, Username);
                cred.RetrievePassword();
                return JsonSerializer.Deserialize<EpicSession>(cred.Password);
            }
            catch { return null; }
        }

        public static void Clear()
        {
            try
            {
                var vault = new PasswordVault();
                vault.Remove(vault.Retrieve(Resource, Username));
            }
            catch { }
        }
    }

    public static class EpicLoginService
    {
        private const string OAuthHost = "account-public-service-prod03.ol.epicgames.com";
        private const string TokenEndpoint = $"https://{OAuthHost}/account/api/oauth/token";
        private const string ExchangeEndpoint = $"https://{OAuthHost}/account/api/oauth/exchange";

        public const string LauncherClientId = "34a02cf8f4414e29b15921876da36f9a";
        public const string LauncherClientSecret = "daafbccc737745039dffe53d94fc76cf";

        public const string EpicAppId = "Hemomancer";
        private const string EpicSandboxId = "0a18471f93d448e897a7f7de9e39ae8e";
        private const string EpicDeploymentId = "a5aa686defa64131b1edc48c31b40d1a";

        private const string UserAgent =
            "UELauncher/11.0.1-14907503+++Portal+Release-Live Windows/10.0.19041.1.256.64bit";

        private static readonly HttpClient _http;
        private static EpicSession? _sessionCache;
        private static readonly object _cacheLock = new();

        static EpicLoginService()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }

        private static string BasicAuth =>
            Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{LauncherClientId}:{LauncherClientSecret}"));

        private static string SessionFilePath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AmongUsModManager", "epic_session.json");

        // ─── 認証URL生成 ──────────────────────────────────────────────────
        // ブラウザで開くと Epic ログイン → 完了後に /id/api/redirect へリダイレクト。
        // そのページを開いたまま JSON 内の authorizationCode をコピーしてもらう。
        public static string GetAuthUrl()
        {
            string redirectUrl = Uri.EscapeDataString(
                $"https://www.epicgames.com/id/api/redirect" +
                $"?clientId={LauncherClientId}&responseType=code");
            return $"https://www.epicgames.com/id/login?redirectUrl={redirectUrl}";
        }

        // AccountPage から呼び出すラッパー（引数は互換性のために残す）
        public static string BuildAuthUrl(string _redirectUri = "") => GetAuthUrl();

        /// <summary>
        /// ブラウザでEpicログインページを開く。
        /// ユーザーがログインすると epicgames.com/id/api/redirect に JSON が表示されるので
        /// "authorizationCode" の値をコピーして LoginWithAuthCodeAsync に渡す。
        /// </summary>
        public static void OpenBrowserForLogin()
        {
            string url = GetAuthUrl();
            LogService.Info("EpicLoginService", $"Epicログインページをブラウザで開きます: {url}");
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        // ─── 認可コードでログイン ─────────────────────────────────────────
        public static async Task<EpicLoginResult> LoginWithAuthCodeAsync(string code)
        {
            string normalized = code.Trim().Replace("\"", "");
            if (string.IsNullOrEmpty(normalized))
                return EpicLoginResult.Fail("認可コードが空です。");

            try
            {
                var session = await OAuthRequestAsync(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = normalized,
                    ["token_type"] = "eg1",
                });

                if (session == null)
                    return EpicLoginResult.Fail("レスポンスのパースに失敗しました。");

                LogService.Info("EpicLoginService",
                    $"ログイン成功: {session.DisplayName} ({session.AccountId})");
                SaveSession(session);
                return EpicLoginResult.Ok(session.DisplayName ?? "", session.AccountId);
            }
            catch (Exception ex)
            {
                LogService.Error("EpicLoginService", "認証コード交換エラー", ex);
                return EpicLoginResult.Fail($"ログインに失敗しました: {ex.Message}");
            }
        }

        // ─── セッション保存 ───────────────────────────────────────────────
        public static void SaveSession(EpicSession session)
        {
            lock (_cacheLock) { _sessionCache = session; }
            EpicKeyring.Save(session);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SessionFilePath)!);
                File.WriteAllText(SessionFilePath,
                    JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true }),
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogService.Warn("EpicLoginService", $"セッションファイル保存失敗: {ex.Message}");
            }

            var config = ConfigService.Load();
            config.EpicAccountId = session.AccountId;
            config.EpicDisplayName = session.DisplayName?.Trim() ?? "";
            ConfigService.Save(config);

            LogService.Info("EpicLoginService",
                $"セッション保存完了: {session.DisplayName} ({session.AccountId})");
        }

        // ─── セッション読み込み ───────────────────────────────────────────
        public static EpicSession? LoadSession()
        {
            lock (_cacheLock) { if (_sessionCache != null) return _sessionCache; }

            var keyringSession = EpicKeyring.Load();
            if (keyringSession != null)
            {
                TrySaveFileOnly(keyringSession);
                lock (_cacheLock) { _sessionCache = keyringSession; }
                return keyringSession;
            }

            try
            {
                if (!File.Exists(SessionFilePath)) return null;
                var fileSession = JsonSerializer.Deserialize<EpicSession>(
                    File.ReadAllText(SessionFilePath, Encoding.UTF8));
                if (fileSession != null)
                {
                    EpicKeyring.Save(fileSession);
                    lock (_cacheLock) { _sessionCache = fileSession; }
                }
                return fileSession;
            }
            catch (Exception ex)
            {
                LogService.Warn("EpicLoginService", $"セッションファイル読み込み失敗: {ex.Message}");
                return null;
            }
        }

        // ─── セッション削除 ───────────────────────────────────────────────
        public static void ClearSession()
        {
            lock (_cacheLock) { _sessionCache = null; }
            EpicKeyring.Clear();
            try { if (File.Exists(SessionFilePath)) File.Delete(SessionFilePath); }
            catch (Exception ex) { LogService.Warn("EpicLoginService", $"セッションファイル削除失敗: {ex.Message}"); }
        }

        public static bool IsLoggedIn() => LoadSession() != null;
        public static bool IsLoggedIn(AppConfig _) => IsLoggedIn();

        // ─── 起動時セッション復元 ─────────────────────────────────────────
        public static async Task<bool> TryRestoreSessionAsync()
        {
            var session = LoadSession();
            if (session == null)
            {
                LogService.Debug("EpicLoginService", "保存済みセッションなし。");
                return false;
            }

            LogService.Info("EpicLoginService", "保存済みセッション発見。refresh_token で復元を試みます。");
            try
            {
                var refreshed = await OAuthRequestAsync(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = session.RefreshToken,
                    ["token_type"] = "eg1",
                });
                if (refreshed == null) throw new Exception("レスポンスがnull");
                LogService.Info("EpicLoginService", $"セッション復元成功: {refreshed.DisplayName}");
                SaveSession(refreshed);
                return true;
            }
            catch (Exception ex)
            {
                LogService.Warn("EpicLoginService", $"セッション復元失敗（期限切れの可能性）: {ex.Message}");
                return false;
            }
        }

        // ─── アクセストークン確保 ─────────────────────────────────────────
        public static async Task<string?> EnsureAccessTokenAsync()
        {
            var session = LoadSession();
            if (session == null)
            {
                LogService.Warn("EpicLoginService", "セッションなし。ログインが必要です。");
                return null;
            }

            try
            {
                var refreshed = await OAuthRequestAsync(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = session.RefreshToken,
                    ["token_type"] = "eg1",
                });
                if (refreshed == null) throw new Exception("レスポンスがnull");
                SaveSession(refreshed);
                return refreshed.AccessToken;
            }
            catch (Exception ex)
            {
                LogService.Warn("EpicLoginService", $"トークン更新失敗。再ログインが必要です: {ex.Message}");
                return null;
            }
        }

        // ─── ゲーム起動 ───────────────────────────────────────────────────
        public static async Task<LaunchResult> LaunchDirectAsync(string exePath, string workDir)
        {
            if (!IsLoggedIn())
                return LaunchResult.Fail(
                    "Epic アカウントにログインしていません。\nアカウントページでログインしてください。");

            string? accessToken = await EnsureAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
                return LaunchResult.Fail("Epic トークンの取得に失敗しました。\n再ログインしてください。");

            string? exchangeCode = await GetGameTokenAsync(accessToken);
            if (string.IsNullOrEmpty(exchangeCode))
                return LaunchResult.Fail("exchange_code の取得に失敗しました。\n再ログインしてください。");

            var session = LoadSession()!;
            string[] argParts =
            {
                $"-epicapp={EpicAppId}", "-epicenv=Prod", "-EpicPortal",
                $"-epicusername={Quote(session.DisplayName ?? "")}",
                $"-epicuserid={session.AccountId}",
                "-epiclocale=ja",
                $"-epicsandboxid={EpicSandboxId}",
                $"-epicdeploymentid={EpicDeploymentId}",
                "-AUTH_LOGIN=unused",
                $"-AUTH_PASSWORD={exchangeCode}",
                "-AUTH_TYPE=exchangecode",
            };
            string args = string.Join(" ", argParts);
            LogService.Info("EpicLoginService",
                $"Epic 直接起動: {exePath}\n引数(exchange_code 省略): " + args.Replace(exchangeCode, "***"));
            try
            {
                var proc = Process.Start(new ProcessStartInfo(exePath, args)
                { WorkingDirectory = workDir, UseShellExecute = false });
                return LaunchResult.Ok(proc);
            }
            catch (Exception ex)
            {
                LogService.Error("EpicLoginService", "Among Us 起動エラー", ex);
                return LaunchResult.Fail($"起動に失敗しました: {ex.Message}");
            }
        }

        // ─── ログアウト ───────────────────────────────────────────────────
        public static void Logout()
        {
            ClearSession();
            var config = ConfigService.Load();
            config.EpicAccountId = "";
            config.EpicDisplayName = "";
            ConfigService.Save(config);
            LogService.Info("EpicLoginService", "Epic ログアウト完了。");
        }

        // ─── Launcher 操作 ────────────────────────────────────────────────
        public static bool IsLauncherRunning()
            => Process.GetProcessesByName("EpicGamesLauncher").Length > 0;

        public static void LaunchEpicLauncher()
        {
            try { Process.Start(new ProcessStartInfo("com.epicgames.launcher://") { UseShellExecute = true }); }
            catch (Exception ex) { LogService.Error("EpicLoginService", "Launcher起動失敗", ex); }
        }

        // ─── ゲームトークン取得 ───────────────────────────────────────────
        public static async Task<string?> GetGameTokenAsync(string accessToken)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, ExchangeEndpoint);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var resp = await _http.SendAsync(req);
                string json = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    LogService.Warn("EpicLoginService", $"exchange_code 取得失敗: {json}");
                    return null;
                }
                using var doc = JsonDocument.Parse(json);
                string? code = doc.RootElement.GetProperty("code").GetString();
                LogService.Info("EpicLoginService", "exchange_code 取得成功。");
                return code;
            }
            catch (Exception ex)
            {
                LogService.Error("EpicLoginService", "exchange_code 取得エラー", ex);
                return null;
            }
        }

        public static Task<string?> GetExchangeCodeAsync(string accessToken) => GetGameTokenAsync(accessToken);

        // ─── 内部ユーティリティ ───────────────────────────────────────────
        private static async Task<EpicSession?> OAuthRequestAsync(Dictionary<string, string> fields)
        {
            var body = new FormUrlEncodedContent(fields);
            var req = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint) { Content = body };
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", BasicAuth);

            var resp = await _http.SendAsync(req);
            string json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                LogService.Warn("EpicLoginService", $"OAuth 失敗 ({resp.StatusCode}): {json}");
                throw new Exception($"Epic OAuth 失敗 ({resp.StatusCode}): {json}");
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new EpicSession
            {
                AccessToken = root.GetProperty("access_token").GetString() ?? "",
                RefreshToken = root.GetProperty("refresh_token").GetString() ?? "",
                AccountId = root.GetProperty("account_id").GetString() ?? "",
                DisplayName = root.TryGetProperty("displayName", out var dn) ? dn.GetString() : null,
            };
        }

        private static void TrySaveFileOnly(EpicSession session)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SessionFilePath)!);
                File.WriteAllText(SessionFilePath,
                    JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true }),
                    Encoding.UTF8);
            }
            catch { }
        }

        private static string Quote(string s) => s.Contains(' ') ? $"\"{s}\"" : s;
    }

    public class EpicLoginResult
    {
        public bool Success { get; private set; }
        public string DisplayName { get; private set; } = "";
        public string AccountId { get; private set; } = "";
        public string Error { get; private set; } = "";

        public static EpicLoginResult Ok(string displayName, string accountId)
            => new() { Success = true, DisplayName = displayName, AccountId = accountId };
        public static EpicLoginResult Fail(string error)
            => new() { Success = false, Error = error };
    }

    public class LaunchResult
    {
        public bool Success { get; private set; }
        public Process? Process { get; private set; }
        public string Error { get; private set; } = "";

        public static LaunchResult Ok(Process? proc) => new() { Success = true, Process = proc };
        public static LaunchResult Fail(string error) => new() { Success = false, Error = error };
    }
}
