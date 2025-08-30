using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Among_Us_ModManager.Pages;

namespace Among_Us_ModManager.Pages
{
    public partial class SelectEXEPath : Page
    {
        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AmongUsModManager", "Vanilla_Config.json");

        private class VanillaConfig
        {
            public string AmongUsExePath { get; set; }

            public void Save()
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this));
            }
        }

        public SelectEXEPath()
        {
            InitializeComponent();
            Loaded += SelectEXEPath_Loaded;
        }

        private void SelectEXEPath_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(ConfigPath))
            {
                if (NavigationService != null)
                {
                    NavigationService.Navigate(new MainMenuPage());
                }
                else
                {
                    var navWindow = Window.GetWindow(this) as NavigationWindow;
                    navWindow?.Navigate(new MainMenuPage());
                }
            }
        }

        private void SelectAmongUsExe_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Among Us.exe を選択",
                Filter = "実行ファイル (*.exe)|*.exe",
                FileName = "Among Us.exe"
            };

            if (dialog.ShowDialog() == true)
            {
                var filePath = dialog.FileName;
                if (Path.GetFileName(filePath) != "Among Us.exe")
                {
                    MessageBox.Show("Among Us.exe を選択してください");
                    return;
                }

                var config = new VanillaConfig { AmongUsExePath = filePath };
                config.Save();

                SelectAmongUsExeButton.Visibility = Visibility.Collapsed;

                if (NavigationService != null)
                {
                    NavigationService.Navigate(new MainMenuPage());
                }
                else
                {
                    var navWindow = Window.GetWindow(this) as NavigationWindow;
                    navWindow?.Navigate(new MainMenuPage());
                }
            }
        }
    }
}
