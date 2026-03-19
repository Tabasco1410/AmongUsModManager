using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace AmongUsModManager.Services
{
    public static class AUFileDetector
    {
        // Among Us.exeはあるかな？っていう確認だよん
        public static bool IsValidPath(string path)
            => !string.IsNullOrEmpty(path) && File.Exists(Path.Combine(path, "Among Us.exe"));
        //steam
        public static string? GetSteamPath()
        {
            try
            {
                
                string? steamRoot = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
                if (steamRoot == null) return null;
                steamRoot = steamRoot.Replace("/", "\\");
               
                var candidates = new List<string>
                {
                    Path.Combine(steamRoot, @"steamapps\common\Among Us"),
                };

               
                string vdfPath = Path.Combine(steamRoot, @"steamapps\libraryfolders.vdf");
                if (File.Exists(vdfPath))
                {
                    foreach (var line in File.ReadAllLines(vdfPath))
                    {
                       
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("\"path\""))
                        {
                            var parts = trimmed.Split('"');
                            if (parts.Length >= 4)
                            {
                                string libPath = parts[3].Replace("\\\\", "\\");
                                candidates.Add(Path.Combine(libPath, @"steamapps\common\Among Us"));
                            }
                        }
                    }
                }

                foreach (var c in candidates)
                    if (IsValidPath(c)) return c;
            }
            catch { /* 検出失敗は無視 */ }
            return null;
        }

        //Epic
        public static string? GetEpicPath()
        {
            try
            {
                var epicRoots = new List<string>//探してみる.........ここにはあるっしょ！
                {
                    @"C:\Program Files\Epic Games\AmongUs",
                    @"C:\Program Files (x86)\Epic Games\AmongUs",
                    @"D:\Epic Games\AmongUs",
                    @"D:\Program Files\Epic Games\AmongUs",
                };

              
                string? launcherKey = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\EpicGames\Unreal Engine",
                    "INSTALLDIR", null) as string;
                if (launcherKey != null)
                    epicRoots.Add(Path.Combine(Path.GetDirectoryName(launcherKey) ?? "", "AmongUs"));

                foreach (var c in epicRoots)
                    if (IsValidPath(c)) return c;
            }
            catch { }
            return null;
        }

        //MS Store
        public static string? GetMicrosoftStorePath()
        {
            try
            {
               //たぶんここ
                string windowsApps = @"C:\Program Files\WindowsApps";
                if (!Directory.Exists(windowsApps)) return null;

                foreach (var dir in Directory.GetDirectories(windowsApps, "Innersloth*"))
                {
                    if (IsValidPath(dir)) return dir;
                }
            }
            catch { /* アクセス権がない場合は無視 */ }
            return null;
        }

        
        public static string? GetItchPath()
        {
            try
            {
                //よくわかんない　使ったことないし
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var candidates = new List<string>
                {
                    Path.Combine(localAppData, @"itch\apps\among-us"),
                    Path.Combine(localAppData, @"itch\apps\Among Us"),
                };

                foreach (var c in candidates)
                    if (IsValidPath(c)) return c;
            }
            catch { }
            return null;
        }

        public static (string? Path, string Platform) DetectAny()
        {
            var steam = GetSteamPath();
            if (steam != null) return (steam, "Steam");

            var epic = GetEpicPath();
            if (epic != null) return (epic, "Epic Games");

            var ms = GetMicrosoftStorePath();
            if (ms != null) return (ms, "Microsoft Store");

            var itch = GetItchPath();
            if (itch != null) return (itch, "itch.io");

            return (null, "");
        }
    }
}
