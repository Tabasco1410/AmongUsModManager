using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Navigation;

namespace Among_Us_ModManager
{
    public partial class MainWindow : Window
    {
        private readonly string configPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "AmongUsModManager", "WindowConfig.json");

        public MainWindow()
        {
            InitializeComponent();

            // ウィンドウサイズ・位置を復元
            LoadWindowSize();

            // アプリ起動時に最初のページへ遷移
            MainFrame.Navigate(new Pages.MainMenuPage());
        }

        private void LoadWindowSize()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<WindowConfig>(json);
                    if (config != null)
                    {
                        Width = config.Width;
                        Height = config.Height;
                        Top = config.Top;
                        Left = config.Left;
                        WindowState = config.IsMaximized ? WindowState.Maximized : WindowState.Normal;
                    }
                }
            }
            catch
            {
                // 読み込み失敗は無視
            }
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

                var dir = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(config);
                File.WriteAllText(configPath, json);
            }
            catch
            {
                // 保存失敗は無視
            }
        }

        // ウィンドウが閉じられる直前にサイズ保存
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowSize();
            base.OnClosing(e);
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            // 必要なら処理をここに書く
        }
    }

    public class WindowConfig
    {
        public double Width { get; set; } = 800;   // デフォルト幅
        public double Height { get; set; } = 600;  // デフォルト高さ
        public double Top { get; set; } = 100;
        public double Left { get; set; } = 100;
        public bool IsMaximized { get; set; } = false;
    }
}
