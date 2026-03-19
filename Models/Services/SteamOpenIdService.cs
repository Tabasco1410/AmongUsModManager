using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AmongUsModManager.Services;

namespace AmongUsModManager.Models.Services
{
    
    public static class SteamOpenIdService
    {
        private const int    CallbackPort = 7685;
        private const string CallbackPath = "/steam/callback";
        private static readonly string CallbackUrl =
            $"http://localhost:{CallbackPort}{CallbackPath}";

        private static readonly string LoginUrl =
            "https://steamcommunity.com/openid/login" +
            "?openid.ns=http://specs.openid.net/auth/2.0" +
            "&openid.mode=checkid_setup" +
            "&openid.return_to=" + Uri.EscapeDataString(CallbackUrl) +
            "&openid.realm=" + Uri.EscapeDataString($"http://localhost:{CallbackPort}/") +
            "&openid.identity=http://specs.openid.net/auth/2.0/identifier_select" +
            "&openid.claimed_id=http://specs.openid.net/auth/2.0/identifier_select";

        public class OpenIdResult
        {
            public bool   Success  { get; set; }
            public string SteamId  { get; set; } = "";
            public string Error    { get; set; } = "";
        }

        public static async Task<OpenIdResult> AuthenticateAsync(CancellationToken ct)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{CallbackPort}/");

            try
            {
                listener.Start();
                LogService.Info("SteamOpenId", $"ローカルサーバー起動: {CallbackUrl}");

                Process.Start(new ProcessStartInfo(LoginUrl) { UseShellExecute = true });

                
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromMinutes(5));

                var contextTask = listener.GetContextAsync();
                var cancelTask  = Task.Delay(Timeout.Infinite, cts.Token);

                var completed = await Task.WhenAny(contextTask, cancelTask);

                if (completed == cancelTask)
                {
                    listener.Stop();
                    return new OpenIdResult { Error = "タイムアウトしました。もう一度やり直してください。" };
                }

                var context  = await contextTask;
                var query    = context.Request.QueryString;
                var claimedId = query["openid.claimed_id"] ?? "";

                
                var match = Regex.Match(claimedId,
                    @"https://steamcommunity\.com/openid/id/(\d+)");

              
                string html = match.Success
                    ? "<html><body style='font-family:sans-serif;text-align:center;padding:60px'>" +
                      "<h2>✅ ログイン成功！</h2><p>AmongUsModManager に戻ってください。</p>" +
                      "<script>window.close();</script></body></html>"
                    : "<html><body style='font-family:sans-serif;text-align:center;padding:60px'>" +
                      "<h2>❌ ログイン失敗</h2><p>もう一度お試しください。</p></body></html>";

                var buf = System.Text.Encoding.UTF8.GetBytes(html);
                context.Response.ContentType     = "text/html; charset=utf-8";
                context.Response.ContentLength64 = buf.Length;
                await context.Response.OutputStream.WriteAsync(buf, 0, buf.Length, ct);
                context.Response.Close();
                listener.Stop();

                if (match.Success)
                {
                    string steamId = match.Groups[1].Value;
                    LogService.Info("SteamOpenId", $"SteamID取得成功: {steamId}");
                    return new OpenIdResult { Success = true, SteamId = steamId };
                }
                else
                {
                    LogService.Warn("SteamOpenId", $"SteamID取得失敗: claimed_id={claimedId}");
                    return new OpenIdResult { Error = "SteamIDの取得に失敗しました。" };
                }
            }
            catch (OperationCanceledException)
            {
                listener.Stop();
                return new OpenIdResult { Error = "キャンセルされました。" };
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 5)
            {
                
                LogService.Error("SteamOpenId", "ポートのアクセス権エラー", ex);
                return new OpenIdResult
                {
                    Error = $"ポート {CallbackPort} を開けませんでした。\n" +
                            "別のアプリが使用中の可能性があります。"
                };
            }
            catch (Exception ex)
            {
                listener.Stop();
                LogService.Error("SteamOpenId", "OpenID認証エラー", ex);
                return new OpenIdResult { Error = $"エラー: {ex.Message}" };
            }
        }

     
        public static async Task<string> FetchUserNameAsync(string steamId)
        {
            try
            {
                var http = new System.Net.Http.HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager-App");
               
                var xml = await http.GetStringAsync(
                    $"https://steamcommunity.com/profiles/{steamId}?xml=1");
                var match = Regex.Match(xml, @"<steamID><!\[CDATA\[(.+?)\]\]></steamID>");
                return match.Success ? match.Groups[1].Value : steamId;
            }
            catch
            {
                return steamId; 
            }
        }
    }
}
