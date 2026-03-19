using System;
using System.IO;
using System.Linq;
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

        // 現在のセッションで使うログファイルパス（Initialize で確定する）
        private static string _logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AmongUsModManager", "LogOutput.log");

        private static readonly object _lock = new object();
        private static int _callCounter = 0;

        // ログファイルを保持する最大数（古いものから削除）
        private const int MaxLogFiles = 10;

#if DEBUG
        public record LogEntry(LogLevel Level, string Source, string Text, DateTime Timestamp);
        public static event Action<LogEntry>? LogWritten;
#endif

        public static void Initialize(bool appendMode)
        {
            try
            {
                Directory.CreateDirectory(AppDataFolder);

                if (appendMode)
                {
                    // 追記モード: 従来どおり LogOutput.log に追記
                    _logPath = Path.Combine(AppDataFolder, "LogOutput.log");
                }
                else
                {
                    // 新ファイルモード: 起動日時をファイル名に含める
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    _logPath = Path.Combine(AppDataFolder, $"LogOutput_{timestamp}.log");

                    // 古いログファイルを整理（MaxLogFiles 件を超えたら古い順に削除）
                    CleanupOldLogs();
                }

                Info("LogService", $"=== AmongUsModManager 起動 (mode={(appendMode ? "追記" : "新規ファイル")}, PID={Environment.ProcessId}) ===");
                Debug("LogService", $"AppDataFolder: {AppDataFolder}");
                Debug("LogService", $"LogFile: {_logPath}");
                Debug("LogService", $"OS: {Environment.OSVersion}, 64bit: {Environment.Is64BitProcess}");
                Debug("LogService", $"Thread: {Thread.CurrentThread.ManagedThreadId}");
            }
            catch { /* ログ初期化失敗は無視 */ }
        }

        private static void CleanupOldLogs()
        {
            try
            {
                // LogOutput_*.log にマッチするファイルを古い順に並べて超過分を削除
                var logFiles = Directory.GetFiles(AppDataFolder, "LogOutput_*.log")
                    .OrderBy(f => f)
                    .ToList();

                // MaxLogFiles - 1 件まで残して新ファイル用の枠を確保
                int excess = logFiles.Count - (MaxLogFiles - 1);
                for (int i = 0; i < excess; i++)
                {
                    try { File.Delete(logFiles[i]); }
                    catch { /* 削除失敗は無視 */ }
                }
            }
            catch { }
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
                    File.AppendAllText(_logPath, lineStr + Environment.NewLine, Encoding.UTF8);
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine(lineStr);
                LogWritten?.Invoke(new LogEntry(level, source, lineStr, DateTime.Now));
#endif
            }
            catch { /* ログ書き込み失敗は無視 */ }
        }
    }
}
