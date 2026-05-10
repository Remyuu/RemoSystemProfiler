using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Security.Principal;
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
    private readonly PdhCpuFrequencyReader _cpuFrequencyReader = new();

    private bool _opened;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

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
            bool requiresAdministrator = !IsRunningAsAdministrator();
            StorageDeviceReading[] storageDevices = hardwareTree
                .Where(hardware => hardware.HardwareType == HardwareType.Storage)
                .Select(BuildStorage)
                .ToArray();

            SystemSnapshot snapshot = new(
                DateTimeOffset.Now,
                "LibreHardwareMonitor",
                BuildCpu(hardwareTree.FirstOrDefault(hardware => hardware.HardwareType == HardwareType.Cpu)),
                BuildMemory(hardwareTree),
                hardwareTree.Where(IsGpuHardware).Select(BuildGpu).ToArray(),
                storageDevices.Length == 0 ? BuildFixedDriveStorageFallback() : storageDevices);

            if (snapshot.Cpu is null && snapshot.Memory is null && snapshot.Gpus.Count == 0 && snapshot.StorageDevices.Count == 0)
            {
                return HardwareMonitorReadResult.Unavailable("No supported hardware sensors found; try running as administrator");
            }

            return HardwareMonitorReadResult.Available(snapshot, requiresAdministrator);
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

        _cpuFrequencyReader.Dispose();
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

    private static bool IsRunningAsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
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

    private CpuDeviceReading? BuildCpu(IHardware? cpu)
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
        float clockMHz = _cpuFrequencyReader.ReadEffectiveClockMHz() ?? ReadCpuClockFromSensors(sensors);

        return new CpuDeviceReading(
            cpu.Name,
            cores.Count,
            averageLoad,
            clockMHz,
            BuildMetricReadings(sensors, SensorType.Temperature),
            BuildMetricReadings(sensors, SensorType.Power),
            BuildMetricReadings(sensors, SensorType.Clock),
            BuildMetricReadings(sensors, SensorType.Voltage),
            BuildMetricReadings(sensors, SensorType.Current),
            cores);
    }

    private static MemoryDeviceReading? BuildMemory(IReadOnlyList<IHardware> hardwareTree)
    {
        IHardware? memory = hardwareTree.FirstOrDefault(hardware => hardware.HardwareType == HardwareType.Memory);
        PhysicalMemorySnapshot? physicalMemory = ReadPhysicalMemory();
        if (physicalMemory is null && memory is null)
        {
            return null;
        }

        ISensor[] sensors = memory is null ? [] : ActiveSensors(memory);
        IReadOnlyList<MetricReading> usageSensors = physicalMemory is null
            ? []
            : [new MetricReading("Physical memory", "Load", physicalMemory.LoadPercent, $"{physicalMemory.LoadPercent:0.#}%", physicalMemory.LoadPercent)];
        IReadOnlyList<MetricReading> dataSensors = physicalMemory is null
            ? []
            : [
                new MetricReading("Used", "Data", physicalMemory.UsedGiB, MetricFormatter.FormatDataGigabytes(physicalMemory.UsedGiB), 0),
                new MetricReading("Total", "Data", physicalMemory.TotalGiB, MetricFormatter.FormatDataGigabytes(physicalMemory.TotalGiB), 0),
                new MetricReading("Available", "Data", physicalMemory.AvailableGiB, MetricFormatter.FormatDataGigabytes(physicalMemory.AvailableGiB), 0)
            ];

        return new MemoryDeviceReading(
            memory?.Name ?? "Physical Memory",
            usageSensors,
            BuildMemoryTemperatureReadings(hardwareTree, sensors),
            dataSensors);
    }

    private static IReadOnlyList<MetricReading> BuildMemoryTemperatureReadings(
        IReadOnlyList<IHardware> hardwareTree,
        IReadOnlyList<ISensor> memorySensors)
    {
        MetricReading[] directReadings = BuildMetricReadings(memorySensors, SensorType.Temperature).ToArray();
        if (directReadings.Length > 0)
        {
            return directReadings;
        }

        MetricReading[] moduleReadings = hardwareTree
            .SelectMany(hardware => hardware.Sensors.Select(sensor => new HardwareSensor(hardware, sensor)))
            .Where(item => item.Sensor.SensorType == SensorType.Temperature
                && item.Sensor.Value.HasValue
                && IsMemoryTemperatureSensor(item.Hardware, item.Sensor))
            .OrderBy(item => item.Hardware.Name)
            .ThenBy(item => item.Sensor.Index)
            .ThenBy(item => item.Sensor.Name)
            .Select(item => ToMetricReading(item.Sensor))
            .ToArray();
        return moduleReadings;
    }

    private static GpuDeviceReading BuildGpu(IHardware gpu)
    {
        ISensor[] sensors = ActiveSensors(gpu);
        return new GpuDeviceReading(
            gpu.Name,
            BuildMetricReadings(sensors, SensorType.Load, IsGpuLoadNamed),
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
            BuildStorageTemperatureReadings(sensors),
            BuildMetricReadings(sensors, SensorType.Throughput),
            BuildMetricReadings(sensors, SensorType.Data)
                .Concat(BuildMetricReadings(sensors, SensorType.SmallData))
                .ToArray());
    }

    private static IReadOnlyList<MetricReading> BuildStorageTemperatureReadings(IReadOnlyList<ISensor> sensors)
    {
        ISensor[] temperatures = SensorsOfType(sensors, SensorType.Temperature)
            .OrderBy(sensor => sensor.Index)
            .ThenBy(sensor => sensor.Name)
            .ToArray();
        if (temperatures.Length == 0)
        {
            return [];
        }

        ISensor primary = temperatures.FirstOrDefault(IsPrimaryStorageTemperatureName)
            ?? temperatures.FirstOrDefault(sensor => sensor.Name.Contains("Composite", StringComparison.OrdinalIgnoreCase))
            ?? temperatures.FirstOrDefault(sensor => sensor.Name.Equals("Temperature 1", StringComparison.OrdinalIgnoreCase))
            ?? temperatures[0];
        return [ToMetricReading(primary)];
    }

    private static StorageDeviceReading[] BuildFixedDriveStorageFallback()
    {
        return DriveInfo.GetDrives()
            .Where(drive => drive.DriveType == DriveType.Fixed && drive.IsReady && drive.TotalSize > 0)
            .OrderBy(drive => drive.Name)
            .Select(BuildFixedDriveReading)
            .ToArray();
    }

    private static StorageDeviceReading BuildFixedDriveReading(DriveInfo drive)
    {
        float totalGiB = BytesToGiB((ulong)drive.TotalSize);
        float freeGiB = BytesToGiB((ulong)drive.AvailableFreeSpace);
        float usedGiB = Math.Max(0, totalGiB - freeGiB);
        float usagePercent = totalGiB <= 0 ? 0 : usedGiB / totalGiB * 100f;
        string label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.Name.TrimEnd('\\') : $"{drive.Name.TrimEnd('\\')} {drive.VolumeLabel}";

        return new StorageDeviceReading(
            label,
            [new MetricReading("Filesystem", "Load", usagePercent, $"{usagePercent:0.#}%", usagePercent)],
            [],
            [],
            [
                new MetricReading("Used", "Data", usedGiB, MetricFormatter.FormatDataGigabytes(usedGiB), 0),
                new MetricReading("Total", "Data", totalGiB, MetricFormatter.FormatDataGigabytes(totalGiB), 0),
                new MetricReading("Free", "Data", freeGiB, MetricFormatter.FormatDataGigabytes(freeGiB), 0)
            ]);
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

    private static float ReadCpuClockFromSensors(IReadOnlyList<ISensor> sensors)
    {
        return Average(SensorsOfType(sensors, SensorType.Clock)
            .Where(IsCpuCoreClockSensor)
            .Select(sensor => sensor.Value.GetValueOrDefault()));
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

    private static bool IsMemoryTemperatureSensor(IHardware hardware, ISensor sensor)
    {
        if (IsGpuHardware(hardware))
        {
            return false;
        }

        string name = $"{hardware.Name} {sensor.Name}";
        if (name.Contains("VRAM", StringComparison.OrdinalIgnoreCase)
            || name.Contains("GPU", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Graphics", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return hardware.HardwareType == HardwareType.Memory
            || name.Contains("DIMM", StringComparison.OrdinalIgnoreCase)
            || name.Contains("DRAM", StringComparison.OrdinalIgnoreCase)
            || name.Contains("DDR", StringComparison.OrdinalIgnoreCase)
            || name.Contains("SPD", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPrimaryStorageTemperatureName(ISensor sensor)
    {
        return sensor.Name.Equals("Temperature", StringComparison.OrdinalIgnoreCase)
            || sensor.Name.Equals("Drive Temperature", StringComparison.OrdinalIgnoreCase)
            || sensor.Name.Equals("Composite Temperature", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGpuLoadNamed(ISensor sensor)
    {
        return (sensor.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase)
                || sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase)
                || sensor.Name.Contains("D3D", StringComparison.OrdinalIgnoreCase))
            && !IsMemoryNamed(sensor)
            && !sensor.Name.Contains("Video", StringComparison.OrdinalIgnoreCase)
            && !sensor.Name.Contains("Bus", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCpuCoreClockSensor(ISensor sensor)
    {
        return sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase)
            && !sensor.Name.Contains("Bus", StringComparison.OrdinalIgnoreCase)
            && !sensor.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase)
            && !sensor.Name.Contains("Fabric", StringComparison.OrdinalIgnoreCase);
    }

    private static PhysicalMemorySnapshot? ReadPhysicalMemory()
    {
        MemoryStatusEx status = new()
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
        };

        if (!GlobalMemoryStatusEx(ref status) || status.TotalPhys == 0)
        {
            return null;
        }

        float totalGiB = BytesToGiB(status.TotalPhys);
        float availableGiB = BytesToGiB(status.AvailPhys);
        float usedGiB = Math.Max(0, totalGiB - availableGiB);
        float loadPercent = Math.Clamp(status.MemoryLoad, 0, 100);
        return new PhysicalMemorySnapshot(totalGiB, availableGiB, usedGiB, loadPercent);
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

    private readonly record struct HardwareSensor(IHardware Hardware, ISensor Sensor);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    private sealed record PhysicalMemorySnapshot(
        float TotalGiB,
        float AvailableGiB,
        float UsedGiB,
        float LoadPercent);

    private static float BytesToGiB(ulong bytes)
    {
        return (float)(bytes / 1024d / 1024d / 1024d);
    }
}

public readonly record struct HardwareMonitorReadResult(
    bool IsAvailable,
    SystemSnapshot? Snapshot,
    string Message,
    bool RequiresAdministrator)
{
    public static HardwareMonitorReadResult Available(SystemSnapshot snapshot, bool requiresAdministrator) =>
        new(true, snapshot, requiresAdministrator ? "Run as administrator for full hardware sensors" : "Connected", requiresAdministrator);

    public static HardwareMonitorReadResult Unavailable(string message) => new(false, null, message, false);
}
