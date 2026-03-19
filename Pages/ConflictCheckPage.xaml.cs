using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using AmongUsModManager.Models;
using AmongUsModManager.Models.Services;
using AmongUsModManager.Services;

namespace AmongUsModManager.Pages
{
    public sealed partial class ConflictCheckPage : Page
    {
        // 競合チェックから除外するDLL（Reactor本体・BepInExコア等）
        private static readonly HashSet<string> ExcludedDlls = new(StringComparer.OrdinalIgnoreCase)
        {
            // BepInEx コア
            "0Harmony.dll", "BepInEx.dll", "BepInEx.Preloader.dll",
            "BepInEx.Unity.dll", "BepInEx.Core.dll",
            "Mono.Cecil.dll", "Mono.Cecil.Mdb.dll", "Mono.Cecil.Pdb.dll",
            "MonoMod.RuntimeDetour.dll", "MonoMod.Utils.dll",
            // Reactor
            "Reactor.dll", "Reactor.OxygenFilter.dll",
            // MiraAPI
            "MiraAPI.dll",
        };

        public ConflictCheckPage()
        {
            this.InitializeComponent();
        }

        private async void StartCheck_Click(object sender, RoutedEventArgs e)
        {
            CheckingRing.IsActive = true;
            SummaryText.Text = "チェック中...";

            // 前回の結果を消す（EmptyStateは残す）
            var toRemove = ResultPanel.Children
                .Where(c => c != EmptyState)
                .ToList();
            foreach (var c in toRemove)
                ResultPanel.Children.Remove(c);
            EmptyState.Visibility = Visibility.Collapsed;

            try
            {
                var results = await Task.Run(RunCheck);
                ShowResults(results);
            }
            catch (Exception ex)
            {
                LogService.Error("ConflictCheckPage", "競合チェックエラー", ex);
                SummaryText.Text = $"エラー: {ex.Message}";
            }
            finally
            {
                CheckingRing.IsActive = false;
            }
        }

        // ─── チェックロジック ─────────────────────────────────────────
        private List<ConflictResult> RunCheck()
        {
            var config = ConfigService.Load();
            var mods = config.VanillaPaths ?? new List<VanillaPathInfo>();

            // 各ModのBepInEx/pluginsにあるDLLを収集
            // key: DLL名（小文字）, value: (ModName, DllPath)のリスト
            var dllMap = new Dictionary<string, List<(string ModName, string DllPath)>>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var mod in mods)
            {
                if (string.IsNullOrEmpty(mod.Path) || !Directory.Exists(mod.Path))
                    continue;

                string pluginsDir = Path.Combine(mod.Path, "BepInEx", "plugins");
                if (!Directory.Exists(pluginsDir)) continue;

                // pluginsフォルダ以下のDLLを再帰的に取得
                var dlls = Directory.GetFiles(pluginsDir, "*.dll", SearchOption.AllDirectories);
                foreach (var dll in dlls)
                {
                    string name = Path.GetFileName(dll);
                    if (ExcludedDlls.Contains(name)) continue;

                    if (!dllMap.ContainsKey(name))
                        dllMap[name] = new List<(string, string)>();
                    dllMap[name].Add((mod.Name, dll));
                }
            }

            // 2つ以上のModに同じDLL名が存在 → 競合
            return dllMap
                .Where(kv => kv.Value.Count >= 2)
                .Select(kv => new ConflictResult
                {
                    DllName = kv.Key,
                    Entries = kv.Value
                })
                .OrderBy(r => r.DllName)
                .ToList();
        }

        // ─── 結果表示 ─────────────────────────────────────────────────
        private void ShowResults(List<ConflictResult> results)
        {
            if (results.Count == 0)
            {
                SummaryText.Text = "✅ 競合は見つかりませんでした";
                EmptyState.Visibility = Visibility.Visible;
                var emptyTb = (StackPanel)EmptyState;
                // EmptyStateのテキストを更新
                if (EmptyState.Children[1] is TextBlock tb)
                    tb.Text = "✅ 競合は見つかりませんでした";
                return;
            }

            SummaryText.Text = $"⚠️ {results.Count} 件の競合が見つかりました";

            foreach (var conflict in results)
            {
                // カードを作成
                var card = new Border
                {
                    Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                    BorderBrush = new SolidColorBrush(Colors.OrangeRed),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 12, 16, 12),
                };

                var inner = new StackPanel { Spacing = 8 };

                // DLL名ヘッダー
                var header = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8
                };
                header.Children.Add(new FontIcon
                {
                    Glyph = "\uE7BA",
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Colors.OrangeRed),
                    VerticalAlignment = VerticalAlignment.Center
                });
                header.Children.Add(new TextBlock
                {
                    Text = conflict.DllName,
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                });
                inner.Children.Add(header);

                // 重複しているModのリスト
                foreach (var (modName, dllPath) in conflict.Entries)
                {
                    var row = new StackPanel { Spacing = 2, Margin = new Thickness(24, 0, 0, 0) };
                    row.Children.Add(new TextBlock
                    {
                        Text = $"📦 {modName}",
                        FontSize = 13,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    });
                    row.Children.Add(new TextBlock
                    {
                        Text = dllPath,
                        FontSize = 11,
                        Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });
                    inner.Children.Add(row);
                }

                card.Child = inner;
                ResultPanel.Children.Add(card);
            }
        }

        private class ConflictResult
        {
            public string DllName { get; set; } = "";
            public List<(string ModName, string DllPath)> Entries { get; set; } = new();
        }
    }
}
