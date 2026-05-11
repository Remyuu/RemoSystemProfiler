using Avalonia.Controls;
using RemoSystemProfiler.Core;

namespace RemoSystemProfiler;

public enum UiLanguage
{
    English,
    Chinese
}

public static class Localization
{
    private static readonly IReadOnlyDictionary<string, string> EnglishResources = new Dictionary<string, string>
    {
        ["Ui_WindowTitle"] = "Remo System Profiler",
        ["Ui_Performance"] = "Performance",
        ["Ui_Settings"] = "Settings",
        ["Ui_Dashboard"] = "Dashboard",
        ["Ui_ChartRange"] = "Chart range",
        ["Ui_UpdateInterval"] = "Update interval",
        ["Ui_Theme"] = "Theme",
        ["Ui_Language"] = "Language",
        ["Ui_English"] = "English",
        ["Ui_Chinese"] = "Chinese",
        ["Ui_RangeTenSeconds"] = "10 seconds",
        ["Ui_RangeThirtySeconds"] = "30 seconds",
        ["Ui_RangeOneMinute"] = "1 minute",
        ["Ui_RangeFiveMinutes"] = "5 minutes",
        ["Ui_IntervalHalfSecond"] = "0.5 second",
        ["Ui_IntervalOneSecond"] = "1 second",
        ["Ui_IntervalTwoSeconds"] = "2 seconds",
        ["Ui_IntervalFiveSeconds"] = "5 seconds",
        ["Ui_ThemeSystem"] = "System",
        ["Ui_ThemeLight"] = "Light",
        ["Ui_ThemeDark"] = "Dark",
        ["Ui_About"] = "About",
        ["Ui_AppSubtitle"] = "Avalonia hardware monitor",
        ["Ui_Version"] = "Version",
        ["Ui_Website"] = "Website",
        ["Ui_BuiltWith"] = "Built with Avalonia 12 and LibreHardwareMonitor.",
        ["Ui_LicenseNotice"] = "Third-party components retain their original licenses.",
        ["Ui_InstallPawnIo"] = "Install PawnIO",
        ["Ui_Load"] = "Load",
        ["Ui_Temp"] = "Temp",
        ["Ui_Power"] = "Power",
        ["Ui_LogicalProcessors"] = "Logical processors",
        ["Ui_Utilization"] = "Utilization",
        ["Ui_Speed"] = "Speed",
        ["Ui_PackagePower"] = "Package power",
        ["Ui_PeakTemp"] = "Peak temp",
        ["Ui_CoresThreads"] = "Cores / threads",
        ["Ui_Memory"] = "Memory",
        ["Ui_CpuSensors"] = "CPU sensors",
        ["Ui_GpuDevices"] = "GPU devices",
        ["Ui_StorageDevices"] = "Storage devices",
        ["Ui_StartupTitle"] = "Connecting to hardware backend",
        ["Ui_StartupSubtitle"] = "Initializing LibreHardwareMonitor and enumerating devices"
    };

    private static readonly IReadOnlyDictionary<string, string> ChineseResources = new Dictionary<string, string>
    {
        ["Ui_WindowTitle"] = "Remo System Profiler",
        ["Ui_Performance"] = "性能",
        ["Ui_Settings"] = "设置",
        ["Ui_Dashboard"] = "仪表盘",
        ["Ui_ChartRange"] = "图表范围",
        ["Ui_UpdateInterval"] = "刷新间隔",
        ["Ui_Theme"] = "主题",
        ["Ui_Language"] = "语言",
        ["Ui_English"] = "English",
        ["Ui_Chinese"] = "中文",
        ["Ui_RangeTenSeconds"] = "10 秒",
        ["Ui_RangeThirtySeconds"] = "30 秒",
        ["Ui_RangeOneMinute"] = "1 分钟",
        ["Ui_RangeFiveMinutes"] = "5 分钟",
        ["Ui_IntervalHalfSecond"] = "0.5 秒",
        ["Ui_IntervalOneSecond"] = "1 秒",
        ["Ui_IntervalTwoSeconds"] = "2 秒",
        ["Ui_IntervalFiveSeconds"] = "5 秒",
        ["Ui_ThemeSystem"] = "跟随系统",
        ["Ui_ThemeLight"] = "浅色",
        ["Ui_ThemeDark"] = "深色",
        ["Ui_About"] = "关于",
        ["Ui_AppSubtitle"] = "Avalonia 硬件监视器",
        ["Ui_Version"] = "版本",
        ["Ui_Website"] = "网站",
        ["Ui_BuiltWith"] = "基于 Avalonia 12 和 LibreHardwareMonitor 构建。",
        ["Ui_LicenseNotice"] = "第三方组件保留其原始许可证。",
        ["Ui_InstallPawnIo"] = "安装 PawnIO",
        ["Ui_Load"] = "负载",
        ["Ui_Temp"] = "温度",
        ["Ui_Power"] = "功耗",
        ["Ui_LogicalProcessors"] = "逻辑处理器",
        ["Ui_Utilization"] = "使用率",
        ["Ui_Speed"] = "频率",
        ["Ui_PackagePower"] = "封装功耗",
        ["Ui_PeakTemp"] = "最高温度",
        ["Ui_CoresThreads"] = "核心 / 线程",
        ["Ui_Memory"] = "内存",
        ["Ui_CpuSensors"] = "CPU 传感器",
        ["Ui_GpuDevices"] = "GPU 设备",
        ["Ui_StorageDevices"] = "存储设备",
        ["Ui_StartupTitle"] = "正在连接硬件后端",
        ["Ui_StartupSubtitle"] = "正在初始化 LibreHardwareMonitor 并枚举设备"
    };

