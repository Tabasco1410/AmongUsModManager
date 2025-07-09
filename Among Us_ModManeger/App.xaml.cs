using CefSharp;
using CefSharp.Wpf;
using System.Runtime.ConstrainedExecution;
using System.Windows;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var settings = new CefSettings();
        Cef.Initialize(settings);   // 137 系は暗黙初期化でも動くが明示が安全
    }
}
