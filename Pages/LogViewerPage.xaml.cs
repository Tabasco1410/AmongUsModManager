using System;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AmongUsModManager.Models.Services;

namespace AmongUsModManager.Pages
{
    public sealed partial class LogViewerPage : Page
    {
        private static readonly string LogFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AmongUsModManager");

        // 現在選択中のログファイルパス
        private string? _currentLogPath;

        public LogViewerPage()
        {
            this.InitializeComponent();
            ApplyStrings();
            LocalizationService.LanguageChanged += ApplyStrings;
            this.Unloaded += (_, _) => LocalizationService.LanguageChanged -= ApplyStrings;
            RefreshFileList();
        }

        private void ApplyStrings()
        {
            LogViewerPageTitle.Text      = LocalizationService.Get("Log_Viewer");
            LogFileLabel.Text            = LocalizationService.Get("Log_File");
            LogRefreshBtn.Content        = LocalizationService.Get("Log_Refresh");
            LogOpenFolderBtn.Content     = LocalizationService.Get("Log_OpenFolder");
            LogClearBtn.Content          = LocalizationService.Get("Log_Clear");
        }

        // ─── ファイルリスト更新 ───────────────────────────────────────
        private void RefreshFileList()
        {
            if (!Directory.Exists(LogFolder)) return;

            // LogOutput_*.log（新ファイルモード）と LogOutput.log（上書きモード）を両方収集
            var files = Directory.GetFiles(LogFolder, "LogOutput*.log")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();

            if (files.Count == 0)
            {
                LogText.Text = LocalizationService.Get("Log_NoFiles");
                return;
            }

            LogFileCombo.Items.Clear();
            foreach (var f in files)
            {
                string label = Path.GetFileName(f);
                if (f == files[0]) label += "  " + LocalizationService.Get("Log_Latest");
                LogFileCombo.Items.Add(new LogFileItem { DisplayName = label, FilePath = f });
            }
            LogFileCombo.SelectedIndex = 0; // 最新を選択（SelectionChangedが発火してLoadLogが呼ばれる）
        }

        private void LoadLog(string path)
        {
            _currentLogPath = path;
            if (!File.Exists(path))
            {
                LogText.Text = LocalizationService.Get("Log_NotFound");
                return;
            }
            try
            {
                // FileShare.ReadWrite で他プロセスが書き込み中でも読める
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, System.Text.Encoding.UTF8);
                LogText.Text = reader.ReadToEnd();

                LogScrollViewer.UpdateLayout();
                LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
            }
            catch (Exception ex)
            {
                LogText.Text = $"ログ読み込みエラー: {ex.Message}";
            }
        }

        // ─── イベント ─────────────────────────────────────────────────
        private void LogFileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LogFileCombo.SelectedItem is LogFileItem item)
                LoadLog(item.FilePath);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshFileList();
            // ファイルリストは変わらない場合、現在のファイルを再読み込み
            if (_currentLogPath != null && File.Exists(_currentLogPath))
                LoadLog(_currentLogPath);
        }

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(LogFolder))
                await Windows.System.Launcher.LaunchFolderPathAsync(LogFolder);
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLogPath == null) return;

            var dialog = new ContentDialog
            {
                Title = LocalizationService.Get("Log_ClearTitle"),
                Content = string.Format(LocalizationService.Get("Log_ClearConfirm"), Path.GetFileName(_currentLogPath)),
                PrimaryButtonText = LocalizationService.Get("Common_Delete"),
                CloseButtonText = LocalizationService.Get("Common_Cancel"),
                XamlRoot = this.XamlRoot
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                try
                {
                    File.WriteAllText(_currentLogPath, "", System.Text.Encoding.UTF8);
                    LoadLog(_currentLogPath);
                }
                catch (Exception ex)
                {
                    LogText.Text = $"クリアエラー: {ex.Message}";
                }
            }
        }

        // ComboBox のアイテム用クラス
        private class LogFileItem
        {
            public string DisplayName { get; set; } = "";
            public string FilePath    { get; set; } = "";
            public override string ToString() => DisplayName;
        }
    }
}
