using Microsoft.UI.Xaml;

namespace RemoSystemProfiler;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow mainWindow = new();
        _window = mainWindow;
        mainWindow.Activate();
        mainWindow.InitializeAfterActivation();
    }

    internal void ClearMainWindow(Window window) => _window = ReferenceEquals(_window, window) ? null : _window;
}
