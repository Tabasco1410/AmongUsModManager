using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

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
            LoadWindowSize();
        }

        #region ウィンドウサイズの保存・復元
        private void LoadWindowSize()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    appConfig = JsonSerializer.Deserialize<AppWindowConfig>(json) ?? new AppWindowConfig();
                }

                var config = appConfig.SettingsWindow;
                Width = config.Width;
                Height = config.Height;
                Top = config.Top;
                Left = config.Left;
                WindowState = config.IsMaximized ? WindowState.Maximized : WindowState.Normal;
            }
            catch { }
        }

        private void SaveWindowSize()
        {
            try
            {
                var config = new WindowConfig
                {
                    Width = this.Width,
                    Height = this.Height,
                    Top = this.Top,
                    Left = this.Left,
                    IsMaximized = this.WindowState == WindowState.Maximized
                };

                appConfig.SettingsWindow = config;

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
            SaveWindowSize();
            base.OnClosing(e);
        }
        #endregion

        #region メニュークリック
        private void BtnNotice_Click(object sender, RoutedEventArgs e)
        {
            // News ページを右側に表示
            DetailFrame.Navigate(new News());
        }
        #endregion
    }

    public class WindowConfig
    {
        public double Width { get; set; } = 800;
        public double Height { get; set; } = 600;
        public double Top { get; set; } = 100;
        public double Left { get; set; } = 100;
        public bool IsMaximized { get; set; } = false;
    }

    public class AppWindowConfig
    {
        public WindowConfig MainWindow { get; set; } = new WindowConfig();
        public WindowConfig SettingsWindow { get; set; } = new WindowConfig();
    }
}
