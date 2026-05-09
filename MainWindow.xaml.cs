using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;

namespace RemoSystemProfiler;

public sealed partial class MainWindow : Window
{
    private readonly HardwareMonitorReader _reader = new();
    private readonly DispatcherQueueTimer _timer;
    private readonly ObservableCollection<MetricItemViewModel> _cpuTemperatureSensors = [];
    private readonly ObservableCollection<MetricItemViewModel> _cpuPowerSensors = [];
    private readonly ObservableCollection<CoreItemViewModel> _cpuCores = [];
    private readonly ObservableCollection<MetricItemViewModel> _memoryMetrics = [];
    private readonly ObservableCollection<GpuDeviceViewModel> _gpus = [];
    private readonly ObservableCollection<StorageDeviceViewModel> _storageDevices = [];
    private bool _isClosed;

    public MainWindow()
    {
        InitializeComponent();
        BindStableCollections();

        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
        ConfigureWindow();

        ThemePicker.SelectedIndex = 0;
        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
        Closed += OnClosed;

        Refresh();
        _timer.Start();
    }

    private void BindStableCollections()
    {
        CpuTemperatureList.ItemsSource = _cpuTemperatureSensors;
        CpuPowerList.ItemsSource = _cpuPowerSensors;
        CpuCoreList.ItemsSource = _cpuCores;
        MemoryMetricList.ItemsSource = _memoryMetrics;
        GpuList.ItemsSource = _gpus;
        StorageList.ItemsSource = _storageDevices;
    }

