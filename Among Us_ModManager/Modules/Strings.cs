using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Among_Us_ModManager.Modules
{
    /// <summary>
    /// 多言語文字列管理クラス
    /// 外部 CSV または埋め込みリソースから読み込む
    /// </summary>
    internal static class Strings
    {
        private static Dictionary<string, string> _localizedStrings = new();
        private static string _language = "JA"; // デフォルト言語

        /// <summary>
        /// 言語を設定 (例: "JA", "EN", "ZH-CN", "ZH-TW")
        /// </summary>
        public static void SetLanguage(string langCode)
        {
            _language = langCode;
        }

        /// <summary>
        /// CSVを読み込む（外部ファイル優先、なければ埋め込みリソース）
        /// </summary>
        public static void Load()
        {
            _localizedStrings.Clear();

            // 1. 外部ファイルパス候補
            string[] possiblePaths =
            {
                Path.Combine(AppContext.BaseDirectory, "Strings.csv"),
                Path.Combine(AppContext.BaseDirectory, "Resources", "Strings.csv")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    LoadFromFile(path);
                    return;
                }
            }

            // 2. 外部ファイルがない場合は埋め込みリソース
            LoadFromEmbeddedResource();
        }

        /// <summary>
        /// 外部ファイルから読み込む
        /// </summary>
        private static void LoadFromFile(string csvPath)
        {
            var lines = File.ReadAllLines(csvPath, Encoding.UTF8);
            ParseCsvLines(lines);
        }

        /// <summary>
        /// 埋め込みリソースから読み込む
        /// </summary>
        private static void LoadFromEmbeddedResource()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "Among_Us_ModManager.Resources.Strings.csv";

            using Stream stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var lines = new List<string>();
            while (!reader.EndOfStream)
                lines.Add(reader.ReadLine());

            ParseCsvLines(lines);
        }

        /// <summary>
        /// CSV行の解析
        /// </summary>
        private static void ParseCsvLines(IList<string> lines)
        {
            if (lines.Count < 2)
                throw new InvalidOperationException("Strings.csv is invalid (not enough lines).");

            var headers = SplitCsvLine(lines[0]);
            int langIndex = Array.IndexOf(headers, _language);
            if (langIndex == -1)
                throw new InvalidOperationException($"Language '{_language}' not found in header.");

            for (int i = 1; i < lines.Count; i++)
            {
                var cols = SplitCsvLine(lines[i]);
                if (cols.Length <= langIndex) continue;

                string key = cols[0].Trim();
                string value = cols[langIndex].Trim();

                if (!string.IsNullOrEmpty(key))
                    _localizedStrings[key] = value;
            }
        }

        /// <summary>
        /// キーから翻訳文字列を取得
        /// </summary>
        public static string Get(string key)
        {
            return _localizedStrings.TryGetValue(key, out var value) ? value : key;
        }

        /// <summary>
        /// CSVの1行を分割（簡易対応：カンマや引用符に対応）
        /// </summary>
        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            foreach (var c in line)
            {
                if (c == '\"') inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else sb.Append(c);
            }

            result.Add(sb.ToString());
            return result.ToArray();
        }
    }
}
