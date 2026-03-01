using System;
using Microsoft.UI.Xaml;
using AmongUsModManager.Services;
using AmongUsModManager.Models.Services;

namespace AmongUsModManager
{
    public partial class App : Application
    {
        public Window m_window { get; private set; }
        public static Window? MainWindowInstance { get; private set; }
        public static string AppVersion { get; } = "1.4.2";
        /// <summary>
        /// プレリリース/かどうか。
        /// true にすると、デバッグビルドのときに自動アップデートチェックが無効になる。
        /// リリースビルドでは常にflase扱い。
        /// </summary>
        public const bool IsPreRelease = true;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
           
            var config = ConfigService.Load();
            LogService.Initialize(config.LogAppendMode);
            LogService.Info("App", $"AmongUsModManager v{AppVersion} 起動");

            config.LastLaunchTime = DateTime.Now;
            ConfigService.Save(config);

            m_window = new MainWindow();
            MainWindowInstance = m_window;
            m_window.Activate();

            LogService.Info("App", "MainWindow アクティブ化完了");
        }
    }
}
