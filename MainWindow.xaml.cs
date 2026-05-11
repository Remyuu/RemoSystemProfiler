using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using WinRT.Interop;

namespace RemoSystemProfiler;

public sealed partial class MainWindow : Window
{
    private readonly HardwareMonitorReader _reader = new();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly ObservableCollection<OverviewItemViewModel> _overviewItems = [];
    private readonly ObservableCollection<SensorGroupViewModel> _cpuSensorGroups = [];
    private readonly ObservableCollection<CoreItemViewModel> _cpuCores = [];
    private readonly ObservableCollection<MetricItemViewModel> _memoryMetrics = [];
    private readonly ObservableCollection<GpuDeviceViewModel> _gpus = [];
    private readonly ObservableCollection<StorageDeviceViewModel> _storageDevices = [];
    private AppWindow? _appWindow;
    private Task? _pollingTask;
    private int _pollIntervalMilliseconds = 1000;
    private bool _isClosed;
    private bool _startupOverlayDismissed;

    public MainWindow()
    {
        InitializeComponent();
        BindCollections();
        ConfigureStartupOverlayBrush();

        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
        Root.ActualThemeChanged += Root_ActualThemeChanged;
        Closed += OnClosed;

        ChartRangePicker.SelectedIndex = 0;
        UpdateIntervalPicker.SelectedIndex = 1;
        ThemePicker.SelectedIndex = 0;
        UpdatedText.Text = DateTime.Now.ToString("HH:mm:ss");
    }

    private void ConfigureStartupOverlayBrush()
    {
        StartupOverlay.Background = new AcrylicBrush
        {
            TintColor = ColorHelper.FromArgb(255, 17, 18, 20),
            TintOpacity = 0.78,
            FallbackColor = ColorHelper.FromArgb(240, 17, 18, 20)
        };
    }

