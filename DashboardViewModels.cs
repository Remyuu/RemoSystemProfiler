using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace RemoSystemProfiler;

public abstract class ObservableDashboardItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public interface IDashboardItem<in TData, out TKey>
    where TKey : notnull
{
    TKey Key { get; }

    void Update(TData data);
}

public sealed class MetricItemViewModel : ObservableDashboardItem, IDashboardItem<MetricReading, string>
{
    private string _label = string.Empty;
    private string _valueText = "--";

    public MetricItemViewModel(MetricReading reading)
    {
        Key = MetricKey(reading);
        Update(reading);
    }

    public string Key { get; }

    public string Label { get => _label; private set => SetProperty(ref _label, value); }

    public string ValueText { get => _valueText; private set => SetProperty(ref _valueText, value); }

    public void Update(MetricReading reading)
    {
        Label = reading.Label;
        ValueText = reading.ValueText;
    }

    public static string MetricKey(MetricReading reading) => $"{reading.Kind}:{reading.Name}";
}

public sealed record SensorGroupReading(
    string Key,
    string Title,
    IReadOnlyList<MetricReading> Metrics,
    bool IsExpandedByDefault);

public sealed class SensorGroupViewModel : ObservableDashboardItem, IDashboardItem<SensorGroupReading, string>
{
    private string _title = string.Empty;
    private string _statusText = "--";
    private string _summaryOneLabel = "--";
    private string _summaryOneText = "--";
    private string _summaryTwoLabel = "--";
    private string _summaryTwoText = "--";
    private string _summaryThreeLabel = "--";
    private string _summaryThreeText = "--";
    private bool _isExpanded;

    public SensorGroupViewModel(SensorGroupReading reading)
    {
        Key = reading.Key;
        _isExpanded = reading.IsExpandedByDefault;
        Update(reading);
    }

    public string Key { get; }

    public ObservableCollection<MetricItemViewModel> Metrics { get; } = [];

    public string Title { get => _title; private set => SetProperty(ref _title, value); }

    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

    public string SummaryOneLabel { get => _summaryOneLabel; private set => SetProperty(ref _summaryOneLabel, value); }

    public string SummaryOneText { get => _summaryOneText; private set => SetProperty(ref _summaryOneText, value); }

    public string SummaryTwoLabel { get => _summaryTwoLabel; private set => SetProperty(ref _summaryTwoLabel, value); }

    public string SummaryTwoText { get => _summaryTwoText; private set => SetProperty(ref _summaryTwoText, value); }

    public string SummaryThreeLabel { get => _summaryThreeLabel; private set => SetProperty(ref _summaryThreeLabel, value); }

    public string SummaryThreeText { get => _summaryThreeText; private set => SetProperty(ref _summaryThreeText, value); }

