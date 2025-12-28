using System;
using Microsoft.UI.Xaml;

namespace AmongUsModManager
{
    public partial class App : Application
    {
        public Window m_window { get; private set; }
        public static Window? MainWindowInstance { get; private set; }

   
        public static string AppVersion { get; } = "1.4.1";

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            MainWindowInstance = m_window;
            m_window.Activate();
        }

      
    }
}
