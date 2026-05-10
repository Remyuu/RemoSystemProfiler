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

public sealed class MetricItemViewModel : ObservableDashboardItem
{
    private string _label = string.Empty;
    private string _valueText = "--";
    private double _gaugeValue;

    public MetricItemViewModel(MetricReading reading)
    {
        Key = MetricKey(reading);
        Update(reading);
    }

    public string Key { get; }

    public string Label
    {
        get => _label;
        private set => SetProperty(ref _label, value);
    }

    public string ValueText
    {
        get => _valueText;
        private set => SetProperty(ref _valueText, value);
    }

    public double GaugeValue
    {
        get => _gaugeValue;
        private set => SetProperty(ref _gaugeValue, value);
    }

    public void Update(MetricReading reading)
    {
        Label = reading.Label;
        ValueText = reading.ValueText;
        GaugeValue = reading.GaugeValue;
    }

    public static string MetricKey(MetricReading reading) => $"{reading.Kind}:{reading.Name}";
}

public sealed record SensorGroupReading(
    string Key,
    string Title,
    IReadOnlyList<MetricReading> Metrics,
    bool IsExpandedByDefault);

public sealed class SensorGroupViewModel : ObservableDashboardItem
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

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string SummaryOneLabel
    {
        get => _summaryOneLabel;
        private set => SetProperty(ref _summaryOneLabel, value);
    }

    public string SummaryOneText
    {
        get => _summaryOneText;
        private set => SetProperty(ref _summaryOneText, value);
    }

    public string SummaryTwoLabel
    {
        get => _summaryTwoLabel;
        private set => SetProperty(ref _summaryTwoLabel, value);
    }

    public string SummaryTwoText
    {
        get => _summaryTwoText;
        private set => SetProperty(ref _summaryTwoText, value);
    }

    public string SummaryThreeLabel
    {
        get => _summaryThreeLabel;
        private set => SetProperty(ref _summaryThreeLabel, value);
    }

    public string SummaryThreeText
    {
        get => _summaryThreeText;
        private set => SetProperty(ref _summaryThreeText, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public void Update(SensorGroupReading reading)
    {
        Title = reading.Title;
        StatusText = DashboardStatus.SensorGroupStatus(reading.Metrics);
        ApplySummary(reading);
        DashboardCollection.Sync(
            Metrics,
            reading.Metrics,
            item => item.Key,
            MetricItemViewModel.MetricKey,
            metric => new MetricItemViewModel(metric),
            (item, metric) => item.Update(metric));
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

public sealed class OverviewItemViewModel : ObservableDashboardItem
{
    private const int MaxHistorySeconds = 10;
    private const double SparklineWidth = 60;
    private const double SparklineHeight = 32;

    private readonly Queue<double> _history = new();
    private string _title = string.Empty;
    private string _primaryText = "--";
    private string _secondaryText = "--";
    private string _detailText = "--";
    private double _gaugeValue;
    private SolidColorBrush _accentBrush = null!;
    private PointCollection _sparklinePoints = [];

    public OverviewItemViewModel(OverviewReading reading)
    {
        Key = reading.Key;
        _accentBrush = reading.AccentBrush;
        Update(reading);
    }

    public string Key { get; }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string PrimaryText
    {
        get => _primaryText;
        private set => SetProperty(ref _primaryText, value);
    }

    public string SecondaryText
    {
        get => _secondaryText;
        private set => SetProperty(ref _secondaryText, value);
    }

    public string DetailText
    {
        get => _detailText;
        private set => SetProperty(ref _detailText, value);
    }

    public double GaugeValue
    {
        get => _gaugeValue;
        private set => SetProperty(ref _gaugeValue, value);
    }

    public SolidColorBrush AccentBrush
    {
        get => _accentBrush;
        private set => SetProperty(ref _accentBrush, value);
    }

    public PointCollection SparklinePoints
    {
        get => _sparklinePoints;
        private set => SetProperty(ref _sparklinePoints, value);
    }

    public void Update(OverviewReading reading)
    {
        Title = reading.Title;
        PrimaryText = reading.PrimaryText;
        SecondaryText = reading.SecondaryText;
        DetailText = reading.DetailText;
        GaugeValue = Math.Clamp(reading.GaugeValue, 0, 100);
        AccentBrush = reading.AccentBrush;

        _history.Enqueue(GaugeValue);
        while (_history.Count > MaxHistorySeconds)
        {
            _history.Dequeue();
        }

        SparklinePoints = BuildSparkline(_history);
    }

    private static PointCollection BuildSparkline(IReadOnlyCollection<double> history)
    {
        PointCollection points = [];
        if (history.Count == 0)
        {
            return points;
        }

        double step = history.Count == 1 ? SparklineWidth : SparklineWidth / (history.Count - 1);
        int index = 0;
        foreach (double value in history)
        {
            double x = index * step;
            double y = SparklineHeight - (Math.Clamp(value, 0, 100) / 100d * SparklineHeight);
            points.Add(new Point(x, y));
            index++;
        }

        return points;
    }
}

public sealed class CoreItemViewModel : ObservableDashboardItem
{
    private const int MaxHistorySeconds = 10;
    private const double SparklineWidth = 120;
    private const double SparklineHeight = 56;

    private readonly Queue<double> _history = new();
    private string _name = string.Empty;
    private int _loadPercent;
    private string _temperatureText = "--";
    private string _loadText = "--";
    private string _powerText = "--";
    private SolidColorBrush _loadBrush = new(Colors.DeepSkyBlue);
    private PointCollection _sparklinePoints = [];

    public CoreItemViewModel(CoreReading reading)
    {
        Key = reading.Index;
        Update(reading);
    }

    public int Key { get; }

    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    public int LoadPercent
    {
        get => _loadPercent;
        private set => SetProperty(ref _loadPercent, value);
    }

    public string TemperatureText
    {
        get => _temperatureText;
        private set => SetProperty(ref _temperatureText, value);
    }

    public string LoadText
    {
        get => _loadText;
        private set => SetProperty(ref _loadText, value);
    }

    public string PowerText
    {
        get => _powerText;
        private set => SetProperty(ref _powerText, value);
    }

    public SolidColorBrush LoadBrush
    {
        get => _loadBrush;
        private set => SetProperty(ref _loadBrush, value);
    }

    public PointCollection SparklinePoints
    {
        get => _sparklinePoints;
        private set => SetProperty(ref _sparklinePoints, value);
    }

    public void Update(CoreReading reading)
    {
        Name = reading.Name;
        LoadPercent = reading.LoadPercent;
        TemperatureText = reading.TemperatureText;
        LoadText = reading.LoadText;
        PowerText = reading.PowerText;
        LoadBrush = BuildLoadBrush(reading.LoadPercent);

        _history.Enqueue(Math.Clamp(reading.LoadPercent, 0, 100));
        while (_history.Count > MaxHistorySeconds)
        {
            _history.Dequeue();
        }

        SparklinePoints = BuildSparkline(_history);
    }

    private static PointCollection BuildSparkline(IReadOnlyCollection<double> history)
    {
        PointCollection points = [];
        if (history.Count == 0)
        {
            return points;
        }

        double step = history.Count == 1 ? SparklineWidth : SparklineWidth / (history.Count - 1);
        int index = 0;
        foreach (double value in history)
        {
            points.Add(new Point(index * step, SparklineHeight - Math.Clamp(value, 0, 100) / 100d * SparklineHeight));
            index++;
        }

        return points;
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

public sealed class GpuDeviceViewModel : ObservableDashboardItem
{
    private string _name = string.Empty;
    private string _sensorCountText = "--";
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

    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    public string SensorCountText
    {
        get => _sensorCountText;
        private set => SetProperty(ref _sensorCountText, value);
    }

    public string LoadText
    {
        get => _loadText;
        private set => SetProperty(ref _loadText, value);
    }

    public string TemperatureText
    {
        get => _temperatureText;
        private set => SetProperty(ref _temperatureText, value);
    }

    public string PowerText
    {
        get => _powerText;
        private set => SetProperty(ref _powerText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public void Update(GpuDeviceReading reading)
    {
        Name = reading.Name;
        SensorCountText = reading.SensorCountText;
        LoadText = reading.LoadText;
        TemperatureText = reading.TemperatureText;
        PowerText = reading.PowerText;
        StatusText = DashboardStatus.DeviceStatus(
            reading.TemperatureSensors.Concat(reading.LoadSensors),
            85,
            75);
        DashboardCollection.Sync(
            PowerSensors,
            reading.PowerSensors,
            item => item.Key,
            MetricItemViewModel.MetricKey,
            reading => new MetricItemViewModel(reading),
            (item, reading) => item.Update(reading));
        DashboardCollection.Sync(
            TemperatureSensors,
            reading.TemperatureSensors,
            item => item.Key,
            MetricItemViewModel.MetricKey,
            reading => new MetricItemViewModel(reading),
            (item, reading) => item.Update(reading));
        DashboardCollection.Sync(
            MemorySensors,
            reading.MemorySensors,
            item => item.Key,
            MetricItemViewModel.MetricKey,
            reading => new MetricItemViewModel(reading),
            (item, reading) => item.Update(reading));
    }
}

public sealed class StorageDeviceViewModel : ObservableDashboardItem
{
    private string _name = string.Empty;
    private string _sensorCountText = "--";
    private string _usageText = "--";
    private string _temperatureText = "--";
    private string _readWriteText = "--";
    private string _statusText = "--";
    private bool _isExpanded;

    public StorageDeviceViewModel(StorageDeviceReading reading)
    {
        Key = reading.Name;
        Update(reading);
    }

    public string Key { get; }

    public ObservableCollection<MetricItemViewModel> Metrics { get; } = [];

    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    public string SensorCountText
    {
        get => _sensorCountText;
        private set => SetProperty(ref _sensorCountText, value);
    }

    public string UsageText
    {
        get => _usageText;
        private set => SetProperty(ref _usageText, value);
    }

    public string TemperatureText
    {
        get => _temperatureText;
        private set => SetProperty(ref _temperatureText, value);
    }

    public string ReadWriteText
    {
        get => _readWriteText;
        private set => SetProperty(ref _readWriteText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public void Update(StorageDeviceReading reading)
    {
        Name = reading.Name;
        SensorCountText = reading.SensorCountText;
        UsageText = reading.UsageText;
        TemperatureText = reading.TemperatureText;
        ReadWriteText = reading.ReadWriteText;
        StatusText = DashboardStatus.DeviceStatus(
            reading.TemperatureSensors.Concat(reading.UsageSensors),
            60,
            50);
        DashboardCollection.Sync(
            Metrics,
            reading.Metrics,
            item => item.Key,
            MetricItemViewModel.MetricKey,
            reading => new MetricItemViewModel(reading),
            (item, reading) => item.Update(reading));
    }
}

public static class DashboardCollection
{
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
