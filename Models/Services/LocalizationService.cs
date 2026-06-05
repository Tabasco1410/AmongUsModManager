using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AmongUsModManager.Models.Services
{
    public static class LocalizationService
    {
        private static readonly Dictionary<string, string[]> _strings = new(StringComparer.OrdinalIgnoreCase);
        private static int[] _langIds = Array.Empty<int>();
        private static int _currentLangIndex = 0;

        public static int CurrentLanguageId => _langIds.Length > _currentLangIndex ? _langIds[_currentLangIndex] : 0;
        public static IReadOnlyList<int> AvailableLanguageIds => _langIds;

        public static event Action? LanguageChanged;

        private static readonly Dictionary<int, string> _langNames = new()
        {
            { 0,  "English" },
            { 1,  "Español (Latino)" },
            { 2,  "Português (BR)" },
            { 3,  "Português" },
            { 4,  "한국어" },
            { 5,  "Русский" },
            { 6,  "Nederlands" },
            { 7,  "Filipino" },
            { 8,  "Français" },
            { 9,  "Deutsch" },
            { 10, "Italiano" },
            { 11, "日本語" },
            { 12, "Español" },
            { 13, "中文（简体）" },
            { 14, "中文（繁體）" },
        };

        public static string GetLanguageName(int langId)
            => _langNames.TryGetValue(langId, out var name) ? name : langId.ToString();

        public static void Load(string csvPath)
        {
            _strings.Clear();
            _langIds = Array.Empty<int>();

            if (!File.Exists(csvPath)) return;

            var lines = File.ReadAllLines(csvPath, Encoding.UTF8);
            bool headerParsed = false;

            foreach (var rawLine in lines)
            {
                var trimmed = rawLine.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (trimmed.StartsWith("\"##") || trimmed.StartsWith("##")) continue;
                if (trimmed.StartsWith("\"#")  || trimmed.StartsWith("#"))  continue;

                var cols = ParseCsvLine(trimmed);
                if (cols.Count == 0) continue;

                if (!headerParsed)
                {
                    if (cols[0].Equals("id", StringComparison.OrdinalIgnoreCase))
                    {
                        var ids = new List<int>();
                        for (int i = 1; i < cols.Count; i++)
                        {
                            if (int.TryParse(cols[i], out int langId))
                                ids.Add(langId);
                        }
                        _langIds = ids.ToArray();
                        headerParsed = true;
                    }
                    continue;
                }

                string key = cols[0];
                if (string.IsNullOrWhiteSpace(key) || key.StartsWith("#")) continue;

                var values = new string[_langIds.Length];
                for (int i = 0; i < _langIds.Length; i++)
                {
                    int colIndex = i + 1;
                    values[i] = colIndex < cols.Count ? cols[colIndex] : string.Empty;
                }
                _strings[key] = values;
            }
        }

        public static void SetLanguage(int langId)
        {
            int idx = Array.IndexOf(_langIds, langId);
            if (idx < 0) idx = 0;
            _currentLangIndex = idx;
            LanguageChanged?.Invoke();
        }

        public static string Get(string key, int? langId = null)
        {
            int index = _currentLangIndex;
            if (langId.HasValue)
            {
                int idx = Array.IndexOf(_langIds, langId.Value);
                if (idx >= 0) index = idx;
            }

            if (_strings.TryGetValue(key, out var values))
            {
                if (index < values.Length && !string.IsNullOrEmpty(values[index]))
                    return values[index];

                if (index != 0 && values.Length > 0 && !string.IsNullOrEmpty(values[0]))
                    return values[0];
            }

            return key;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            int i = 0;
            while (i < line.Length)
            {
                if (line[i] == '"')
                {
                    i++;
                    var sb = new StringBuilder();
                    while (i < line.Length)
                    {
                        if (line[i] == '"')
                        {
                            i++;
                            if (i < line.Length && line[i] == '"')
                            {
                                sb.Append('"');
                                i++;
                            }
                            else break;
                        }
                        else
                        {
                            sb.Append(line[i]);
                            i++;
                        }
                    }
                    result.Add(sb.ToString());
                    if (i < line.Length && line[i] == ',') i++;
                }
                else
                {
                    int start = i;
                    while (i < line.Length && line[i] != ',') i++;
                    result.Add(line.Substring(start, i - start));
                    if (i < line.Length && line[i] == ',') i++;
                }
            }
            return result;
        }
    }
}
