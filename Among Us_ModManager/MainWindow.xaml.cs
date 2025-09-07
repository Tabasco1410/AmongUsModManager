using Among_Us_ModManager.Modules.Updates;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace Among_Us_ModManager
{
    public partial class MainWindow : Window
    {
        private readonly string configPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "AmongUsModManager", "WindowConfig.json");

        private AppWindowConfig appConfig = new AppWindowConfig();

        public void NavigateToPage(System.Windows.Controls.Page page)
        {
            MainFrame.Navigate(page);
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadWindowSize();
            CheckForceUpdateAsync();
            MainFrame.Navigate(new Pages.SelectEXEPath());
        }

        private async void CheckForceUpdateAsync()
        {
            string owner = "Tabasco1410";
            string repo = "AmongUsModManager";

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AmongUsModManager");

                string latestReleaseUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
                string latestJson = await client.GetStringAsync(latestReleaseUrl);
                using var doc = JsonDocument.Parse(latestJson);

                var assets = doc.RootElement.GetProperty("assets").EnumerateArray();
                string? jsonUrl = null;
                foreach (var asset in assets)
                {
                    if (asset.GetProperty("name").GetString() == "force_update.json")
                    {
                        jsonUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }

                if (jsonUrl == null)
                    return;

                string json = await client.GetStringAsync(jsonUrl);
                var jsonDoc = JsonDocument.Parse(json);
                bool forceUpdate = jsonDoc.RootElement.GetProperty("force_update").GetBoolean();

                if (forceUpdate)
                    await AppUpdater.StartUpdaterAndExit();
            }
            catch
            {
            }
        }

        private void LoadWindowSize()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    appConfig = JsonSerializer.Deserialize<AppWindowConfig>(json) ?? new AppWindowConfig();
                }

                var config = appConfig.MainWindow;
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

                appConfig.MainWindow = config;

                var dir = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(dir))
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

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
        }
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
