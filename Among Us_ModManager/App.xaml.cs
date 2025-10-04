using Among_Us_ModManager.Pages.Settings;
using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls; // ContextMenu, MenuItem
using System.Windows.Controls.Primitives; // PlacementMode

namespace Among_Us_ModManager
{
    public partial class App : Application
    {
        private TaskbarIcon? trayIcon;
        private ContextMenu? contextMenu;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 設定ロード
            string settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AmongUsModManager", "Settings.json"
            );

            SettingsConfig settings;
            if (File.Exists(settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(settingsPath);
                    settings = JsonConvert.DeserializeObject<SettingsConfig>(json) ?? new SettingsConfig();
                }
                catch
                {
                    settings = new SettingsConfig();
                }
            }
            else
            {
                settings = new SettingsConfig();
            }

            // タスクトレイアイコン作成
            trayIcon = new TaskbarIcon
            {
                Icon = new Icon("icon_N.ico"),
                ToolTipText = "Among Us Mod Manager"
            };

            // コンテキストメニュー
            contextMenu = new ContextMenu();

            var showItem = new MenuItem { Header = "AmongUsModManagerを開く" };
            showItem.Click += (s, ev) => ShowMainWindow();

            var exitItem = new MenuItem { Header = "アプリを終了する" };
            exitItem.Click += (s, ev) => Shutdown();

            var closeMenuItem = new MenuItem { Header = "メニューを閉じる" };
            closeMenuItem.Click += (s, ev) =>
            {
                if (contextMenu != null)
                    contextMenu.IsOpen = false;
            };

            contextMenu.Items.Add(showItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(exitItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(closeMenuItem);

            // 左クリック → アプリを開く
            trayIcon.TrayLeftMouseUp += (s, ev) =>
            {
                Dispatcher.Invoke(ShowMainWindow);
            };

            // 右クリック → マウス位置にメニューを表示
            trayIcon.TrayRightMouseUp += (s, ev) =>
            {
                if (contextMenu != null)
                {
                    contextMenu.Placement = PlacementMode.MousePoint; // クリック位置
                    contextMenu.StaysOpen = false; // 外をクリックしたら閉じる
                    contextMenu.IsOpen = true;
                }
            };

            // 最初にメインウィンドウを出す
            var mainWindow = new MainWindow();
            Current.MainWindow = mainWindow;
            mainWindow.Show();
        }

        private void ShowMainWindow()
        {
            var main = Current.MainWindow ?? new MainWindow();
            Current.MainWindow = main;
            main.Show();
            main.WindowState = WindowState.Normal;
            main.Activate();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            trayIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