    public void InitializeAfterActivation()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ConfigureWindow();
            ApplyTitleBarTheme();
            StartPolling();
        });
    }

    private void BindCollections()
    {
        HardwareOverviewList.ItemsSource = _overviewItems;
        CpuSensorGroupList.ItemsSource = _cpuSensorGroups;
        CpuCoreList.ItemsSource = _cpuCores;
        MemoryMetricList.ItemsSource = _memoryMetrics;
        GpuList.ItemsSource = _gpus;
        StorageList.ItemsSource = _storageDevices;
    }

    private void ConfigureWindow()
    {
        IntPtr hwnd = WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        ExtendsContentIntoTitleBar = false;
        ResizeToWorkArea(_appWindow, windowId);
    }

    private static void ResizeToWorkArea(AppWindow appWindow, WindowId windowId)
    {
        DisplayArea display = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        RectInt32 area = display.WorkArea;
        int width = Math.Min(area.Width - 24, Math.Min(1880, Math.Max(1500, (int)Math.Round(area.Width * 0.72))));
        int height = Math.Min(area.Height - 32, Math.Min(1040, Math.Max(900, (int)Math.Round(area.Height * 0.78))));
        int x = area.X + Math.Max(12, Math.Min(120, (area.Width - width) / 2));
        int y = area.Y + Math.Max(12, Math.Min(120, (area.Height - height) / 2));
        appWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    private void StartPolling()
    {
        if (_pollingTask is not null)
        {
            return;
        }

        _pollingTask = Task.Run(() => PollLoopAsync(_shutdown.Token));
    }

    private async Task PollLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await ReadAndDispatchAsync(token);
                await Task.Delay(TimeSpan.FromMilliseconds(_pollIntervalMilliseconds), token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal close path.
        }
    }

    private async Task ReadAndDispatchAsync(CancellationToken token)
    {
        HardwareMonitorReadResult result = await Task.Run(_reader.Read, token).ConfigureAwait(false);
        if (token.IsCancellationRequested)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            if (!_isClosed)
            {
                ShowResult(result);
            }
        });
    }

    private void ShowResult(HardwareMonitorReadResult result)
    {
        UpdatedText.Text = DateTime.Now.ToString("HH:mm:ss");
        if (!result.IsAvailable || result.Snapshot is null)
        {
            ShowUnavailable(result.Message, result.DriverStatus);
            DismissStartupOverlay(result.DriverStatus.NeedsInstallation
                ? "PawnIO is required for full sensor access"
                : "Sensor backend unavailable");
            return;
        }

        ShowSnapshot(result.Snapshot, result);
        DismissStartupOverlay(result.DriverStatus.NeedsInstallation
            ? "PawnIO is required for full sensor access"
            : result.RequiresAdministrator
            ? "Connected with limited sensor access"
            : "Connected to hardware backend");
    }

    private void ShowSnapshot(SystemSnapshot snapshot, HardwareMonitorReadResult result)
    {
        bool driverLimited = !result.DriverStatus.IsReady;
        bool limited = result.RequiresAdministrator || driverLimited;
        StatusDot.Fill = new SolidColorBrush(limited ? Colors.OrangeRed : Colors.LimeGreen);
        StatusText.Text = driverLimited
            ? result.DriverStatus.Message
            : result.RequiresAdministrator
            ? $"Limited access\n{snapshot.Source}\nRun as administrator"
            : $"Connected\n{snapshot.Source}\n{result.DriverStatus.SummaryText}";
        StatusText.TextWrapping = TextWrapping.Wrap;
        ToolTipService.SetToolTip(StatusText, limited ? BuildLimitedStatusTooltip(result) : null);
        PawnIoDownloadLink.Visibility = result.DriverStatus.NeedsInstallation ? Visibility.Visible : Visibility.Collapsed;
        UpdatedText.Text = snapshot.SampledAtText;
        HardwareSummaryText.Text = $"{snapshot.Gpus.Count} GPU | {snapshot.StorageDevices.Count} storage";

        ShowCpu(snapshot.Cpu);
        ShowMemory(snapshot.Memory);
        SyncDeviceCollection(_gpus, snapshot.Gpus, gpu => gpu.Name, gpu => new GpuDeviceViewModel(gpu));
        SyncDeviceCollection(_storageDevices, snapshot.StorageDevices, storage => storage.Name, storage => new StorageDeviceViewModel(storage));
        ShowOverview(snapshot);
    }

    private void ShowCpu(CpuDeviceReading? cpu)
    {
        if (cpu is null)
        {
            CpuNameText.Text = "CPU sensors unavailable";
            CpuPackagePowerText.Text = CpuPeakTempText.Text = CpuLoadSummaryText.Text = CoreCountText.Text = ClockText.Text = "--";
            SyncDeviceCollection(_cpuSensorGroups, BuildCpuSensorGroups(null), reading => reading.Key, reading => new SensorGroupViewModel(reading));
            SyncDeviceCollection(_cpuCores, Array.Empty<CoreReading>(), core => core.Index, core => new CoreItemViewModel(core));
            return;
        }

        CpuNameText.Text = cpu.Name;
        CpuPackagePowerText.Text = cpu.PackagePowerText;
        CpuPeakTempText.Text = cpu.MaxTemperatureText;
        CpuLoadSummaryText.Text = cpu.AverageLoadText;
        CoreCountText.Text = cpu.CoreCountText;
        ClockText.Text = cpu.ClockText;
        SyncDeviceCollection(_cpuSensorGroups, BuildCpuSensorGroups(cpu), reading => reading.Key, reading => new SensorGroupViewModel(reading));
        SyncDeviceCollection(_cpuCores, cpu.Cores, core => core.Index, core => new CoreItemViewModel(core));
    }

    private void ShowMemory(MemoryDeviceReading? memory)
    {
        if (memory is null)
        {
            MemoryUsageText.Text = MemoryCapacityText.Text = "--";
            MemoryTempText.Text = string.Empty;
            SyncDeviceCollection(_memoryMetrics, Array.Empty<MetricReading>(), MetricItemViewModel.MetricKey, reading => new MetricItemViewModel(reading));
            return;
        }

        MemoryUsageText.Text = memory.UsageText;
        MemoryTempText.Text = memory.TemperatureText;
        MemoryCapacityText.Text = memory.CapacityText;
        SyncDeviceCollection(_memoryMetrics, memory.Metrics, MetricItemViewModel.MetricKey, reading => new MetricItemViewModel(reading));
    }

    private void ShowUnavailable(string message, SensorDriverStatus driverStatus)
    {
        StatusDot.Fill = new SolidColorBrush(Colors.OrangeRed);
        StatusText.Text = driverStatus.NeedsInstallation ? driverStatus.Message : message;
        StatusText.TextWrapping = TextWrapping.Wrap;
        ToolTipService.SetToolTip(StatusText, driverStatus.NeedsInstallation ? $"{driverStatus.Message}\n{message}" : message);
        PawnIoDownloadLink.Visibility = driverStatus.NeedsInstallation ? Visibility.Visible : Visibility.Collapsed;
        HardwareSummaryText.Text = "Hardware sensors unavailable";
        ShowCpu(null);
        ShowMemory(null);
        SyncDeviceCollection(_gpus, Array.Empty<GpuDeviceReading>(), gpu => gpu.Name, gpu => new GpuDeviceViewModel(gpu));
        SyncDeviceCollection(_storageDevices, Array.Empty<StorageDeviceReading>(), storage => storage.Name, storage => new StorageDeviceViewModel(storage));
        SyncDeviceCollection(_overviewItems, Array.Empty<OverviewReading>(), reading => reading.Key, reading => new OverviewItemViewModel(reading));
    }

    private static string BuildLimitedStatusTooltip(HardwareMonitorReadResult result)
    {
        if (!result.DriverStatus.IsReady)
        {
            return result.DriverStatus.NeedsInstallation
                ? "Install PawnIO, then restart Remo System Profiler as administrator."
                : result.DriverStatus.Message;
        }

        return result.Message;
    }

    private void DismissStartupOverlay(string message)
    {
        if (_startupOverlayDismissed)
        {
            return;
        }

        _startupOverlayDismissed = true;
        StartupStatusText.Text = message;

        DoubleAnimation fade = new()
        {
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(240)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fade, StartupOverlay);
        Storyboard.SetTargetProperty(fade, "Opacity");

        Storyboard storyboard = new();
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (_isClosed)
            {
                return;
            }

            StartupOverlay.Visibility = Visibility.Collapsed;
            StartupOverlay.IsHitTestVisible = false;
        };
        storyboard.Begin();
    }

    private void ShowOverview(SystemSnapshot snapshot)
    {
        List<OverviewReading> readings = [];
        if (snapshot.Cpu is { } cpu)
        {
            readings.Add(new("cpu", "CPU", cpu.AverageLoadText, cpu.ClockText, cpu.PackagePowerText, cpu.AverageLoadPercent, ResourceBrush("BlueBrush")));
        }

        if (snapshot.Memory is { } memory)
        {
            readings.Add(new("memory", "Memory", memory.UsageText, memory.CapacityText, memory.TemperatureText, memory.UsageGauge, ResourceBrush("GreenBrush")));
        }

        for (int i = 0; i < snapshot.Gpus.Count; i++)
        {
            GpuDeviceReading gpu = snapshot.Gpus[i];
            readings.Add(new($"gpu:{gpu.Name}", $"GPU {i}", gpu.LoadText, gpu.Name, $"{gpu.PowerText} | {gpu.TemperatureText}", gpu.LoadGauge, ResourceBrush("PurpleBrush")));
        }

        for (int i = 0; i < snapshot.StorageDevices.Count; i++)
        {
            StorageDeviceReading storage = snapshot.StorageDevices[i];
            readings.Add(new($"storage:{storage.Name}", $"Disk {i}", storage.UsageText, storage.Name, $"{storage.ReadWriteText} | {storage.TemperatureText}", storage.ActivityGauge, ResourceBrush("AmberBrush")));
        }

        SyncDeviceCollection(_overviewItems, readings, reading => reading.Key, reading => new OverviewItemViewModel(reading));
    }

    private void ThemePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Root.RequestedTheme = ThemePicker.SelectedIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
        ApplyTitleBarTheme();
    }

    private void ChartRangePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ChartHistorySettings.DisplaySeconds = (int)SelectedNumberTag(ChartRangePicker, 10);
    }

    private void UpdateIntervalPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _pollIntervalMilliseconds = (int)Math.Round(SelectedNumberTag(UpdateIntervalPicker, 1) * 1000d);
        ChartHistorySettings.SampleIntervalSeconds = _pollIntervalMilliseconds / 1000d;
    }

    private static double SelectedNumberTag(ComboBox comboBox, double fallback)
    {
        return comboBox.SelectedItem is ComboBoxItem { Tag: { } tag }
            && double.TryParse(tag.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            ? value
            : fallback;
    }

    private void SidebarResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double targetWidth = SidebarColumn.ActualWidth + e.HorizontalChange;
        double minWidth = SidebarColumn.MinWidth;
        double maxWidth = SidebarColumn.MaxWidth;
        SidebarColumn.Width = new GridLength(Math.Clamp(targetWidth, minWidth, maxWidth));
    }

    private void Root_ActualThemeChanged(FrameworkElement sender, object args) => ApplyTitleBarTheme();

    private void ApplyTitleBarTheme()
    {
        if (_appWindow is null || !AppWindowTitleBar.IsCustomizationSupported())
        {
            return;
        }

        bool light = ResolveActiveTheme() == ElementTheme.Light;
        Windows.UI.Color background = light ? ColorHelper.FromArgb(255, 245, 247, 250) : ColorHelper.FromArgb(255, 17, 18, 20);
        Windows.UI.Color foreground = light ? Colors.Black : Colors.White;
        Windows.UI.Color muted = light ? ColorHelper.FromArgb(255, 102, 112, 133) : ColorHelper.FromArgb(255, 126, 135, 150);
        Windows.UI.Color hover = light ? ColorHelper.FromArgb(255, 229, 234, 242) : ColorHelper.FromArgb(255, 43, 45, 51);

        AppWindowTitleBar titleBar = _appWindow.TitleBar;
        titleBar.BackgroundColor = background;
        titleBar.InactiveBackgroundColor = background;
        titleBar.ButtonBackgroundColor = background;
        titleBar.ButtonInactiveBackgroundColor = background;
        titleBar.ButtonForegroundColor = foreground;
        titleBar.ButtonInactiveForegroundColor = muted;
        titleBar.ButtonHoverBackgroundColor = hover;
        titleBar.ButtonPressedBackgroundColor = light ? ColorHelper.FromArgb(255, 209, 216, 226) : ColorHelper.FromArgb(255, 58, 61, 68);
    }

    private ElementTheme ResolveActiveTheme()
    {
        return ThemePicker.SelectedIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => Root.ActualTheme == ElementTheme.Light ? ElementTheme.Light : ElementTheme.Dark
        };
    }

    private SolidColorBrush ResourceBrush(string key) => (SolidColorBrush)Application.Current.Resources[key];

    private static SensorGroupReading[] BuildCpuSensorGroups(CpuDeviceReading? cpu) =>
    [
        new("temperature", "Temperature", cpu?.TemperatureSensors ?? Array.Empty<MetricReading>(), false),
        new("power", "Power", cpu?.PowerSensors ?? Array.Empty<MetricReading>(), false),
        new("clock", "Clock", cpu?.ClockSensors ?? Array.Empty<MetricReading>(), false),
        new("voltage", "Voltage", cpu?.VoltageSensors ?? Array.Empty<MetricReading>(), false)
    ];

    private static void SyncDeviceCollection<TView, TData, TKey>(
        ObservableCollection<TView> collection,
        IEnumerable<TData> readings,
        Func<TData, TKey> key,
        Func<TData, TView> create)
        where TView : IDashboardItem<TData, TKey>
        where TKey : notnull
    {
        DashboardCollection.SyncItems(collection, readings, key, create);
    }

    private void OnClosed(object sender, WindowEventArgs e) => ShutdownNow();

    private void ShutdownNow()
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        _shutdown.Cancel();
        Closed -= OnClosed;
        Root.ActualThemeChanged -= Root_ActualThemeChanged;
        _reader.Dispose();
        _shutdown.Dispose();
        ((App)Application.Current).ClearMainWindow(this);
    }

}
