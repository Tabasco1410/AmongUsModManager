using System;
using System.Net;
using System.Threading.Tasks;

namespace Among_Us_ModManeger.Auth
{
    public static class OAuthCallbackListener
    {
        private static HttpListener listener;

        /// <summary>
        /// 指定のURLでHTTPサーバーを立ててコールバックを待機し、codeを取得する
        /// </summary>
        /// <param name="prefix">例: "http://localhost:57853/callback/"</param>
        /// <returns>codeパラメータ</returns>
        public static async Task<string> WaitForCodeAsync(string prefix)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException("HttpListener is not supported on this platform.");

            listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            try
            {
                var context = await listener.GetContextAsync();

                var query = context.Request.QueryString;
                var code = query.Get("code");

                // ブラウザに返す簡単なHTMLを返す
                string responseString = "<html><body><h1>認証が完了しました。ウィンドウを閉じてください。</h1></body></html>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();

                return code;
            }
            finally
            {
                listener.Stop();
                listener.Close();
            }
        }
    }
}
