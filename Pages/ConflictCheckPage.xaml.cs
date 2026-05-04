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
        private static readonly HashSet<string> ExcludedDlls = new(StringComparer.OrdinalIgnoreCase)
        {
            "0Harmony.dll", "BepInEx.dll", "BepInEx.Preloader.dll",
            "BepInEx.Unity.dll", "BepInEx.Core.dll",
            "Mono.Cecil.dll", "Mono.Cecil.Mdb.dll", "Mono.Cecil.Pdb.dll",
            "MonoMod.RuntimeDetour.dll", "MonoMod.Utils.dll",
            "Reactor.dll", "Reactor.OxygenFilter.dll",
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

        private List<ConflictResult> RunCheck()
        {
            var config = ConfigService.Load();
            var mods = config.VanillaPaths ?? new List<VanillaPathInfo>();

            var results = new List<ConflictResult>();

            foreach (var mod in mods)
            {
                if (string.IsNullOrEmpty(mod.Path) || !Directory.Exists(mod.Path))
                    continue;

                // このModのpluginsフォルダ内だけで重複を探す
                string pluginsDir = Path.Combine(mod.Path, "BepInEx", "plugins");
                if (!Directory.Exists(pluginsDir)) continue;

                var dlls = Directory.GetFiles(pluginsDir, "*.dll", SearchOption.AllDirectories);

                // ファイル名でグループ化して2件以上 = 同フォルダ内重複
                var duplicates = dlls
                    .Where(dll => !ExcludedDlls.Contains(Path.GetFileName(dll)))
                    .GroupBy(dll => Path.GetFileName(dll), StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() >= 2);

                foreach (var group in duplicates)
                {
                    results.Add(new ConflictResult
                    {
                        ModName = mod.Name,
                        DllName = group.Key,
                        Paths = group.ToList(),
                    });
                }
            }

            return results.OrderBy(r => r.ModName).ThenBy(r => r.DllName).ToList();
        }

        private void ShowResults(List<ConflictResult> results)
        {
            if (results.Count == 0)
            {
                SummaryText.Text = "✅ 競合は見つかりませんでした";
                EmptyState.Visibility = Visibility.Visible;
                if (EmptyState.Children[1] is TextBlock tb)
                    tb.Text = "✅ 競合は見つかりませんでした";
                return;
            }

            SummaryText.Text = $"⚠️ {results.Count} 件の競合が見つかりました";

            foreach (var conflict in results)
            {
                var card = new Border
                {
                    Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                    BorderBrush = new SolidColorBrush(Colors.OrangeRed),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 12, 16, 12),
                };

                var inner = new StackPanel { Spacing = 8 };

                // ヘッダー：Mod名 / DLL名
                var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                header.Children.Add(new FontIcon
                {
                    Glyph = "\uE7BA",
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Colors.OrangeRed),
                    VerticalAlignment = VerticalAlignment.Center
                });
                header.Children.Add(new TextBlock
                {
                    Text = $"[{conflict.ModName}]  {conflict.DllName}",
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                });
                inner.Children.Add(header);

                // 重複しているパスの一覧
                foreach (var path in conflict.Paths)
                {
                    inner.Children.Add(new TextBlock
                    {
                        Text = path,
                        FontSize = 11,
                        Margin = new Thickness(24, 0, 0, 0),
                        Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                        TextTrimming = TextTrimming.CharacterEllipsis,
                    });
                }

                card.Child = inner;
                ResultPanel.Children.Add(card);
            }
        }

        private class ConflictResult
        {
            public string ModName { get; set; } = "";
            public string DllName { get; set; } = "";
            public List<string> Paths { get; set; } = new();
        }
    }
}
