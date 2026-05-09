using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

public sealed class CoreItemViewModel : ObservableDashboardItem
{
    private string _name = string.Empty;
    private int _loadPercent;
    private string _temperatureText = "--";
    private string _loadText = "--";
    private string _powerText = "--";

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

    public void Update(CoreReading reading)
    {
        Name = reading.Name;
        LoadPercent = reading.LoadPercent;
        TemperatureText = reading.TemperatureText;
        LoadText = reading.LoadText;
        PowerText = reading.PowerText;
    }
}

public sealed class GpuDeviceViewModel : ObservableDashboardItem
{
    private string _name = string.Empty;
    private string _sensorCountText = "--";
    private string _temperatureText = "--";
    private string _powerText = "--";

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

    public void Update(GpuDeviceReading reading)
    {
        Name = reading.Name;
        SensorCountText = reading.SensorCountText;
        TemperatureText = reading.TemperatureText;
        PowerText = reading.PowerText;
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
    private string _temperatureText = "--";
    private string _readWriteText = "--";

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

    public void Update(StorageDeviceReading reading)
    {
        Name = reading.Name;
        SensorCountText = reading.SensorCountText;
        TemperatureText = reading.TemperatureText;
        ReadWriteText = reading.ReadWriteText;
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