    public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }

    public void Update(SensorGroupReading reading)
    {
        Title = reading.Title;
        StatusText = DashboardStatus.SensorGroupStatus(reading.Metrics);
        ApplySummary(reading);
        DashboardCollection.SyncItems(
            Metrics,
            reading.Metrics,
            MetricItemViewModel.MetricKey,
            metric => new MetricItemViewModel(metric));
    }

    private void ApplySummary(SensorGroupReading reading)
    {
        MetricReading[] metrics = reading.Metrics.ToArray();
        if (metrics.Length == 0)
        {
            SetSummary("--", "--", "--", "--", "Sensors", "0");
            return;
        }

        switch (reading.Key)
        {
            case "temperature":
                SetSummary(
                    "Peak",
                    MetricFormatter.FormatTemperature(metrics.Max(metric => metric.Value)),
                    "Avg",
                    MetricFormatter.FormatTemperature(metrics.Average(metric => metric.Value)),
                    "Sensors",
                    metrics.Length.ToString());
                break;
            case "power":
                MetricReading? package = metrics.FirstOrDefault(metric => metric.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
                    ?? metrics.FirstOrDefault(metric => metric.Name.Contains("Total", StringComparison.OrdinalIgnoreCase));
                SetSummary(
                    package is null ? "Total" : "Package",
                    package?.ValueText ?? MetricFormatter.FormatPower(metrics.Sum(metric => metric.Value)),
                    "Peak rail",
                    MetricFormatter.FormatPower(metrics.Max(metric => metric.Value)),
                    "Sensors",
                    metrics.Length.ToString());
                break;
            case "clock":
                SetSummary(
                    "Avg",
                    FormatClock(metrics.Average(metric => metric.Value)),
                    "Max",
                    FormatClock(metrics.Max(metric => metric.Value)),
                    "Sensors",
                    metrics.Length.ToString());
                break;
            case "voltage":
                SetSummary(
                    "Max",
                    FormatVoltage(metrics.Max(metric => metric.Value)),
                    "Avg",
                    FormatVoltage(metrics.Average(metric => metric.Value)),
                    "Sensors",
                    metrics.Length.ToString());
                break;
            default:
                SetSummary("Primary", metrics[0].ValueText, "Sensors", metrics.Length.ToString(), "--", "--");
                break;
        }
    }

    private void SetSummary(
        string oneLabel,
        string oneText,
        string twoLabel,
        string twoText,
        string threeLabel,
        string threeText)
    {
        SummaryOneLabel = oneLabel;
        SummaryOneText = oneText;
        SummaryTwoLabel = twoLabel;
        SummaryTwoText = twoText;
        SummaryThreeLabel = threeLabel;
        SummaryThreeText = threeText;
    }

    private static string FormatClock(double valueMHz) => valueMHz >= 1000
        ? $"{valueMHz / 1000d:0.00} GHz"
        : $"{valueMHz:0} MHz";

    private static string FormatVoltage(double value) => $"{value:0.###} V";
}

internal static class DashboardStatus
{
    public static string SensorGroupStatus(IReadOnlyList<MetricReading> metrics)
    {
        if (metrics.Count == 0)
        {
            return "no sensors";
        }

        string thermal = ThermalStatus(metrics, 85, 75);
        if (!string.IsNullOrEmpty(thermal))
        {
            return thermal;
        }

        int warnings = metrics.Count(IsGaugeWarning);
        return warnings > 0 ? $"{warnings} warning" : $"{metrics.Count} sensors";
    }

    public static string DeviceStatus(IEnumerable<MetricReading> metrics, float hotThreshold, float warmThreshold)
    {
        MetricReading[] readings = metrics.ToArray();
        if (readings.Length == 0)
        {
            return "no sensors";
        }

        string thermal = ThermalStatus(readings, hotThreshold, warmThreshold);
        if (!string.IsNullOrEmpty(thermal))
        {
            return thermal;
        }

        int warnings = readings.Count(IsGaugeWarning);
        return warnings > 0 ? $"{warnings} warning" : "normal";
    }

