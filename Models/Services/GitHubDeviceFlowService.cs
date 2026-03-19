using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AmongUsModManager.Services;

namespace AmongUsModManager.Models.Services
{
   
    public static class GitHubDeviceFlowService
    {
       
        private const string ClientId = "Ov23li9ApMIBBeILZsML";

        private const string DeviceCodeUrl  = "https://github.com/login/device/code";
        private const string TokenUrl       = "https://github.com/login/oauth/access_token";
        private const string Scope          = "public_repo";

        private static readonly HttpClient _http = new HttpClient();

        static GitHubDeviceFlowService()
        {
            _http.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager-App");
            _http.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public class DeviceCodeResponse
        {
            public string device_code  { get; set; } = "";
            public string user_code    { get; set; } = "";
            public string verification_uri { get; set; } = "";
            public int    expires_in   { get; set; }
            public int    interval     { get; set; } = 5;
        }

        public class TokenResult
        {
            public bool   Success      { get; set; }
            public string AccessToken  { get; set; } = "";
            public string Error        { get; set; } = "";
        }

        public static async Task<DeviceCodeResponse?> RequestDeviceCodeAsync()
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", ClientId),
                    new KeyValuePair<string, string>("scope", Scope)
                });

                var response = await _http.PostAsync(DeviceCodeUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    LogService.Warn("DeviceFlow", $"デバイスコード取得失敗: {response.StatusCode}");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<DeviceCodeResponse>();
            }
            catch (Exception ex)
            {
                LogService.Error("DeviceFlow", "デバイスコード取得エラー", ex);
                return null;
            }
        }

        public static async Task<TokenResult> PollForTokenAsync(
            string deviceCode, int intervalSec, CancellationToken ct)
        {
            int elapsed = 0;
            int timeout = 300; 

            while (elapsed < timeout)
            {
                try
                {
                    await Task.Delay(intervalSec * 1000, ct);
                    elapsed += intervalSec;
                }
                catch (TaskCanceledException)
                {
                    return new TokenResult { Error = "キャンセルされました" };
                }

                try
                {
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("client_id", ClientId),
                        new KeyValuePair<string, string>("device_code", deviceCode),
                        new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code")
                    });

                    var response = await _http.PostAsync(TokenUrl, content);
                    var json = await response.Content.ReadFromJsonAsync<TokenPollResponse>();

                    if (json == null) continue;

                    if (!string.IsNullOrEmpty(json.access_token))
                    {
                        LogService.Info("DeviceFlow", "アクセストークン取得成功");
                        return new TokenResult { Success = true, AccessToken = json.access_token };
                    }

                    switch (json.error)
                    {
                        case "authorization_pending":
                           
                            continue;
                        case "slow_down":
                            
                            intervalSec += 5;
                            continue;
                        case "expired_token":
                            return new TokenResult { Error = "コードの有効期限が切れました。もう一度やり直してください。" };
                        case "access_denied":
                            return new TokenResult { Error = "アクセスが拒否されました。" };
                        default:
                            return new TokenResult { Error = $"エラー: {json.error}" };
                    }
                }
                catch (Exception ex)
                {
                    LogService.Error("DeviceFlow", "ポーリングエラー", ex);
                }
            }

            return new TokenResult { Error = "タイムアウトしました。もう一度やり直してください。" };
        }

        private class TokenPollResponse
        {
            public string? access_token { get; set; }
            public string? error        { get; set; }
        }
    }
}
