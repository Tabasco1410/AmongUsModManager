using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Diagnostics; // FileVersionInfoのために必要
using System.Threading.Tasks; // async/awaitのために必要

namespace Among_Us_ModManeger
{
    /// <summary>
    /// Among UsのインストールパスとMODの検出を管理するヘルパークラス。
    /// </summary>
    public class AmongUsModDetector
    {
        private const string VanillaConfigFileName = "Vanilla_Config.json";
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmongUsModManeger");
        private static readonly string VanillaConfigPath = Path.Combine(AppDataFolder, VanillaConfigFileName);

        /// <summary>
        /// Among Usのインストールフォルダのリストを検出します。
        /// Vanilla_Config.jsonで指定されたExePathの親の親ディレクトリを起点とします。
        /// </summary>
        /// <returns>検出されたAmongUsInstallationオブジェクトのリスト</returns>
        public static List<AmongUsInstallation> DetectAmongUsInstallations()
        {
            List<AmongUsInstallation> installations = new List<AmongUsInstallation>();

            if (!File.Exists(VanillaConfigPath))
            {
                Debug.WriteLine("DEBUG: Vanilla_Config.json が見つかりません。");
                return installations;
            }

            try
            {
                var configJson = File.ReadAllText(VanillaConfigPath);
                var config = JsonSerializer.Deserialize<VanillaConfig>(configJson);

                if (string.IsNullOrWhiteSpace(config?.ExePath) || !File.Exists(config.ExePath))
                {
                    Debug.WriteLine("DEBUG: Vanilla_Config.json に有効な ExePath が設定されていません、またはファイルが存在しません。");
                    return installations;
                }

                // Vanilla_Config.jsonで指定されたAmong Us.exeの親ディレクトリが、Among Usのインストールフォルダ
                string baseAmongUsDir = Path.GetDirectoryName(config.ExePath);
                if (baseAmongUsDir == null)
                {
                    Debug.WriteLine("DEBUG: Among Us.exe の親ディレクトリを取得できませんでした。");
                    return installations;
                }

                // Among Usインストールフォルダの親ディレクトリを検索ルートとする
                // 例: "C:\Program Files (x86)\Steam\steamapps\common\Among Us" の場合、
                // baseAmongUsDir は "C:\Program Files (x86)\Steam\steamapps\common\Among Us"
                // searchRoot は "C:\Program Files (x86)\Steam\steamapps\common"
                DirectoryInfo rootDirInfo = Directory.GetParent(baseAmongUsDir);
                string searchRoot = rootDirInfo?.FullName ?? Path.GetPathRoot(baseAmongUsDir); // 親が見つからなければドライブのルート

                if (string.IsNullOrEmpty(searchRoot))
                {
                    Debug.WriteLine("DEBUG: 有効な検索ルートパスを特定できませんでした。");
                    return installations;
                }

                Debug.WriteLine($"DEBUG: Among Usの検索ルート: {searchRoot}");

                // 検索ルート以下のすべてのディレクトリを列挙し、Among Us.exeとBepInExフォルダが存在するものを検出
                foreach (string dir in Directory.GetDirectories(searchRoot, "*", SearchOption.AllDirectories))
                {
                    string potentialExePath = Path.Combine(dir, "Among Us.exe");
                    string potentialBepInExPath = Path.Combine(dir, "BepInEx");

                    if (File.Exists(potentialExePath) && Directory.Exists(potentialBepInExPath))
                    {
                        installations.Add(new AmongUsInstallation(dir));
                        Debug.WriteLine($"DEBUG: Among Usインストール検出: {dir}");
                    }
                }

                // 検出されたインストールパスのリストから重複を除去し、リストに変換
                return installations.DistinctBy(i => i.InstallPath).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: Among Usインストールパス検出中にエラーが発生しました: {ex.Message}");
                // エラー発生時は空のリストを返す
                return new List<AmongUsInstallation>();
            }
        }

        /// <summary>
        /// 指定されたAmong Usインストールパス内のDLLファイルからバージョン情報を取得します。
        /// </summary>
        /// <param name="amongUsInstallPath">Among Usのインストールパス</param>
        /// <param name="dllRelativePath">BepInExフォルダからのDLL相対パス (例: "plugins/TownOfHost.dll")</param>
        /// <returns>検出されたバージョン文字列、または "不明" (ファイルが存在しない、またはバージョン情報が取得できない場合)</returns>
        public static string GetDllVersion(string amongUsInstallPath, string dllRelativePath)
        {
            try
            {
                string bepInExPath = Path.Combine(amongUsInstallPath, "BepInEx");
                string fullDllPath = Path.Combine(bepInExPath, dllRelativePath);

                if (File.Exists(fullDllPath))
                {
                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(fullDllPath);
                    if (!string.IsNullOrEmpty(versionInfo.ProductVersion))
                    {
                        return versionInfo.ProductVersion;
                    }
                    else if (!string.IsNullOrEmpty(versionInfo.FileVersion))
                    {
                        return versionInfo.FileVersion;
                    }
                    else
                    {
                        Debug.WriteLine($"DEBUG: ファイル '{fullDllPath}' にバージョン情報が見つかりません。");
                        return "バージョン情報なし";
                    }
                }
                else
                {
                    Debug.WriteLine($"DEBUG: ファイル '{fullDllPath}' が見つかりません。");
                    return "ファイルなし";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: DLLバージョン取得エラー ({dllRelativePath}): {ex.Message}");
                return "エラー";
            }
        }
    }
}
