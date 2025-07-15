using Among_Us_ModManager;
using Among_Us_ModManager.Models;
using CefSharp;
using CefSharp.Wpf;
using System;
using System.Windows;
using ModelAppVersion = Among_Us_ModManager.Models.AppVersion;
using System.IO;

namespace AmongUsModManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                string appDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AmongUsModManager");

                string logPath = Path.Combine(appDataFolder, "LogOutput.log");
                string backupLogPath = Path.Combine(appDataFolder, "LogOutput_Old.log");

                if (File.Exists(logPath))
                {
                    if (File.Exists(backupLogPath))
                        File.Delete(backupLogPath);

                    File.Move(logPath, backupLogPath); // バックアップ
                }

                File.WriteAllText(logPath, ""); // 空の新ログファイル作成
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ログ初期化失敗: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            LogOutput.Write("----------------------------------------------------------------------");
            LogOutput.Write("");
            LogOutput.Write($"バージョン: v{ModelAppVersion.Version}（{ModelAppVersion.ReleaseDate}）");
            LogOutput.Write("アプリの起動が完了しました。");
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
