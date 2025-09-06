using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Among_Us_ModManager.Auth
{
    public static class OAuthLogin
    {
        private static readonly string TokenFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AmongUsModManager", "token.json"); // JSONに変更

        private static readonly GitHubOAuthService oauthService = new();

        // public に変更
        public class TokenData
        {
            public string AccessToken { get; set; }
            public string UserName { get; set; }
            public bool IsAdmin { get; set; }
        }

        public static async Task<TokenData> LoginAsync()
        {
            try
            {
                // ブラウザで認証ページを開く
                oauthService.OpenLoginPage();

                // ローカルサーバーでcallbackを受け取る
                string code = await OAuthCallbackListener.WaitForCodeAsync("http://localhost:57853/callback/");

                if (string.IsNullOrEmpty(code))
                {
                    MessageBox.Show("認証コードを取得できませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                // codeを使ってアクセストークンを取得
                var token = await oauthService.ExchangeCodeForTokenAsync(code);

                // アクセストークンでユーザー情報を取得
                string userName = await oauthService.GetUserNameAsync(token);
                bool isAdmin = userName == "Tabasco1410"; // 管理者判定

                var tokenData = new TokenData
                {
                    AccessToken = token,
                    UserName = userName,
                    IsAdmin = isAdmin
                };

                SaveToken(tokenData);

                return tokenData;
            }
            catch (Exception ex)
            {
                MessageBox.Show("ログインに失敗しました: " + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public static TokenData LoadToken()
        {
            if (!File.Exists(TokenFilePath))
                return null;

            try
            {
                string json = File.ReadAllText(TokenFilePath);
                return JsonSerializer.Deserialize<TokenData>(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("トークン読み込みに失敗しました: " + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private static void SaveToken(TokenData tokenData)
        {
            try
            {
                var dir = Path.GetDirectoryName(TokenFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(tokenData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(TokenFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("トークン保存に失敗しました: " + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