    public static UiLanguage CurrentLanguage { get; private set; } = UiLanguage.English;

    public static int CurrentLanguageIndex => CurrentLanguage == UiLanguage.Chinese ? 1 : 0;

    public static bool SetLanguageFromIndex(int selectedIndex)
    {
        UiLanguage language = selectedIndex == 1 ? UiLanguage.Chinese : UiLanguage.English;
        if (language == CurrentLanguage)
        {
            return false;
        }

        CurrentLanguage = language;
        return true;
    }

    public static void ApplyToResources(IResourceDictionary resources)
    {
        foreach ((string key, string value) in ActiveResources)
        {
            resources[key] = value;
        }
    }

    public static string WaitingForHardwareSensors => CurrentLanguage == UiLanguage.Chinese ? "等待硬件传感器" : "Waiting for hardware sensors";

    public static string WaitingForSensors => CurrentLanguage == UiLanguage.Chinese ? "等待传感器" : "Waiting for sensors";

    public static string OpeningSensorBackend => CurrentLanguage == UiLanguage.Chinese ? "正在打开传感器后端" : "Opening sensor backend";

    public static string ExpandSidebar => CurrentLanguage == UiLanguage.Chinese ? "展开侧边栏" : "Expand sidebar";

    public static string CollapseSidebar => CurrentLanguage == UiLanguage.Chinese ? "收起侧边栏" : "Collapse sidebar";

    public static string PawnIoRequired => CurrentLanguage == UiLanguage.Chinese
        ? "完整传感器访问需要 PawnIO"
        : "PawnIO is required for full sensor access";

    public static string SensorBackendUnavailable => CurrentLanguage == UiLanguage.Chinese ? "传感器后端不可用" : "Sensor backend unavailable";

    public static string ConnectedLimited => CurrentLanguage == UiLanguage.Chinese ? "已连接，但传感器访问受限" : "Connected with limited sensor access";

    public static string ConnectedBackend => CurrentLanguage == UiLanguage.Chinese ? "已连接到硬件后端" : "Connected to hardware backend";

    public static string CpuSensorsUnavailable => CurrentLanguage == UiLanguage.Chinese ? "CPU 传感器不可用" : "CPU sensors unavailable";

    public static string HardwareSensorsUnavailable => CurrentLanguage == UiLanguage.Chinese ? "硬件传感器不可用" : "Hardware sensors unavailable";

    public static string InstallPawnIoRestartAdmin => CurrentLanguage == UiLanguage.Chinese
        ? "安装 PawnIO，然后以管理员身份重启 Remo System Profiler。"
        : "Install PawnIO, then restart Remo System Profiler as administrator.";

    public static string ConnectedStatus(string source, string driverSummary) => CurrentLanguage == UiLanguage.Chinese
        ? $"已连接\n{source}\n{driverSummary}"
        : $"Connected\n{source}\n{driverSummary}";

    public static string LimitedAccessStatus(string source) => CurrentLanguage == UiLanguage.Chinese
        ? $"受限访问\n{source}\n请以管理员身份运行"
        : $"Limited access\n{source}\nRun as administrator";

    public static string HardwareSummary(int gpuCount, int storageCount) => CurrentLanguage == UiLanguage.Chinese
        ? $"{gpuCount} 个 GPU | {storageCount} 个存储设备"
        : $"{gpuCount} GPU | {storageCount} storage";

    public static string OverviewMemory => CurrentLanguage == UiLanguage.Chinese ? "内存" : "Memory";

    public static string OverviewGpu(int index) => CurrentLanguage == UiLanguage.Chinese ? $"GPU {index}" : $"GPU {index}";

    public static string OverviewDisk(int index) => CurrentLanguage == UiLanguage.Chinese ? $"磁盘 {index}" : $"Disk {index}";

