using System.Windows;
using CefSharp;
using CefSharp.Wpf;
using Among_Us_ModManager;
using Among_Us_ModManager.Models;
using ModelAppVersion = Among_Us_ModManager.Models.AppVersion;

namespace AmongUsModManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LogOutput.Write("----------------------------------------------------------------------");
            LogOutput.Write("");
            LogOutput.Write($"バージョン: v{ModelAppVersion.Version}（{ModelAppVersion.ReleaseDate}）");
            LogOutput.Write("アプリの起動が完了しました。");

            // CefSharp の初期化はここでは行わない

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

    }
}
