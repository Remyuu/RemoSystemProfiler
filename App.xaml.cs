using Microsoft.UI.Xaml;

namespace RemoSystemProfiler;

public partial class App : Application
{
    private Window? _window;
    private bool _isShuttingDown;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }

    internal void ShutdownFromMainWindow()
    {
        if (_isShuttingDown)
        {
            return;
        }

        _isShuttingDown = true;
        _window = null;
        Exit();

        // WinUI/Windows App SDK shutdown can leave native or sensor-library threads
        // alive under the Visual Studio debugger. Main window close is app exit here.
        Environment.Exit(0);
    }
}
