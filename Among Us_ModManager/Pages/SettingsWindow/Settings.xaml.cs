using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Among_Us_ModManager.Pages.Settings
{
    public partial class SettingsPage : Page
    {
        private readonly string configPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "AmongUsModManager", "Settings.json");

        private SettingsConfig config = new SettingsConfig();

        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonConvert.DeserializeObject<SettingsConfig>(json) ?? new SettingsConfig();
                }
                catch
                {
                    config = new SettingsConfig(); // 読み込めなければデフォルト
                }
            }
            else
            {
                config = new SettingsConfig(); // 初回起動
            }

            ExePathTextBox.Text = config.AmongUsExePath ?? "";
            BackgroundStartCheckBox.IsChecked = config.RunInBackground;
        }

        private void BrowseExePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Among Us.exe を選択してください",
                Filter = "Among Us 実行ファイル|Among Us.exe|すべてのファイル|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                ExePathTextBox.Text = dialog.FileName;
                config.AmongUsExePath = dialog.FileName;
                SaveSettings();
            }
        }

        private void BackgroundStartChanged(object sender, RoutedEventArgs e)
        {
            // TextBox の値が空でなければ config に反映
            if (!string.IsNullOrWhiteSpace(ExePathTextBox.Text))
                config.AmongUsExePath = ExePathTextBox.Text;

            config.RunInBackground = BackgroundStartCheckBox.IsChecked == true;
            SaveSettings();
        }


        private void SaveSettings()
        {
            try
            {
                // TextBox が空でない場合にのみ上書き
                if (!string.IsNullOrWhiteSpace(ExePathTextBox.Text))
                    config.AmongUsExePath = ExePathTextBox.Text;

                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定の保存に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }

    public class SettingsConfig
    {
        [JsonProperty("AmongUsExePath")]
        public string AmongUsExePath { get; set; }

        [JsonProperty("RunInBackground")]
        public bool RunInBackground { get; set; } = true; // デフォルトはオン
    }
}
