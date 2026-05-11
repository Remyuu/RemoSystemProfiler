using System.Management;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using LibreHardwareMonitor.Hardware;
using RemoSystemProfiler.Core;
using LhmPawnIo = LibreHardwareMonitor.PawnIo.PawnIo;

namespace RemoSystemProfiler.Backends.Windows;

public sealed class WindowsHardwareMonitorBackend : IHardwareMonitorBackend
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
    private readonly WindowsLogicalProcessorLoadReader _logicalProcessorLoadReader = new();

    private bool _opened;

    private sealed record CpuTopology(int PhysicalCores, int LogicalProcessors);

    public string Name => "Windows LibreHardwareMonitor";

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    public HardwareMonitorReadResult Read()
    {
        SensorDriverStatus driverStatus = ReadPawnIoStatus();
        try
        {
            EnsureOpen();
            driverStatus = ReadPawnIoStatus();

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
                driverStatus,
                BuildCpu(hardwareTree.FirstOrDefault(hardware => hardware.HardwareType == HardwareType.Cpu)),
                BuildMemory(hardwareTree),
                hardwareTree.Where(IsGpuHardware).Select(BuildGpu).ToArray(),
                storageDevices.Length == 0 ? BuildFixedDriveStorageFallback() : storageDevices);

            if (snapshot.Cpu is null && snapshot.Memory is null && snapshot.Gpus.Count == 0 && snapshot.StorageDevices.Count == 0)
            {
                return HardwareMonitorReadResult.Unavailable("No supported hardware sensors found; try running as administrator", driverStatus);
            }

            return HardwareMonitorReadResult.Available(snapshot, requiresAdministrator);
        }
        catch (Exception ex)
        {
            return HardwareMonitorReadResult.Unavailable($"Sensor read failed: {ex.Message}", driverStatus);
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

    private static SensorDriverStatus ReadPawnIoStatus()
    {
        try
        {
            bool installed = LhmPawnIo.IsInstalled;
            bool loaded = CanOpenPawnIoDevice();
            string? version = LhmPawnIo.Version?.ToString();
            string message = (installed, loaded) switch
            {
                (true, true) => string.IsNullOrWhiteSpace(version) ? "PawnIO ready" : $"PawnIO {version}",
                (true, false) => "PawnIO installed but not loaded; restart as administrator",
                _ => "PawnIO missing; install it for motherboard, fan, and low-level sensors"
            };

            return new SensorDriverStatus(installed, loaded, version, message);
        }
        catch (Exception ex)
        {
            return new SensorDriverStatus(false, false, null, $"PawnIO status unavailable: {ex.Message}");
        }
    }

    private static bool CanOpenPawnIoDevice()
    {
        const uint genericRead = 0x80000000;
        const uint genericWrite = 0x40000000;
        const uint fileShareRead = 0x00000001;
        const uint fileShareWrite = 0x00000002;
        const uint openExisting = 3;
        const uint fileAttributeNormal = 0x00000080;

        using SafeFileHandle handle = CreateFile(
            @"\\?\GLOBALROOT\Device\PawnIO",
            genericRead | genericWrite,
            fileShareRead | fileShareWrite,
            IntPtr.Zero,
            openExisting,
            fileAttributeNormal,
            IntPtr.Zero);
        return !handle.IsInvalid && !handle.IsClosed;
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
        IReadOnlyList<CoreReading> sensorCores = BuildCoreReadings(sensors);
        CpuTopology topology = ReadCpuTopology(sensorCores.Count);
        IReadOnlyList<CoreReading> cores = _logicalProcessorLoadReader.ReadLoadPercentages(topology.LogicalProcessors)
            ?? NormalizeCoreReadings(sensorCores, topology.LogicalProcessors);
        float averageLoad = FindSensor(sensors, SensorType.Load, "CPU Total")?.Value
            ?? FindSensor(sensors, SensorType.Load, "Total")?.Value
            ?? Average(cores.Select(core => (float)core.LoadPercent));
        float clockMHz = _cpuFrequencyReader.ReadEffectiveClockMHz() ?? ReadCpuClockFromSensors(sensors);

        return new CpuDeviceReading(
            cpu.Name,
            topology.PhysicalCores,
            topology.LogicalProcessors,
            averageLoad,
            clockMHz,
            BuildMetricReadings(sensors, SensorType.Temperature),
            BuildMetricReadings(sensors, SensorType.Power),
            BuildMetricReadings(sensors, SensorType.Clock),
            BuildMetricReadings(sensors, SensorType.Voltage),
            cores);
    }

    private static CpuTopology ReadCpuTopology(int sensorLogicalProcessorCount)
    {
        int physicalCores = 0;
        int logicalProcessors = 0;

        try
        {
            using ManagementObjectSearcher searcher = new("SELECT NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
            foreach (ManagementBaseObject item in searcher.Get())
            {
                physicalCores += Convert.ToInt32(item["NumberOfCores"] ?? 0);
                logicalProcessors += Convert.ToInt32(item["NumberOfLogicalProcessors"] ?? 0);
            }
        }
        catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException)
        {
        }

        logicalProcessors = logicalProcessors > 0
            ? logicalProcessors
            : Math.Max(sensorLogicalProcessorCount, Environment.ProcessorCount);
        physicalCores = physicalCores > 0
            ? physicalCores
            : logicalProcessors;

        return new CpuTopology(physicalCores, logicalProcessors);
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
        MetricReading[] directReadings = BuildMetricReadings(memorySensors, SensorType.Temperature, IsMemoryModuleTemperatureSensor).ToArray();
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
            BuildMetricReadings(sensors, [SensorType.Load, SensorType.Data, SensorType.SmallData, SensorType.Temperature], IsMemoryNamed));
    }

    private static StorageDeviceReading BuildStorage(IHardware storage)
    {
        ISensor[] sensors = ActiveSensors(storage);
        return new StorageDeviceReading(
            storage.Name,
            BuildMetricReadings(sensors, SensorType.Load),
            BuildStorageTemperatureReadings(sensors),
            BuildMetricReadings(sensors, SensorType.Throughput),
            BuildMetricReadings(sensors, [SensorType.Data, SensorType.SmallData]));
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
        Dictionary<int, int> loads = new();
        HashSet<int> usedIndexes = [];
        int fallbackIndex = 0;
        foreach (ISensor sensor in SensorsOfType(sensors, SensorType.Load).Where(IsCoreNamed).OrderBy(sensor => sensor.Index).ThenBy(sensor => sensor.Name))
        {
            int index = TryGetCoreIndex(sensor.Name) ?? fallbackIndex;
            if (usedIndexes.Contains(index))
            {
                index = NextAvailableCoreIndex(usedIndexes, ref fallbackIndex);
            }

            usedIndexes.Add(index);
            fallbackIndex = Math.Max(fallbackIndex, index + 1);
            loads[index] = (int)Math.Round(Math.Clamp(sensor.Value.GetValueOrDefault(), 0, 100));
        }

        return loads
            .OrderBy(pair => pair.Key)
            .Select(pair => new CoreReading(pair.Key, pair.Value))
            .ToArray();
    }

    private static IReadOnlyList<CoreReading> NormalizeCoreReadings(IReadOnlyList<CoreReading> readings, int logicalProcessorCount)
    {
        if (logicalProcessorCount <= 0 || readings.Count == logicalProcessorCount)
        {
            return readings;
        }

        if (logicalProcessorCount <= readings.Count)
        {
            return readings
                .OrderBy(reading => reading.Index)
                .Take(logicalProcessorCount)
                .Select((reading, index) => new CoreReading(index, reading.LoadPercent))
                .ToArray();
        }

        if (readings.Count == 0)
        {
            return Enumerable.Range(0, logicalProcessorCount)
                .Select(index => new CoreReading(index, 0))
                .ToArray();
        }

        CoreReading[] ordered = readings.OrderBy(reading => reading.Index).ToArray();
        CoreReading[] normalized = new CoreReading[logicalProcessorCount];
        for (int index = 0; index < normalized.Length; index++)
        {
            int sourceIndex = Math.Min(ordered.Length - 1, (int)(index * ordered.Length / (double)logicalProcessorCount));
            normalized[index] = new CoreReading(index, ordered[sourceIndex].LoadPercent);
        }

        return normalized;
    }

    private static int NextAvailableCoreIndex(HashSet<int> usedIndexes, ref int fallbackIndex)
    {
        while (usedIndexes.Contains(fallbackIndex))
        {
            fallbackIndex++;
        }

        return fallbackIndex++;
    }

    private static IReadOnlyList<MetricReading> BuildMetricReadings(
        IReadOnlyList<ISensor> sensors,
        SensorType type,
        Func<ISensor, bool>? filter = null)
    {
        return BuildMetricReadings(sensors, [type], filter);
    }

    private static IReadOnlyList<MetricReading> BuildMetricReadings(
        IReadOnlyList<ISensor> sensors,
        IReadOnlyCollection<SensorType> types,
        Func<ISensor, bool>? filter = null)
    {
        return sensors
            .Where(sensor => types.Contains(sensor.SensorType) && sensor.Value.HasValue)
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

    private static bool IsMemoryNamed(ISensor sensor) => ContainsAny(sensor.Name, "Memory", "VRAM", "FB");

    private static bool IsMemoryTemperatureSensor(IHardware hardware, ISensor sensor)
    {
        if (IsGpuHardware(hardware))
        {
            return false;
        }

        string name = $"{hardware.Name} {sensor.Name}";
        return !ContainsAny(name, "VRAM", "GPU", "Graphics")
            && IsMemoryModuleTemperatureSensor(sensor)
            && (hardware.HardwareType == HardwareType.Memory
                || ContainsAny(name, "DIMM", "DRAM", "DDR", "SPD"));
    }

    private static bool IsMemoryModuleTemperatureSensor(ISensor sensor)
    {
        string name = sensor.Name;
        return !ContainsAny(name, "Resolution", "Limit", "Critical", "Threshold")
            && ContainsAny(name, "DIMM", "DRAM", "DDR", "SPD", "Module");
    }

    private static bool IsPrimaryStorageTemperatureName(ISensor sensor)
    {
        return sensor.Name.Equals("Temperature", StringComparison.OrdinalIgnoreCase)
            || sensor.Name.Equals("Drive Temperature", StringComparison.OrdinalIgnoreCase)
            || sensor.Name.Equals("Composite Temperature", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGpuLoadNamed(ISensor sensor) =>
        ContainsAny(sensor.Name, "GPU", "Core", "D3D")
        && !IsMemoryNamed(sensor)
        && !ContainsAny(sensor.Name, "Video", "Bus");

    private static bool IsCpuCoreClockSensor(ISensor sensor) =>
        sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase)
        && !ContainsAny(sensor.Name, "Bus", "Memory", "Fabric");

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
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
