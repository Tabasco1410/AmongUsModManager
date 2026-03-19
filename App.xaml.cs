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
        public static string AppVersion { get; } = "1.4.2";
        /// <summary>
<<<<<<< HEAD
        /// プレリリースかどうか。
        /// true にすると、デバッグビルドのときに自動アップデートチェックが無効になる。
        /// リリースビルドでは常にfalse扱い。
=======
        /// プレリリース/かどうか。
        /// true にすると、デバッグビルドのときに自動アップデートチェックが無効になる。
        /// リリースビルドでは常にflase扱い。
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
        /// </summary>
        public const bool IsPreRelease = true;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
<<<<<<< HEAD
            var config = ConfigService.Load();

            // ログ初期化（AppendMode=true→上書き、false→起動ごとに新ファイル）
            LogService.Initialize(config.LogAppendMode);
            LogService.Info("App", $"AmongUsModManager v{AppVersion} 起動");

            // Toast通知マネージャー初期化
            NotificationService.Initialize();

=======
           
            var config = ConfigService.Load();
            LogService.Initialize(config.LogAppendMode);
            LogService.Info("App", $"AmongUsModManager v{AppVersion} 起動");

>>>>>>> 9b70396323094b50176708b54875479518ab7e99
            config.LastLaunchTime = DateTime.Now;
            ConfigService.Save(config);

            m_window = new MainWindow();
            MainWindowInstance = m_window;
            m_window.Activate();

            LogService.Info("App", "MainWindow アクティブ化完了");
        }
    }
}
