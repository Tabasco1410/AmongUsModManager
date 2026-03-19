using System;
using System.IO;
using System.Text;
using System.Text.Json;
using AmongUsModManager.Models;

namespace AmongUsModManager.Models.Services
{
    /// <summary>
    /// 共有コード（.aumanager ファイル）を生成・読み込みます。
    /// コードはBase64エンコードされたJSONで、Mod情報とダウンロードURLを含んでいます。
    /// </summary>
    public static class ShareCodeService
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
        };

        /// <summary>
        /// 共有コードを生成し、.aumanager ファイルに保存します。
        /// コード = "AUMM-" + Base64
        /// </summary>
        public static string Generate(VanillaPathInfo mod, string platform, string saveDirectory,
            string? downloadUrl = null)
        {
            string ghOwner = mod.GitHubOwner ?? "";
            string ghRepo  = mod.GitHubRepo  ?? "";
            string version = mod.CurrentVersion ?? "";

            // ダウンロードURLが指定されていなければGitHubリリースURLを生成する
            string dlUrl = downloadUrl ?? (string.IsNullOrEmpty(ghOwner) ? "" :
                $"https://github.com/{ghOwner}/{ghRepo}/releases/tag/{version}");

            var data = new ShareCodeData
            {
                ModName     = mod.Name,
                GitHubOwner = ghOwner,
                GitHubRepo  = ghRepo,
                Version     = version,
                Platform    = platform,
                DownloadUrl = dlUrl,
                GeneratedAt = DateTime.UtcNow
            };

         
            string json  = JsonSerializer.Serialize(data, _options);
            string b64   = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
                           .Replace('+', '-').Replace('/', '_').Replace("=", "");
            string code  = "AUMM-" + b64;

            // .aumanager ファイルに保存
            try
            {
                if (!Directory.Exists(saveDirectory))
                    Directory.CreateDirectory(saveDirectory);

                string fileName = $"{SanitizeFileName(mod.Name)}_{version}.aumanager";
                string filePath = Path.Combine(saveDirectory, fileName);
                var fileContent = new ShareCodeFile { Code = code, Data = data };
                File.WriteAllText(filePath, JsonSerializer.Serialize(fileContent,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { /* コードは返す */ }

            return code;
        }

      
        public static ShareCodeData? Decode(string codeOrPath)
        {
            // .aumanager ファイルパスの場合
            if (File.Exists(codeOrPath) && codeOrPath.EndsWith(".aumanager", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var file = JsonSerializer.Deserialize<ShareCodeFile>(File.ReadAllText(codeOrPath));
                    if (file?.Code != null)
                        return DecodeCode(file.Code) ?? file.Data;
                    return file?.Data;
                }
                catch { return null; }
            }

          
            return DecodeCode(codeOrPath);
        }

        private static ShareCodeData? DecodeCode(string code)
        {
            if (!code.StartsWith("AUMM-")) return null;
            try
            {
                string b64 = code.Substring(5).Replace('-', '+').Replace('_', '/');
                // パディング補完
                int pad = b64.Length % 4;
                if (pad > 0) b64 += new string('=', 4 - pad);
                string json = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                return JsonSerializer.Deserialize<ShareCodeData>(json);
            }
            catch { return null; }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }

    public class ShareCodeData
    {
        public string ModName     { get; set; } = "";
        public string GitHubOwner { get; set; } = "";
        public string GitHubRepo  { get; set; } = "";
        public string Version     { get; set; } = "";
        public string Platform    { get; set; } = "";
        public string DownloadUrl { get; set; } = "";  
        public DateTime GeneratedAt { get; set; }
    }

    public class ShareCodeFile
    {
        public string Code { get; set; } = "";
        public ShareCodeData Data { get; set; } = new();
    }
}
