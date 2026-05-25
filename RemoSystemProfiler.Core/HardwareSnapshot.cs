namespace RemoSystemProfiler.Core;

public sealed record SystemSnapshot(
    DateTimeOffset SampledAt,
    string Source,
    SensorDriverStatus DriverStatus,
    CpuDeviceReading? Cpu,
    MemoryDeviceReading? Memory,
    IReadOnlyList<GpuDeviceReading> Gpus,
    IReadOnlyList<StorageDeviceReading> StorageDevices,
    NetworkDeviceReading? Network)
{
    public string SampledAtText => SampledAt.LocalDateTime.ToString("HH:mm:ss");
}

public sealed record SensorDriverStatus(
    bool IsInstalled,
    bool IsLoaded,
    string? Version,
    string Message)
{
    public bool NeedsInstallation => !IsInstalled;

    public bool IsReady => IsInstalled && IsLoaded;

    public string SummaryText => IsReady
        ? string.IsNullOrWhiteSpace(Version) ? "PawnIO ready" : $"PawnIO {Version}"
        : Message;
}

public sealed record CpuDeviceReading(
    string Name,
    int CoreCount,
    int LogicalProcessorCount,
    float AverageLoadPercent,
    float ClockMHz,
    IReadOnlyList<MetricReading> TemperatureSensors,
    IReadOnlyList<MetricReading> PowerSensors,
    IReadOnlyList<MetricReading> ClockSensors,
    IReadOnlyList<MetricReading> VoltageSensors,
    IReadOnlyList<CoreReading> Cores)
{
    public string CoreCountText => CoreCount > 0 && LogicalProcessorCount > 0
        ? $"{CoreCount}c / {LogicalProcessorCount}t"
        : "--";

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
    int LoadPercent,
    float ClockMHz = 0)
{
    public string LoadText => $"{LoadPercent}%";

    public double ClockGhz => ClockMHz > 0 ? ClockMHz / 1000d : 0;
}

