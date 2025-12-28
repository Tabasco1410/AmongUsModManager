using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using AmongUsModManager.Services;
using AmongUsModManager.Models;

namespace AmongUsModManager.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public ObservableCollection<VanillaPathInfo> VanillaPaths { get; } = new ObservableCollection<VanillaPathInfo>();
        public ObservableCollection<string> DetectedPaths { get; } = new ObservableCollection<string>();

        private string _pendingTag;

        public SettingsPage()
        {
            this.InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var config = ConfigService.Load();
            if (config != null)
            {
                VanillaPaths.Clear();
                if (config.VanillaPaths != null)
                {
                    foreach (var info in config.VanillaPaths)
                    {
                      
                        VanillaPaths.Add(new VanillaPathInfo { Name = info.Name, Path = info.Path });
                    }
                }
                ModDataPathTextBox.Text = config.ModDataPath ?? string.Empty;
            }
         
        }

        public bool HasUnsavedChanges()
        {
            var config = ConfigService.Load();
            if (config == null) return false;

            if (VanillaPaths.Count != (config.VanillaPaths?.Count ?? 0)) return true;

            for (int i = 0; i < VanillaPaths.Count; i++)
            {
                if (VanillaPaths[i].Name != config.VanillaPaths[i].Name ||
                    VanillaPaths[i].Path != config.VanillaPaths[i].Path)
                    return true;
            }

            if (ModDataPathTextBox.Text != (config.ModDataPath ?? string.Empty)) return true;

            return false;
        }

        public void ShowUnsavedWarning(string tag)
        {
            _pendingTag = tag;
            UnsavedChangesBar.IsOpen = true; 
        }

        private void SaveAndExit_Click(object sender, RoutedEventArgs e)
        {
            
            ExecuteSave();

            UnsavedChangesBar.IsOpen = false;

          
            if (App.MainWindowInstance is MainWindow mw)
            {
                mw.NavigateToPendingPage(_pendingTag);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSave();
            StatusMessage.Text = "設定を保存しました。";
            StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 128, 0));
        }

     
        private void ExecuteSave()
        {
            var config = ConfigService.Load() ?? new AppConfig();
            config.VanillaPaths = VanillaPaths.ToList();
            config.ModDataPath = ModDataPathTextBox.Text;
            if (VanillaPaths.Count > 0) config.GameInstallPath = VanillaPaths[0].Path;

            ConfigService.Save(config);
            UnsavedChangesBar.IsOpen = false; 
        }

       
        private async Task<StorageFolder?> SelectFolder()
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            IntPtr hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(folderPicker, hwnd);
            try { return await folderPicker.PickSingleFolderAsync(); } catch { return null; }
        }

        private async void SelectScanTarget_Click(object sender, RoutedEventArgs e)
        {
            var folder = await SelectFolder();
            if (folder != null) ScanTargetTextBox.Text = folder.Path;
        }

        private void ClearScanTarget_Click(object sender, RoutedEventArgs e) => ScanTargetTextBox.Text = string.Empty;

        private async void ScanDrives_Click(object sender, RoutedEventArgs e)
        {
            DetectedPaths.Clear();
            AddAllButton.IsEnabled = false;
            StatusMessage.Text = "スキャン中...";
            string targetPath = ScanTargetTextBox.Text;

            var foundPaths = await Task.Run(() =>
            {
                var results = new List<string>();
                if (!string.IsNullOrEmpty(targetPath) && Directory.Exists(targetPath))
                    SearchAmongUs(targetPath, results);
                else
                {
                    var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
                    foreach (var drive in drives) SearchAmongUs(drive.RootDirectory.FullName, results);
                }
                return results;
            });

            foreach (var path in foundPaths)
            {
                if (!VanillaPaths.Any(v => v.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    DetectedPaths.Add(path);
            }
            StatusMessage.Text = $"{DetectedPaths.Count} 件見つかりました。";
            AddAllButton.IsEnabled = DetectedPaths.Count > 0;
        }

        private void SearchAmongUs(string root, List<string> results)
        {
            try
            {
                if (File.Exists(Path.Combine(root, "Among Us.exe"))) { results.Add(root); return; }
                string[] skip = { "$Recycle.Bin", "System Volume Information", "Windows", "ProgramData" };
                foreach (var dir in Directory.GetDirectories(root))
                {
                    if (skip.Any(s => dir.Contains(s))) continue;
                    SearchAmongUs(dir, results);
                }
            }
            catch { }
        }

        private void AddDetectedSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string path)
            {
                VanillaPaths.Add(new VanillaPathInfo { Name = Path.GetFileName(path), Path = path });
                DetectedPaths.Remove(path);
                AddAllButton.IsEnabled = DetectedPaths.Count > 0;
            }
        }

        private void AddAllDetected_Click(object sender, RoutedEventArgs e)
        {
            foreach (var path in DetectedPaths.ToList())
                VanillaPaths.Add(new VanillaPathInfo { Name = Path.GetFileName(path), Path = path });
            DetectedPaths.Clear();
            AddAllButton.IsEnabled = false;
        }

        private void RemovePath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is VanillaPathInfo info)
                VanillaPaths.Remove(info);
        }

        private async void ChangeModPath_Click(object sender, RoutedEventArgs e)
        {
            var folder = await SelectFolder();
            if (folder != null) ModDataPathTextBox.Text = folder.Path;
        }
    }
}
