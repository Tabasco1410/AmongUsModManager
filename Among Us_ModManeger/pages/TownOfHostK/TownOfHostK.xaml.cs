using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Among_Us_ModManeger.Pages.TownOfHostK
{
    public partial class TownOfHostK : Page
    {
        private readonly string configFolderPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmongUsModManeger");

        private readonly string configFilePath;

        private const string ModKey = "TownOfHostK";

        public TownOfHostK()
        {
            InitializeComponent();
            configFilePath = Path.Combine(configFolderPath, "Mods_Config.json");
            LoadConfig();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void SelectExeButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Among Us 実行ファイル (*.exe)|Among Us.exe",
                Title = "Among Us.exe を選択してください"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (File.Exists(openFileDialog.FileName))
                {
                    ExePathTextBox.Text = openFileDialog.FileName;
                }
                else
                {
                    MessageBox.Show("指定されたファイルは存在しません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SavePathButton_Click(object sender, RoutedEventArgs e)
        {
            var exePath = ExePathTextBox.Text.Trim();
            var folderName = CopiedFolderNameTextBox.Text.Trim();

            if (!File.Exists(exePath))
            {
                MessageBox.Show("指定されたパスに Among Us.exe が存在しません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(folderName))
            {
                MessageBox.Show("コピー後のフォルダ名を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveConfig(exePath, folderName);
            
            // TODO: 次の画面への遷移などがあればここで行う
            NavigationService?.Navigate(new TownOfHostK_Install());

        }

        private void SaveConfig(string exePath, string folderName)
        {
            try
            {
                Directory.CreateDirectory(configFolderPath);

                Dictionary<string, ModConfig> allConfigs = new();
                if (File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    var existing = JsonSerializer.Deserialize<ModConfigContainer>(json);
                    if (existing?.Mods != null)
                        allConfigs = existing.Mods;
                }

                allConfigs[ModKey] = new ModConfig
                {
                    ExePath = exePath,
                    CopiedFolderName = folderName
                };

                var container = new ModConfigContainer { Mods = allConfigs };
                var newJson = JsonSerializer.Serialize(container, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configFilePath, newJson);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定の保存中にエラーが発生しました:\n{ex.Message}", "保存エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    var container = JsonSerializer.Deserialize<ModConfigContainer>(json);
                    if (container?.Mods != null && container.Mods.TryGetValue(ModKey, out var config))
                    {
                        if (!string.IsNullOrEmpty(config.ExePath)) ExePathTextBox.Text = config.ExePath;
                        if (!string.IsNullOrEmpty(config.CopiedFolderName)) CopiedFolderNameTextBox.Text = config.CopiedFolderName;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定の読み込み中にエラーが発生しました:\n{ex.Message}", "読み込みエラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private class ModConfig
        {
            public string? ExePath { get; set; }
            public string? CopiedFolderName { get; set; }
        }

        private class ModConfigContainer
        {
            public Dictionary<string, ModConfig>? Mods { get; set; }
        }
    }
}
