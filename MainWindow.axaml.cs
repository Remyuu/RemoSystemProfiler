using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
    private const double BenchmarkCurtainHeight = 600;
    private const int BenchmarkCurtainAnimationMilliseconds = 220;
    private const double DashboardContentVerticalMargin = 14;
    private const double PressScale = 0.985;

    private static readonly int[] ChartRangesSeconds = [10, 30, 60, 300];
    private static readonly double[] UpdateIntervalsSeconds = [0.5, 1, 2, 5];

    private readonly IHardwareMonitorBackend _backend = new WindowsHardwareMonitorBackend();
    private readonly BenchmarkApiClient _benchmarkApi = new();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly MainWindowViewModel _viewModel = new();
    private readonly List<OverviewReading> _overviewBuffer = new(capacity: 8);
    private readonly Dictionary<Control, PressVisualState> _pressedVisualStates = [];
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
    private BenchmarkSensorAccumulator _benchmarkSensors = new();
    private BenchmarkUploadDto? _lastBenchmarkUpload;
    private TranslateTransform? _benchmarkCurtainTransform;
    private TranslateTransform? _dashboardContentTransform;
    private GitHubReleaseAssetInfo? _pendingUpdateAsset;
    private Size _lastNormalWindowSize = new(InitialWindowWidth, InitialWindowHeight);
    private HardwareMonitorReadResult? _lastResult;
    private bool _isClosed;
    private bool _startupOverlayDismissed;
    private bool _pawnIoPromptDismissed;
    private bool _isRefreshingLocalizedSelectionBoxes;

    public MainWindow()
    {
        ApplyStoredDashboardSettings(DashboardSettingsStore.Load());
        InitializeComponent();
        DataContext = _viewModel;
        BenchmarkPanel.RunRequested += async (_, _) => await RunCpuBenchmarkAsync().ConfigureAwait(true);
        BenchmarkPanel.CancelRequested += (_, _) => CancelCpuBenchmark();
        BenchmarkPanel.UploadRequested += async (_, _) => await UploadBenchmarkAsync().ConfigureAwait(true);
        BenchmarkPanel.RefreshLeaderboardRequested += async (_, _) => await RefreshLeaderboardAsync().ConfigureAwait(true);
        BenchmarkPanel.ToggleLeaderboardScoreRequested += async (_, _) => await ToggleLeaderboardScoreAsync().ConfigureAwait(true);
        RefreshDashboardFlyoutLocalization();
        Opened += OnOpened;
        Closed += OnClosed;
        AddHandler(InputElement.PointerPressedEvent, PressableControl_PointerPressed, RoutingStrategies.Tunnel);
        AddHandler(InputElement.PointerReleasedEvent, PressableControl_PointerReleased, RoutingStrategies.Tunnel);
        AddHandler(InputElement.PointerCaptureLostEvent, PressableControl_PointerCaptureLost, RoutingStrategies.Tunnel);
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

    private void PressableControl_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPoint point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        Control? control = FindPressableControl(e.Source);
        if (control is null || _pressedVisualStates.ContainsKey(control))
        {
            return;
        }

        _pressedVisualStates[control] = new PressVisualState(control.RenderTransform, control.RenderTransformOrigin);
        control.RenderTransformOrigin = RelativePoint.Center;
        Point offset = ResolvePressOffset(control);
        TransformGroup transform = new();
        if (control.RenderTransform is Transform originalTransform)
        {
            transform.Children.Add(originalTransform);
        }

        transform.Children.Add(new ScaleTransform(PressScale, PressScale));
        transform.Children.Add(new TranslateTransform(offset.X, offset.Y));
        control.RenderTransform = transform;
    }

    private void PressableControl_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (FindPressableControl(e.Source) is { } control)
        {
            RestorePressedVisual(control);
        }
    }

    private void PressableControl_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (FindPressableControl(e.Source) is { } control)
        {
            RestorePressedVisual(control);
            return;
        }

        RestoreAllPressedVisuals();
    }

    private void RestorePressedVisual(Control control)
    {
        if (!_pressedVisualStates.Remove(control, out PressVisualState? state) || state is null)
        {
            return;
        }

        control.RenderTransform = state.RenderTransform;
        control.RenderTransformOrigin = state.RenderTransformOrigin;
    }

    private void RestoreAllPressedVisuals()
    {
        foreach ((Control control, PressVisualState state) in _pressedVisualStates.ToArray())
        {
            control.RenderTransform = state.RenderTransform;
            control.RenderTransformOrigin = state.RenderTransformOrigin;
        }

        _pressedVisualStates.Clear();
    }

    private Point ResolvePressOffset(Control control)
    {
        Point center = control.TranslatePoint(new Point(control.Bounds.Width * 0.5, control.Bounds.Height * 0.5), this)
            ?? new Point(Bounds.Width * 0.5, Bounds.Height * 0.5);
        double x = (Bounds.Width * 0.5) - center.X;
        double y = (Bounds.Height * 0.5) - center.Y;
        double length = Math.Sqrt(x * x + y * y);
        if (length < 1)
        {
            return new Point(0, 0.8);
        }

        double offsetX = Math.Clamp(x / length * 1.1, -1.1, 1.1);
        double offsetY = Math.Clamp(0.45 + y / length * 0.85, -0.6, 1.25);
        return new Point(offsetX, offsetY);
    }

    private static Control? FindPressableControl(object? source)
    {
        for (Visual? current = source as Visual; current is not null; current = current.GetVisualParent() as Visual)
        {
            if (current is Button button && button.IsEnabled)
            {
                return button;
            }

            if (current is ComboBox comboBox && comboBox.IsEnabled)
            {
                return comboBox;
            }

            if (current is NumericUpDown numericUpDown && numericUpDown.IsEnabled)
            {
                return numericUpDown;
            }
        }

        return null;
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
                int delayMilliseconds = _viewModel.IsBenchmarkRunning
                    ? Math.Min(_pollIntervalMilliseconds, 1000)
                    : _pollIntervalMilliseconds;
                await Task.Delay(TimeSpan.FromMilliseconds(delayMilliseconds), token).ConfigureAwait(false);
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
            UpdatePawnIoPrompt(result.DriverStatus);
            DismissStartupOverlay(result.DriverStatus.NeedsInstallation
                ? Localization.PawnIoRequired
                : Localization.SensorBackendUnavailable);
            return;
        }

        ShowSnapshot(result.Snapshot, result);
        UpdatePawnIoPrompt(result.DriverStatus);
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
        _viewModel.IsPawnIoDownloadVisible = ShouldShowPawnIoInstaller(result.DriverStatus);
        _viewModel.UpdatedText = snapshot.SampledAtText;
        _viewModel.HardwareSummaryText = Localization.HardwareSummary(snapshot.Gpus.Count, snapshot.StorageDevices.Count);

        ShowCpu(snapshot.Cpu);
        if (_viewModel.IsBenchmarkRunning && snapshot.Cpu is not null)
        {
            AddBenchmarkSensorSample(snapshot.Cpu);
        }

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
            SyncDeviceCollection(_viewModel.CpuOverallUsage, Array.Empty<CoreReading>(), core => core.Index, core => new CoreItemViewModel(core));
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
        SyncDeviceCollection(
            _viewModel.CpuOverallUsage,
            [new CoreReading(-1, Math.Clamp((int)Math.Round(cpu.AverageLoadPercent), 0, 100))],
            core => core.Index,
            core => new CoreItemViewModel(core));
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
        _viewModel.IsPawnIoDownloadVisible = ShouldShowPawnIoInstaller(driverStatus);
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

    private static bool ShouldShowPawnIoInstaller(SensorDriverStatus status)
    {
        return !status.IsInstalled
            || IsLegacyPawnIoVersion(status.Version)
            || status.Message.Equals(
                "PawnIO installation found but the driver is unavailable; uninstall PawnIO, then install again",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLegacyPawnIoVersion(string? versionText)
    {
        if (string.IsNullOrWhiteSpace(versionText))
        {
            return false;
        }

        string normalized = versionText.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..];
        }

        int suffixIndex = normalized.IndexOfAny(['-', '+']);
        if (suffixIndex >= 0)
        {
            normalized = normalized[..suffixIndex];
        }

        return Version.TryParse(normalized, out Version? version)
            && version < new Version(2, 1, 0);
    }

    private void UpdatePawnIoPrompt(SensorDriverStatus status)
    {
        if (!status.NeedsInstallation)
        {
            _viewModel.IsPawnIoPromptVisible = false;
            _pawnIoPromptDismissed = false;
            return;
        }

        if (!_pawnIoPromptDismissed)
        {
            _viewModel.IsPawnIoPromptVisible = true;
        }
    }

    private void PawnIoPromptCancel_Click(object? sender, RoutedEventArgs e)
    {
        _pawnIoPromptDismissed = true;
        _viewModel.IsPawnIoPromptVisible = false;
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
        if (_isRefreshingLocalizedSelectionBoxes)
        {
            return;
        }

        ApplyThemeSelection();
    }

    private void ChartRangePicker_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshingLocalizedSelectionBoxes)
        {
            return;
        }

        ApplyChartRangeSelection();
    }

    private void UpdateIntervalPicker_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshingLocalizedSelectionBoxes)
        {
            return;
        }

        ApplyUpdateIntervalSelection();
    }

    private void CpuCoreGraphToggle_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.IsCpuOverallView = !_viewModel.IsCpuOverallView;
    }

    private void LanguagePicker_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshingLocalizedSelectionBoxes)
        {
            return;
        }

        if (sender is ComboBox { SelectedIndex: >= 0 } languagePicker
            && languagePicker.SelectedIndex != _viewModel.SelectedLanguageIndex)
        {
            _viewModel.SelectedLanguageIndex = languagePicker.SelectedIndex;
        }

        if (!ApplyLanguageSelection())
        {
            return;
        }

        _viewModel.RefreshLocalizedChrome();
        RefreshDashboardFlyoutLocalization();
        if (_lastResult is { } result)
        {
            ShowResult(result);
        }
        else
        {
            _viewModel.RefreshWaitingText();
        }
    }

    private void ApplyStoredDashboardSettings(DashboardSettings settings)
    {
        _viewModel.SelectedChartRangeIndex = settings.ChartRangeIndex;
        _viewModel.SelectedUpdateIntervalIndex = settings.UpdateIntervalIndex;
        _viewModel.SelectedThemeIndex = settings.ThemeIndex;
        _viewModel.SelectedLanguageIndex = settings.LanguageIndex;
        _viewModel.IsCpuOverallView = settings.CpuOverallView;
        _viewModel.BenchmarkDisplayNameText = settings.BenchmarkDisplayName;
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

    private void RefreshDashboardFlyoutLocalization()
    {
        FlyoutDashboardTitle.Text = Localization.Resource("Ui_Dashboard");
        FlyoutChartRangeLabel.Text = Localization.Resource("Ui_ChartRange");
        FlyoutRangeTenSecondsItem.Content = Localization.Resource("Ui_RangeTenSeconds");
        FlyoutRangeThirtySecondsItem.Content = Localization.Resource("Ui_RangeThirtySeconds");
        FlyoutRangeOneMinuteItem.Content = Localization.Resource("Ui_RangeOneMinute");
        FlyoutRangeFiveMinutesItem.Content = Localization.Resource("Ui_RangeFiveMinutes");

        FlyoutUpdateIntervalLabel.Text = Localization.Resource("Ui_UpdateInterval");
        FlyoutIntervalHalfSecondItem.Content = Localization.Resource("Ui_IntervalHalfSecond");
        FlyoutIntervalOneSecondItem.Content = Localization.Resource("Ui_IntervalOneSecond");
        FlyoutIntervalTwoSecondsItem.Content = Localization.Resource("Ui_IntervalTwoSeconds");
        FlyoutIntervalFiveSecondsItem.Content = Localization.Resource("Ui_IntervalFiveSeconds");

        FlyoutThemeLabel.Text = Localization.Resource("Ui_Theme");
        FlyoutThemeSystemItem.Content = Localization.Resource("Ui_ThemeSystem");
        FlyoutThemeLightItem.Content = Localization.Resource("Ui_ThemeLight");
        FlyoutThemeDarkItem.Content = Localization.Resource("Ui_ThemeDark");

        FlyoutLanguageLabel.Text = Localization.Resource("Ui_Language");
        FlyoutEnglishItem.Content = Localization.Resource("Ui_English");
        FlyoutChineseItem.Content = Localization.Resource("Ui_Chinese");
        FlyoutJapaneseItem.Content = Localization.Resource("Ui_Japanese");
        FlyoutTraditionalChineseItem.Content = Localization.Resource("Ui_TraditionalChinese");
        FlyoutSpanishItem.Content = Localization.Resource("Ui_Spanish");
        FlyoutGermanItem.Content = Localization.Resource("Ui_German");
        FlyoutFrenchItem.Content = Localization.Resource("Ui_French");

        FlyoutAboutTitle.Text = Localization.Resource("Ui_About");
        FlyoutAppSubtitleText.Text = Localization.Resource("Ui_AppSubtitle");
        FlyoutVersionLabel.Text = Localization.Resource("Ui_Version");
        FlyoutWebsiteLabel.Text = Localization.Resource("Ui_Website");
        FlyoutCheckUpdatesButton.Content = Localization.Resource("Ui_CheckForUpdates");
        FlyoutInstallUpdateButton.Content = Localization.Resource("Ui_DownloadAndInstall");
        FlyoutOpenReleaseButton.Content = Localization.Resource("Ui_OpenRelease");
        FlyoutReleaseNotesLabel.Text = Localization.Resource("Ui_ReleaseNotes");
        FlyoutBuiltWithText.Text = Localization.Resource("Ui_BuiltWith");
        FlyoutLicenseNoticeText.Text = Localization.Resource("Ui_LicenseNotice");
        if (!_viewModel.IsUpdateCheckRunning && !_viewModel.IsReleaseNotesVisible)
        {
            _viewModel.UpdateStatusText = Localization.UpdateIdle;
        }

        RefreshBenchmarkLocalization();
    }

    private void RefreshBenchmarkLocalization()
    {
        BenchmarkPanel.RefreshLocalization();
        QueueLocalizedSelectionBoxRefresh();
    }

    private void QueueLocalizedSelectionBoxRefresh()
    {
        Dispatcher.UIThread.Post(RefreshLocalizedSelectionBoxes, DispatcherPriority.Render);
    }

    private void RefreshLocalizedSelectionBoxes()
    {
        _isRefreshingLocalizedSelectionBoxes = true;
        try
        {
            RefreshSelectionBox(FlyoutChartRangePicker);
            RefreshSelectionBox(FlyoutUpdateIntervalPicker);
            RefreshSelectionBox(FlyoutThemePicker);
            RefreshSelectionBox(FlyoutLanguagePicker);
            BenchmarkPanel.RefreshLocalizedSelectionBoxes();
        }
        finally
        {
            _isRefreshingLocalizedSelectionBoxes = false;
        }
    }

    private static void RefreshSelectionBox(ComboBox comboBox)
    {
        int selectedIndex = comboBox.SelectedIndex;
        if (selectedIndex < 0)
        {
            return;
        }

        comboBox.SelectedIndex = -1;
        comboBox.SelectedIndex = selectedIndex;
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
            _viewModel.SelectedLanguageIndex,
            _viewModel.IsCpuOverallView,
            BenchmarkPayload.NormalizeDisplayName(_viewModel.BenchmarkDisplayNameText)));
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

    private async Task RunCpuBenchmarkAsync()
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
        BenchmarkRunProfile profile = _viewModel.ResolveBenchmarkProfile();
        string benchmarkVersion = _viewModel.ResolveBenchmarkVersion();
        BenchmarkProfilePlan plan = _viewModel.ResolveBenchmarkPlan();
        string modeText = _viewModel.ResolveBenchmarkModeText();

        _benchmarkSensors = new BenchmarkSensorAccumulator();
        _viewModel.BenchmarkTelemetrySamples.Clear();
        if (_lastResult?.Snapshot?.Cpu is { } currentCpu)
        {
            AddBenchmarkSensorSample(currentCpu);
        }

        _viewModel.IsBenchmarkRunning = true;
        _viewModel.BenchmarkStatusText = Localization.BenchmarkRunningMode(modeText);
        _viewModel.BenchmarkModeText = modeText;
        _viewModel.BenchmarkThreadsText = Localization.BenchmarkThreadCount(workerCount);
        _viewModel.BenchmarkScoreText = "--";
        _viewModel.BenchmarkMixedScoreText = "--";
        _viewModel.BenchmarkSciMarkText = "--";
        _viewModel.BenchmarkZstdCompressionText = "--";
        _viewModel.BenchmarkZstdDecompressionText = "--";
        _viewModel.BenchmarkHashText = "--";
        _viewModel.BenchmarkCpuFrequencyText = "--";
        _viewModel.BenchmarkCpuTemperatureText = "--";
        _viewModel.BenchmarkPowerThermalText = "--";
        _viewModel.BenchmarkValidationText = "--";
        _viewModel.IsBenchmarkUploadAvailable = false;
        _viewModel.BenchmarkUploadStatusText = Localization.BenchmarkUploadNoResult;
        _viewModel.BenchmarkProgressValue = 0;
        _viewModel.BenchmarkProgressText = MainWindowViewModel.FormatBenchmarkProgress(0, TimeSpan.Zero, plan.TotalDuration);
        _lastBenchmarkUpload = null;

        Progress<BenchmarkProgress> progress = new(ShowBenchmarkProgress);
        try
        {
            BenchmarkResult result = await BenchmarkRunner.RunAsync(benchmarkVersion, profile, workerCount, progress, benchmark.Token).ConfigureAwait(true);
            ShowBenchmarkResult(result);
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

    private void CancelCpuBenchmark()
    {
        if (_benchmarkCancellation is { IsCancellationRequested: false } benchmark)
        {
            _viewModel.BenchmarkStatusText = Localization.BenchmarkCanceling;
            benchmark.Cancel();
        }
    }

    private void ShowBenchmarkProgress(BenchmarkProgress progress)
    {
        double percent = Math.Clamp(progress.Ratio * 100d, 0, 100);
        _viewModel.BenchmarkProgressValue = percent;
        _viewModel.BenchmarkProgressText = MainWindowViewModel.FormatBenchmarkProgress(percent, progress.Elapsed, progress.Duration);
        ShowCompletedBenchmarkWorkload(progress);
        _viewModel.BenchmarkStatusText = Localization.BenchmarkRunningProgress(progress.StageName, progress.Elapsed.TotalSeconds, progress.Duration.TotalSeconds);
    }

    private void ShowCompletedBenchmarkWorkload(BenchmarkProgress progress)
    {
        if (progress.CompletedWorkload is not { } workload)
        {
            return;
        }

        switch (workload.Name)
        {
            case "SciMark":
                _viewModel.BenchmarkSciMarkText = Math.Round(workload.Score).ToString("N0", CultureInfo.InvariantCulture);
                break;
            case "zstd compression":
                _viewModel.BenchmarkZstdCompressionText = workload.ThroughputText;
                break;
            case "zstd decompression":
                _viewModel.BenchmarkZstdDecompressionText = workload.ThroughputText;
                break;
            case "XxHash3":
                _viewModel.BenchmarkHashText = workload.ThroughputText;
                break;
        }

    }

    private void ShowBenchmarkResult(BenchmarkResult result)
    {
        _viewModel.BenchmarkProgressValue = 100;
        _viewModel.BenchmarkProgressText = MainWindowViewModel.FormatBenchmarkProgress(100, TimeSpan.FromSeconds(result.ElapsedSeconds), TimeSpan.FromSeconds(result.ElapsedSeconds));
        _viewModel.BenchmarkScoreText = BenchmarkRunner.SupportsCpuCoreScore(result.Version)
            ? Math.Round(result.CpuCoreScore).ToString("N0", CultureInfo.InvariantCulture)
            : "--";
        _viewModel.BenchmarkMixedScoreText = Math.Round(result.CpuMixedScore).ToString("N0", CultureInfo.InvariantCulture);
        _viewModel.BenchmarkModeText = Localization.BenchmarkModeText(result.WorkerCount == 1 ? 0 : 1);
        _viewModel.BenchmarkSciMarkText = Math.Round(result.SciMark.Score).ToString("N0", CultureInfo.InvariantCulture);
        _viewModel.BenchmarkZstdCompressionText = result.ZstdCompression.ThroughputText;
        _viewModel.BenchmarkZstdDecompressionText = result.ZstdDecompression.ThroughputText;
        _viewModel.BenchmarkHashText = result.Hash.ThroughputText;
        _viewModel.BenchmarkThreadsText = Localization.BenchmarkThreadCount(result.WorkerCount);
        BenchmarkSensorSummary sensorSummary = _benchmarkSensors.Summarize();
        _viewModel.BenchmarkCpuFrequencyText = sensorSummary.AverageFrequencyText;
        _viewModel.BenchmarkCpuTemperatureText = sensorSummary.MaxTemperatureText;
        _viewModel.BenchmarkPowerThermalText = sensorSummary.PowerThermalText;
        _viewModel.BenchmarkValidationText = result.IsValid ? Localization.BenchmarkValidationOk : Localization.BenchmarkValidationFailed;
        _viewModel.BenchmarkStatusText = Localization.BenchmarkCompletedAt(DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
        _lastBenchmarkUpload = CreateBenchmarkUploadDto(result, sensorSummary);
        _viewModel.IsBenchmarkUploadAvailable = result.IsValid;
        _viewModel.BenchmarkUploadStatusText = result.IsValid
            ? Localization.BenchmarkUploadButton
            : Localization.BenchmarkValidationFailed;
    }

    private BenchmarkUploadDto CreateBenchmarkUploadDto(BenchmarkResult result, BenchmarkSensorSummary sensorSummary)
    {
        CpuDeviceReading? cpu = _lastResult?.Snapshot?.Cpu;
        BenchmarkUploadDto dto = new()
        {
            AppVersion = _viewModel.CurrentVersion,
            BenchmarkVersion = result.Version,
            Profile = BenchmarkPayload.ProfileToApiValue(result.Profile),
            Mode = BenchmarkPayload.ModeToApiValue(result.WorkerCount),
            DisplayName = BenchmarkPayload.NormalizeDisplayName(_viewModel.BenchmarkDisplayNameText),
            Score = result.CpuMixedScore,
            CpuCoreScore = BenchmarkRunner.SupportsCpuCoreScore(result.Version) ? result.CpuCoreScore : null,
            CpuMixedScore = result.CpuMixedScore,
            SciMarkScore = result.SciMark.Score,
            ZstdCompressGbps = result.ZstdCompression.ThroughputGbps,
            ZstdDecompressGbps = result.ZstdDecompression.ThroughputGbps,
            ZstdRatio = BenchmarkRunner.IsLegacyVersion(result.Version) ? result.ZstdRatio : null,
            XxHash3Gbps = result.Hash.ThroughputGbps,
            CpuName = string.IsNullOrWhiteSpace(cpu?.Name) ? null : cpu.Name,
            CpuCores = PositiveOrNull(cpu?.CoreCount),
            CpuThreads = PositiveOrNull(cpu?.LogicalProcessorCount),
            AvgFrequencyGhz = sensorSummary.AverageFrequencyGhz,
            MaxTemperatureC = sensorSummary.MaxTemperatureC,
            PowerThermalStatus = sensorSummary.PowerThermalText,
            ValidationStatus = result.IsValid ? BenchmarkPayload.ValidationOk : "FAILED",
            InstallationId = BenchmarkIdentityStore.GetOrCreateInstallationId(),
            ClientCreatedAt = BenchmarkPayload.UtcTimestamp(DateTimeOffset.UtcNow),
            TelemetrySamples = sensorSummary.TelemetrySamples
        };

        BenchmarkPayload.NormalizeMetrics(dto);
        dto.Checksum = result.IsValid ? BenchmarkPayload.ComputeChecksum(dto) : string.Empty;
        return dto;
    }

    private async Task UploadBenchmarkAsync()
    {
        if (_lastBenchmarkUpload is null || !_viewModel.CanUploadBenchmarkResult)
        {
            return;
        }

        _viewModel.IsBenchmarkUploadRunning = true;
        _viewModel.BenchmarkUploadStatusText = Localization.BenchmarkUploading;
        _lastBenchmarkUpload.DisplayName = BenchmarkPayload.NormalizeDisplayName(_viewModel.BenchmarkDisplayNameText);
        _viewModel.BenchmarkDisplayNameText = _lastBenchmarkUpload.DisplayName;

        try
        {
            BenchmarkUploadResult result = await _benchmarkApi.UploadAsync(_lastBenchmarkUpload, _shutdown.Token).ConfigureAwait(true);
            _viewModel.BenchmarkUploadStatusText = result.Status switch
            {
                BenchmarkUploadStatus.Uploaded => Localization.BenchmarkUploadSuccess,
                BenchmarkUploadStatus.Duplicate => Localization.BenchmarkUploadDuplicate,
                BenchmarkUploadStatus.RateLimited => Localization.BenchmarkUploadRateLimited,
                BenchmarkUploadStatus.Invalid => Localization.BenchmarkUploadFailed(result.Message),
                _ => Localization.BenchmarkUploadFailed(ShortError(result.Message))
            };

            if (result.Status is BenchmarkUploadStatus.Uploaded or BenchmarkUploadStatus.Duplicate)
            {
                await RefreshLeaderboardAsync().ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            // Window shutdown cancels the upload flow.
        }
        finally
        {
            if (!_isClosed)
            {
                _viewModel.IsBenchmarkUploadRunning = false;
            }
        }
    }

    private async Task ToggleLeaderboardScoreAsync()
    {
        if (!_viewModel.IsLeaderboardScorePickerEnabled)
        {
            return;
        }

        _viewModel.SelectedLeaderboardScoreIndex = _viewModel.SelectedLeaderboardScoreIndex == 0 ? 1 : 0;
        await RefreshLeaderboardAsync().ConfigureAwait(true);
    }

    private async Task RefreshLeaderboardAsync()
    {
        if (_viewModel.IsLeaderboardLoading)
        {
            return;
        }

        _viewModel.IsLeaderboardLoading = true;
        _viewModel.LeaderboardStatusText = Localization.LeaderboardLoadingButton;

        try
        {
            BenchmarkScoreKind scoreKind = _viewModel.ResolveLeaderboardScoreKind();
            BenchmarkLeaderboardQuery query = new()
            {
                BenchmarkVersion = _viewModel.ResolveBenchmarkVersion(),
                Profile = BenchmarkPayload.ProfileToApiValue(_viewModel.ResolveBenchmarkProfile()),
                Mode = BenchmarkPayload.ModeToApiValue(_viewModel.ResolveBenchmarkWorkerCount()),
                ScoreKind = scoreKind,
                Limit = 50
            };
            IReadOnlyList<BenchmarkLeaderboardEntry> entries = await _benchmarkApi.GetLeaderboardAsync(query, _shutdown.Token).ConfigureAwait(true);

            _viewModel.LeaderboardEntries.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                _viewModel.LeaderboardEntries.Add(new LeaderboardEntryViewModel(i + 1, entries[i], scoreKind));
            }
            _viewModel.LeaderboardStatusText = entries.Count == 0
                ? Localization.LeaderboardEmpty
                : Localization.LeaderboardLoaded(entries.Count);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            // Window shutdown cancels the leaderboard request.
        }
        catch (Exception ex)
        {
            _viewModel.LeaderboardStatusText = Localization.LeaderboardFailed(ShortError(ex.Message));
        }
        finally
        {
            if (!_isClosed)
            {
                _viewModel.IsLeaderboardLoading = false;
            }
        }
    }

    private static int? PositiveOrNull(int? value) => value is > 0 ? value : null;

    private void AddBenchmarkSensorSample(CpuDeviceReading cpu)
    {
        BenchmarkTelemetrySample? sample = _benchmarkSensors.Add(cpu);
        if (sample is not null)
        {
            _viewModel.BenchmarkTelemetrySamples.Add(sample);
        }
    }

    private static string ShortError(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Unknown error";
        }

        string normalized = text.ReplaceLineEndings(" ").Trim();
        return normalized.Length <= 120 ? normalized : normalized[..120] + "...";
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

    private async void PawnIoLink_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.IsPawnIoInstallRunning)
        {
            return;
        }

        _viewModel.IsPawnIoInstallRunning = true;
        _viewModel.PawnIoInstallButtonText = Localization.PawnIoInstallDownloading;
        _viewModel.StatusBrush = DashboardBrushes.Amber;
        _viewModel.StatusText = Localization.PawnIoInstallDownloading;
        _viewModel.StatusToolTip = Localization.PawnIoInstallerSource;

        try
        {
            Progress<DownloadProgressInfo> progress = new(UpdatePawnIoDownloadProgress);
            PawnIoInstallResult result = await PawnIoInstaller.DownloadAndInstallLatestAsync(progress, _shutdown.Token);
            if (_isClosed)
            {
                return;
            }

            _viewModel.StatusBrush = result.IsSuccess ? DashboardBrushes.Amber : DashboardBrushes.OrangeRed;
            _viewModel.StatusText = result.Message;
            _viewModel.StatusToolTip = result.Message;
            if (result.IsSuccess && !result.RequiresRestart)
            {
                await Task.Delay(800, _shutdown.Token);
                await Task.Run(() => ReadAndDispatch(_shutdown.Token), _shutdown.Token);
            }
            else if (result.IsSuccess)
            {
                _viewModel.IsPawnIoPromptVisible = false;
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            // Window shutdown cancels the in-flight installer flow.
        }
        catch (Exception ex)
        {
            _viewModel.StatusBrush = DashboardBrushes.OrangeRed;
            _viewModel.StatusText = Localization.PawnIoInstallFailed(ex.Message);
            _viewModel.StatusToolTip = _viewModel.StatusText;
        }
        finally
        {
            if (!_isClosed)
            {
                _viewModel.IsPawnIoInstallRunning = false;
                _viewModel.PawnIoInstallButtonText = Localization.Resource("Ui_InstallPawnIo");
            }
        }
    }

    private void UpdatePawnIoDownloadProgress(DownloadProgressInfo value)
    {
        double progress = Math.Clamp(value.Percent, 0, 100);
        if (progress >= 100)
        {
            _viewModel.PawnIoInstallButtonText = Localization.PawnIoInstallInstalling;
            _viewModel.StatusText = Localization.PawnIoInstallInstalling;
            return;
        }

        string progressText = value.TotalBytes > 0
            ? Localization.PawnIoInstallDownloadingProgress(progress)
            : Localization.PawnIoInstallDownloading;
        _viewModel.PawnIoInstallButtonText = progressText;
        _viewModel.StatusText = $"{progressText}\n{FormatDownloadSpeed(value.BytesPerSecond)}";
    }

    private void WebsiteLink_Click(object? sender, RoutedEventArgs e) => OpenUri("https://remoooo.com");

    private void GithubLink_Click(object? sender, RoutedEventArgs e) => OpenUri("https://github.com/Remyuu/RemoSystemProfiler");

    private async void CheckUpdates_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.IsUpdateCheckRunning)
        {
            return;
        }

        _viewModel.IsUpdateCheckRunning = true;
        _viewModel.IsOpenReleaseVisible = false;
        _viewModel.IsInstallUpdateVisible = false;
        _viewModel.IsUpdateProgressVisible = false;
        _viewModel.UpdateProgressValue = 0;
        _viewModel.UpdateProgressText = "0%";
        _viewModel.IsReleaseNotesVisible = false;
        _viewModel.ReleaseNotesText = string.Empty;
        _viewModel.UpdateStatusText = Localization.UpdateChecking;
        _pendingUpdateAsset = null;

        GitHubReleaseCheckResult result = await GitHubReleaseChecker.CheckLatestAsync(_viewModel.CurrentVersion, _shutdown.Token);
        if (_isClosed)
        {
            return;
        }

        _viewModel.IsUpdateCheckRunning = false;
        if (!result.IsSuccess)
        {
            _viewModel.UpdateStatusText = Localization.UpdateCheckFailed(result.ErrorMessage ?? "Unknown error");
            return;
        }

        if (!result.HasRelease || result.Release is null)
        {
            _viewModel.UpdateStatusText = Localization.UpdateNoReleases;
            return;
        }

        _viewModel.LatestReleaseUrl = result.Release.Url;
        _viewModel.ReleaseNotesText = result.Release.Notes;
        _viewModel.IsReleaseNotesVisible = true;
        _viewModel.IsOpenReleaseVisible = true;
        _pendingUpdateAsset = result.IsUpdateAvailable ? result.Release.DownloadAsset : null;
        _viewModel.IsInstallUpdateVisible = _pendingUpdateAsset is not null;
        _viewModel.UpdateStatusText = result.IsUpdateAvailable && _pendingUpdateAsset is null
            ? Localization.UpdateNoDownloadAsset
            : result.IsUpdateAvailable
            ? Localization.UpdateAvailable(result.Release.Version)
            : Localization.UpdateAlreadyLatest(_viewModel.VersionText);
    }

    private void OpenLatestRelease_Click(object? sender, RoutedEventArgs e) => OpenUri(_viewModel.LatestReleaseUrl);

    private async void InstallUpdate_Click(object? sender, RoutedEventArgs e)
    {
        if (_pendingUpdateAsset is null || _viewModel.IsUpdateInstallRunning)
        {
            return;
        }

        _viewModel.IsUpdateInstallRunning = true;
        _viewModel.IsUpdateProgressVisible = true;
        _viewModel.UpdateProgressValue = 0;
        _viewModel.UpdateProgressText = "0%";
        _viewModel.UpdateStatusText = Localization.UpdateDownloading;

        try
        {
            Progress<DownloadProgressInfo> progress = new(UpdateDownloadProgress);
            UpdateInstallResult result = await AppUpdater.DownloadAndStartUpdateAsync(_pendingUpdateAsset, progress, _shutdown.Token);
            if (_isClosed)
            {
                return;
            }

            _viewModel.UpdateStatusText = Localization.UpdatePreparing;
            _viewModel.UpdateStatusText = result.Message;
            _viewModel.IsUpdateInstallRunning = false;
            _viewModel.IsUpdateProgressVisible = result.IsSuccess && result.ShouldCloseApplication;
            if (result.IsSuccess && result.ShouldCloseApplication)
            {
                Close();
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            // Closing the app already cancels in-flight update work.
        }
        catch (Exception ex)
        {
            _viewModel.IsUpdateInstallRunning = false;
            _viewModel.IsUpdateProgressVisible = false;
            _viewModel.UpdateStatusText = Localization.UpdateInstallFailed(ex.Message);
        }
    }

    private void UpdateDownloadProgress(DownloadProgressInfo value)
    {
        double progress = Math.Clamp(value.Percent, 0, 100);
        _viewModel.UpdateProgressValue = progress;
        _viewModel.UpdateProgressText = $"{progress:0}% · {FormatDownloadSpeed(value.BytesPerSecond)}";
        _viewModel.UpdateStatusText = progress >= 100
            ? Localization.UpdatePreparing
            : Localization.UpdateDownloading;
    }

    private static string FormatDownloadSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond <= 0)
        {
            return "-- MB/s";
        }

        string[] units = ["B/s", "KB/s", "MB/s", "GB/s"];
        double value = bytesPerSecond;
        int unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{value:0} {units[unitIndex]}"
            : $"{value:0.0} {units[unitIndex]}";
    }

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

        SaveDashboardSettings();
        _isClosed = true;
        _shutdown.Cancel();
        CancelCpuBenchmark();
        StopSidebarWidthAnimation();
        StopBenchmarkCurtainAnimation();
        Closed -= OnClosed;
        _backend.Dispose();
        _benchmarkApi.Dispose();
        _shutdown.Dispose();
    }

    private sealed class BenchmarkSensorAccumulator
    {
        private readonly long _startedAt = Stopwatch.GetTimestamp();
        private readonly List<BenchmarkTelemetrySample> _samples = [];
        private double _frequencyTotal;
        private int _frequencySamples;
        private float _maxTemperature = float.MinValue;
        private string? _powerThermalText;

        public BenchmarkTelemetrySample? Add(CpuDeviceReading cpu)
        {
            double elapsedSeconds = Math.Max(0, (Stopwatch.GetTimestamp() - _startedAt) / (double)Stopwatch.Frequency);
            double? frequencyGhz = null;
            if (cpu.ClockMHz > 0)
            {
                _frequencyTotal += cpu.ClockMHz;
                _frequencySamples++;
                frequencyGhz = cpu.ClockMHz / 1000d;
            }

            double? maxTemperature = null;
            foreach (MetricReading sensor in cpu.TemperatureSensors)
            {
                _maxTemperature = Math.Max(_maxTemperature, sensor.Value);
                maxTemperature = maxTemperature is null
                    ? sensor.Value
                    : Math.Max(maxTemperature.Value, sensor.Value);
            }

            _powerThermalText ??= FindPowerThermalText(cpu);
            BenchmarkTelemetrySample sample = new()
            {
                ElapsedSeconds = elapsedSeconds,
                CpuLoadPercent = Math.Clamp(cpu.AverageLoadPercent, 0, 100),
                CpuMaxTemperatureC = maxTemperature,
                CpuPackagePowerW = cpu.PackagePower?.Value,
                CpuClockGhz = frequencyGhz
            };
            _samples.Add(sample);
            return sample;
        }

        public BenchmarkSensorSummary Summarize()
        {
            double averageFrequencyGhzValue = _frequencySamples == 0
                ? 0
                : _frequencyTotal / _frequencySamples / 1000d;
            double? averageFrequencyGhz = _frequencySamples == 0 ? null : averageFrequencyGhzValue;
            double? maxTemperatureC = _maxTemperature <= float.MinValue
                ? null
                : _maxTemperature;
            string frequencyText = _frequencySamples == 0
                ? "--"
                : $"{averageFrequencyGhzValue:0.00} GHz";
            string temperatureText = _maxTemperature <= float.MinValue
                ? "--"
                : MetricFormatter.FormatTemperature(_maxTemperature);
            return new BenchmarkSensorSummary(
                frequencyText,
                temperatureText,
                _powerThermalText ?? Localization.NotReported,
                averageFrequencyGhz,
                maxTemperatureC,
                _samples.ToArray());
        }

        private static string? FindPowerThermalText(CpuDeviceReading cpu)
        {
            MetricReading? limitSensor = cpu.PowerSensors
                .Concat(cpu.TemperatureSensors)
                .Concat(cpu.ClockSensors)
                .FirstOrDefault(IsLimitOrThrottlingSensor);
            if (limitSensor is not null)
            {
                return $"{limitSensor.Name}: {limitSensor.ValueText}";
            }

            return cpu.PackagePower is null
                ? null
                : $"{cpu.PackagePower.ValueText} / {Localization.NotReported}";
        }

        private static bool IsLimitOrThrottlingSensor(MetricReading sensor)
        {
            return sensor.Name.Contains("Throttle", StringComparison.OrdinalIgnoreCase)
                || sensor.Name.Contains("Throttling", StringComparison.OrdinalIgnoreCase)
                || sensor.Name.Contains("Power Limit", StringComparison.OrdinalIgnoreCase)
                || sensor.Name.Contains("Thermal Limit", StringComparison.OrdinalIgnoreCase)
                || sensor.Name.Contains("PROCHOT", StringComparison.OrdinalIgnoreCase);
        }
    }

    private readonly record struct BenchmarkSensorSummary(
        string AverageFrequencyText,
        string MaxTemperatureText,
        string PowerThermalText,
        double? AverageFrequencyGhz,
        double? MaxTemperatureC,
        IReadOnlyList<BenchmarkTelemetrySample> TelemetrySamples);

    private sealed record PressVisualState(ITransform? RenderTransform, RelativePoint RenderTransformOrigin);
}
