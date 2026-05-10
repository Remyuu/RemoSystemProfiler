using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint action, uint param, out NativeRect rect, uint update);

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
        RectInt32 area = GetPrimaryWorkArea(display.WorkArea);
        int width = Math.Min(area.Width - 24, Math.Min(1880, Math.Max(1500, (int)Math.Round(area.Width * 0.72))));
        int height = Math.Min(area.Height - 32, Math.Min(1040, Math.Max(900, (int)Math.Round(area.Height * 0.78))));
        int x = area.X + Math.Max(12, Math.Min(120, (area.Width - width) / 2));
        int y = area.Y + Math.Max(12, Math.Min(120, (area.Height - height) / 2));
        appWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    private static RectInt32 GetPrimaryWorkArea(RectInt32 fallback)
    {
        const uint SpiGetWorkArea = 0x0030;
        if (!SystemParametersInfo(SpiGetWorkArea, 0, out NativeRect rect, 0))
        {
            return fallback;
        }

        int width = Math.Max(0, rect.Right - rect.Left);
        int height = Math.Max(0, rect.Bottom - rect.Top);
        return width == 0 || height == 0
            ? fallback
            : new RectInt32(rect.Left, rect.Top, width, height);
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
            ShowUnavailable(result.Message);
            DismissStartupOverlay("Sensor backend unavailable");
            return;
        }

        ShowSnapshot(result.Snapshot, result);
        DismissStartupOverlay(result.RequiresAdministrator
            ? "Connected with limited sensor access"
            : "Connected to hardware backend");
    }

    private void ShowSnapshot(SystemSnapshot snapshot, HardwareMonitorReadResult result)
    {
        bool limited = result.RequiresAdministrator;
        StatusDot.Fill = new SolidColorBrush(limited ? Colors.OrangeRed : Colors.LimeGreen);
        StatusText.Text = limited
            ? $"Limited via {snapshot.Source}\nRun as administrator for full hardware sensors"
            : $"Connected via {snapshot.Source}";
        StatusText.TextWrapping = limited ? TextWrapping.WrapWholeWords : TextWrapping.NoWrap;
        ToolTipService.SetToolTip(StatusText, limited ? result.Message : null);
        UpdatedText.Text = snapshot.SampledAtText;
        HardwareSummaryText.Text = BuildHardwareSummary(snapshot);

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
            SyncSensorGroupCollection(_cpuSensorGroups, BuildCpuSensorGroups(null));
            SyncDeviceCollection(_cpuCores, Array.Empty<CoreReading>(), core => core.Index, core => new CoreItemViewModel(core));
            return;
        }

        CpuNameText.Text = cpu.Name;
        CpuPackagePowerText.Text = cpu.PackagePowerText;
        CpuPeakTempText.Text = cpu.MaxTemperatureText;
        CpuLoadSummaryText.Text = cpu.AverageLoadText;
        CoreCountText.Text = cpu.CoreCountText;
        ClockText.Text = cpu.ClockText;
        SyncSensorGroupCollection(_cpuSensorGroups, BuildCpuSensorGroups(cpu));
        SyncDeviceCollection(_cpuCores, cpu.Cores, core => core.Index, core => new CoreItemViewModel(core));
    }

    private void ShowMemory(MemoryDeviceReading? memory)
    {
        if (memory is null)
        {
            MemoryUsageText.Text = MemoryCapacityText.Text = "--";
            MemoryTempText.Text = string.Empty;
            SyncMetricCollection(_memoryMetrics, []);
            return;
        }

        MemoryUsageText.Text = memory.UsageText;
        MemoryTempText.Text = memory.TemperatureText;
        MemoryCapacityText.Text = memory.CapacityText;
        SyncMetricCollection(_memoryMetrics, memory.Metrics);
    }

    private void ShowUnavailable(string message)
    {
        StatusDot.Fill = new SolidColorBrush(Colors.OrangeRed);
        StatusText.Text = message;
        StatusText.TextWrapping = TextWrapping.Wrap;
        ToolTipService.SetToolTip(StatusText, message);
        HardwareSummaryText.Text = "Hardware sensors unavailable";
        ShowCpu(null);
        ShowMemory(null);
        SyncDeviceCollection(_gpus, Array.Empty<GpuDeviceReading>(), gpu => gpu.Name, gpu => new GpuDeviceViewModel(gpu));
        SyncDeviceCollection(_storageDevices, Array.Empty<StorageDeviceReading>(), storage => storage.Name, storage => new StorageDeviceViewModel(storage));
        SyncOverviewCollection([]);
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

        SyncOverviewCollection(readings);
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
        ChartHistorySettings.DisplaySeconds = ChartRangePicker.SelectedIndex switch
        {
            1 => 30,
            2 => 60,
            3 => 300,
            _ => 10
        };
    }

    private void UpdateIntervalPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _pollIntervalMilliseconds = UpdateIntervalPicker.SelectedIndex switch
        {
            0 => 500,
            2 => 2000,
            3 => 5000,
            _ => 1000
        };
        ChartHistorySettings.SampleIntervalSeconds = _pollIntervalMilliseconds / 1000d;
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

    private static string BuildHardwareSummary(SystemSnapshot snapshot)
    {
        return $"{snapshot.Gpus.Count} GPU | {snapshot.StorageDevices.Count} storage";
    }

    private static void SyncMetricCollection(ObservableCollection<MetricItemViewModel> collection, IEnumerable<MetricReading> readings)
    {
        SyncDeviceCollection(collection, readings, MetricItemViewModel.MetricKey, reading => new MetricItemViewModel(reading));
    }

    private static void SyncSensorGroupCollection(ObservableCollection<SensorGroupViewModel> collection, IEnumerable<SensorGroupReading> readings)
    {
        DashboardCollection.Sync(
            collection,
            readings,
            item => item.Key,
            reading => reading.Key,
            reading => new SensorGroupViewModel(reading),
            (item, reading) => item.Update(reading));
    }

    private static IEnumerable<SensorGroupReading> BuildCpuSensorGroups(CpuDeviceReading? cpu)
    {
        yield return new SensorGroupReading("temperature", "Temperature", cpu?.TemperatureSensors ?? Array.Empty<MetricReading>(), false);
        yield return new SensorGroupReading("power", "Power", cpu?.PowerSensors ?? Array.Empty<MetricReading>(), false);
        yield return new SensorGroupReading("clock", "Clock", cpu?.ClockSensors ?? Array.Empty<MetricReading>(), false);
        yield return new SensorGroupReading("voltage", "Voltage", cpu?.VoltageSensors ?? Array.Empty<MetricReading>(), false);
    }

    private void SyncOverviewCollection(IEnumerable<OverviewReading> readings)
    {
        DashboardCollection.Sync(_overviewItems, readings, item => item.Key, reading => reading.Key, reading => new OverviewItemViewModel(reading), (item, reading) => item.Update(reading));
    }

    private static void SyncDeviceCollection<TView, TData, TKey>(
        ObservableCollection<TView> collection,
        IEnumerable<TData> readings,
        Func<TData, TKey> key,
        Func<TData, TView> create)
        where TKey : notnull
    {
        DashboardCollection.Sync(collection, readings, ViewKey, key, create, Update);

        static TKey ViewKey(TView view) => view switch
        {
            CoreItemViewModel core => (TKey)(object)core.Key,
            GpuDeviceViewModel gpu => (TKey)(object)gpu.Key,
            StorageDeviceViewModel storage => (TKey)(object)storage.Key,
            MetricItemViewModel metric => (TKey)(object)metric.Key,
            _ => throw new NotSupportedException(typeof(TView).Name)
        };

        static void Update(TView view, TData data)
        {
            switch (view, data)
            {
                case (CoreItemViewModel core, CoreReading reading):
                    core.Update(reading);
                    break;
                case (GpuDeviceViewModel gpu, GpuDeviceReading reading):
                    gpu.Update(reading);
                    break;
                case (StorageDeviceViewModel storage, StorageDeviceReading reading):
                    storage.Update(reading);
                    break;
                case (MetricItemViewModel metric, MetricReading reading):
                    metric.Update(reading);
                    break;
            }
        }
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

    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
