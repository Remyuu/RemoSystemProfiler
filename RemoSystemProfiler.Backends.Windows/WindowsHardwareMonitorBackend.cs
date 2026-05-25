using LibreHardwareMonitor.Hardware;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using RemoSystemProfiler.Core;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using LhmPawnIo = LibreHardwareMonitor.PawnIo.PawnIo;

namespace RemoSystemProfiler.Backends.Windows;

public sealed class WindowsHardwareMonitorBackend : IHardwareMonitorBackend
{
    private static readonly Regex CoreNumberRegex = new(@"Core\s*#?\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly TimeSpan StatusCacheDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan FixedDriveFallbackCacheDuration = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan SlowHardwareUpdateInterval = TimeSpan.FromSeconds(3);

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
    private readonly List<IHardware> _hardwareTreeBuffer = new(capacity: 16);
    private readonly Dictionary<string, NetworkTrafficSample> _networkSamples = [];

    private bool _opened;
    private SensorDriverStatus? _cachedPawnIoStatus;
    private DateTimeOffset _cachedPawnIoStatusAt;
    private bool? _cachedIsAdministrator;
    private DateTimeOffset _cachedIsAdministratorAt;
    private CpuTopology? _cachedCpuTopology;
    private MemoryModuleInfo? _cachedMemoryModuleInfo;
    private StorageDeviceReading[]? _cachedFixedDriveFallback;
    private DateTimeOffset _cachedFixedDriveFallbackAt;
    private DateTimeOffset _lastSlowHardwareUpdateAt;
    private string? _selectedNetworkInterfaceId;

    private sealed record CpuTopology(int PhysicalCores, int LogicalProcessors);

    private sealed record MemoryModuleInfo(string TypeText, string SpeedText);

    private sealed record NetworkTrafficSample(long BytesReceived, long BytesSent, DateTimeOffset SampledAt);

    private sealed record NetworkCandidate(NetworkDeviceReading Reading, bool HasDefaultGateway, double TotalBitsPerSecond);

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
        SensorDriverStatus driverStatus = GetPawnIoStatus();
        try
        {
            EnsureOpen();
            driverStatus = GetPawnIoStatus();

            UpdateHardwareDomains();

            IReadOnlyList<IHardware> hardwareTree = BuildHardwareTree();
            bool requiresAdministrator = !GetIsRunningAsAdministrator();
            IHardware? cpuHardware = null;
            IHardware? memoryHardware = null;
            List<IHardware> gpuHardware = [];
            List<IHardware> storageHardware = [];
            for (int i = 0; i < hardwareTree.Count; i++)
            {
                IHardware hardware = hardwareTree[i];
                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    cpuHardware ??= hardware;
                }
                else if (hardware.HardwareType == HardwareType.Memory)
                {
                    memoryHardware ??= hardware;
                }
                else if (hardware.HardwareType == HardwareType.Storage)
                {
                    storageHardware.Add(hardware);
                }
                else if (IsGpuHardware(hardware))
                {
                    gpuHardware.Add(hardware);
                }
            }

            SystemSnapshot snapshot = new(
                DateTimeOffset.Now,
                "LibreHardwareMonitor",
                driverStatus,
                BuildCpu(cpuHardware),
                BuildMemory(hardwareTree, memoryHardware),
                BuildGpus(gpuHardware),
                storageHardware.Count == 0 ? GetFixedDriveStorageFallback() : BuildStorageDevices(storageHardware),
                BuildNetworkReading());

            if (snapshot.Cpu is null && snapshot.Memory is null && snapshot.Gpus.Count == 0 && snapshot.StorageDevices.Count == 0 && snapshot.Network is null)
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

    private void UpdateHardwareDomains()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        bool updateSlowHardware = _lastSlowHardwareUpdateAt == default
            || now - _lastSlowHardwareUpdateAt >= SlowHardwareUpdateInterval;

        foreach (IHardware hardware in _computer.Hardware)
        {
            UpdateHardwareTree(hardware, updateSlowHardware, parentAllowsUpdate: true);
        }

        if (updateSlowHardware)
        {
            _lastSlowHardwareUpdateAt = now;
        }
    }

