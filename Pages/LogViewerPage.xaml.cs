using System;
using System.IO;
<<<<<<< HEAD
using System.Linq;
=======
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AmongUsModManager.Services;

namespace AmongUsModManager.Pages
{
    public sealed partial class LogViewerPage : Page
    {
<<<<<<< HEAD
        private static readonly string LogFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AmongUsModManager");

        // 現在選択中のログファイルパス
        private string? _currentLogPath;
=======
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AmongUsModManager", "LogOutput.log");
>>>>>>> 9b70396323094b50176708b54875479518ab7e99

        public LogViewerPage()
        {
            this.InitializeComponent();
<<<<<<< HEAD
            RefreshFileList();
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
=======
            LoadLog();
        }

        private void LoadLog()
        {
            if (!File.Exists(LogPath))
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
            {
                LogText.Text = "ログファイルがまだ存在しません。";
                return;
            }
<<<<<<< HEAD

            // ComboBox に表示名をセット
            LogFileCombo.Items.Clear();
            foreach (var f in files)
            {
                string label = Path.GetFileName(f);
                // 最新ファイルには "(最新)" を付ける
                if (f == files[0]) label += "  （最新）";
                LogFileCombo.Items.Add(new LogFileItem { DisplayName = label, FilePath = f });
            }
            LogFileCombo.SelectedIndex = 0; // 最新を選択（SelectionChangedが発火してLoadLogが呼ばれる）
        }

        private void LoadLog(string path)
        {
            _currentLogPath = path;
            if (!File.Exists(path))
            {
                LogText.Text = "ログファイルが見つかりません。";
                return;
            }
            try
            {
                // FileShare.ReadWrite で他プロセスが書き込み中でも読める
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, System.Text.Encoding.UTF8);
                LogText.Text = reader.ReadToEnd();

=======
            try
            {
                LogText.Text = File.ReadAllText(LogPath, System.Text.Encoding.UTF8);
                
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
                LogScrollViewer.UpdateLayout();
                LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
            }
            catch (Exception ex)
            {
                LogText.Text = $"ログ読み込みエラー: {ex.Message}";
            }
        }

<<<<<<< HEAD
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
=======
        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadLog();

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string folder = Path.GetDirectoryName(LogPath) ?? "";
            if (Directory.Exists(folder))
                await Windows.System.Launcher.LaunchFolderPathAsync(folder);
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
<<<<<<< HEAD
            if (_currentLogPath == null) return;

            var dialog = new ContentDialog
            {
                Title = "ログをクリア",
                Content = $"{Path.GetFileName(_currentLogPath)} の内容を削除しますか？",
=======
            var dialog = new ContentDialog
            {
                Title = "ログをクリア",
                Content = "LogOutput.log の内容を削除しますか？",
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
                PrimaryButtonText = "削除",
                CloseButtonText = "キャンセル",
                XamlRoot = this.XamlRoot
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
<<<<<<< HEAD
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
=======
                try { File.WriteAllText(LogPath, "", System.Text.Encoding.UTF8); LoadLog(); }
                catch (Exception ex) { LogText.Text = $"クリアエラー: {ex.Message}"; }
            }
        }
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
    }
}
