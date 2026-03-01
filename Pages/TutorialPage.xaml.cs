using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AmongUsModManager.Services;

namespace AmongUsModManager.Pages
{
    
    public class TutorialSlide
    {
        public string Icon        { get; set; } = "";
        public string Title       { get; set; } = "";
        public string Description { get; set; } = "";
        public string Hint        { get; set; } = "";
    }

    
    
    
    
    public sealed partial class TutorialPage : Page
    {
        public TutorialPage()
        {
            this.InitializeComponent();
            LogService.Info("TutorialPage", "旧チュートリアルページ → ホームへリダイレクト");
            this.Loaded += TutorialPage_Loaded;
        }

        private void TutorialPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.MainWindowInstance is MainWindow mw)
            {
                mw.SetNavigationUI(true);
                mw.NavigateToPage("Home");
            }
        }
    }
}
