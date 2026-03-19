using System;
using Microsoft.UI.Xaml;
using AmongUsModManager.Services;
using AmongUsModManager.Models.Services;

namespace AmongUsModManager
{
    public partial class App : Application
    {
        public Window m_window { get; private set; } = null!;
        public static Window? MainWindowInstance { get; private set; }
        public static string AppVersion { get; } = "1.4.3";
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

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var config = ConfigService.Load();

            // ログ初期化（AppendMode=true→上書き、false→起動ごとに新ファイル）
            LogService.Initialize(config.LogAppendMode);
            LogService.Info("App", $"AmongUsModManager v{AppVersion} 起動");

            // Toast通知マネージャー初期化
            NotificationService.Initialize();

            config.LastLaunchTime = DateTime.Now;
            ConfigService.Save(config);

            m_window = new MainWindow();
            MainWindowInstance = m_window;
            m_window.Activate();

            LogService.Info("App", "MainWindow アクティブ化完了");
        }
    }
}