    private static void UpdateHardwareTree(IHardware hardware, bool updateSlowHardware, bool parentAllowsUpdate)
    {
        bool updateThisHardware = parentAllowsUpdate && ShouldUpdateHardware(hardware, updateSlowHardware);
        if (!updateThisHardware)
        {
            return;
        }

        hardware.Update();
        foreach (IHardware subHardware in hardware.SubHardware)
        {
            UpdateHardwareTree(subHardware, updateSlowHardware, updateThisHardware);
        }
    }

    private static bool ShouldUpdateHardware(IHardware hardware, bool updateSlowHardware)
    {
        return hardware.HardwareType switch
        {
            HardwareType.Cpu or HardwareType.Memory => true,
            HardwareType.GpuAmd or HardwareType.GpuIntel or HardwareType.GpuNvidia => updateSlowHardware,
            HardwareType.Storage or HardwareType.Motherboard or HardwareType.SuperIO => updateSlowHardware,
            _ => updateSlowHardware
        };
    }

    private IReadOnlyList<IHardware> BuildHardwareTree()
    {
        _hardwareTreeBuffer.Clear();
        foreach (IHardware hardware in _computer.Hardware)
        {
            AddHardwareTree(hardware, _hardwareTreeBuffer);
        }

        return _hardwareTreeBuffer;
    }

    private static void AddHardwareTree(IHardware hardware, List<IHardware> hardwareTree)
    {
        hardwareTree.Add(hardware);
        foreach (IHardware subHardware in hardware.SubHardware)
        {
            AddHardwareTree(subHardware, hardwareTree);
        }
    }

    private bool GetIsRunningAsAdministrator()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (_cachedIsAdministrator.HasValue && now - _cachedIsAdministratorAt < StatusCacheDuration)
        {
            return _cachedIsAdministrator.Value;
        }

