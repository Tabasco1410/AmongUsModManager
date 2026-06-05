using System.Collections.Generic;

namespace AmongUsModManager.Models
{
    public class NewsItem
    {
        public string  Id          { get; set; } = string.Empty;
        public string  Title       { get; set; } = string.Empty;
        public string  Date        { get; set; } = string.Empty;
        public string  Content     { get; set; } = string.Empty;
        public string? ContentFile { get; set; }
        public string  Url         { get; set; } = string.Empty;
        public List<string>? Images { get; set; }

        /// <summary>
        /// 重要なお知らせフラグ。サーバー側 JSON の "isImportant": true で設定する。
        /// 未指定の場合は false。
        /// </summary>
        public bool IsImportant { get; set; }

        /// <summary>
        /// カテゴリ: "info" | "update" | "important" | "warning" | "maintenance"
        /// 未指定の場合は "info"。
        /// </summary>
        public string Category { get; set; } = "info";

        /// <summary>
        /// 表示優先度（大きいほど上に表示）。通常は 0。
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>IsImportant か Category が重要系なら true</summary>
        public bool IsUrgent =>
            IsImportant
            || Category == "important"
            || Category == "warning"
            || Category == "maintenance";
    }
}
