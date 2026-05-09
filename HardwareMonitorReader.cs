using System.Text.RegularExpressions;
using LibreHardwareMonitor.Hardware;

namespace RemoSystemProfiler;

public sealed class HardwareMonitorReader : IDisposable
{
    private static readonly Regex CoreNumberRegex = new(@"Core\s*#?\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
        IsStorageEnabled = true,
        IsMotherboardEnabled = true
    };

    private bool _opened;

    public HardwareMonitorReadResult Read()
    {
        try
        {
            EnsureOpen();

            foreach (IHardware hardware in _computer.Hardware)
            {
                UpdateHardwareTree(hardware);
            }

            IHardware[] hardwareTree = _computer.Hardware.SelectMany(FlattenHardware).ToArray();
            SystemSnapshot snapshot = new(
                DateTimeOffset.Now,
                "LibreHardwareMonitor",
                BuildCpu(hardwareTree.FirstOrDefault(hardware => hardware.HardwareType == HardwareType.Cpu)),
                BuildMemory(hardwareTree.FirstOrDefault(hardware => hardware.HardwareType == HardwareType.Memory)),
                hardwareTree.Where(IsGpuHardware).Select(BuildGpu).ToArray(),
                hardwareTree.Where(hardware => hardware.HardwareType == HardwareType.Storage).Select(BuildStorage).ToArray());

            if (snapshot.Cpu is null && snapshot.Memory is null && snapshot.Gpus.Count == 0 && snapshot.StorageDevices.Count == 0)
            {
                return HardwareMonitorReadResult.Unavailable("No supported hardware sensors found; try running as administrator");
            }

            return HardwareMonitorReadResult.Available(snapshot);
        }
        catch (Exception ex)
        {
            return HardwareMonitorReadResult.Unavailable($"Sensor read failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_opened)
        {
            _computer.Close();
        }
    }

    private void EnsureOpen()
    {
        if (_opened)
        {
            return;
        }

        _computer.Open();
        _opened = true;
    }

    private static void UpdateHardwareTree(IHardware hardware)
    {
        hardware.Update();
        foreach (IHardware subHardware in hardware.SubHardware)
        {
            UpdateHardwareTree(subHardware);
        }
    }

    private static IEnumerable<IHardware> FlattenHardware(IHardware hardware)
    {
        yield return hardware;
        foreach (IHardware subHardware in hardware.SubHardware)
        {
            foreach (IHardware nested in FlattenHardware(subHardware))
            {
                yield return nested;
            }
        }
    }

    private static CpuDeviceReading? BuildCpu(IHardware? cpu)
    {
        if (cpu is null)
        {
            return null;
        }

        ISensor[] sensors = ActiveSensors(cpu);
        IReadOnlyList<CoreReading> cores = BuildCoreReadings(sensors);
        float averageLoad = FindSensor(sensors, SensorType.Load, "CPU Total")?.Value
            ?? FindSensor(sensors, SensorType.Load, "Total")?.Value
            ?? Average(cores.Select(core => (float)core.LoadPercent));
        float clockMHz = Average(SensorsOfType(sensors, SensorType.Clock)
            .Where(sensor => IsCoreNamed(sensor) || sensor.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase))
            .Select(sensor => sensor.Value.GetValueOrDefault()));

        return new CpuDeviceReading(
            cpu.Name,
            cores.Count,
            averageLoad,
            clockMHz,
            BuildMetricReadings(sensors, SensorType.Temperature),
            BuildMetricReadings(sensors, SensorType.Power),
            cores);
    }

    private static MemoryDeviceReading? BuildMemory(IHardware? memory)
    {
        if (memory is null)
        {
            return null;
        }

        ISensor[] sensors = ActiveSensors(memory);
        ISensor[] physicalMemorySensors = sensors.Where(IsPhysicalMemorySensor).ToArray();
        return new MemoryDeviceReading(
            memory.Name,
            BuildMetricReadings(physicalMemorySensors, SensorType.Load),
            BuildMetricReadings(physicalMemorySensors, SensorType.Temperature),
            BuildMetricReadings(physicalMemorySensors, SensorType.Data)
                .Concat(BuildMetricReadings(physicalMemorySensors, SensorType.SmallData))
                .ToArray());
    }

    private static GpuDeviceReading BuildGpu(IHardware gpu)
    {
        ISensor[] sensors = ActiveSensors(gpu);
        return new GpuDeviceReading(
            gpu.Name,
            BuildMetricReadings(sensors, SensorType.Power),
            BuildMetricReadings(sensors, SensorType.Temperature),
            BuildMetricReadings(sensors, SensorType.Load, IsMemoryNamed)
                .Concat(BuildMetricReadings(sensors, SensorType.Data, IsMemoryNamed))
                .Concat(BuildMetricReadings(sensors, SensorType.SmallData, IsMemoryNamed))
                .Concat(BuildMetricReadings(sensors, SensorType.Temperature, IsMemoryNamed))
                .ToArray());
    }

    private static StorageDeviceReading BuildStorage(IHardware storage)
    {
        ISensor[] sensors = ActiveSensors(storage);
        return new StorageDeviceReading(
            storage.Name,
            BuildMetricReadings(sensors, SensorType.Load),
            BuildMetricReadings(sensors, SensorType.Temperature),
            BuildMetricReadings(sensors, SensorType.Throughput),
            BuildMetricReadings(sensors, SensorType.Data)
                .Concat(BuildMetricReadings(sensors, SensorType.SmallData))
                .ToArray());
    }

    private static IReadOnlyList<CoreReading> BuildCoreReadings(IReadOnlyList<ISensor> sensors)
    {
        Dictionary<int, CoreBuilder> builders = new();
        AddCoreValues(builders, SensorsOfType(sensors, SensorType.Temperature).Where(IsCoreNamed), (builder, value) => builder.TemperatureCelsius = value);
        AddCoreValues(builders, SensorsOfType(sensors, SensorType.Load).Where(IsCoreNamed), (builder, value) => builder.LoadPercent = (int)Math.Round(Math.Clamp(value, 0, 100)));
        AddCoreValues(builders, SensorsOfType(sensors, SensorType.Power).Where(IsCoreNamed), (builder, value) => builder.PowerWatts = value);

        return builders
            .OrderBy(pair => pair.Key)
            .Select(pair => new CoreReading(
                pair.Key,
                $"Core {pair.Key + 1}",
                pair.Value.TemperatureCelsius,
                pair.Value.LoadPercent ?? 0,
                pair.Value.PowerWatts))
            .ToArray();
    }

    private static void AddCoreValues(
        IDictionary<int, CoreBuilder> builders,
        IEnumerable<ISensor> sensors,
        Action<CoreBuilder, float> apply)
    {
        int fallbackIndex = 0;
        foreach (ISensor sensor in sensors.OrderBy(sensor => sensor.Index).ThenBy(sensor => sensor.Name))
        {
            int index = TryGetCoreIndex(sensor.Name) ?? fallbackIndex;
            fallbackIndex++;

            if (!builders.TryGetValue(index, out CoreBuilder? builder))
            {
                builder = new CoreBuilder();
                builders[index] = builder;
            }

            apply(builder, sensor.Value.GetValueOrDefault());
        }
    }

    private static IReadOnlyList<MetricReading> BuildMetricReadings(
        IReadOnlyList<ISensor> sensors,
        SensorType type,
        Func<ISensor, bool>? filter = null)
    {
        return SensorsOfType(sensors, type)
            .Where(sensor => filter?.Invoke(sensor) ?? true)
            .OrderBy(sensor => sensor.Index)
            .ThenBy(sensor => sensor.Name)
            .Select(ToMetricReading)
            .ToArray();
    }

    private static MetricReading ToMetricReading(ISensor sensor)
    {
        float value = sensor.Value.GetValueOrDefault();
        return new MetricReading(
            sensor.Name,
            sensor.SensorType.ToString(),
            value,
            FormatSensorValue(sensor.SensorType, value),
            GaugeValue(sensor.SensorType, value));
    }

    private static ISensor[] ActiveSensors(IHardware hardware)
    {
        return FlattenHardware(hardware)
            .SelectMany(item => item.Sensors)
            .Where(sensor => sensor.Value.HasValue)
            .ToArray();
    }

    private static IEnumerable<ISensor> SensorsOfType(IEnumerable<ISensor> sensors, SensorType type)
    {
        return sensors.Where(sensor => sensor.SensorType == type && sensor.Value.HasValue);
    }

    private static ISensor? FindSensor(IEnumerable<ISensor> sensors, SensorType type, string nameContains)
    {
        return SensorsOfType(sensors, type).FirstOrDefault(sensor =>
            sensor.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsGpuHardware(IHardware hardware)
    {
        return hardware.HardwareType is HardwareType.GpuAmd or HardwareType.GpuNvidia or HardwareType.GpuIntel;
    }

    private static bool IsCoreNamed(ISensor sensor)
    {
        return sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase)
            && !sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase)
            && !sensor.Name.Contains("Max", StringComparison.OrdinalIgnoreCase)
            && !sensor.Name.Contains("Total", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMemoryNamed(ISensor sensor)
    {
        return sensor.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase)
            || sensor.Name.Contains("VRAM", StringComparison.OrdinalIgnoreCase)
            || sensor.Name.Contains("FB", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPhysicalMemorySensor(ISensor sensor)
    {
        return !sensor.Name.Contains("Virtual", StringComparison.OrdinalIgnoreCase)
            && !sensor.Name.Contains("Page File", StringComparison.OrdinalIgnoreCase)
            && !sensor.Name.Contains("Swap", StringComparison.OrdinalIgnoreCase);
    }

    private static int? TryGetCoreIndex(string name)
    {
        Match match = CoreNumberRegex.Match(name);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out int oneBasedIndex))
        {
            return null;
        }

        return Math.Max(0, oneBasedIndex - 1);
    }

    private static string FormatSensorValue(SensorType type, float value)
    {
        return type switch
        {
            SensorType.Temperature => MetricFormatter.FormatTemperature(value),
            SensorType.Power => MetricFormatter.FormatPower(value),
            SensorType.Load or SensorType.Control or SensorType.Level => $"{value:0.#}%",
            SensorType.Clock => value >= 1000 ? $"{value / 1000f:0.00} GHz" : $"{value:0} MHz",
            SensorType.Data => $"{value:0.0} GB",
            SensorType.SmallData => $"{value:0} MB",
            SensorType.Throughput => FormatThroughput(value),
            SensorType.Voltage => $"{value:0.###} V",
            SensorType.Fan => $"{value:0} RPM",
            _ => $"{value:0.##}"
        };
    }

    private static double GaugeValue(SensorType type, float value)
    {
        return type switch
        {
            SensorType.Load or SensorType.Control or SensorType.Level => Math.Clamp(value, 0, 100),
            SensorType.Temperature => Math.Clamp(value, 0, 100),
            _ => 0
        };
    }

    private static string FormatThroughput(float bytesPerSecond)
    {
        string[] units = ["B/s", "KB/s", "MB/s", "GB/s"];
        double value = Math.Max(0, bytesPerSecond);
        int unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.#} {units[unitIndex]}";
    }

    private static float Average(IEnumerable<float> values)
    {
        float[] array = values.ToArray();
        return array.Length == 0 ? 0 : array.Average();
    }

    private sealed class CoreBuilder
    {
        public float? TemperatureCelsius { get; set; }

        public int? LoadPercent { get; set; }

        public float? PowerWatts { get; set; }
    }
}

public readonly record struct HardwareMonitorReadResult(bool IsAvailable, SystemSnapshot? Snapshot, string Message)
{
    public static HardwareMonitorReadResult Available(SystemSnapshot snapshot) => new(true, snapshot, "Connected");

    public static HardwareMonitorReadResult Unavailable(string message) => new(false, null, message);
}
