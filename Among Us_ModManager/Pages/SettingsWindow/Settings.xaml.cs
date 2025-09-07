using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;

namespace Among_Us_ModManager.Pages.Settings
{
    public partial class SettingsPage : Page
    {
        private readonly string configPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "AmongUsModManager", "Settings.json");

        private readonly string languageCsvPath =
            Path.Combine(CurrentDirectory(), "string.csv"); // string.csv の配置場所

        private SettingsConfig config = new SettingsConfig();
        private bool _isInitializing = false; // 初期化フラグ

        private Dictionary<string, string> languageDict = new Dictionary<string, string>();

        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();
            LoadLanguageOptions();
            LoadPlatformOptions();
        }

        private void LoadSettings()
        {
            _isInitializing = true;
            LogOutput.Write("LoadSettings 開始");

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonConvert.DeserializeObject<SettingsConfig>(json) ?? new SettingsConfig();
                    LogOutput.Write($"設定読み込み成功: AmongUsExePath={config.AmongUsExePath}, RunInBackground={config.RunInBackground}, Language={config.Language}, Platform={config.Platform}");
                }
                catch (Exception ex)
                {
                    LogOutput.Write($"設定読み込み失敗: {ex.Message}");
                    config = new SettingsConfig();
                }
            }
            else
            {
                config = new SettingsConfig();
                LogOutput.Write("設定ファイルが存在しないため新規作成");
                SaveSettings();
            }

            ExePathTextBox.Text = config.AmongUsExePath ?? "";
            BackgroundStartCheckBox.IsChecked = config.RunInBackground;

            _isInitializing = false;
            LogOutput.Write("LoadSettings 完了");
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
                if (!string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    ExePathTextBox.Text = dialog.FileName;
                    config.AmongUsExePath = dialog.FileName;
                    LogOutput.Write($"BrowseExePath_Click: ファイル選択 {config.AmongUsExePath}");
                    SaveSettings();
                }
            }
        }

        private void BackgroundStartChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            if (!string.IsNullOrWhiteSpace(ExePathTextBox.Text))
                config.AmongUsExePath = ExePathTextBox.Text;

            config.RunInBackground = BackgroundStartCheckBox.IsChecked == true;
            LogOutput.Write($"BackgroundStartChanged: AmongUsExePath={config.AmongUsExePath}, RunInBackground={config.RunInBackground}");

            if (!string.IsNullOrWhiteSpace(config.AmongUsExePath))
                SaveSettings();
        }

        private void LoadLanguageOptions()
        {
            _isInitializing = true;

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string resourceName = "Among_Us_ModManager.Resources.Strings.csv";

            var resourceNames = assembly.GetManifestResourceNames();
            if (!resourceNames.Contains(resourceName))
            {
                LogOutput.Write($"埋め込みリソース '{resourceName}' が見つかりません。");
                foreach (var n in resourceNames) LogOutput.Write($"存在するリソース: {n}");
                _isInitializing = false;
                return;
            }

            using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);

            var lines = new List<string>();
            while (!reader.EndOfStream)
                lines.Add(reader.ReadLine());

            if (lines.Count < 1) { _isInitializing = false; return; }

            var headers = lines[0].Split(',');

            languageDict.Clear();
            LanguageComboBox.Items.Clear();

            for (int i = 1; i < headers.Length; i++)
            {
                string lang = headers[i].Trim();
                if (string.IsNullOrEmpty(lang)) continue;

                languageDict[lang] = lang;
                ComboBoxItem item = new ComboBoxItem { Tag = lang, Content = lang };
                LanguageComboBox.Items.Add(item);

                if (config.Language == lang) LanguageComboBox.SelectedItem = item;
            }

            if (LanguageComboBox.SelectedItem == null && LanguageComboBox.Items.Count > 0)
                LanguageComboBox.SelectedIndex = 0;

            _isInitializing = false;
        }

        private void LoadPlatformOptions()
        {
            _isInitializing = true;

            foreach (ComboBoxItem item in PlatformComboBox.Items)
            {
                if ((string)item.Content == config.Platform)
                {
                    PlatformComboBox.SelectedItem = item;
                    break;
                }
            }

            if (PlatformComboBox.SelectedItem == null && PlatformComboBox.Items.Count > 0)
                PlatformComboBox.SelectedIndex = 0;

            _isInitializing = false;
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (LanguageComboBox.SelectedItem is ComboBoxItem item)
            {
                string selectedLang = item.Tag?.ToString() ?? "JA";
                if (!string.IsNullOrWhiteSpace(selectedLang))
                {
                    config.Language = selectedLang;
                    SaveSettings();
                    LogOutput.Write($"LanguageComboBox_SelectionChanged: Language={config.Language}");
                }
            }
        }

        private void PlatformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (PlatformComboBox.SelectedItem is ComboBoxItem item)
            {
                string selectedPlatform = item.Content.ToString() ?? "Steam";
                config.Platform = selectedPlatform;
                SaveSettings();
                LogOutput.Write($"PlatformComboBox_SelectionChanged: Platform={config.Platform}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
                LogOutput.Write($"SaveSettings 成功: {json}");
            }
            catch (Exception ex)
            {
                LogOutput.Write($"SaveSettings 失敗: {ex.Message}");
                MessageBox.Show($"設定の保存に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 実行中ディレクトリを取得 (環境による CurrentDirectory の不安定さ回避)
        /// </summary>
        private static string CurrentDirectory()
        {
            return AppContext.BaseDirectory;
        }
    }

    public class SettingsConfig
    {
        [JsonProperty("AmongUsExePath", Required = Required.Default)]
        public string AmongUsExePath { get; set; } = "";

        [JsonProperty("RunInBackground", Required = Required.Default)]
        public bool RunInBackground { get; set; } = true;

        [JsonProperty("Language", Required = Required.Default)]
        public string Language { get; set; } = "JA"; // デフォルト日本語

        [JsonProperty("Platform", Required = Required.Default)]
        public string Platform { get; set; } = "Steam"; // デフォルト Steam
    }
}
