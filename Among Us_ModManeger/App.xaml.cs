using System.Windows;
using System.Threading.Tasks;

namespace Among_Us_ModManeger
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // ここでメインウィンドウを表示する例
            // あなたのアプリケーションがMainMenuPageから始まる場合、
            // StartupUri="MainWindow.xaml" の代わりに、
            // app.Run()の前に以下のようなナビゲーションを追加することも検討してください。
            // var mainWindow = new MainWindow();
            // var mainMenuPage = new Pages.MainMenuPage();
            // mainWindow.Content = mainMenuPage;
            // mainWindow.Show();

            // 現在のStartupUri="MainWindow.xaml"の想定では、
            // MainWindow.xamlとそのコードビハインドが必要です。
            // ここでは基本的なMainWindowのインスタンス化と表示を維持します。
            var mainWindow = new MainWindow(); // MainWindow クラスがプロジェクトに存在する必要があります
            mainWindow.Show();
        }
    }
}
