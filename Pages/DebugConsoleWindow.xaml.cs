using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.UI;
using AmongUsModManager.Services;

namespace AmongUsModManager.Pages
{
    public sealed partial class DebugConsoleWindow : Window
    {
        private readonly List<object> _allEntries = new();
        private readonly ObservableCollection<LogDisplayItem> _displayItems = new();

        public DebugConsoleWindow()
        {
            InitializeComponent();
            Title = "🛠 Debug Console";
            LogItemsControl.ItemsSource = _displayItems;
            LoadExistingLogs();
#if DEBUG
            LogService.LogWritten += OnLogWritten;
            this.Closed += (_, _) => LogService.LogWritten -= OnLogWritten;
#else
            this.Closed += (_, _) => { };
#endif
            this.Activated += OnFirstActivated;
        }

        private bool _setupDone = false;
        private void OnFirstActivated(object sender, WindowActivatedEventArgs e)
        {
            if (_setupDone) return;
            _setupDone = true;
            this.Activated -= OnFirstActivated;
            SetupWindow();
        }

        private void SetupWindow()
        {
            var appWindow = this.AppWindow;
            appWindow.Resize(new SizeInt32(900, 500));
            if (appWindow.Presenter is OverlappedPresenter p)
                p.IsAlwaysOnTop = true;
            var wa = DisplayArea.Primary.WorkArea;
            appWindow.Move(new PointInt32(wa.X + wa.Width - 910, wa.Y + wa.Height - 520));
        }

        private void LoadExistingLogs()
        {
            try
            {
                string logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AmongUsModManager", "LogOutput.log");
                if (!System.IO.File.Exists(logPath)) return;
#if DEBUG
                foreach (var line in System.IO.File.ReadAllLines(logPath))
                    _allEntries.Add(new LogService.LogEntry(ParseLevel(line), "", line, DateTime.Now));
                RebuildDisplay();
                UpdateStatus();
#endif
            }
            catch { }
        }

        private LogLevel ParseLevel(string line)
        {
            if (line.Contains("[Trace]") || line.Contains("[Trace ")) return LogLevel.Trace;
            if (line.Contains("[Debug]") || line.Contains("[Debug ")) return LogLevel.Debug;
            if (line.Contains("[Warn ]") || line.Contains("[Warn  ")) return LogLevel.Warn;
            if (line.Contains("[Error]") || line.Contains("[Error ")) return LogLevel.Error;
            return LogLevel.Info;
        }

#if DEBUG
        private void OnLogWritten(LogService.LogEntry entry)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _allEntries.Add(entry);
                if (IsVisible(entry.Level))
                {
                    _displayItems.Add(ToDisplayItem(entry));
                    ScrollToBottom();
                }
                UpdateStatus();
            });
        }
#endif

#if DEBUG
        private static LogDisplayItem ToDisplayItem(LogService.LogEntry entry) => new()
        {
            Text = entry.Text,
            Foreground = entry.Level == LogLevel.Error
                ? new SolidColorBrush(Color.FromArgb(255, 255, 107, 107))
                : new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
        };
#else
        private static LogDisplayItem ToDisplayItem(object _) => new()
        {
            Text = "",
            Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
        };
#endif

        private bool IsVisible(LogLevel level) => level switch
        {
            LogLevel.Trace => BtnTrace.IsChecked == true,
            LogLevel.Debug => BtnDebug.IsChecked == true,
            LogLevel.Info => BtnInfo.IsChecked == true,
            LogLevel.Warn => BtnWarn.IsChecked == true,
            LogLevel.Error => BtnError.IsChecked == true,
            _ => true
        };

        private void RebuildDisplay()
        {
            _displayItems.Clear();
#if DEBUG
            foreach (var e in _allEntries.OfType<LogService.LogEntry>().Where(e => IsVisible(e.Level)))
                _displayItems.Add(ToDisplayItem(e));
#endif
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            if (BtnAutoScroll.IsChecked != true) return;

            // WinUI3 では ItemsControl のレイアウト反映が非同期になるため
            // DispatcherQueue を2段階にして確実に最終行までスクロールさせる
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                LogScrollViewer.UpdateLayout();
                LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null, disableAnimation: true);
            });
        }

        private void UpdateStatus()
        {
            int total = _allEntries.Count;
            int shown = _displayItems.Count;
#if DEBUG
            int errors = _allEntries.OfType<LogService.LogEntry>().Count(e => e.Level == LogLevel.Error);
            int warns  = _allEntries.OfType<LogService.LogEntry>().Count(e => e.Level == LogLevel.Warn);
            CountText.Text  = $"  {shown} / {total} 件";
            StatusText.Text = errors > 0
                ? $"⛔ Error: {errors}  ⚠ Warn: {warns}  | Total: {total}"
                : warns > 0 ? $"⚠ Warn: {warns}  | Total: {total}" : $"✅ Total: {total}";
#else
            CountText.Text = $"  {shown} 件";
            StatusText.Text = $"Total: {shown}";
#endif
        }

        private void FilterChanged(object sender, RoutedEventArgs e) => RebuildDisplay();

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _allEntries.Clear();
            _displayItems.Clear();
            UpdateStatus();
        }
    }

    public class LogDisplayItem
    {
        public string Text { get; set; } = "";
        public SolidColorBrush Foreground { get; set; } = new(Color.FromArgb(255, 255, 255, 255));
    }
}
