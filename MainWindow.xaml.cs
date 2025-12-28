using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AmongUsModManager.Pages;
using AmongUsModManager.Services;

namespace AmongUsModManager
{
    public sealed partial class MainWindow : Window
    {
        public Dictionary<string, string> LocalizedStrings { get; private set; } = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();
            LoadLocalizedStrings();

            var config = ConfigService.Load();
            if (config != null && !string.IsNullOrEmpty(config.GameInstallPath))
            {
                SetNavigationUI(true);
                ContentFrame.Navigate(typeof(HomePage));
            }
            else
            {
                SetNavigationUI(false);
                ContentFrame.Navigate(typeof(SetupPage));
            }
        }

        public void SetNavigationUI(bool isSetupComplete)
        {
            if (isSetupComplete)
            {
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                NavView.IsPaneOpen = true;
                HomeItem.Visibility = Visibility.Visible;
                ModInstallItem.Visibility = Visibility.Visible;
                LibraryItem.Visibility = Visibility.Visible;
                NavSeparator.Visibility = Visibility.Visible;
            }
            else
            {
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
                NavView.IsPaneOpen = false;
                HomeItem.Visibility = Visibility.Collapsed;
                ModInstallItem.Visibility = Visibility.Collapsed;
                LibraryItem.Visibility = Visibility.Collapsed;
                NavSeparator.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadLocalizedStrings()
        {
            string currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "AmongUsModManager.Resources.strings.csv";
            try
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return;
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        var content = reader.ReadToEnd();
                        var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines.Skip(1))
                        {
                            var parts = line.Split(',');
                            if (parts.Length >= 3)
                            {
                                string key = parts[0].Trim();
                                string value = (currentCulture == "ja") ? parts[2].Trim() : parts[1].Trim();
                                LocalizedStrings[key] = value;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            string tag = args.IsSettingsSelected ? "Settings" : (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();

          
            if (ContentFrame.Content is SettingsPage settingsPage && tag != "Settings")
            {
                if (settingsPage.HasUnsavedChanges())
                {
                    
                    settingsPage.ShowUnsavedWarning(tag);

                  
                    return;
                }
            }

           
            NavigateToPendingPage(tag);
        }

       
        public void NavigateToPage(string tag)
        {
            NavigateToPendingPage(tag);
        }

        public void NavigateToPendingPage(string tag)
        {
            if (tag == "Settings")
            {
                if (ContentFrame.Content is not SettingsPage)
                    ContentFrame.Navigate(typeof(SettingsPage));
            }
            else if (tag == "Home") ContentFrame.Navigate(typeof(HomePage));
            else if (tag == "ModInstall") ContentFrame.Navigate(typeof(ModInstallPage));
            else if (tag == "Library") ContentFrame.Navigate(typeof(LibraryPage));
            else if (tag == "Information") ContentFrame.Navigate(typeof(InformationPage));
        }
    }
}
