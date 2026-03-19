using System;
using Markdig;
//claudeくんをここで使ってみた
//claudeくんが書いたメモなので残しておいてみる
//てかclaudeくんって.csファイルふつうに作って渡すのすごいねすごい
namespace AmongUsModManager.Models
{
    /// <summary>
    /// MarkdownテキストをWebView2で表示可能なHTMLに変換するヘルパー。
    /// NuGetパッケージ「Markdig」が必要です。
    ///   Install-Package Markdig
    /// </summary>
    public static class MarkdownHelper
    {
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        public static string ToHtml(string markdown, bool isDarkTheme = true)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                markdown = "*(リリースノートなし)*";

            string body = Markdown.ToHtml(markdown, Pipeline);

            string bgColor     = isDarkTheme ? "#1f1f1f" : "#ffffff";
            string textColor   = isDarkTheme ? "#e0e0e0" : "#1a1a1a";
            string codeBg      = isDarkTheme ? "#2d2d2d" : "#f4f4f4";
            string borderColor = isDarkTheme ? "#444444" : "#cccccc";
            string linkColor   = isDarkTheme ? "#60cdff" : "#0067c0";
            string theme       = isDarkTheme ? "dark"    : "light";

            // CSS内の { } はそのまま文字列連結で書く（C#補間と衝突しないように）
            string css =
                "* { box-sizing: border-box; margin: 0; padding: 0; }" +
                "body {" +
                "  font-family: 'Yu Gothic UI', 'Segoe UI', sans-serif;" +
                "  font-size: 13px;" +
                "  line-height: 1.7;" +
                "  background: " + bgColor + ";" +
                "  color: " + textColor + ";" +
                "  padding: 8px 12px;" +
                "  word-break: break-word;" +
                "}" +
                "h1, h2, h3, h4 {" +
                "  margin: 12px 0 6px;" +
                "  font-weight: 600;" +
                "  border-bottom: 1px solid " + borderColor + ";" +
                "  padding-bottom: 4px;" +
                "}" +
                "h1 { font-size: 1.3em; }" +
                "h2 { font-size: 1.15em; }" +
                "h3 { font-size: 1.05em; }" +
                "p { margin: 6px 0; }" +
                "ul, ol { margin: 6px 0 6px 20px; }" +
                "li { margin: 2px 0; }" +
                "a { color: " + linkColor + "; text-decoration: none; }" +
                "a:hover { text-decoration: underline; }" +
                "code {" +
                "  background: " + codeBg + ";" +
                "  border-radius: 3px;" +
                "  padding: 1px 5px;" +
                "  font-family: 'Cascadia Code', 'Consolas', monospace;" +
                "  font-size: 12px;" +
                "}" +
                "pre {" +
                "  background: " + codeBg + ";" +
                "  border-radius: 6px;" +
                "  padding: 10px 12px;" +
                "  overflow-x: auto;" +
                "  margin: 8px 0;" +
                "}" +
                "pre code { background: none; padding: 0; }" +
                "blockquote {" +
                "  border-left: 3px solid " + borderColor + ";" +
                "  margin: 8px 0;" +
                "  padding: 4px 12px;" +
                "  opacity: 0.8;" +
                "}" +
                "table {" +
                "  border-collapse: collapse;" +
                "  width: 100%;" +
                "  margin: 8px 0;" +
                "  font-size: 12px;" +
                "}" +
                "th, td {" +
                "  border: 1px solid " + borderColor + ";" +
                "  padding: 5px 10px;" +
                "  text-align: left;" +
                "}" +
                "th { background: " + codeBg + "; font-weight: 600; }" +
                "hr { border: none; border-top: 1px solid " + borderColor + "; margin: 10px 0; }" +
                "img { max-width: 100%; border-radius: 4px; }";

            return
                "<!DOCTYPE html>\n" +
                "<html>\n" +
                "<head>\n" +
                "<meta charset=\"utf-8\"/>\n" +
                "<meta name=\"color-scheme\" content=\"" + theme + "\"/>\n" +
                "<style>\n" + css + "\n</style>\n" +
                "</head>\n" +
                "<body>\n" +
                body +
                "\n</body>\n" +
                "</html>";
        }
    }
}
