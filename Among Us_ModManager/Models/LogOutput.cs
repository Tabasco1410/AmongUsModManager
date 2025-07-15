using System;
using System.IO;

namespace Among_Us_ModManager
{
    public static class LogOutput
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmongUsModManager");
        private static readonly string LogFilePath = Path.Combine(AppDataFolder, "LogOutput.log");

        static LogOutput()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                    Directory.CreateDirectory(AppDataFolder);
            }
            catch
            {
                // フォルダ作成失敗は無視
            }
        }

        public static void Write(string message)
        {
            try
            {
                var log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, log);
            }
            catch
            {
                // ログ書き込み失敗は無視
            }
        }
    }
}