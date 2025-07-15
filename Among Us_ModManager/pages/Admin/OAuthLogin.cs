using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Among_Us_ModManager.Auth
{
    public static class OAuthLogin
    {
        private static readonly string TokenFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AmongUsModManager", "token.txt");

        private static readonly GitHubOAuthService oauthService = new();

        public static async Task<string> LoginAsync()
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

                SaveToken(token);

                return token;
            }
            catch (Exception ex)
            {
                MessageBox.Show("ログインに失敗しました: " + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public static string LoadToken()
        {
            if (File.Exists(TokenFilePath))
            {
                return File.ReadAllText(TokenFilePath);
            }
            return null;
        }

        private static void SaveToken(string token)
        {
            try
            {
                var dir = Path.GetDirectoryName(TokenFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(TokenFilePath, token);
            }
            catch (Exception ex)
            {
                MessageBox.Show("トークン保存に失敗しました: " + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