        bool value = IsRunningAsAdministrator();
        _cachedIsAdministrator = value;
        _cachedIsAdministratorAt = now;
        return value;
    }

    private static bool IsRunningAsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private SensorDriverStatus GetPawnIoStatus()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (_cachedPawnIoStatus is not null && now - _cachedPawnIoStatusAt < StatusCacheDuration)
        {
            return _cachedPawnIoStatus;
        }

        SensorDriverStatus status = ReadPawnIoStatus();
        _cachedPawnIoStatus = status;
        _cachedPawnIoStatusAt = now;
        return status;
    }

    private static SensorDriverStatus ReadPawnIoStatus()
    {
        try
        {
            bool lhmInstalled = LhmPawnIo.IsInstalled;
            bool installRecordFound = HasPawnIoInstallRecord();
            bool installed = lhmInstalled || installRecordFound;
            bool loaded = CanOpenPawnIoDevice();
            string? version = ReadPawnIoVersion();
            string message = (installed, loaded, lhmInstalled) switch
            {
                (true, true, _) => string.IsNullOrWhiteSpace(version) ? "PawnIO ready" : $"PawnIO {version}",
                (true, false, true) => "PawnIO installed but not loaded; restart as administrator",
                (true, false, false) => "PawnIO installation found but the driver is unavailable; uninstall PawnIO, then install again",
                _ => "PawnIO missing; install it for motherboard, fan, and low-level sensors"
            };

            return new SensorDriverStatus(installed, loaded, version, message);
        }
        catch (Exception ex)
        {
            return new SensorDriverStatus(false, false, null, $"PawnIO status unavailable: {ex.Message}");
        }
    }

    private static string? ReadPawnIoVersion()
    {
        try
        {
            return LhmPawnIo.Version?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static bool HasPawnIoInstallRecord()
    {
        return HasPawnIoServiceKey()
            || HasPawnIoUninstallEntry(RegistryView.Registry64)
            || HasPawnIoUninstallEntry(RegistryView.Registry32);
    }

    private static bool HasPawnIoServiceKey()
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\PawnIO");
            return key is not null;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasPawnIoUninstallEntry(RegistryView view)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using RegistryKey? uninstallKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstallKey is null)
            {
                return false;
            }

            foreach (string subKeyName in uninstallKey.GetSubKeyNames())
            {
                using RegistryKey? appKey = uninstallKey.OpenSubKey(subKeyName);
                if (appKey?.GetValue("DisplayName") is string displayName
                    && displayName.Contains("PawnIO", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
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

    private CpuDeviceReading? BuildCpu(IHardware? cpu)
    {
        if (cpu is null)
        {
            return null;
        }

        ISensor[] sensors = ActiveSensors(cpu);
        IReadOnlyList<CoreReading> sensorCores = BuildCoreReadings(sensors);
        IReadOnlyDictionary<int, float> coreClocks = BuildCoreClockReadings(sensors);
        CpuTopology topology = ReadCpuTopologyCached(sensorCores.Count);
        IReadOnlyList<CoreReading> cores = AttachCoreClocks(
            _logicalProcessorLoadReader.ReadLoadPercentages(topology.LogicalProcessors)
                ?? NormalizeCoreReadings(sensorCores, topology.LogicalProcessors),
            coreClocks);
        float averageLoad = FindSensor(sensors, SensorType.Load, "CPU Total")?.Value
            ?? FindSensor(sensors, SensorType.Load, "Total")?.Value
            ?? AverageCoreLoad(cores);
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

    private CpuTopology ReadCpuTopologyCached(int sensorLogicalProcessorCount)
    {
        if (_cachedCpuTopology is { } topology)
        {
            return topology.LogicalProcessors > 0
                ? topology
                : new CpuTopology(
                    Math.Max(1, sensorLogicalProcessorCount),
                    Math.Max(sensorLogicalProcessorCount, Environment.ProcessorCount));
        }

        _cachedCpuTopology = ReadCpuTopology(sensorLogicalProcessorCount);
        return _cachedCpuTopology;
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

    private MemoryDeviceReading? BuildMemory(IReadOnlyList<IHardware> hardwareTree, IHardware? memory)
    {
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
        MemoryModuleInfo moduleInfo = GetMemoryModuleInfo();

        return new MemoryDeviceReading(
            memory?.Name ?? "Physical Memory",
            usageSensors,
            BuildMemoryTemperatureReadings(hardwareTree, sensors),
            dataSensors,
            moduleInfo.TypeText,
            moduleInfo.SpeedText);
    }

    private MemoryModuleInfo GetMemoryModuleInfo()
    {
        if (_cachedMemoryModuleInfo is { } moduleInfo)
        {
            return moduleInfo;
        }

        _cachedMemoryModuleInfo = ReadMemoryModuleInfo();
        return _cachedMemoryModuleInfo;
    }

    private static MemoryModuleInfo ReadMemoryModuleInfo()
    {
        try
        {
            SortedSet<string> types = new(StringComparer.OrdinalIgnoreCase);
            SortedSet<int> speeds = [];
            using ManagementObjectSearcher searcher = new(
                "SELECT SMBIOSMemoryType, MemoryType, ConfiguredClockSpeed, Speed FROM Win32_PhysicalMemory");
            foreach (ManagementBaseObject item in searcher.Get())
            {
                string type = FormatMemoryType(ReadUInt16(item, "SMBIOSMemoryType"), ReadUInt16(item, "MemoryType"));
                if (!string.IsNullOrWhiteSpace(type))
                {
                    types.Add(type);
                }

                int speed = ReadInt32(item, "ConfiguredClockSpeed");
                if (speed <= 0)
                {
                    speed = ReadInt32(item, "Speed");
                }

                if (speed > 0)
                {
                    speeds.Add(speed);
                }
            }

            string typeText = types.Count == 0 ? "--" : string.Join(" / ", types);
            string speedText = speeds.Count == 0 ? "--" : string.Join(" / ", speeds.Select(speed => $"{speed} MT/s"));
            return new MemoryModuleInfo(typeText, speedText);
        }
        catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException or InvalidOperationException)
        {
            return new MemoryModuleInfo("--", "--");
        }
    }

    private static ushort ReadUInt16(ManagementBaseObject item, string propertyName)
    {
        return item[propertyName] is null ? (ushort)0 : Convert.ToUInt16(item[propertyName]);
    }

    private static int ReadInt32(ManagementBaseObject item, string propertyName)
    {
        return item[propertyName] is null ? 0 : Convert.ToInt32(item[propertyName]);
    }

    private static string FormatMemoryType(ushort smbiosMemoryType, ushort legacyMemoryType)
    {
        return smbiosMemoryType switch
        {
            20 => "DDR",
            21 => "DDR2",
            24 => "DDR3",
            26 => "DDR4",
            30 => "LPDDR",
            31 => "LPDDR2",
            32 => "LPDDR3",
            33 => "LPDDR4",
            34 => "DDR5",
            35 => "LPDDR5",
            _ => legacyMemoryType switch
            {
                20 => "DDR",
                21 => "DDR2",
                24 => "DDR3",
                26 => "DDR4",
                _ => string.Empty
            }
        };
    }

    private static IReadOnlyList<MetricReading> BuildMemoryTemperatureReadings(
        IReadOnlyList<IHardware> hardwareTree,
        IReadOnlyList<ISensor> memorySensors)
    {
        IReadOnlyList<MetricReading> directReadings = BuildMetricReadings(memorySensors, SensorType.Temperature, IsMemoryModuleTemperatureSensor);
        if (directReadings.Count > 0)
        {
            return directReadings;
        }

        List<HardwareSensor> moduleSensors = [];
        for (int i = 0; i < hardwareTree.Count; i++)
        {
            IHardware hardware = hardwareTree[i];
            foreach (ISensor sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Temperature
                    && sensor.Value.HasValue
                    && IsMemoryTemperatureSensor(hardware, sensor))
                {
                    moduleSensors.Add(new HardwareSensor(hardware, sensor));
                }
            }
        }

        if (moduleSensors.Count == 0)
        {
            return [];
        }

        moduleSensors.Sort(CompareHardwareSensor);
        MetricReading[] readings = new MetricReading[moduleSensors.Count];
        for (int i = 0; i < readings.Length; i++)
        {
            readings[i] = ToMetricReading(moduleSensors[i].Sensor);
        }

        return readings;
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

    private static GpuDeviceReading[] BuildGpus(IReadOnlyList<IHardware> gpuHardware)
    {
        if (gpuHardware.Count == 0)
        {
            return [];
        }

        GpuDeviceReading[] readings = new GpuDeviceReading[gpuHardware.Count];
        for (int i = 0; i < readings.Length; i++)
        {
            readings[i] = BuildGpu(gpuHardware[i]);
        }

        return readings;
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

    private static StorageDeviceReading[] BuildStorageDevices(IReadOnlyList<IHardware> storageHardware)
    {
        if (storageHardware.Count == 0)
        {
            return [];
        }

        StorageDeviceReading[] readings = new StorageDeviceReading[storageHardware.Count];
        for (int i = 0; i < readings.Length; i++)
        {
            readings[i] = BuildStorage(storageHardware[i]);
        }

        return readings;
    }

    private static IReadOnlyList<MetricReading> BuildStorageTemperatureReadings(IReadOnlyList<ISensor> sensors)
    {
        List<ISensor> temperatures = MatchingSensors(sensors, SensorType.Temperature, null);
        temperatures.Sort(CompareSensor);
        if (temperatures.Count == 0)
        {
            return [];
        }

        ISensor primary = temperatures.FirstOrDefault(IsPrimaryStorageTemperatureName)
            ?? temperatures.FirstOrDefault(sensor => sensor.Name.Contains("Composite", StringComparison.OrdinalIgnoreCase))
            ?? temperatures.FirstOrDefault(sensor => sensor.Name.Equals("Temperature 1", StringComparison.OrdinalIgnoreCase))
            ?? temperatures[0];
        return [ToMetricReading(primary)];
    }

    private StorageDeviceReading[] GetFixedDriveStorageFallback()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (_cachedFixedDriveFallback is not null && now - _cachedFixedDriveFallbackAt < FixedDriveFallbackCacheDuration)
        {
            return _cachedFixedDriveFallback;
        }

        _cachedFixedDriveFallback = BuildFixedDriveStorageFallback();
        _cachedFixedDriveFallbackAt = now;
        return _cachedFixedDriveFallback;
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

    private NetworkDeviceReading? BuildNetworkReading()
    {
        DateTimeOffset sampledAt = DateTimeOffset.UtcNow;
        List<NetworkCandidate> candidates = [];
        HashSet<string> currentIds = [];

        foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (!IsSupportedNetworkAdapter(adapter))
            {
                continue;
            }

            IPInterfaceStatistics statistics;
            try
            {
                statistics = adapter.GetIPStatistics();
            }
            catch (NetworkInformationException)
            {
                continue;
            }

            currentIds.Add(adapter.Id);
            double receiveBitsPerSecond = 0;
            double sendBitsPerSecond = 0;
            if (_networkSamples.TryGetValue(adapter.Id, out NetworkTrafficSample? previous))
            {
                double seconds = (sampledAt - previous.SampledAt).TotalSeconds;
                if (seconds > 0
                    && statistics.BytesReceived >= previous.BytesReceived
                    && statistics.BytesSent >= previous.BytesSent)
                {
                    receiveBitsPerSecond = (statistics.BytesReceived - previous.BytesReceived) * 8d / seconds;
                    sendBitsPerSecond = (statistics.BytesSent - previous.BytesSent) * 8d / seconds;
                }
            }

            _networkSamples[adapter.Id] = new(statistics.BytesReceived, statistics.BytesSent, sampledAt);
            NetworkDeviceReading reading = new(
                adapter.Id,
                adapter.Name,
                adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211,
                receiveBitsPerSecond,
                sendBitsPerSecond,
                Math.Max(0, adapter.Speed));
            candidates.Add(new(reading, HasDefaultGateway(adapter), receiveBitsPerSecond + sendBitsPerSecond));
        }

        foreach (string id in _networkSamples.Keys.Where(id => !currentIds.Contains(id)).ToArray())
        {
            _networkSamples.Remove(id);
        }

        if (candidates.Count == 0)
        {
            _selectedNetworkInterfaceId = null;
            return null;
        }

        IReadOnlyList<NetworkCandidate> preferred = candidates.Any(candidate => candidate.HasDefaultGateway)
            ? candidates.Where(candidate => candidate.HasDefaultGateway).ToArray()
            : candidates;
        NetworkCandidate busiest = preferred.OrderByDescending(candidate => candidate.TotalBitsPerSecond).First();
        NetworkCandidate? selected = preferred.FirstOrDefault(candidate => candidate.Reading.InterfaceId == _selectedNetworkInterfaceId);
        if (busiest.TotalBitsPerSecond > 0 || selected is null)
        {
            selected = busiest;
        }

        _selectedNetworkInterfaceId = selected.Reading.InterfaceId;
        return selected.Reading;
    }

    private static bool IsSupportedNetworkAdapter(NetworkInterface adapter)
    {
        if (adapter.OperationalStatus != OperationalStatus.Up
            || adapter.NetworkInterfaceType is not (
                NetworkInterfaceType.Ethernet
                or NetworkInterfaceType.GigabitEthernet
                or NetworkInterfaceType.FastEthernetFx
                or NetworkInterfaceType.FastEthernetT
                or NetworkInterfaceType.Wireless80211))
        {
            return false;
        }

        string identity = $"{adapter.Name} {adapter.Description}";
        return !ContainsAny(identity, "Virtual", "VPN", "Hyper-V", "VMware", "VirtualBox", "vEthernet", "Loopback", "TAP", "TUN", "WireGuard", "Bluetooth");
    }

    private static bool HasDefaultGateway(NetworkInterface adapter)
    {
        try
        {
            return adapter.GetIPProperties().GatewayAddresses.Count > 0;
        }
        catch (NetworkInformationException)
        {
            return false;
        }
    }

    private static IReadOnlyList<CoreReading> BuildCoreReadings(IReadOnlyList<ISensor> sensors)
    {
        Dictionary<int, int> loads = new();
        HashSet<int> usedIndexes = [];
        int fallbackIndex = 0;
        List<ISensor> loadSensors = MatchingSensors(sensors, SensorType.Load, IsCoreNamed);
        loadSensors.Sort(CompareSensor);
        foreach (ISensor sensor in loadSensors)
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

        int[] keys = loads.Keys.ToArray();
        Array.Sort(keys);
        CoreReading[] readings = new CoreReading[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            int key = keys[i];
            readings[i] = new CoreReading(key, loads[key]);
        }

        return readings;
    }

    private static IReadOnlyDictionary<int, float> BuildCoreClockReadings(IReadOnlyList<ISensor> sensors)
    {
        Dictionary<int, float> clocks = [];
        HashSet<int> usedIndexes = [];
        int fallbackIndex = 0;
        List<ISensor> clockSensors = MatchingSensors(sensors, SensorType.Clock, IsCpuCoreClockSensor);
        clockSensors.Sort(CompareSensor);
        foreach (ISensor sensor in clockSensors)
        {
            int index = TryGetCoreIndex(sensor.Name) ?? fallbackIndex;
            if (usedIndexes.Contains(index))
            {
                index = NextAvailableCoreIndex(usedIndexes, ref fallbackIndex);
            }

            float clockMHz = sensor.Value.GetValueOrDefault();
            if (clockMHz <= 0 || clockMHz > 10000)
            {
                continue;
            }

            usedIndexes.Add(index);
            fallbackIndex = Math.Max(fallbackIndex, index + 1);
            clocks[index] = clockMHz;
        }

        return clocks;
    }

    private static IReadOnlyList<CoreReading> AttachCoreClocks(IReadOnlyList<CoreReading> readings, IReadOnlyDictionary<int, float> clocks)
    {
        if (readings.Count == 0 || clocks.Count == 0)
        {
            return readings;
        }

        CoreReading[] updated = new CoreReading[readings.Count];
        for (int i = 0; i < updated.Length; i++)
        {
            CoreReading reading = readings[i];
            updated[i] = new CoreReading(
                reading.Index,
                reading.LoadPercent,
                clocks.TryGetValue(reading.Index, out float clockMHz) ? clockMHz : reading.ClockMHz);
        }

        return updated;
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
                .Select((reading, index) => new CoreReading(index, reading.LoadPercent, reading.ClockMHz))
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
            normalized[index] = new CoreReading(index, ordered[sourceIndex].LoadPercent, ordered[sourceIndex].ClockMHz);
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
        List<ISensor> matched = MatchingSensors(sensors, type, filter);
        return ToMetricReadings(matched);
    }

    private static IReadOnlyList<MetricReading> BuildMetricReadings(
        IReadOnlyList<ISensor> sensors,
        IReadOnlyCollection<SensorType> types,
        Func<ISensor, bool>? filter = null)
    {
        List<ISensor> matched = [];
        for (int i = 0; i < sensors.Count; i++)
        {
            ISensor sensor = sensors[i];
            if (sensor.Value.HasValue
                && ContainsSensorType(types, sensor.SensorType)
                && (filter?.Invoke(sensor) ?? true))
            {
                matched.Add(sensor);
            }
        }

        return ToMetricReadings(matched);
    }

    private static List<ISensor> MatchingSensors(IReadOnlyList<ISensor> sensors, SensorType type, Func<ISensor, bool>? filter)
    {
        List<ISensor> matched = [];
        for (int i = 0; i < sensors.Count; i++)
        {
            ISensor sensor = sensors[i];
            if (sensor.SensorType == type
                && sensor.Value.HasValue
                && (filter?.Invoke(sensor) ?? true))
            {
                matched.Add(sensor);
            }
        }

        return matched;
    }

    private static MetricReading[] ToMetricReadings(List<ISensor> sensors)
    {
        if (sensors.Count == 0)
        {
            return [];
        }

        sensors.Sort(CompareSensor);
        MetricReading[] readings = new MetricReading[sensors.Count];
        for (int i = 0; i < sensors.Count; i++)
        {
            readings[i] = ToMetricReading(sensors[i]);
        }

        return readings;
    }

    private static bool ContainsSensorType(IReadOnlyCollection<SensorType> types, SensorType type)
    {
        foreach (SensorType candidate in types)
        {
            if (candidate == type)
            {
                return true;
            }
        }

        return false;
    }

    private static int CompareSensor(ISensor left, ISensor right)
    {
        int indexComparison = left.Index.CompareTo(right.Index);
        return indexComparison != 0
            ? indexComparison
            : string.Compare(left.Name, right.Name, StringComparison.Ordinal);
    }

    private static int CompareHardwareSensor(HardwareSensor left, HardwareSensor right)
    {
        int hardwareComparison = string.Compare(left.Hardware.Name, right.Hardware.Name, StringComparison.Ordinal);
        return hardwareComparison != 0
            ? hardwareComparison
            : CompareSensor(left.Sensor, right.Sensor);
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
        List<ISensor> sensors = [];
        AddActiveSensors(hardware, sensors);
        return sensors.ToArray();
    }

    private static void AddActiveSensors(IHardware hardware, List<ISensor> sensors)
    {
        foreach (ISensor sensor in hardware.Sensors)
        {
            if (sensor.Value.HasValue)
            {
                sensors.Add(sensor);
            }
        }

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            AddActiveSensors(subHardware, sensors);
        }
    }

    private static ISensor? FindSensor(IEnumerable<ISensor> sensors, SensorType type, string nameContains)
    {
        foreach (ISensor sensor in sensors)
        {
            if (sensor.SensorType == type
                && sensor.Value.HasValue
                && sensor.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
            {
                return sensor;
            }
        }

        return null;
    }

    private static bool IsGpuHardware(IHardware hardware)
    {
        return hardware.HardwareType is HardwareType.GpuAmd or HardwareType.GpuNvidia or HardwareType.GpuIntel;
    }

    private static float ReadCpuClockFromSensors(IReadOnlyList<ISensor> sensors)
    {
        float total = 0;
        int count = 0;
        for (int i = 0; i < sensors.Count; i++)
        {
            ISensor sensor = sensors[i];
            if (sensor.SensorType == SensorType.Clock && sensor.Value.HasValue && IsCpuCoreClockSensor(sensor))
            {
                total += sensor.Value.GetValueOrDefault();
                count++;
            }
        }

        return count == 0 ? 0 : total / count;
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
        && !ContainsAny(sensor.Name, "Bus", "Memory", "Fabric", "Max", "Total");

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

    private static float AverageCoreLoad(IReadOnlyList<CoreReading> cores)
    {
        if (cores.Count == 0)
        {
            return 0;
        }

        float total = 0;
        for (int i = 0; i < cores.Count; i++)
        {
            total += cores[i].LoadPercent;
        }

        return total / cores.Count;
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