    private void ConfigureWindow()
    {
        IntPtr hwnd = WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new SizeInt32(1500, 960));
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
        }
    }

    private void Refresh()
    {
        if (_isClosed)
        {
            return;
        }

        HardwareMonitorReadResult result = _reader.Read();
        UpdatedText.Text = DateTime.Now.ToString("HH:mm:ss");

        if (!result.IsAvailable || result.Snapshot is null)
        {
            ShowUnavailable(result.Message);
            return;
        }

        ShowSnapshot(result.Snapshot);
    }

    private void ShowSnapshot(SystemSnapshot snapshot)
    {
        StatusDot.Fill = new SolidColorBrush(Colors.LimeGreen);
        StatusText.Text = $"Connected via {snapshot.Source}";
        StatusText.TextWrapping = TextWrapping.NoWrap;
        ToolTipService.SetToolTip(StatusText, null);
        UpdatedText.Text = snapshot.SampledAtText;
        HardwareSummaryText.Text = BuildHardwareSummary(snapshot);

        ShowCpu(snapshot.Cpu);
        ShowMemory(snapshot.Memory);
        ShowGpus(snapshot.Gpus);
        ShowStorage(snapshot.StorageDevices);
    }

    private void ShowCpu(CpuDeviceReading? cpu)
    {
        if (cpu is null)
        {
            CpuNameText.Text = "CPU sensors unavailable";
            CpuPackagePowerText.Text = "--";
            CpuPeakTempText.Text = "--";
            CpuLoadSummaryText.Text = "--";
            CoreCountText.Text = "--";
            ClockText.Text = "--";
            SyncMetricCollection(_cpuTemperatureSensors, Array.Empty<MetricReading>());
            SyncMetricCollection(_cpuPowerSensors, Array.Empty<MetricReading>());
            DashboardCollection.Sync(
                _cpuCores,
                Array.Empty<CoreReading>(),
                item => item.Key,
                reading => reading.Index,
                reading => new CoreItemViewModel(reading),
                (item, reading) => item.Update(reading));
            return;
        }

        CpuNameText.Text = cpu.Name;
        CpuPackagePowerText.Text = cpu.PackagePowerText;
        CpuPeakTempText.Text = cpu.MaxTemperatureText;
        CpuLoadSummaryText.Text = cpu.AverageLoadText;
        CoreCountText.Text = cpu.CoreCountText;
        ClockText.Text = cpu.ClockText;
        SyncMetricCollection(_cpuTemperatureSensors, cpu.TemperatureSensors);
        SyncMetricCollection(_cpuPowerSensors, cpu.PowerSensors);
        DashboardCollection.Sync(
            _cpuCores,
            cpu.Cores,
            item => item.Key,
            reading => reading.Index,
            reading => new CoreItemViewModel(reading),
            (item, reading) => item.Update(reading));
    }

    private void ShowMemory(MemoryDeviceReading? memory)
    {
        if (memory is null)
        {
            MemoryUsageText.Text = "--";
            MemoryTempText.Text = "--";
            MemoryCapacityText.Text = "--";
            SyncMetricCollection(_memoryMetrics, Array.Empty<MetricReading>());
            return;
        }

        MemoryUsageText.Text = memory.UsageText;
        MemoryTempText.Text = memory.TemperatureText;
        MemoryCapacityText.Text = memory.CapacityText;
        SyncMetricCollection(_memoryMetrics, memory.Metrics);
    }

    private void ShowGpus(IReadOnlyList<GpuDeviceReading> gpus)
    {
        GpuCountText.Text = gpus.Count == 1 ? "1 GPU" : $"{gpus.Count} GPUs";
        GpuPeakTempText.Text = HighestTemperature(gpus.SelectMany(gpu => gpu.TemperatureSensors));
        GpuPowerText.Text = TotalPower(gpus.SelectMany(gpu => gpu.PowerSensors));
        DashboardCollection.Sync(
            _gpus,
            gpus,
            item => item.Key,
            gpu => gpu.Name,
            gpu => new GpuDeviceViewModel(gpu),
            (item, gpu) => item.Update(gpu));
    }

    private void ShowStorage(IReadOnlyList<StorageDeviceReading> storageDevices)
    {
        StorageCountText.Text = storageDevices.Count == 1 ? "1 disk" : $"{storageDevices.Count} disks";
        StoragePeakTempText.Text = HighestTemperature(storageDevices.SelectMany(storage => storage.TemperatureSensors));
        StorageIoText.Text = storageDevices.Select(storage => storage.ReadWriteText).FirstOrDefault(text => text != "--") ?? "--";
        DashboardCollection.Sync(
            _storageDevices,
            storageDevices,
            item => item.Key,
            storage => storage.Name,
            storage => new StorageDeviceViewModel(storage),
            (item, storage) => item.Update(storage));
    }

    private void ShowUnavailable(string message)
    {
        StatusDot.Fill = new SolidColorBrush(Colors.OrangeRed);
        StatusText.Text = message;
        StatusText.TextWrapping = TextWrapping.Wrap;
        ToolTipService.SetToolTip(StatusText, message);
        HardwareSummaryText.Text = "Hardware sensors unavailable";
        UpdatedText.Text = DateTime.Now.ToString("HH:mm:ss");

        ShowCpu(null);
        ShowMemory(null);
        ShowGpus(Array.Empty<GpuDeviceReading>());
        ShowStorage(Array.Empty<StorageDeviceReading>());
    }

    private static string BuildHardwareSummary(SystemSnapshot snapshot)
    {
        string cpu = snapshot.Cpu is null ? "CPU unavailable" : "CPU online";
        string memory = snapshot.Memory is null ? "memory unavailable" : "memory online";
        return $"{cpu} | {memory} | {snapshot.Gpus.Count} GPU | {snapshot.StorageDevices.Count} storage | {snapshot.Source}";
    }

    private static void SyncMetricCollection(
        ObservableCollection<MetricItemViewModel> collection,
        IEnumerable<MetricReading> readings)
    {
        DashboardCollection.Sync(
            collection,
            readings,
            item => item.Key,
            MetricItemViewModel.MetricKey,
            reading => new MetricItemViewModel(reading),
            (item, reading) => item.Update(reading));
    }

    private static string HighestTemperature(IEnumerable<MetricReading> sensors)
    {
        MetricReading[] readings = sensors.ToArray();
        return readings.Length == 0 ? "--" : MetricFormatter.FormatTemperature(readings.Max(sensor => sensor.Value));
    }

    private static string TotalPower(IEnumerable<MetricReading> sensors)
    {
        MetricReading[] readings = sensors.ToArray();
        return readings.Length == 0 ? "--" : MetricFormatter.FormatPower(readings.Sum(sensor => sensor.Value));
    }

    private void ThemePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Root.RequestedTheme = ThemePicker.SelectedIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object e)
    {
        Refresh();
    }

    private void OnClosed(object sender, WindowEventArgs e)
    {
        _isClosed = true;
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        Closed -= OnClosed;
        _reader.Dispose();
        ((App)Application.Current).ShutdownFromMainWindow();
    }
}
