using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Among_Us_ModManager.Modules;

namespace Among_Us_ModManager.Pages
{
    public partial class SettingsWindow : Window
    {
        private readonly string configPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "AmongUsModManager", "WindowConfig.json");

        private AppWindowConfig appConfig = new AppWindowConfig();

        public SettingsWindow()
        {
            InitializeComponent();

            // CSVから文字列読み込み & 言語設定
            Strings.SetLanguage("JA"); // 例: 日本語
            Strings.Load();

            // サイズ固定
            this.Width = 900;
            this.Height = 600;
            this.ResizeMode = ResizeMode.NoResize;
            this.WindowStyle = WindowStyle.SingleBorderWindow;

            // ウィンドウタイトル
            this.Title = Strings.Get("MenuTitle");

            // ボタンのテキスト
            BtnNotice.Content = Strings.Get("Notice");
            BtnSettings.Content = Strings.Get("Settings");

            LoadWindowPosition();
            LoadLastPage();
        }

        #region ウィンドウ位置の保存・復元
        private void LoadWindowPosition()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    appConfig = JsonSerializer.Deserialize<AppWindowConfig>(json) ?? new AppWindowConfig();
                }

                var config = appConfig.SettingsWindow;
                this.Top = config.Top;
                this.Left = config.Left;
            }
            catch
            {
                this.Top = 100;
                this.Left = 100;
            }
        }

        private void SaveWindowPosition()
        {
            try
            {
                var config = appConfig.SettingsWindow;
                config.Top = this.Top;
                config.Left = this.Left;

                var dir = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(appConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }
            catch { }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowPosition();

            // 最後に開いたページも保存
            try
            {
                var dir = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(appConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }
            catch { }

            base.OnClosing(e);
        }
        #endregion

        #region メニュー復元
        private void LoadLastPage()
        {
            try
            {
                if (appConfig.LastSettingsPage == "Settings")
                {
                    DetailFrame.Navigate(new Among_Us_ModManager.Pages.Settings.SettingsPage());
                }
                else
                {
                    DetailFrame.Navigate(new News());
                }
            }
            catch
            {
                DetailFrame.Navigate(new News());
            }
        }
        #endregion

        #region メニュークリック
        private void BtnNotice_Click(object sender, RoutedEventArgs e)
        {
            DetailFrame.Navigate(new News());
            appConfig.LastSettingsPage = "Notice";
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            DetailFrame.Navigate(new Among_Us_ModManager.Pages.Settings.SettingsPage());
            appConfig.LastSettingsPage = "Settings";
        }
        #endregion
    }

    public class WindowConfig
    {
        public double Top { get; set; } = 100;
        public double Left { get; set; } = 100;
    }

    public class AppWindowConfig
    {
        public WindowConfig MainWindow { get; set; } = new WindowConfig();
        public WindowConfig SettingsWindow { get; set; } = new WindowConfig();

        public string LastSettingsPage { get; set; } = "Notice";
    }
}