    public static string SensorGroupTitle(string key) => CurrentLanguage == UiLanguage.Chinese
        ? key switch
        {
            "temperature" => "温度",
            "power" => "功耗",
            "clock" => "频率",
            "voltage" => "电压",
            _ => key
        }
        : key switch
        {
            "temperature" => "Temperature",
            "power" => "Power",
            "clock" => "Clock",
            "voltage" => "Voltage",
            _ => key
        };

    public static string SummaryLabel(string key) => CurrentLanguage == UiLanguage.Chinese
        ? key switch
        {
            "peak" => "峰值",
            "avg" => "平均",
            "sensors" => "传感器",
            "total" => "总计",
            "package" => "封装",
            "peakRail" => "峰值轨",
            "max" => "最大",
            "primary" => "主要",
            _ => key
        }
        : key switch
        {
            "peak" => "Peak",
            "avg" => "Avg",
            "sensors" => "Sensors",
            "total" => "Total",
            "package" => "Package",
            "peakRail" => "Peak rail",
            "max" => "Max",
            "primary" => "Primary",
            _ => key
        };

    public static string MetricLabel(string name, string kind)
    {
        string localizedName = CurrentLanguage == UiLanguage.Chinese ? LocalizeMetricName(name) : name;
        string localizedKind = CurrentLanguage == UiLanguage.Chinese ? LocalizeMetricKind(kind) : kind;
        return $"{localizedName} ({localizedKind})";
    }

    public static string NoSensors => CurrentLanguage == UiLanguage.Chinese ? "无传感器" : "no sensors";

    public static string Normal => CurrentLanguage == UiLanguage.Chinese ? "正常" : "normal";

    public static string SensorCount(int count) => CurrentLanguage == UiLanguage.Chinese ? $"{count} 个传感器" : $"{count} sensors";

    public static string WarningCount(int count) => CurrentLanguage == UiLanguage.Chinese ? $"{count} 个警告" : $"{count} warning";

    public static string HotCount(int count) => CurrentLanguage == UiLanguage.Chinese ? $"{count} 个过热" : $"{count} hot";

    public static string WarmCount(int count) => CurrentLanguage == UiLanguage.Chinese ? $"{count} 个偏热" : $"{count} warm";

    public static string DriverSummary(SensorDriverStatus status)
    {
        if (status.IsReady)
        {
            return string.IsNullOrWhiteSpace(status.Version) ? "PawnIO ready" : $"PawnIO {status.Version}";
        }

        return DriverMessage(status.Message);
    }

    public static string DriverMessage(string message)
    {
        if (CurrentLanguage != UiLanguage.Chinese)
        {
            return message;
        }

        if (message.Equals("PawnIO installed but not loaded; restart as administrator", StringComparison.OrdinalIgnoreCase))
        {
            return "PawnIO 已安装但未加载，请以管理员身份重启";
        }

        if (message.Equals("PawnIO missing; install it for motherboard, fan, and low-level sensors", StringComparison.OrdinalIgnoreCase))
        {
            return "缺少 PawnIO；安装后可读取主板、风扇和底层传感器";
        }

        if (message.StartsWith("PawnIO status unavailable:", StringComparison.OrdinalIgnoreCase))
        {
            return "无法读取 PawnIO 状态：" + message["PawnIO status unavailable:".Length..].Trim();
        }

        return message;
    }

    public static string ResultMessage(string message)
    {
        if (CurrentLanguage != UiLanguage.Chinese)
        {
            return message;
        }

        return message switch
        {
            "Elevated permissions may be required for full hardware sensors" => "可能需要提升权限才能读取完整硬件传感器",
            "No supported hardware sensors found; try running as administrator" => "未找到支持的硬件传感器，请尝试以管理员身份运行",
            _ when message.StartsWith("Sensor read failed:", StringComparison.OrdinalIgnoreCase) => "传感器读取失败：" + message["Sensor read failed:".Length..].Trim(),
            _ => DriverMessage(message)
        };
    }

    private static IReadOnlyDictionary<string, string> ActiveResources => CurrentLanguage == UiLanguage.Chinese ? ChineseResources : EnglishResources;

    private static string LocalizeMetricName(string name) => name switch
    {
        "Physical memory" => "物理内存",
        "Used" => "已用",
        "Total" => "总量",
        "Available" => "可用",
        "Filesystem" => "文件系统",
        "Free" => "空闲",
        _ => name
    };

    private static string LocalizeMetricKind(string kind) => kind switch
    {
        "Load" => "负载",
        "Control" => "控制",
        "Level" => "级别",
        "Temperature" => "温度",
        "Power" => "功耗",
        "Clock" => "频率",
        "Voltage" => "电压",
        "Data" => "数据",
        "SmallData" => "小数据",
        "Fan" => "风扇",
        "Throughput" => "吞吐",
        _ => kind
    };
}
