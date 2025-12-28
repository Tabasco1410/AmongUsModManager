using System.IO;
using Microsoft.Win32;

namespace AmongUsModManager.Services
{
    public static class AUFileDetector
    {
        public static string GetSteamPath()
        {
            string path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            if (path != null)
            {
                string fullPath = Path.Combine(path.Replace("/", "\\"), @"steamapps\common\Among Us");
                if (File.Exists(Path.Combine(fullPath, "Among Us.exe"))) return fullPath;
            }
            return null;
        }

        public static string GetEpicPath()
        {
            // ここにないかもしれないけどね！
            string commonPath = @"C:\Program Files\Epic Games\AmongUs";
            if (File.Exists(Path.Combine(commonPath, "Among Us.exe"))) return commonPath;
            return null;
        }
    }
}