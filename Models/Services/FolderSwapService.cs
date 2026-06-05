using System;
using System.IO;

namespace AmongUsModManager.Models.Services
{
    public class FolderSwapState
    {
        public string OriginalVanillaPath { get; init; } = "";
        public string TempVanillaPath     { get; init; } = "";
        public string OriginalModPath     { get; init; } = "";
        public string SwappedModPath      { get; init; } = "";
        public bool   IsRestored          { get; set;  }
    }

    public static class FolderSwapService
    {
        public static FolderSwapState SwapFolders(string vanillaPath, string modPath)
        {
            string parent      = Path.GetDirectoryName(Path.GetFullPath(vanillaPath))!;
            string vanillaName = Path.GetFileName(vanillaPath);
            string tempPath    = Path.Combine(parent, $"Vanilla_{vanillaName}");

            LogService.Info("FolderSwapService", $"スワップ開始: バニラ={vanillaPath}, Mod={modPath}");
            Directory.Move(vanillaPath, tempPath);
            Directory.Move(modPath, vanillaPath);
            LogService.Info("FolderSwapService", "スワップ完了");

            return new FolderSwapState
            {
                OriginalVanillaPath = vanillaPath,
                TempVanillaPath     = tempPath,
                OriginalModPath     = modPath,
                SwappedModPath      = vanillaPath
            };
        }

        public static void RestoreFolders(FolderSwapState state)
        {
            if (state.IsRestored) return;
            state.IsRestored = true;

            LogService.Info("FolderSwapService", "フォルダを復元します");

            if (Directory.Exists(state.SwappedModPath))
                Directory.Move(state.SwappedModPath, state.OriginalModPath);
            else
                LogService.Warn("FolderSwapService", $"スワップ済みModフォルダが見つかりません: {state.SwappedModPath}");

            if (Directory.Exists(state.TempVanillaPath))
                Directory.Move(state.TempVanillaPath, state.OriginalVanillaPath);
            else
                LogService.Warn("FolderSwapService", $"一時バニラフォルダが見つかりません: {state.TempVanillaPath}");

            LogService.Info("FolderSwapService", "フォルダ復元完了");
        }

        public static void RecoverIfNeeded(string parentDir)
        {
            if (string.IsNullOrEmpty(parentDir) || !Directory.Exists(parentDir)) return;
            try
            {
                foreach (var dir in Directory.GetDirectories(parentDir))
                {
                    string name = Path.GetFileName(dir);
                    if (!name.StartsWith("Vanilla_", StringComparison.OrdinalIgnoreCase)) continue;

                    string originalName = name["Vanilla_".Length..];
                    string originalPath = Path.Combine(parentDir, originalName);
                    if (!Directory.Exists(originalPath))
                    {
                        LogService.Warn("FolderSwapService", $"未復元スワップを検出: {dir} → {originalPath}");
                        Directory.Move(dir, originalPath);
                        LogService.Info("FolderSwapService", $"自動復元完了: {originalPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("FolderSwapService", "スワップ自動復元中にエラーが発生", ex);
            }
        }
    }
}
