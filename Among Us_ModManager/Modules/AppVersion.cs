using System;
using System.Reflection;

namespace Among_Us_ModManager.Modules
{
    public static class AppVersion
    {
        // .csproj の <Version>
        public static string Version =>
            Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "Unknown";

        // .csproj の <FileVersion>
        public static string FileVersion =>
            Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            ?? "Unknown";

        // .csproj の <AssemblyVersion>
        public static string AssemblyVersion =>
            Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "Unknown";

        // リリース日や Notes は必要なら手動管理（自動化は難しい）
        public const string ReleaseDate = "2025-09-07";
        public const string Notes = "起動時の軽量化、設定画面の実装、modの自動インストールを実装しました。";
    }
}
