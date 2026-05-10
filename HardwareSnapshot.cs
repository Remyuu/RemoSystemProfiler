namespace RemoSystemProfiler;

public sealed record SystemSnapshot(
    DateTimeOffset SampledAt,
    string Source,
    CpuDeviceReading? Cpu,
    MemoryDeviceReading? Memory,
    IReadOnlyList<GpuDeviceReading> Gpus,
    IReadOnlyList<StorageDeviceReading> StorageDevices)
{
    public string SampledAtText => SampledAt.LocalDateTime.ToString("HH:mm:ss");

    public string HardwareSummaryText
    {
        get
        {
            int count = (Cpu is null ? 0 : 1) + (Memory is null ? 0 : 1) + Gpus.Count + StorageDevices.Count;
            return $"{count} hardware groups";
        }
    }
}

public sealed record CpuDeviceReading(
    string Name,
    int CoreCount,
    float AverageLoadPercent,
    float ClockMHz,
    IReadOnlyList<MetricReading> TemperatureSensors,
    IReadOnlyList<MetricReading> PowerSensors,
    IReadOnlyList<MetricReading> ClockSensors,
    IReadOnlyList<MetricReading> VoltageSensors,
    IReadOnlyList<MetricReading> CurrentSensors,
    IReadOnlyList<CoreReading> Cores)
{
    public string CoreCountText => CoreCount > 0 ? CoreCount.ToString() : "--";

    public string AverageLoadText => $"{AverageLoadPercent:0}%";

    public string ClockText => ClockMHz > 0 ? $"{ClockMHz / 1000f:0.00} GHz" : "--";

    public string MaxTemperatureText => TemperatureSensors.Count == 0
        ? "--"
        : MetricFormatter.FormatTemperature(TemperatureSensors.Max(sensor => sensor.Value));

    public string PackagePowerText => PackagePower is null ? "--" : PackagePower.ValueText;

    public MetricReading? PackagePower => PowerSensors.FirstOrDefault(sensor =>
            sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
        ?? PowerSensors.FirstOrDefault(sensor =>
            sensor.Name.Contains("Total", StringComparison.OrdinalIgnoreCase))
        ?? PowerSensors.FirstOrDefault();
}

public sealed record CoreReading(
    int Index,
    string Name,
    float? TemperatureCelsius,
    int LoadPercent,
    float? PowerWatts)
{
    public string TemperatureText => TemperatureCelsius is null
        ? "--"
        : MetricFormatter.FormatTemperature(TemperatureCelsius.Value);

    public string LoadText => $"{LoadPercent}%";

    public string PowerText => PowerWatts is null ? "--" : $"{PowerWatts.Value:0.0} W";
}

public sealed record MemoryDeviceReading(
    string Name,
    IReadOnlyList<MetricReading> UsageSensors,
    IReadOnlyList<MetricReading> TemperatureSensors,
    IReadOnlyList<MetricReading> DataSensors)
{
    public IReadOnlyList<MetricReading> Metrics => UsageSensors.Concat(DataSensors).Concat(TemperatureSensors).ToArray();

    public string UsageText => UsageSensors.FirstOrDefault()?.ValueText ?? "--";

    public string TemperatureText => TemperatureSensors.Count == 0
        ? string.Empty
        : MetricFormatter.FormatTemperature(TemperatureSensors.Max(sensor => sensor.Value));

    public string CapacityText => DataSensors.Count == 0 ? "--" : string.Join(" / ", DataSensors.Take(2).Select(sensor => sensor.ValueText));

    public double UsageGauge => UsageSensors.FirstOrDefault()?.GaugeValue ?? 0;
}

public sealed record GpuDeviceReading(
    string Name,
    IReadOnlyList<MetricReading> LoadSensors,
    IReadOnlyList<MetricReading> PowerSensors,
    IReadOnlyList<MetricReading> TemperatureSensors,
    IReadOnlyList<MetricReading> MemorySensors)
{
    public string LoadText => LoadSensors.FirstOrDefault()?.ValueText ?? "--";

    public string PowerText => PowerSensors.Count == 0 ? "--" : MetricFormatter.FormatPower(PowerSensors.Sum(sensor => sensor.Value));

    public string TemperatureText => TemperatureSensors.Count == 0
        ? "--"
        : MetricFormatter.FormatTemperature(TemperatureSensors.Max(sensor => sensor.Value));

    public string MemoryText => MemorySensors.FirstOrDefault(sensor =>
            sensor.Name.Contains("Used", StringComparison.OrdinalIgnoreCase)
            || sensor.Name.Contains("Load", StringComparison.OrdinalIgnoreCase)
            || sensor.Kind.Equals("Load", StringComparison.OrdinalIgnoreCase))
        ?.ValueText ?? "--";

    public string SensorCountText => $"{LoadSensors.Count + PowerSensors.Count + TemperatureSensors.Count + MemorySensors.Count} sensors";

    public double LoadGauge => LoadSensors.FirstOrDefault()?.GaugeValue ?? 0;
}

public sealed record StorageDeviceReading(
    string Name,
    IReadOnlyList<MetricReading> UsageSensors,
    IReadOnlyList<MetricReading> TemperatureSensors,
    IReadOnlyList<MetricReading> ThroughputSensors,
    IReadOnlyList<MetricReading> DataSensors)
{
    public IReadOnlyList<MetricReading> Metrics => UsageSensors
        .Concat(TemperatureSensors)
        .Concat(ThroughputSensors)
        .Concat(DataSensors)
        .ToArray();

    public string UsageText => UsageSensors.FirstOrDefault()?.ValueText ?? "--";

    public string TemperatureText => TemperatureSensors.Count == 0
        ? "--"
        : MetricFormatter.FormatTemperature(TemperatureSensors.Max(sensor => sensor.Value));

    public string ReadWriteText => ThroughputSensors.Count == 0
        ? "--"
        : string.Join(" / ", ThroughputSensors.Take(2).Select(sensor => sensor.ValueText));

    public string SensorCountText => $"{Metrics.Count} sensors";

    public double UsageGauge => UsageSensors.FirstOrDefault()?.GaugeValue ?? 0;
}

public sealed record MetricReading(
    string Name,
    string Kind,
    float Value,
    string ValueText,
    double GaugeValue)
{
    public string Label => $"{Name} ({Kind})";
}

public static class MetricFormatter
{
    public static string FormatTemperature(float value) => $"{value:0.#} C";

    public static string FormatPower(float value) => $"{value:0.0} W";

    public static string FormatDataGigabytes(float value) => $"{value:0.0} GB";
}
