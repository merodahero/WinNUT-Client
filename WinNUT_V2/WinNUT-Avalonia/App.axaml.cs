using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using WinNUT_Avalonia.ViewModels;
using WinNUT_Avalonia.Views;

namespace WinNUT_Avalonia;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
            };
            desktop.MainWindow = _mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void TrayShow_OnClick(object? sender, System.EventArgs e) => _mainWindow?.RestoreFromTray();

    private void TrayExit_OnClick(object? sender, System.EventArgs e) => _mainWindow?.ExitApplication();
}
