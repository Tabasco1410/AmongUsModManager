using System;
using System.IO;
using System.Security.Principal;
using Microsoft.UI.Xaml;
using AmongUsModManager.Models.Services;

namespace AmongUsModManager
{
    public partial class App : Application
    {
        public Window m_window { get; private set; } = null!;
        public static Window? MainWindowInstance { get; private set; }
        public static string AppVersion { get; } = "1.4.5.3.1";
        /// <summary>
        /// プレリリースかどうか。
        /// true にすると、デバッグビルドのときに自動アップデートチェックが無効になる。
        /// リリースビルドでは常にfalse扱い。
        /// </summary>
        public const bool IsPreRelease = false;

        public App()
        {
            this.InitializeComponent();
        }

        public static bool IsRunningAsAdmin()
            => new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var config = ConfigService.Load();

            if (config.AlwaysRunAsAdmin && !IsRunningAsAdmin())
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo(
                        System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName)
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                    };
                    System.Diagnostics.Process.Start(psi);
                }
                catch { }
                Exit();
                return;
            }

            var csvPath = Path.Combine(AppContext.BaseDirectory, "Resources", "string.csv");
            LocalizationService.Load(csvPath);
            LocalizationService.SetLanguage(config.Language);

            // ログ初期化（AppendMode=true→上書き、false→起動ごとに新ファイル）
            LogService.Initialize(config.LogAppendMode);
            LogService.Info("App", $"AmongUsModManager v{AppVersion} を起動しました");

            // Toast通知マネージャー初期化
            NotificationService.Initialize();

            config.LastLaunchTime = DateTime.Now;
            ConfigService.Save(config);

            m_window = new MainWindow();
            MainWindowInstance = m_window;
            m_window.Activate();

            LogService.Info("App", "MainWindowをアクティブ化しました");
        }
    }
}
