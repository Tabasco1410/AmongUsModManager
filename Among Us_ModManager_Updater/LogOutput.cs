using System;
using System.IO;

namespace Among_Us_ModManager_Updater
{
    internal static class LogOutput
    {
        private static readonly string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AmongUsModManager", "Updater"
        );

        private static readonly string logPath = Path.Combine(logDirectory, "LogOutput.log");

        static LogOutput()
        {
            try
            {
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);
            }
            catch { }
        }

        public static void Log(string message)
        {
            string timeStamped = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n";

            try
            {
                File.AppendAllText(logPath, timeStamped);
            }
            catch { }
        }
    }
}