public sealed record MemoryDeviceReading(
    string Name,
    IReadOnlyList<MetricReading> UsageSensors,
    IReadOnlyList<MetricReading> TemperatureSensors,
    IReadOnlyList<MetricReading> DataSensors,
    string TypeText = "--",
    string SpeedText = "--")
{
    public IReadOnlyList<MetricReading> Metrics { get; } = MetricReadingCollection.Combine(UsageSensors, DataSensors, TemperatureSensors);

    public string UsageText => UsageSensors.FirstOrDefault()?.ValueText ?? "--";

    public string TemperatureText => TemperatureSensors.Count == 0
        ? string.Empty
        : MetricFormatter.FormatTemperature(TemperatureSensors.Average(sensor => sensor.Value));

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
    public string LoadText => PrimaryLoad?.ValueText ?? "--";

    public string PowerText
    {
        get
        {
            if (PowerSensors.Count == 0)
            {
                return "--";
            }

            MetricReading? integratedGpuSoc = IsIntegratedGpuName(Name)
                ? PowerSensors.FirstOrDefault(sensor => sensor.Name.Equals("GPU SoC", StringComparison.OrdinalIgnoreCase))
                : null;
            return integratedGpuSoc?.ValueText ?? MetricFormatter.FormatPower(PowerSensors.Sum(sensor => sensor.Value));
        }
    }

    public string TemperatureText => PrimaryTemperature?.ValueText ?? "--";

    public double LoadGauge => PrimaryLoad?.GaugeValue ?? 0;

    private MetricReading? PrimaryLoad => LoadSensors.FirstOrDefault(IsD3D3DLoad)
        ?? LoadSensors.FirstOrDefault(sensor => sensor.Name.Contains("3D", StringComparison.OrdinalIgnoreCase)
            && sensor.Name.Contains("D3D", StringComparison.OrdinalIgnoreCase))
        ?? LoadSensors.FirstOrDefault(sensor => sensor.Name.Equals("GPU Core", StringComparison.OrdinalIgnoreCase))
        ?? LoadSensors.FirstOrDefault(sensor => sensor.Name.Equals("Core", StringComparison.OrdinalIgnoreCase))
        ?? LoadSensors.FirstOrDefault();

    private MetricReading? PrimaryTemperature
    {
        get
        {
            if (TemperatureSensors.Count == 0)
            {
                return null;
            }

            return IsIntegratedGpuName(Name)
                ? TemperatureSensors.FirstOrDefault(sensor => sensor.Name.Equals("GPU VR SoC", StringComparison.OrdinalIgnoreCase))
                    ?? TemperatureSensors.FirstOrDefault(IsGpuCoreTemperature)
                    ?? TemperatureSensors.FirstOrDefault()
                : TemperatureSensors.FirstOrDefault(IsGpuCoreTemperature)
                    ?? TemperatureSensors.FirstOrDefault(sensor => sensor.Name.Equals("GPU Temperature", StringComparison.OrdinalIgnoreCase))
                    ?? TemperatureSensors.FirstOrDefault();
        }
    }

    private static bool IsGpuCoreTemperature(MetricReading sensor)
    {
        return sensor.Name.Equals("GPU Core", StringComparison.OrdinalIgnoreCase)
            || sensor.Name.Equals("Core", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsD3D3DLoad(MetricReading sensor)
    {
        return sensor.Name.Equals("D3D 3D", StringComparison.OrdinalIgnoreCase)
            || sensor.Name.Equals("D3D 3D Engine", StringComparison.OrdinalIgnoreCase)
            || sensor.Name.Equals("3D", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIntegratedGpuName(string name)
    {
        return name.Contains("Radeon(TM) Graphics", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Integrated", StringComparison.OrdinalIgnoreCase)
            || name.Contains("iGPU", StringComparison.OrdinalIgnoreCase)
            || name.Contains("UHD Graphics", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Iris", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record StorageDeviceReading(
    string Name,
    IReadOnlyList<MetricReading> UsageSensors,
    IReadOnlyList<MetricReading> TemperatureSensors,
    IReadOnlyList<MetricReading> ThroughputSensors,
    IReadOnlyList<MetricReading> DataSensors)
{
    public IReadOnlyList<MetricReading> Metrics { get; } = MetricReadingCollection.Combine(UsageSensors, TemperatureSensors, ThroughputSensors, DataSensors);

    public string UsageText => UsageSensors.FirstOrDefault()?.ValueText ?? "--";

    public string TemperatureText => TemperatureSensors.Count == 0
        ? "--"
        : MetricFormatter.FormatTemperature(TemperatureSensors.Max(sensor => sensor.Value));

    public string ReadWriteText => ThroughputSensors.Count == 0
        ? "--"
        : string.Join(" / ", ThroughputSensors.Take(2).Select(sensor => sensor.ValueText));

    public double ActivityGauge
    {
        get
        {
            if (ThroughputSensors.Count == 0)
            {
                return 0;
            }

            double bytesPerSecond = ThroughputSensors.Sum(sensor => Math.Max(0, sensor.Value));
            const double busyScaleBytesPerSecond = 512d * 1024d * 1024d;
            return Math.Clamp(bytesPerSecond / busyScaleBytesPerSecond * 100d, 0, 100);
        }
    }
}

public sealed record NetworkDeviceReading(
    string InterfaceId,
    string Name,
    bool IsWireless,
    double ReceiveBitsPerSecond,
    double SendBitsPerSecond,
    long LinkSpeedBitsPerSecond)
{
    public string ReceiveText => MetricFormatter.FormatDataRate(ReceiveBitsPerSecond);

    public string SendText => MetricFormatter.FormatDataRate(SendBitsPerSecond);

    public double ActivityGauge => LinkSpeedBitsPerSecond <= 0
        ? 0
        : Math.Clamp(Math.Max(ReceiveBitsPerSecond, SendBitsPerSecond) / LinkSpeedBitsPerSecond * 100d, 0, 100);
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

internal static class MetricReadingCollection
{
    public static IReadOnlyList<MetricReading> Combine(params IReadOnlyList<MetricReading>[] groups)
    {
        int count = 0;
        foreach (IReadOnlyList<MetricReading> group in groups)
        {
            count += group.Count;
        }

        if (count == 0)
        {
            return [];
        }

        MetricReading[] metrics = new MetricReading[count];
        int offset = 0;
        foreach (IReadOnlyList<MetricReading> group in groups)
        {
            for (int i = 0; i < group.Count; i++)
            {
                metrics[offset++] = group[i];
            }
        }

        return metrics;
    }
}

public static class MetricFormatter
{
    public static string FormatTemperature(float value) => $"{value:0.#} C";

    public static string FormatPower(float value) => $"{value:0.0} W";

    public static string FormatDataGigabytes(float value) => $"{value:0.0} GB";

    public static string FormatDataRate(double bitsPerSecond)
    {
        bitsPerSecond = Math.Max(0, bitsPerSecond);
        return bitsPerSecond switch
        {
            >= 1_000_000_000d => $"{bitsPerSecond / 1_000_000_000d:0.0} Gbps",
            >= 1_000_000d => $"{bitsPerSecond / 1_000_000d:0.0} Mbps",
            _ => $"{bitsPerSecond / 1_000d:0.0} Kbps"
        };
    }
}
