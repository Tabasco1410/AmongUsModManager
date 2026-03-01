using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace AmongUsModManager.Services
{

    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
    }


    public static class LogService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AmongUsModManager");

        private static readonly string LogPath = Path.Combine(AppDataFolder, "LogOutput.log");
        private static readonly object _lock = new object();
        private static int _callCounter = 0;

#if DEBUG
        public record LogEntry(LogLevel Level, string Source, string Text, DateTime Timestamp);
        public static event Action<LogEntry>? LogWritten;
#endif


        public static void Initialize(bool appendMode)
        {
            try
            {
                Directory.CreateDirectory(AppDataFolder);
                if (!appendMode && File.Exists(LogPath))
                    File.Delete(LogPath);

                Info("LogService", $"=== AmongUsModManager 起動 (mode={(appendMode ? "追記" : "上書き")}, PID={Environment.ProcessId}) ===");
                Debug("LogService", $"AppDataFolder: {AppDataFolder}");
                Debug("LogService", $"OS: {Environment.OSVersion}, 64bit: {Environment.Is64BitProcess}");
                Debug("LogService", $"Thread: {Thread.CurrentThread.ManagedThreadId}");
            }
            catch { /* ログ初期化失敗は無視 */ }
        }


        public static void Trace(string source, string message,
            [CallerMemberName] string caller = "",
            [CallerLineNumber] int line = 0)
        {
#if DEBUG
            Write(LogLevel.Trace, source, message, caller, line);
#endif
        }


        public static void Debug(string source, string message,
            [CallerMemberName] string caller = "",
            [CallerLineNumber] int line = 0)
        {
#if DEBUG
            Write(LogLevel.Debug, source, message, caller, line);
#endif
        }


        public static void Info(string source, string message,
            [CallerMemberName] string caller = "",
            [CallerLineNumber] int line = 0)
            => Write(LogLevel.Info, source, message, caller, line);


        public static void Warn(string source, string message,
            [CallerMemberName] string caller = "",
            [CallerLineNumber] int line = 0)
            => Write(LogLevel.Warn, source, message, caller, line);


        public static void Error(string source, string message, Exception? ex = null,
            [CallerMemberName] string caller = "",
            [CallerLineNumber] int line = 0)
        {
            string msg = ex == null ? message
                : $"{message} | [{ex.GetType().Name}] {ex.Message}";
#if DEBUG
            if (ex != null)
            {
                msg += $"\n  StackTrace:\n{ex.StackTrace}";
                if (ex.InnerException != null)
                    msg += $"\n  InnerException: [{ex.InnerException.GetType().Name}] {ex.InnerException.Message}";
            }
#endif
            Write(LogLevel.Error, source, msg, caller, line);
        }


        public static void MethodStart(string source,
            [CallerMemberName] string caller = "",
            [CallerLineNumber] int line = 0)
        {
#if DEBUG
            Write(LogLevel.Debug, source, $"▶ {caller}() 開始", caller, line);
#endif
        }


        public static void MethodEnd(string source,
            [CallerMemberName] string caller = "",
            [CallerLineNumber] int line = 0)
        {
#if DEBUG
            Write(LogLevel.Debug, source, $"◀ {caller}() 完了", caller, line);
#endif
        }


        private static void Write(LogLevel level, string source, string message,
            string caller = "", int line = 0)
        {
            try
            {
                int seq = Interlocked.Increment(ref _callCounter);
                int tid = Thread.CurrentThread.ManagedThreadId;

#if DEBUG

                string callerInfo = !string.IsNullOrEmpty(caller) ? $"({caller}:{line})" : "";
                string lineStr = $"[{level,-5}] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{source} {callerInfo}] #{seq:D5} T{tid:D2} {message}";
#else
                string lineStr = $"[{level,-5}] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{source}] {message}";
#endif
                lock (_lock)
                {
                    Directory.CreateDirectory(AppDataFolder);
                    File.AppendAllText(LogPath, lineStr + Environment.NewLine, Encoding.UTF8);
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine(lineStr);
                LogWritten?.Invoke(new LogEntry(level, source, lineStr, DateTime.Now));
#endif
            }
            catch { /* ログ書き込み失敗はばいばい */ }
        }
    }
}