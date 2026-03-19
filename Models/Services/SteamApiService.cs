using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AmongUsModManager.Services;

namespace AmongUsModManager.Models.Services
{
    /// <summary>
    /// Steam Web API 連携
    /// APIキー: https://steamcommunity.com/dev/apikey で取得する。
    /// </summary>
    public static class SteamApiService
    {
        private static readonly HttpClient _http = new HttpClient();

        static SteamApiService()
        {
            _http.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager-App");
        }

        public class SteamUserInfo
        {
            public string SteamId    { get; set; } = "";
            public string UserName   { get; set; } = "";
            public string AvatarUrl  { get; set; } = "";
            public string ProfileUrl { get; set; } = "";
            public int    PlayTimeAmongUs { get; set; } = -1; 
        }

       
        private const string AmongUsAppId = "945360";

        /// <summary>
        /// SteamIDとAPIキーからユーザー情報を取得する。
        /// SteamIDは17桁の数字またはURL
        /// </summary>
        public static async Task<(bool ok, SteamUserInfo? info, string error)> FetchUserInfoAsync(
            string apiKey, string steamIdOrUrl)
        {
            try
            {
                
                string steamId = await ResolveSteamIdAsync(apiKey, steamIdOrUrl);
                if (string.IsNullOrEmpty(steamId))
                    return (false, null, "SteamIDが見つかりませんでした。\n17桁のSteamIDまたはカスタムURLを確認してください。");

                var profileUrl = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/" +
                                 $"?key={apiKey}&steamids={steamId}";
                var profileRes = await _http.GetFromJsonAsync<SteamPlayerSummaryResponse>(profileUrl);
                var player = profileRes?.response?.players?[0];
                if (player == null)
                    return (false, null, "ユーザー情報を取得できませんでした。APIキーまたはSteamIDを確認してください。");

                var info = new SteamUserInfo
                {
                    SteamId    = steamId,
                    UserName   = player.personaname ?? "",
                    AvatarUrl  = player.avatarfull  ?? "",
                    ProfileUrl = player.profileurl  ?? ""
                };

               
                info.PlayTimeAmongUs = await FetchAmongUsPlayTimeAsync(apiKey, steamId);

                LogService.Info("SteamApiService", $"Steam連携成功: {info.UserName} ({steamId})");
                return (true, info, "");
            }
            catch (Exception ex)
            {
                LogService.Error("SteamApiService", "ユーザー情報取得エラー", ex);
                return (false, null, $"接続エラー: {ex.Message}");
            }
        }

       
        private static async Task<string> ResolveSteamIdAsync(string apiKey, string input)
        {
            input = input.Trim().TrimEnd('/');

           
            if (System.Text.RegularExpressions.Regex.IsMatch(input, @"^\d{17}$"))
                return input;

            
            string vanityName = input;
            if (input.Contains("steamcommunity.com/id/"))
            {
                var parts = input.Split("/id/");
                if (parts.Length >= 2) vanityName = parts[1].TrimEnd('/');
            }
            else if (input.Contains("steamcommunity.com/profiles/"))
            {
                var parts = input.Split("/profiles/");
                if (parts.Length >= 2) return parts[1].TrimEnd('/');
            }

            
            var url = $"https://api.steampowered.com/ISteamUser/ResolveVanityURL/v1/" +
                      $"?key={apiKey}&vanityurl={vanityName}";
            var res = await _http.GetFromJsonAsync<VanityUrlResponse>(url);
            if (res?.response?.success == 1)
                return res.response.steamid ?? "";

            return "";
        }

        
        private static async Task<int> FetchAmongUsPlayTimeAsync(string apiKey, string steamId)
        {
            try
            {
                var url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/" +
                          $"?key={apiKey}&steamid={steamId}&include_appinfo=false" +
                          $"&appids_filter[0]={AmongUsAppId}";
                var res = await _http.GetFromJsonAsync<OwnedGamesResponse>(url);
                var game = res?.response?.games?[0];
                return game?.playtime_forever ?? -1;
            }
            catch
            {
                return -1;
            }
        }

        
        private class SteamPlayerSummaryResponse
        {
            public SteamPlayerSummaryResponseInner? response { get; set; }
        }
        private class SteamPlayerSummaryResponseInner
        {
            public SteamPlayer[]? players { get; set; }
        }
        private class SteamPlayer
        {
            public string? personaname { get; set; }
            public string? avatarfull  { get; set; }
            public string? profileurl  { get; set; }
        }

        private class VanityUrlResponse
        {
            public VanityUrlInner? response { get; set; }
        }
        private class VanityUrlInner
        {
            public int     success  { get; set; }
            public string? steamid  { get; set; }
        }

        private class OwnedGamesResponse
        {
            public OwnedGamesInner? response { get; set; }
        }
        private class OwnedGamesInner
        {
            public OwnedGame[]? games { get; set; }
        }
        private class OwnedGame
        {
            public int playtime_forever { get; set; }
        }
    }
}
