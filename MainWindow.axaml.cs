using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using RemoSystemProfiler.Backends.Windows;
using RemoSystemProfiler.Core;

namespace RemoSystemProfiler;

public sealed partial class MainWindow : Window
{
    private const double SidebarCompactWidth = 56;
    private const double SidebarExpandedMinWidth = 108;
    private const double SidebarExpandedWidth = 260;
    private const double SidebarCompactThreshold = SidebarExpandedMinWidth;
    private const double InitialWindowWidth = 1100;
    private const double InitialWindowHeight = 720;
    private const int StartupOverlayFadeMilliseconds = 320;
    private const int StartupOverlayCompletionHoldMilliseconds = 180;

    private static readonly int[] ChartRangesSeconds = [10, 30, 60, 300];
    private static readonly double[] UpdateIntervalsSeconds = [0.5, 1, 2, 5];

    private readonly IHardwareMonitorBackend _backend = new WindowsHardwareMonitorBackend();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly MainWindowViewModel _viewModel = new();
    private Task? _pollingTask;
    private int _pollIntervalMilliseconds = 1000;
    private double _lastExpandedSidebarWidth = SidebarExpandedWidth;
    private bool _isApplyingSidebarWidth;
    private bool _isAnimatingSidebarWidth;
    private CancellationTokenSource? _sidebarWidthAnimation;
    private Size _lastNormalWindowSize = new(InitialWindowWidth, InitialWindowHeight);
    private bool _isClosed;
    private bool _startupOverlayDismissed;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Opened += OnOpened;
        Closed += OnClosed;
        SizeChanged += OnWindowSizeChanged;
        _viewModel.UpdatedText = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        UpdateSidebarMode(SidebarRoot.Bounds.Width);
        StartPolling();
    }

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (WindowState == WindowState.Normal && e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            _lastNormalWindowSize = e.NewSize;
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPoint point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (WindowState == WindowState.Maximized)
        {
            RestoreWindowForDrag(point.Position);
        }

        BeginMoveDrag(e);
        e.Handled = true;
    }

    private void TitleBar_DoubleTapped(object? sender, TappedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        e.Handled = true;
    }

    private void RestoreWindowForDrag(Point pointerPosition)
    {
        double restoredWidth = Math.Max(MinWidth, _lastNormalWindowSize.Width);
        double restoredHeight = Math.Max(MinHeight, _lastNormalWindowSize.Height);
        double sourceWidth = Math.Max(Bounds.Width, restoredWidth);
        double horizontalRatio = Math.Clamp(pointerPosition.X / sourceWidth, 0.12, 0.88);
        double titleBarOffsetY = Math.Clamp(pointerPosition.Y, 8, 28);
        PixelPoint screenPoint = this.PointToScreen(pointerPosition);

        WindowState = WindowState.Normal;
        Width = restoredWidth;
        Height = restoredHeight;
        Position = new PixelPoint(
            screenPoint.X - (int)Math.Round(restoredWidth * horizontalRatio),
            screenPoint.Y - (int)Math.Round(titleBarOffsetY));
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
                await ReadAndDispatchAsync(token).ConfigureAwait(false);
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
        HardwareMonitorReadResult result = await Task.Run(_backend.Read, token).ConfigureAwait(false);
        if (token.IsCancellationRequested)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (!_isClosed)
            {
                ShowResult(result);
            }
        });
    }

    private void ShowResult(HardwareMonitorReadResult result)
    {
        _viewModel.UpdatedText = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
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
        _viewModel.StatusBrush = limited ? DashboardBrushes.OrangeRed : DashboardBrushes.LimeGreen;
        _viewModel.StatusText = driverLimited
            ? result.DriverStatus.Message
            : result.RequiresAdministrator
            ? $"Limited access\n{snapshot.Source}\nRun as administrator"
            : $"Connected\n{snapshot.Source}\n{result.DriverStatus.SummaryText}";
        _viewModel.StatusToolTip = limited ? BuildLimitedStatusTooltip(result) : null;
        _viewModel.IsPawnIoDownloadVisible = result.DriverStatus.NeedsInstallation;
        _viewModel.UpdatedText = snapshot.SampledAtText;
        _viewModel.HardwareSummaryText = $"{snapshot.Gpus.Count} GPU | {snapshot.StorageDevices.Count} storage";
        _viewModel.HeaderHardwareText = BuildHeaderHardwareText(snapshot);

        ShowCpu(snapshot.Cpu);
        ShowMemory(snapshot.Memory);
        SyncDeviceCollection(_viewModel.Gpus, snapshot.Gpus, gpu => gpu.Name, gpu => new GpuDeviceViewModel(gpu));
        SyncDeviceCollection(_viewModel.StorageDevices, snapshot.StorageDevices, storage => storage.Name, storage => new StorageDeviceViewModel(storage));
        ShowOverview(snapshot);
    }

    private void ShowCpu(CpuDeviceReading? cpu)
    {
        if (cpu is null)
        {
            _viewModel.CpuNameText = "CPU sensors unavailable";
            _viewModel.CpuPackagePowerText = "--";
            _viewModel.CpuPeakTempText = "--";
            _viewModel.CpuLoadSummaryText = "--";
            _viewModel.CoreCountText = "--";
            _viewModel.ClockText = "--";
            SyncDeviceCollection(_viewModel.CpuSensorGroups, BuildCpuSensorGroups(null), reading => reading.Key, reading => new SensorGroupViewModel(reading));
            SyncDeviceCollection(_viewModel.CpuCores, Array.Empty<CoreReading>(), core => core.Index, core => new CoreItemViewModel(core));
            return;
        }

        _viewModel.CpuNameText = cpu.Name;
        _viewModel.CpuPackagePowerText = cpu.PackagePowerText;
        _viewModel.CpuPeakTempText = cpu.MaxTemperatureText;
        _viewModel.CpuLoadSummaryText = cpu.AverageLoadText;
        _viewModel.CoreCountText = cpu.CoreCountText;
        _viewModel.ClockText = cpu.ClockText;
        SyncDeviceCollection(_viewModel.CpuSensorGroups, BuildCpuSensorGroups(cpu), reading => reading.Key, reading => new SensorGroupViewModel(reading));
        SyncDeviceCollection(_viewModel.CpuCores, cpu.Cores, core => core.Index, core => new CoreItemViewModel(core));
    }

    private void ShowMemory(MemoryDeviceReading? memory)
    {
        if (memory is null)
        {
            _viewModel.MemoryUsageText = "--";
            _viewModel.MemoryCapacityText = "--";
            _viewModel.MemoryTempText = string.Empty;
            SyncDeviceCollection(_viewModel.MemoryMetrics, Array.Empty<MetricReading>(), MetricItemViewModel.MetricKey, reading => new MetricItemViewModel(reading));
            return;
        }

        _viewModel.MemoryUsageText = memory.UsageText;
        _viewModel.MemoryTempText = memory.TemperatureText;
        _viewModel.MemoryCapacityText = memory.CapacityText;
        SyncDeviceCollection(_viewModel.MemoryMetrics, memory.Metrics, MetricItemViewModel.MetricKey, reading => new MetricItemViewModel(reading));
    }

    private void ShowUnavailable(string message, SensorDriverStatus driverStatus)
    {
        _viewModel.StatusBrush = DashboardBrushes.OrangeRed;
        _viewModel.StatusText = driverStatus.NeedsInstallation ? driverStatus.Message : message;
        _viewModel.StatusToolTip = driverStatus.NeedsInstallation ? $"{driverStatus.Message}\n{message}" : message;
        _viewModel.IsPawnIoDownloadVisible = driverStatus.NeedsInstallation;
        _viewModel.HardwareSummaryText = "Hardware sensors unavailable";
        _viewModel.HeaderHardwareText = "Hardware sensors unavailable";
        ShowCpu(null);
        ShowMemory(null);
        SyncDeviceCollection(_viewModel.Gpus, Array.Empty<GpuDeviceReading>(), gpu => gpu.Name, gpu => new GpuDeviceViewModel(gpu));
        SyncDeviceCollection(_viewModel.StorageDevices, Array.Empty<StorageDeviceReading>(), storage => storage.Name, storage => new StorageDeviceViewModel(storage));
        SyncDeviceCollection(_viewModel.OverviewItems, Array.Empty<OverviewReading>(), reading => reading.Key, reading => new OverviewItemViewModel(reading));
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

    private static string BuildHeaderHardwareText(SystemSnapshot snapshot)
    {
        string cpuName = string.IsNullOrWhiteSpace(snapshot.Cpu?.Name) ? "CPU unavailable" : snapshot.Cpu.Name;
        string memoryText = BuildHeaderMemoryText(snapshot.Memory);
        string gpuText = BuildHeaderGpuText(snapshot.Gpus);
        return $"{cpuName}        {memoryText}        {gpuText}";
    }

    private static string BuildHeaderMemoryText(MemoryDeviceReading? memory)
    {
        if (memory is null)
        {
            return "Memory unavailable";
        }

        MetricReading? total = memory.DataSensors.FirstOrDefault(sensor =>
            sensor.Name.Equals("Total", StringComparison.OrdinalIgnoreCase));
        return total?.ValueText ?? memory.CapacityText;
    }

    private static string BuildHeaderGpuText(IReadOnlyList<GpuDeviceReading> gpus)
    {
        if (gpus.Count == 0)
        {
            return "GPU unavailable";
        }

        return string.Join(" + ", gpus.Select(gpu => SimplifyGpuName(gpu.Name)).Where(name => !string.IsNullOrWhiteSpace(name)));
    }

    private static string SimplifyGpuName(string name)
    {
        return name
            .Replace("NVIDIA GeForce ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("NVIDIA ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("AMD Radeon(TM)", "Radeon", StringComparison.OrdinalIgnoreCase)
            .Replace("AMD Radeon", "Radeon", StringComparison.OrdinalIgnoreCase)
            .Replace(" Laptop GPU", " Laptop", StringComparison.OrdinalIgnoreCase)
            .Replace(" Graphics", " Graphics", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private void DismissStartupOverlay(string message)
    {
        if (_startupOverlayDismissed)
        {
            return;
        }

        _startupOverlayDismissed = true;
        _viewModel.StartupStatusText = message;
        _ = DismissStartupOverlayAsync();
    }

    private async Task DismissStartupOverlayAsync()
    {
        await Task.Delay(StartupOverlayCompletionHoldMilliseconds).ConfigureAwait(true);

        const int steps = 16;
        for (int i = 1; i <= steps; i++)
        {
            double t = i / (double)steps;
            double eased = 1d - Math.Pow(1d - t, 3);
            _viewModel.StartupOverlayOpacity = Math.Max(0, 1d - eased);
            await Task.Delay(StartupOverlayFadeMilliseconds / steps).ConfigureAwait(true);
        }

        _viewModel.StartupOverlayOpacity = 0;
        _viewModel.IsStartupOverlayVisible = false;
    }

    private void ShowOverview(SystemSnapshot snapshot)
    {
        List<OverviewReading> readings = [];
        if (snapshot.Cpu is { } cpu)
        {
            readings.Add(new("cpu", "CPU", cpu.AverageLoadText, cpu.ClockText, cpu.PackagePowerText, cpu.AverageLoadPercent, DashboardBrushes.Blue));
        }

        if (snapshot.Memory is { } memory)
        {
            readings.Add(new("memory", "Memory", memory.UsageText, memory.CapacityText, memory.TemperatureText, memory.UsageGauge, DashboardBrushes.Green));
        }

        for (int i = 0; i < snapshot.Gpus.Count; i++)
        {
            GpuDeviceReading gpu = snapshot.Gpus[i];
            readings.Add(new($"gpu:{gpu.Name}", $"GPU {i}", gpu.LoadText, gpu.Name, $"{gpu.PowerText} | {gpu.TemperatureText}", gpu.LoadGauge, DashboardBrushes.Purple));
        }

        for (int i = 0; i < snapshot.StorageDevices.Count; i++)
        {
            StorageDeviceReading storage = snapshot.StorageDevices[i];
            readings.Add(new($"storage:{storage.Name}", $"Disk {i}", storage.UsageText, storage.Name, $"{storage.ReadWriteText} | {storage.TemperatureText}", storage.ActivityGauge, DashboardBrushes.Amber));
        }

        SyncDeviceCollection(_viewModel.OverviewItems, readings, reading => reading.Key, reading => new OverviewItemViewModel(reading));
    }

    private void ThemePicker_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RequestedThemeVariant = _viewModel.SelectedThemeIndex switch
        {
            1 => ThemeVariant.Light,
            2 => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private void ChartRangePicker_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        int selected = _viewModel.SelectedChartRangeIndex;
        if (selected >= 0 && selected < ChartRangesSeconds.Length)
        {
            ChartHistorySettings.DisplaySeconds = ChartRangesSeconds[selected];
        }
    }

    private void UpdateIntervalPicker_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        int selected = _viewModel.SelectedUpdateIntervalIndex;
        if (selected >= 0 && selected < UpdateIntervalsSeconds.Length)
        {
            _pollIntervalMilliseconds = (int)Math.Round(UpdateIntervalsSeconds[selected] * 1000d);
            ChartHistorySettings.SampleIntervalSeconds = _pollIntervalMilliseconds / 1000d;
        }
    }

    private async void SidebarToggle_Click(object? sender, RoutedEventArgs e)
    {
        bool compact = !_viewModel.IsSidebarCompact;
        if (compact)
        {
            ApplySidebarMode(true);
            await AnimateSidebarWidthAsync(SidebarRoot.Bounds.Width, SidebarCompactWidth).ConfigureAwait(true);
            return;
        }

        await AnimateSidebarWidthAsync(SidebarRoot.Bounds.Width, _lastExpandedSidebarWidth).ConfigureAwait(true);
        ApplySidebarMode(false);
    }

    private void SidebarRoot_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateSidebarMode(e.NewSize.Width);
    }

    private void UpdateSidebarMode(double width)
    {
        if (width <= 0)
        {
            return;
        }

        if (_isApplyingSidebarWidth || _isAnimatingSidebarWidth)
        {
            return;
        }

        if (_viewModel.IsSidebarCompact)
        {
            if (width >= SidebarExpandedMinWidth)
            {
                double expandedWidth = Math.Max(width, SidebarExpandedMinWidth);
                _lastExpandedSidebarWidth = expandedWidth;
                SetSidebarWidth(expandedWidth);
                ApplySidebarMode(false);
                return;
            }

            if (Math.Abs(width - SidebarCompactWidth) > 0.5)
            {
                SetSidebarWidth(SidebarCompactWidth);
            }

            ApplySidebarMode(true);
            return;
        }

        if (width < SidebarCompactThreshold)
        {
            ApplySidebarMode(true);
            SetSidebarWidth(SidebarCompactWidth);
            return;
        }

        _lastExpandedSidebarWidth = Math.Max(width, SidebarExpandedMinWidth);
        ApplySidebarMode(false);
    }

    private void SetSidebarWidth(double width)
    {
        _isApplyingSidebarWidth = true;
        ShellGrid.ColumnDefinitions[0].Width = new GridLength(width);
        _isApplyingSidebarWidth = false;
    }

    private async Task AnimateSidebarWidthAsync(double from, double to)
    {
        StopSidebarWidthAnimation();
        CancellationTokenSource animation = new();
        _sidebarWidthAnimation = animation;
        CancellationToken token = animation.Token;
        _isAnimatingSidebarWidth = true;

        try
        {
            from = Math.Max(0, from);
            to = Math.Max(0, to);
            if (Math.Abs(from - to) < 0.5)
            {
                SetSidebarWidth(to);
                return;
            }

            for (int frame = 1; frame <= DashboardAnimation.Frames; frame++)
            {
                token.ThrowIfCancellationRequested();
                double t = frame / (double)DashboardAnimation.Frames;
                double eased = DashboardAnimation.EaseOutCubic(t);
                SetSidebarWidth(DashboardAnimation.Lerp(from, to, eased));
                await Task.Delay(DashboardAnimation.DurationMilliseconds / DashboardAnimation.Frames, token).ConfigureAwait(true);
            }

            SetSidebarWidth(to);
        }
        catch (OperationCanceledException)
        {
            // Superseded by another sidebar animation or window shutdown.
        }
        finally
        {
            if (ReferenceEquals(_sidebarWidthAnimation, animation))
            {
                _isAnimatingSidebarWidth = false;
                animation.Dispose();
                _sidebarWidthAnimation = null;
            }
        }
    }

    private void StopSidebarWidthAnimation()
    {
        _sidebarWidthAnimation?.Cancel();
    }

    private void ApplySidebarMode(bool compact)
    {
        _viewModel.IsSidebarCompact = compact;
        _viewModel.SidebarMargin = new Avalonia.Thickness(8, 10);
    }

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

    private void PawnIoLink_Click(object? sender, RoutedEventArgs e) => OpenUri("https://pawnio.eu/");

    private void WebsiteLink_Click(object? sender, RoutedEventArgs e) => OpenUri("https://remoooo.com");

    private void GithubLink_Click(object? sender, RoutedEventArgs e) => OpenUri("https://github.com/Remyuu");

    private static void OpenUri(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch
        {
            // Link buttons are convenience affordances; sensor monitoring should not fail if shell launch is blocked.
        }
    }

    private void OnClosed(object? sender, EventArgs e) => ShutdownNow();

    private void ShutdownNow()
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        _shutdown.Cancel();
        StopSidebarWidthAnimation();
        Closed -= OnClosed;
        _backend.Dispose();
        _shutdown.Dispose();
    }
}
