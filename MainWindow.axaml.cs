using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using RemoSystemProfiler.Backends.Windows;
using RemoSystemProfiler.Core;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

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
    private const double BenchmarkCurtainHeight = 284;
    private const int BenchmarkCurtainAnimationMilliseconds = 220;
    private const double DashboardContentVerticalMargin = 14;

    private static readonly int[] ChartRangesSeconds = [10, 30, 60, 300];
    private static readonly double[] UpdateIntervalsSeconds = [0.5, 1, 2, 5];

    private readonly IHardwareMonitorBackend _backend = new WindowsHardwareMonitorBackend();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly MainWindowViewModel _viewModel = new();
    private readonly List<OverviewReading> _overviewBuffer = new(capacity: 8);
    private bool _isApplyingStoredSettings = true;
    private Task? _pollingTask;
    private int _pollIntervalMilliseconds = 1000;
    private double _lastExpandedSidebarWidth = SidebarExpandedWidth;
    private bool _isApplyingSidebarWidth;
    private bool _isAnimatingSidebarWidth;
    private CancellationTokenSource? _sidebarWidthAnimation;
    private bool _isBenchmarkCurtainOpen;
    private CancellationTokenSource? _benchmarkCurtainAnimation;
    private CancellationTokenSource? _benchmarkCancellation;
    private TranslateTransform? _benchmarkCurtainTransform;
    private TranslateTransform? _dashboardContentTransform;
    private Size _lastNormalWindowSize = new(InitialWindowWidth, InitialWindowHeight);
    private HardwareMonitorReadResult? _lastResult;
    private bool _isClosed;
    private bool _startupOverlayDismissed;

    public MainWindow()
    {
        ApplyStoredDashboardSettings(DashboardSettingsStore.Load());
        InitializeComponent();
        DataContext = _viewModel;
        Opened += OnOpened;
        Closed += OnClosed;
        ApplyDashboardSelectionEffects();
        _isApplyingStoredSettings = false;
        SizeChanged += OnWindowSizeChanged;
        _viewModel.UpdatedText = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        _benchmarkCurtainTransform = BenchmarkCurtain.RenderTransform as TranslateTransform
            ?? new TranslateTransform { Y = -BenchmarkCurtainHeight };
        BenchmarkCurtain.RenderTransform = _benchmarkCurtainTransform;
        _dashboardContentTransform = DashboardContent.RenderTransform as TranslateTransform
            ?? new TranslateTransform();
        DashboardContent.RenderTransform = _dashboardContentTransform;
        UpdateDashboardContentHeight(DashboardScrollViewer.Bounds.Height);
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

    private void DashboardScrollViewer_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateDashboardContentHeight(e.NewSize.Height);
    }

    private void UpdateDashboardContentHeight(double scrollViewportHeight)
    {
        if (scrollViewportHeight <= 0)
        {
            return;
        }

        DashboardContent.Height = Math.Max(0, scrollViewportHeight - DashboardContentVerticalMargin);
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
                ReadAndDispatch(token);
                await Task.Delay(TimeSpan.FromMilliseconds(_pollIntervalMilliseconds), token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal close path.
        }
    }

    private void ReadAndDispatch(CancellationToken token)
    {
        HardwareMonitorReadResult result = _backend.Read();
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
        _lastResult = result;
        _viewModel.UpdatedText = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        if (!result.IsAvailable || result.Snapshot is null)
        {
            ShowUnavailable(result.Message, result.DriverStatus);
            DismissStartupOverlay(result.DriverStatus.NeedsInstallation
                ? Localization.PawnIoRequired
                : Localization.SensorBackendUnavailable);
            return;
        }

        ShowSnapshot(result.Snapshot, result);
        DismissStartupOverlay(result.DriverStatus.NeedsInstallation
            ? Localization.PawnIoRequired
            : result.RequiresAdministrator
            ? Localization.ConnectedLimited
            : Localization.ConnectedBackend);
    }

    private void ShowSnapshot(SystemSnapshot snapshot, HardwareMonitorReadResult result)
    {
        bool driverLimited = !result.DriverStatus.IsReady;
        bool limited = result.RequiresAdministrator || driverLimited;
        _viewModel.StatusBrush = limited ? DashboardBrushes.OrangeRed : DashboardBrushes.LimeGreen;
        _viewModel.StatusText = driverLimited
            ? Localization.DriverMessage(result.DriverStatus.Message)
            : result.RequiresAdministrator
            ? Localization.LimitedAccessStatus(snapshot.Source)
            : Localization.ConnectedStatus(snapshot.Source, Localization.DriverSummary(result.DriverStatus));
        _viewModel.StatusToolTip = limited ? BuildLimitedStatusTooltip(result) : null;
        _viewModel.IsPawnIoDownloadVisible = result.DriverStatus.NeedsInstallation;
        _viewModel.UpdatedText = snapshot.SampledAtText;
        _viewModel.HardwareSummaryText = Localization.HardwareSummary(snapshot.Gpus.Count, snapshot.StorageDevices.Count);

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
            _viewModel.CpuNameText = Localization.CpuSensorsUnavailable;
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
        string localizedMessage = Localization.ResultMessage(message);
        string localizedDriverMessage = Localization.DriverMessage(driverStatus.Message);
        _viewModel.StatusBrush = DashboardBrushes.OrangeRed;
        _viewModel.StatusText = driverStatus.NeedsInstallation ? localizedDriverMessage : localizedMessage;
        _viewModel.StatusToolTip = driverStatus.NeedsInstallation ? $"{localizedDriverMessage}\n{localizedMessage}" : localizedMessage;
        _viewModel.IsPawnIoDownloadVisible = driverStatus.NeedsInstallation;
        _viewModel.HardwareSummaryText = Localization.HardwareSensorsUnavailable;
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
                ? Localization.InstallPawnIoRestartAdmin
                : Localization.DriverMessage(result.DriverStatus.Message);
        }

        return Localization.ResultMessage(result.Message);
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
        _overviewBuffer.Clear();
        if (snapshot.Cpu is { } cpu)
        {
            _overviewBuffer.Add(new("cpu", "CPU", cpu.AverageLoadText, cpu.ClockText, cpu.PackagePowerText, cpu.AverageLoadPercent, DashboardBrushes.Blue));
        }

        if (snapshot.Memory is { } memory)
        {
            _overviewBuffer.Add(new("memory", Localization.OverviewMemory, memory.UsageText, memory.CapacityText, memory.TemperatureText, memory.UsageGauge, DashboardBrushes.Green));
        }

        for (int i = 0; i < snapshot.Gpus.Count; i++)
        {
            GpuDeviceReading gpu = snapshot.Gpus[i];
            _overviewBuffer.Add(new($"gpu:{gpu.Name}", Localization.OverviewGpu(i), gpu.LoadText, gpu.Name, $"{gpu.PowerText} | {gpu.TemperatureText}", gpu.LoadGauge, DashboardBrushes.Purple));
        }

        for (int i = 0; i < snapshot.StorageDevices.Count; i++)
        {
            StorageDeviceReading storage = snapshot.StorageDevices[i];
            _overviewBuffer.Add(new($"storage:{storage.Name}", Localization.OverviewDisk(i), storage.UsageText, storage.Name, $"{storage.ReadWriteText} | {storage.TemperatureText}", storage.ActivityGauge, DashboardBrushes.Amber));
        }

        SyncDeviceCollection(_viewModel.OverviewItems, _overviewBuffer, reading => reading.Key, reading => new OverviewItemViewModel(reading) { IsCompact = _viewModel.IsSidebarCompact });
        ApplyOverviewLayoutMode();
    }

    private void ThemePicker_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ApplyThemeSelection();
        SaveDashboardSettings();
    }

    private void ChartRangePicker_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ApplyChartRangeSelection();
        SaveDashboardSettings();
    }

    private void UpdateIntervalPicker_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ApplyUpdateIntervalSelection();
        SaveDashboardSettings();
    }

    private void LanguagePicker_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!ApplyLanguageSelection())
        {
            return;
        }

        _viewModel.RefreshLocalizedChrome();
        if (_lastResult is { } result)
        {
            ShowResult(result);
        }
        else
        {
            _viewModel.RefreshWaitingText();
        }
        SaveDashboardSettings();
    }

    private void ApplyStoredDashboardSettings(DashboardSettings settings)
    {
        _viewModel.SelectedChartRangeIndex = settings.ChartRangeIndex;
        _viewModel.SelectedUpdateIntervalIndex = settings.UpdateIntervalIndex;
        _viewModel.SelectedThemeIndex = settings.ThemeIndex;
        _viewModel.SelectedLanguageIndex = settings.LanguageIndex;
        ApplyLanguageSelection();
    }

    private void ApplyDashboardSelectionEffects()
    {
        ApplyThemeSelection();
        ApplyChartRangeSelection();
        ApplyUpdateIntervalSelection();
    }

    private void ApplyThemeSelection()
    {
        RequestedThemeVariant = _viewModel.SelectedThemeIndex switch
        {
            1 => ThemeVariant.Light,
            2 => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private void ApplyChartRangeSelection()
    {
        int selected = _viewModel.SelectedChartRangeIndex;
        if (selected >= 0 && selected < ChartRangesSeconds.Length)
        {
            ChartHistorySettings.DisplaySeconds = ChartRangesSeconds[selected];
        }
    }

    private void ApplyUpdateIntervalSelection()
    {
        int selected = _viewModel.SelectedUpdateIntervalIndex;
        if (selected >= 0 && selected < UpdateIntervalsSeconds.Length)
        {
            _pollIntervalMilliseconds = (int)Math.Round(UpdateIntervalsSeconds[selected] * 1000d);
            ChartHistorySettings.SampleIntervalSeconds = _pollIntervalMilliseconds / 1000d;
        }
    }

    private bool ApplyLanguageSelection()
    {
        bool changed = Localization.SetLanguageFromIndex(_viewModel.SelectedLanguageIndex);
        if (changed && Application.Current?.Resources is { } resources)
        {
            Localization.ApplyToResources(resources);
        }

        return changed;
    }

    private void SaveDashboardSettings()
    {
        if (_isApplyingStoredSettings)
        {
            return;
        }

        DashboardSettingsStore.Save(new DashboardSettings(
            _viewModel.SelectedChartRangeIndex,
            _viewModel.SelectedUpdateIntervalIndex,
            _viewModel.SelectedThemeIndex,
            _viewModel.SelectedLanguageIndex));
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
        ApplyOverviewLayoutMode();
    }

    private void ApplyOverviewLayoutMode()
    {
        for (int i = 0; i < _viewModel.OverviewItems.Count; i++)
        {
            _viewModel.OverviewItems[i].IsCompact = _viewModel.IsSidebarCompact;
        }
    }

    private static SensorGroupReading[] BuildCpuSensorGroups(CpuDeviceReading? cpu) =>
    [
        new("temperature", Localization.SensorGroupTitle("temperature"), cpu?.TemperatureSensors ?? Array.Empty<MetricReading>(), false),
        new("power", Localization.SensorGroupTitle("power"), cpu?.PowerSensors ?? Array.Empty<MetricReading>(), false),
        new("clock", Localization.SensorGroupTitle("clock"), cpu?.ClockSensors ?? Array.Empty<MetricReading>(), false),
        new("voltage", Localization.SensorGroupTitle("voltage"), cpu?.VoltageSensors ?? Array.Empty<MetricReading>(), false)
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

    private async void BenchmarkToggle_Click(object? sender, RoutedEventArgs e)
    {
        if (_isBenchmarkCurtainOpen)
        {
            CancelCpuBenchmark();
            await AnimateBenchmarkCurtainAsync(open: false).ConfigureAwait(true);
            return;
        }

        await AnimateBenchmarkCurtainAsync(open: true).ConfigureAwait(true);
    }

    private async void CpuBenchmarkRun_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.IsBenchmarkRunning)
        {
            return;
        }

        if (!_isBenchmarkCurtainOpen)
        {
            await AnimateBenchmarkCurtainAsync(open: true).ConfigureAwait(true);
        }

        CancellationTokenSource benchmark = new();
        _benchmarkCancellation = benchmark;
        int workerCount = _viewModel.ResolveBenchmarkWorkerCount();
        TimeSpan duration = _viewModel.ResolveBenchmarkDuration();
        string modeText = _viewModel.ResolveBenchmarkModeText();

        _viewModel.IsBenchmarkRunning = true;
        _viewModel.BenchmarkStatusText = Localization.BenchmarkRunningMode(modeText);
        _viewModel.BenchmarkThreadsText = Localization.BenchmarkThreadCount(workerCount);
        _viewModel.BenchmarkDurationText = Localization.BenchmarkDurationRun((int)Math.Round(duration.TotalSeconds));
        _viewModel.BenchmarkProgressValue = 0;
        _viewModel.BenchmarkProgressText = "0%";

        Progress<CpuBenchmarkProgress> progress = new(ShowCpuBenchmarkProgress);
        try
        {
            CpuBenchmarkResult result = await CpuBenchmarkRunner.RunAsync(duration, workerCount, progress, benchmark.Token).ConfigureAwait(true);
            ShowCpuBenchmarkResult(result);
        }
        catch (OperationCanceledException)
        {
            _viewModel.BenchmarkStatusText = Localization.BenchmarkCanceled;
        }
        catch (Exception ex)
        {
            _viewModel.BenchmarkStatusText = Localization.BenchmarkFailed(ex.Message);
        }
        finally
        {
            if (ReferenceEquals(_benchmarkCancellation, benchmark))
            {
                _benchmarkCancellation = null;
            }

            benchmark.Dispose();
            _viewModel.IsBenchmarkRunning = false;
        }
    }

    private void CpuBenchmarkCancel_Click(object? sender, RoutedEventArgs e) => CancelCpuBenchmark();

    private void CancelCpuBenchmark()
    {
        if (_benchmarkCancellation is { IsCancellationRequested: false } benchmark)
        {
            _viewModel.BenchmarkStatusText = Localization.BenchmarkCanceling;
            benchmark.Cancel();
        }
    }

    private void ShowCpuBenchmarkProgress(CpuBenchmarkProgress progress)
    {
        double percent = Math.Clamp(progress.Ratio * 100d, 0, 100);
        _viewModel.BenchmarkProgressValue = percent;
        _viewModel.BenchmarkProgressText = $"{percent:0}%";
        _viewModel.BenchmarkStatusText = Localization.BenchmarkRunningProgress(progress.Elapsed.TotalSeconds, progress.Duration.TotalSeconds);
    }

    private void ShowCpuBenchmarkResult(CpuBenchmarkResult result)
    {
        _viewModel.BenchmarkProgressValue = 100;
        _viewModel.BenchmarkProgressText = "100%";
        _viewModel.BenchmarkScoreText = Math.Round(result.Score).ToString("N0", CultureInfo.InvariantCulture);
        _viewModel.BenchmarkThroughputText = FormatOperationsPerSecond(result.OperationsPerSecond);
        _viewModel.BenchmarkThreadsText = Localization.BenchmarkThreadCount(result.WorkerCount);
        _viewModel.BenchmarkDurationText = $"{result.ElapsedSeconds:0.0}s";
        _viewModel.BenchmarkStatusText = Localization.BenchmarkCompletedAt(DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
    }

    private static string FormatOperationsPerSecond(double operationsPerSecond)
    {
        return operationsPerSecond switch
        {
            >= 1_000_000_000d => $"{operationsPerSecond / 1_000_000_000d:0.00} B ops/s",
            >= 1_000_000d => $"{operationsPerSecond / 1_000_000d:0.00} M ops/s",
            >= 1_000d => $"{operationsPerSecond / 1_000d:0.00} K ops/s",
            _ => $"{operationsPerSecond:0} ops/s"
        };
    }

    private async Task AnimateBenchmarkCurtainAsync(bool open)
    {
        StopBenchmarkCurtainAnimation();
        CancellationTokenSource animation = new();
        _benchmarkCurtainAnimation = animation;
        CancellationToken token = animation.Token;

        if (open)
        {
            DashboardScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            BenchmarkCurtainHost.IsVisible = true;
            BenchmarkCurtainHost.IsHitTestVisible = true;
        }
        else
        {
            DashboardScrollViewer.Offset = new Vector(DashboardScrollViewer.Offset.X, 0);
        }

        TranslateTransform curtainTransform = _benchmarkCurtainTransform ?? new TranslateTransform { Y = -BenchmarkCurtainHeight };
        _benchmarkCurtainTransform = curtainTransform;
        BenchmarkCurtain.RenderTransform = curtainTransform;
        TranslateTransform contentTransform = _dashboardContentTransform ?? new TranslateTransform();
        _dashboardContentTransform = contentTransform;
        DashboardContent.RenderTransform = contentTransform;

        double fromOffset = curtainTransform.Y;
        double toOffset = open ? 0 : -BenchmarkCurtainHeight;
        double fromContentOffset = contentTransform.Y;
        double toContentOffset = open ? BenchmarkCurtainHeight : 0;
        double fromOpacity = BenchmarkCurtainHost.Opacity;
        double toOpacity = open ? 1 : 0;

        try
        {
            for (int frame = 1; frame <= DashboardAnimation.Frames; frame++)
            {
                token.ThrowIfCancellationRequested();
                double t = frame / (double)DashboardAnimation.Frames;
                double eased = DashboardAnimation.EaseOutCubic(t);
                curtainTransform.Y = DashboardAnimation.Lerp(fromOffset, toOffset, eased);
                contentTransform.Y = DashboardAnimation.Lerp(fromContentOffset, toContentOffset, eased);
                BenchmarkCurtainHost.Opacity = DashboardAnimation.Lerp(fromOpacity, toOpacity, eased);
                await Task.Delay(BenchmarkCurtainAnimationMilliseconds / DashboardAnimation.Frames, token).ConfigureAwait(true);
            }

            curtainTransform.Y = toOffset;
            contentTransform.Y = toContentOffset;
            BenchmarkCurtainHost.Opacity = toOpacity;
        }
        catch (OperationCanceledException)
        {
            // Superseded by another curtain animation or window shutdown.
        }
        finally
        {
            if (ReferenceEquals(_benchmarkCurtainAnimation, animation))
            {
                _isBenchmarkCurtainOpen = open;
                BenchmarkCurtainHost.IsHitTestVisible = open;
                if (!open)
                {
                    BenchmarkCurtainHost.IsVisible = false;
                    DashboardScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    DashboardScrollViewer.Offset = new Vector(DashboardScrollViewer.Offset.X, 0);
                }

                animation.Dispose();
                _benchmarkCurtainAnimation = null;
            }
        }
    }

    private void StopBenchmarkCurtainAnimation()
    {
        _benchmarkCurtainAnimation?.Cancel();
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
        CancelCpuBenchmark();
        StopSidebarWidthAnimation();
        StopBenchmarkCurtainAnimation();
        Closed -= OnClosed;
        _backend.Dispose();
        _shutdown.Dispose();
    }
}