    private static string ThermalStatus(IEnumerable<MetricReading> metrics, float hotThreshold, float warmThreshold)
    {
        MetricReading[] temperatures = metrics
            .Where(metric => metric.Kind.Equals("Temperature", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        int hot = temperatures.Count(metric => metric.Value >= hotThreshold);
        if (hot > 0)
        {
            return $"{hot} hot";
        }

        int warm = temperatures.Count(metric => metric.Value >= warmThreshold);
        return warm > 0 ? $"{warm} warm" : string.Empty;
    }

    private static bool IsGaugeWarning(MetricReading metric)
    {
        if (!metric.Kind.Equals("Load", StringComparison.OrdinalIgnoreCase)
            && !metric.Kind.Equals("Control", StringComparison.OrdinalIgnoreCase)
            && !metric.Kind.Equals("Level", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return metric.GaugeValue >= 95;
    }
}

public sealed record OverviewReading(
    string Key,
    string Title,
    string PrimaryText,
    string SecondaryText,
    string DetailText,
    double GaugeValue,
    SolidColorBrush AccentBrush);

internal sealed class SparklineHistory(double width, double height)
{
    private readonly Queue<double> _values = new();

    public PointCollection Add(double value)
    {
        _values.Enqueue(Math.Clamp(value, 0, 100));
        while (_values.Count > ChartHistorySettings.MaxSamples)
        {
            _values.Dequeue();
        }

        PointCollection points = [];
        double step = _values.Count == 1 ? width : width / (_values.Count - 1);
        int index = 0;
        foreach (double sample in _values)
        {
            points.Add(new Point(index * step, height - sample / 100d * height));
            index++;
        }

        return points;
    }
}

public static class ChartHistorySettings
{
    private static int _displaySeconds = 10;
    private static double _sampleIntervalSeconds = 1;

    public static int DisplaySeconds { get => _displaySeconds; set => _displaySeconds = Math.Clamp(value, 10, 300); }

    public static double SampleIntervalSeconds { get => _sampleIntervalSeconds; set => _sampleIntervalSeconds = Math.Clamp(value, 0.5, 10); }

    public static int MaxSamples => Math.Max(2, (int)Math.Ceiling(DisplaySeconds / SampleIntervalSeconds));
}

public sealed class OverviewItemViewModel : ObservableDashboardItem, IDashboardItem<OverviewReading, string>
{
    private readonly SparklineHistory _history = new(60, 32);
    private string _title = string.Empty;
    private string _primaryText = "--";
    private string _secondaryText = "--";
    private string _detailText = "--";
    private SolidColorBrush _accentBrush = null!;
    private PointCollection _sparklinePoints = [];

    public OverviewItemViewModel(OverviewReading reading)
    {
        Key = reading.Key;
        _accentBrush = reading.AccentBrush;
        Update(reading);
    }

    public string Key { get; }

    public string Title { get => _title; private set => SetProperty(ref _title, value); }

    public string PrimaryText { get => _primaryText; private set => SetProperty(ref _primaryText, value); }

    public string SecondaryText { get => _secondaryText; private set => SetProperty(ref _secondaryText, value); }

    public string DetailText { get => _detailText; private set => SetProperty(ref _detailText, value); }

    public SolidColorBrush AccentBrush { get => _accentBrush; private set => SetProperty(ref _accentBrush, value); }

    public PointCollection SparklinePoints { get => _sparklinePoints; private set => SetProperty(ref _sparklinePoints, value); }

    public void Update(OverviewReading reading)
    {
        Title = reading.Title;
        PrimaryText = reading.PrimaryText;
        SecondaryText = reading.SecondaryText;
        DetailText = reading.DetailText;
        AccentBrush = reading.AccentBrush;

        SparklinePoints = _history.Add(reading.GaugeValue);
    }
}

public sealed class CoreItemViewModel : ObservableDashboardItem, IDashboardItem<CoreReading, int>
{
    private readonly SparklineHistory _history = new(120, 56);
    private string _loadText = "--";
    private SolidColorBrush _loadBrush = new(Colors.DeepSkyBlue);
    private PointCollection _sparklinePoints = [];

    public CoreItemViewModel(CoreReading reading)
    {
        Key = reading.Index;
        Update(reading);
    }

    public int Key { get; }

    public string LoadText { get => _loadText; private set => SetProperty(ref _loadText, value); }

    public SolidColorBrush LoadBrush { get => _loadBrush; private set => SetProperty(ref _loadBrush, value); }

    public PointCollection SparklinePoints { get => _sparklinePoints; private set => SetProperty(ref _sparklinePoints, value); }

    public void Update(CoreReading reading)
    {
        LoadText = reading.LoadText;
        LoadBrush = BuildLoadBrush(reading.LoadPercent);

        SparklinePoints = _history.Add(reading.LoadPercent);
    }

    private static SolidColorBrush BuildLoadBrush(int loadPercent)
    {
        double t = Math.Clamp(loadPercent, 0, 100) / 100d;
        byte red = (byte)Math.Round(Lerp(0x37, 0xF9, t));
        byte green = (byte)Math.Round(Lerp(0xB7, 0x70, t));
        byte blue = (byte)Math.Round(Lerp(0xE8, 0x66, t));
        return new SolidColorBrush(ColorHelper.FromArgb(255, red, green, blue));
    }

    private static double Lerp(double start, double end, double amount) => start + ((end - start) * amount);
}

public sealed class GpuDeviceViewModel : ObservableDashboardItem, IDashboardItem<GpuDeviceReading, string>
{
    private string _name = string.Empty;
    private string _loadText = "--";
    private string _temperatureText = "--";
    private string _powerText = "--";
    private string _statusText = "--";
    private bool _isExpanded;

    public GpuDeviceViewModel(GpuDeviceReading reading)
    {
        Key = reading.Name;
        Update(reading);
    }

    public string Key { get; }

    public ObservableCollection<MetricItemViewModel> PowerSensors { get; } = [];

    public ObservableCollection<MetricItemViewModel> TemperatureSensors { get; } = [];

    public ObservableCollection<MetricItemViewModel> MemorySensors { get; } = [];

    public string Name { get => _name; private set => SetProperty(ref _name, value); }

    public string LoadText { get => _loadText; private set => SetProperty(ref _loadText, value); }

    public string TemperatureText { get => _temperatureText; private set => SetProperty(ref _temperatureText, value); }

    public string PowerText { get => _powerText; private set => SetProperty(ref _powerText, value); }

    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

    public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }

    public void Update(GpuDeviceReading reading)
    {
        Name = reading.Name;
        LoadText = reading.LoadText;
        TemperatureText = reading.TemperatureText;
        PowerText = reading.PowerText;
        StatusText = DashboardStatus.DeviceStatus(
            reading.TemperatureSensors.Concat(reading.LoadSensors),
            85,
            75);
        DashboardCollection.SyncItems(
            PowerSensors,
            reading.PowerSensors,
            MetricItemViewModel.MetricKey,
            reading => new MetricItemViewModel(reading));
        DashboardCollection.SyncItems(
            TemperatureSensors,
            reading.TemperatureSensors,
            MetricItemViewModel.MetricKey,
            reading => new MetricItemViewModel(reading));
        DashboardCollection.SyncItems(
            MemorySensors,
            reading.MemorySensors,
            MetricItemViewModel.MetricKey,
            reading => new MetricItemViewModel(reading));
    }
}

public sealed class StorageDeviceViewModel : ObservableDashboardItem, IDashboardItem<StorageDeviceReading, string>
{
    private string _name = string.Empty;
    private string _usageText = "--";
    private string _statusText = "--";
    private bool _isExpanded;

    public StorageDeviceViewModel(StorageDeviceReading reading)
    {
        Key = reading.Name;
        Update(reading);
    }

    public string Key { get; }

    public ObservableCollection<MetricItemViewModel> Metrics { get; } = [];

    public string Name { get => _name; private set => SetProperty(ref _name, value); }

    public string UsageText { get => _usageText; private set => SetProperty(ref _usageText, value); }

    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

    public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }

    public void Update(StorageDeviceReading reading)
    {
        Name = reading.Name;
        UsageText = reading.UsageText;
        StatusText = DashboardStatus.DeviceStatus(
            reading.TemperatureSensors.Concat(reading.UsageSensors),
            60,
            50);
        DashboardCollection.SyncItems(
            Metrics,
            reading.Metrics,
            MetricItemViewModel.MetricKey,
            reading => new MetricItemViewModel(reading));
    }
}

public static class DashboardCollection
{
    public static void SyncItems<TView, TData, TKey>(
        ObservableCollection<TView> collection,
        IEnumerable<TData> data,
        Func<TData, TKey> dataKey,
        Func<TData, TView> create)
        where TView : IDashboardItem<TData, TKey>
        where TKey : notnull
    {
        Sync(collection, data, item => item.Key, dataKey, create, (item, reading) => item.Update(reading));
    }

    public static void Sync<TView, TData, TKey>(
        ObservableCollection<TView> collection,
        IEnumerable<TData> data,
        Func<TView, TKey> viewKey,
        Func<TData, TKey> dataKey,
        Func<TData, TView> create,
        Action<TView, TData> update)
        where TKey : notnull
    {
        TData[] incoming = data.ToArray();
        bool sameShape = collection.Count == incoming.Length;

        if (sameShape)
        {
            for (int i = 0; i < incoming.Length; i++)
            {
                if (!EqualityComparer<TKey>.Default.Equals(viewKey(collection[i]), dataKey(incoming[i])))
                {
                    sameShape = false;
                    break;
                }
            }
        }

        if (!sameShape)
        {
            collection.Clear();
            foreach (TData item in incoming)
            {
                collection.Add(create(item));
            }

            return;
        }

        for (int i = 0; i < incoming.Length; i++)
        {
            update(collection[i], incoming[i]);
        }
    }
}
