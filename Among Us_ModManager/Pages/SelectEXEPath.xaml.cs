using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Among_Us_ModManager.Modules;

namespace Among_Us_ModManager.Pages
{
    public partial class SelectEXEPath : Page
    {
        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AmongUsModManager", "Settings.json");

        private class SettingConfig
        {
            public string AmongUsExePath { get; set; } = "";

            public void Save()
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
                LogOutput.Write($"SettingConfig.Save(): 保存成功, Path={ConfigPath}, 内容={json}");
            }
        }

        public SelectEXEPath()
        {
            InitializeComponent();

            // CSVから文字列読み込み & 言語設定
            Strings.SetLanguage("JA"); // 例: デフォルト日本語
            Strings.Load();

            // ボタンの文字列を多言語化
            SelectAmongUsExeButton.Content = Strings.Get("SelectExe");

            Loaded += SelectEXEPath_Loaded;
            LogOutput.Write("SelectEXEPath: ページ初期化完了");
        }

        private void SelectEXEPath_Loaded(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("SelectEXEPath_Loaded: ページロード開始");

            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<SettingConfig>(json);

                    if (config != null && !string.IsNullOrWhiteSpace(config.AmongUsExePath))
                    {
                        LogOutput.Write($"SelectEXEPath_Loaded: 設定ファイル発見, AmongUsExePath={config.AmongUsExePath}");
                        NavigateToMainMenu();
                    }
                    else
                    {
                        LogOutput.Write("SelectEXEPath_Loaded: 設定ファイル有 AmongUsExePath が空、EXE選択待ち");
                    }
                }
                catch (Exception ex)
                {
                    LogOutput.Write($"SelectEXEPath_Loaded: 設定ファイル読み込み失敗: {ex.Message}");
                }
            }
            else
            {
                LogOutput.Write("SelectEXEPath_Loaded: 設定ファイルなし、EXE選択待ち");
            }
        }

        private void SelectAmongUsExe_Click(object sender, RoutedEventArgs e)
        {
            LogOutput.Write("SelectAmongUsExe_Click: Among Us.exe 選択ダイアログを表示");

            var dialog = new OpenFileDialog
            {
                Title = Strings.Get("SelectExe"),
                Filter = "実行ファイル (*.exe)|*.exe",
                FileName = "Among Us.exe"
            };

            if (dialog.ShowDialog() == true)
            {
                var filePath = dialog.FileName;
                LogOutput.Write($"SelectAmongUsExe_Click: 選択されたファイル {filePath}");

                if (Path.GetFileName(filePath) != "Among Us.exe")
                {
                    LogOutput.Write("SelectAmongUsExe_Click: 選択ファイルが Among Us.exe ではない");
                    MessageBox.Show(Strings.Get("SelectExe")); // 多言語対応のメッセージ
                    return;
                }

                // 既存設定を読み込む
                SettingConfig config;
                if (File.Exists(ConfigPath))
                {
                    try
                    {
                        string json = File.ReadAllText(ConfigPath);
                        config = JsonSerializer.Deserialize<SettingConfig>(json) ?? new SettingConfig();
                    }
                    catch
                    {
                        config = new SettingConfig();
                    }
                }
                else
                {
                    config = new SettingConfig();
                }

                // AmongUsExePath だけ上書き
                config.AmongUsExePath = filePath;

                // 保存
                config.Save();

                SelectAmongUsExeButton.Visibility = Visibility.Collapsed;
                LogOutput.Write("SelectAmongUsExe_Click: ボタン非表示");

                NavigateToMainMenu();
            }
            else
            {
                LogOutput.Write("SelectAmongUsExe_Click: ダイアログキャンセル");
            }
        }

        private void NavigateToMainMenu()
        {
            if (NavigationService != null)
            {
                LogOutput.Write("NavigateToMainMenu: NavigationService で MainMenuPage へ遷移");
                NavigationService.Navigate(new MainMenuPage());
            }
            else
            {
                var navWindow = Window.GetWindow(this) as NavigationWindow;
                LogOutput.Write("NavigateToMainMenu: NavigationWindow で MainMenuPage へ遷移");
                navWindow?.Navigate(new MainMenuPage());
            }
        }
    }
}
