using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json;
using System;
using System.Drawing; // System.Drawing.Common が必要
using System.IO;
using System.Windows;
using Among_Us_ModManager.Pages.Settings;

namespace Among_Us_ModManager
{
    public partial class App : Application
    {
        private TaskbarIcon trayIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Settings.json を読み込む
            string settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AmongUsModManager", "Settings.json"
            );

            SettingsConfig settings = null;
            if (File.Exists(settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(settingsPath);
                    settings = JsonConvert.DeserializeObject<SettingsConfig>(json);
                }
                catch
                {
                    settings = new SettingsConfig(); // 読み込めなければデフォルト
                }
            }
            else
            {
                settings = new SettingsConfig(); // 初回起動
            }

            // 「アプリを閉じてもバックグラウンドで起動」の設定がオンなら
            if (settings.RunInBackground)
            {
                // タスクトレイにアイコンを作成
                trayIcon = new TaskbarIcon
                {
                    Icon = new Icon("icon_N.ico"), // プロジェクト直下のアイコン
                    ToolTipText = "Among Us Mod Manager"
                };

                // ウィンドウを表示せずバックグラウンド常駐可能
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            trayIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
