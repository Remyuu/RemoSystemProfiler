using Avalonia.Controls;
using RemoSystemProfiler.Core;

namespace RemoSystemProfiler;

public enum UiLanguage
{
    English,
    SimplifiedChinese,
    Japanese,
    TraditionalChinese
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
        ["Ui_Chinese"] = "简体中文",
        ["Ui_Japanese"] = "日本語",
        ["Ui_TraditionalChinese"] = "繁體中文",
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
        ["Ui_Benchmark"] = "Benchmark",
        ["Ui_CpuBenchmark"] = "CPU benchmark",
        ["Ui_Mode"] = "Mode",
        ["Ui_Seconds"] = "Seconds",
        ["Ui_Cores"] = "Cores",
        ["Ui_Score"] = "Score",
        ["Ui_Throughput"] = "Throughput",
        ["Ui_Threads"] = "Threads",
        ["Ui_Duration"] = "Duration",
        ["Ui_Progress"] = "Progress",
        ["Ui_Run"] = "Run",
        ["Ui_Cancel"] = "Cancel",
        ["Ui_SingleCore"] = "Single core",
        ["Ui_MultiCore"] = "Multi core",
        ["Ui_Custom"] = "Custom",
        ["Ui_StartupTitle"] = "Connecting to hardware backend",
        ["Ui_StartupSubtitle"] = "Initializing LibreHardwareMonitor and enumerating devices"
    };

    private static readonly IReadOnlyDictionary<string, string> SimplifiedChineseResources = new Dictionary<string, string>
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
        ["Ui_Chinese"] = "简体中文",
        ["Ui_Japanese"] = "日本語",
        ["Ui_TraditionalChinese"] = "繁體中文",
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
        ["Ui_Benchmark"] = "跑分",
        ["Ui_CpuBenchmark"] = "CPU 跑分",
        ["Ui_Mode"] = "模式",
        ["Ui_Seconds"] = "秒数",
        ["Ui_Cores"] = "核心",
        ["Ui_Score"] = "分数",
        ["Ui_Throughput"] = "吞吐",
        ["Ui_Threads"] = "线程",
        ["Ui_Duration"] = "时长",
        ["Ui_Progress"] = "进度",
        ["Ui_Run"] = "运行",
        ["Ui_Cancel"] = "取消",
        ["Ui_SingleCore"] = "单核",
        ["Ui_MultiCore"] = "多核",
        ["Ui_Custom"] = "自定义",
        ["Ui_StartupTitle"] = "正在连接硬件后端",
        ["Ui_StartupSubtitle"] = "正在初始化 LibreHardwareMonitor 并枚举设备"
    };

    private static readonly IReadOnlyDictionary<string, string> JapaneseResources = new Dictionary<string, string>
    {
        ["Ui_WindowTitle"] = "Remo System Profiler",
        ["Ui_Performance"] = "パフォーマンス",
        ["Ui_Settings"] = "設定",
        ["Ui_Dashboard"] = "ダッシュボード",
        ["Ui_ChartRange"] = "チャート範囲",
        ["Ui_UpdateInterval"] = "更新間隔",
        ["Ui_Theme"] = "テーマ",
        ["Ui_Language"] = "言語",
        ["Ui_English"] = "English",
        ["Ui_Chinese"] = "简体中文",
        ["Ui_Japanese"] = "日本語",
        ["Ui_TraditionalChinese"] = "繁體中文",
        ["Ui_RangeTenSeconds"] = "10 秒",
        ["Ui_RangeThirtySeconds"] = "30 秒",
        ["Ui_RangeOneMinute"] = "1 分",
        ["Ui_RangeFiveMinutes"] = "5 分",
        ["Ui_IntervalHalfSecond"] = "0.5 秒",
        ["Ui_IntervalOneSecond"] = "1 秒",
        ["Ui_IntervalTwoSeconds"] = "2 秒",
        ["Ui_IntervalFiveSeconds"] = "5 秒",
        ["Ui_ThemeSystem"] = "システム",
        ["Ui_ThemeLight"] = "ライト",
        ["Ui_ThemeDark"] = "ダーク",
        ["Ui_About"] = "情報",
        ["Ui_AppSubtitle"] = "Avalonia ハードウェアモニター",
        ["Ui_Version"] = "バージョン",
        ["Ui_Website"] = "ウェブサイト",
        ["Ui_BuiltWith"] = "Avalonia 12 と LibreHardwareMonitor で構築。",
        ["Ui_LicenseNotice"] = "サードパーティコンポーネントは元のライセンスに従います。",
        ["Ui_InstallPawnIo"] = "PawnIO をインストール",
        ["Ui_Load"] = "負荷",
        ["Ui_Temp"] = "温度",
        ["Ui_Power"] = "電力",
        ["Ui_LogicalProcessors"] = "論理プロセッサ",
        ["Ui_Utilization"] = "使用率",
        ["Ui_Speed"] = "速度",
        ["Ui_PackagePower"] = "パッケージ電力",
        ["Ui_PeakTemp"] = "ピーク温度",
        ["Ui_CoresThreads"] = "コア / スレッド",
        ["Ui_Memory"] = "メモリ",
        ["Ui_CpuSensors"] = "CPU センサー",
        ["Ui_GpuDevices"] = "GPU デバイス",
        ["Ui_StorageDevices"] = "ストレージデバイス",
        ["Ui_Benchmark"] = "ベンチマーク",
        ["Ui_CpuBenchmark"] = "CPU ベンチマーク",
        ["Ui_Mode"] = "モード",
        ["Ui_Seconds"] = "秒数",
        ["Ui_Cores"] = "コア",
        ["Ui_Score"] = "スコア",
        ["Ui_Throughput"] = "スループット",
        ["Ui_Threads"] = "スレッド",
        ["Ui_Duration"] = "時間",
        ["Ui_Progress"] = "進捗",
        ["Ui_Run"] = "実行",
        ["Ui_Cancel"] = "キャンセル",
        ["Ui_SingleCore"] = "シングルコア",
        ["Ui_MultiCore"] = "マルチコア",
        ["Ui_Custom"] = "カスタム",
        ["Ui_StartupTitle"] = "ハードウェアバックエンドに接続中",
        ["Ui_StartupSubtitle"] = "LibreHardwareMonitor を初期化しデバイスを列挙中"
    };

    private static readonly IReadOnlyDictionary<string, string> TraditionalChineseResources = new Dictionary<string, string>
    {
        ["Ui_WindowTitle"] = "Remo System Profiler",
        ["Ui_Performance"] = "效能",
        ["Ui_Settings"] = "設定",
        ["Ui_Dashboard"] = "儀表板",
        ["Ui_ChartRange"] = "圖表範圍",
        ["Ui_UpdateInterval"] = "更新間隔",
        ["Ui_Theme"] = "主題",
        ["Ui_Language"] = "語言",
        ["Ui_English"] = "English",
        ["Ui_Chinese"] = "简体中文",
        ["Ui_Japanese"] = "日本語",
        ["Ui_TraditionalChinese"] = "繁體中文",
        ["Ui_RangeTenSeconds"] = "10 秒",
        ["Ui_RangeThirtySeconds"] = "30 秒",
        ["Ui_RangeOneMinute"] = "1 分鐘",
        ["Ui_RangeFiveMinutes"] = "5 分鐘",
        ["Ui_IntervalHalfSecond"] = "0.5 秒",
        ["Ui_IntervalOneSecond"] = "1 秒",
        ["Ui_IntervalTwoSeconds"] = "2 秒",
        ["Ui_IntervalFiveSeconds"] = "5 秒",
        ["Ui_ThemeSystem"] = "跟隨系統",
        ["Ui_ThemeLight"] = "淺色",
        ["Ui_ThemeDark"] = "深色",
        ["Ui_About"] = "關於",
        ["Ui_AppSubtitle"] = "Avalonia 硬體監視器",
        ["Ui_Version"] = "版本",
        ["Ui_Website"] = "網站",
        ["Ui_BuiltWith"] = "以 Avalonia 12 和 LibreHardwareMonitor 建置。",
        ["Ui_LicenseNotice"] = "第三方元件保留其原始授權。",
        ["Ui_InstallPawnIo"] = "安裝 PawnIO",
        ["Ui_Load"] = "負載",
        ["Ui_Temp"] = "溫度",
        ["Ui_Power"] = "功耗",
        ["Ui_LogicalProcessors"] = "邏輯處理器",
        ["Ui_Utilization"] = "使用率",
        ["Ui_Speed"] = "頻率",
        ["Ui_PackagePower"] = "封裝功耗",
        ["Ui_PeakTemp"] = "最高溫度",
        ["Ui_CoresThreads"] = "核心 / 執行緒",
        ["Ui_Memory"] = "記憶體",
        ["Ui_CpuSensors"] = "CPU 感測器",
        ["Ui_GpuDevices"] = "GPU 裝置",
        ["Ui_StorageDevices"] = "儲存裝置",
        ["Ui_Benchmark"] = "跑分",
        ["Ui_CpuBenchmark"] = "CPU 跑分",
        ["Ui_Mode"] = "模式",
        ["Ui_Seconds"] = "秒數",
        ["Ui_Cores"] = "核心",
        ["Ui_Score"] = "分數",
        ["Ui_Throughput"] = "吞吐量",
        ["Ui_Threads"] = "執行緒",
        ["Ui_Duration"] = "時長",
        ["Ui_Progress"] = "進度",
        ["Ui_Run"] = "執行",
        ["Ui_Cancel"] = "取消",
        ["Ui_SingleCore"] = "單核",
        ["Ui_MultiCore"] = "多核",
        ["Ui_Custom"] = "自訂",
        ["Ui_StartupTitle"] = "正在連線到硬體後端",
        ["Ui_StartupSubtitle"] = "正在初始化 LibreHardwareMonitor 並列舉裝置"
    };

    private static readonly IReadOnlyDictionary<UiLanguage, IReadOnlyDictionary<string, string>> ResourceSets =
        new Dictionary<UiLanguage, IReadOnlyDictionary<string, string>>
        {
            [UiLanguage.English] = EnglishResources,
            [UiLanguage.SimplifiedChinese] = SimplifiedChineseResources,
            [UiLanguage.Japanese] = JapaneseResources,
            [UiLanguage.TraditionalChinese] = TraditionalChineseResources
        };

    public static UiLanguage CurrentLanguage { get; private set; } = UiLanguage.English;

    public static int CurrentLanguageIndex => (int)CurrentLanguage;

    public static bool SetLanguageFromIndex(int selectedIndex)
    {
        UiLanguage language = selectedIndex switch
        {
            1 => UiLanguage.SimplifiedChinese,
            2 => UiLanguage.Japanese,
            3 => UiLanguage.TraditionalChinese,
            _ => UiLanguage.English
        };

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

    public static string Resource(string key) => ActiveResources.TryGetValue(key, out string? value) ? value : key;

    public static string WaitingForHardwareSensors => Text("Waiting for hardware sensors", "等待硬件传感器", "ハードウェアセンサーを待機中", "等待硬體感測器");

    public static string WaitingForSensors => Text("Waiting for sensors", "等待传感器", "センサーを待機中", "等待感測器");

    public static string OpeningSensorBackend => Text("Opening sensor backend", "正在打开传感器后端", "センサーバックエンドを開いています", "正在開啟感測器後端");

    public static string ExpandSidebar => Text("Expand sidebar", "展开侧边栏", "サイドバーを展開", "展開側邊欄");

    public static string CollapseSidebar => Text("Collapse sidebar", "收起侧边栏", "サイドバーを折りたたむ", "收合側邊欄");

    public static string PawnIoRequired => Text(
        "PawnIO is required for full sensor access",
        "完整传感器访问需要 PawnIO",
        "完全なセンサーアクセスには PawnIO が必要です",
        "完整感測器存取需要 PawnIO");

    public static string SensorBackendUnavailable => Text("Sensor backend unavailable", "传感器后端不可用", "センサーバックエンドは利用できません", "感測器後端不可用");

    public static string ConnectedLimited => Text("Connected with limited sensor access", "已连接，但传感器访问受限", "接続済み、センサーアクセスは制限されています", "已連線，但感測器存取受限");

    public static string ConnectedBackend => Text("Connected to hardware backend", "已连接到硬件后端", "ハードウェアバックエンドに接続済み", "已連線到硬體後端");

    public static string CpuSensorsUnavailable => Text("CPU sensors unavailable", "CPU 传感器不可用", "CPU センサーは利用できません", "CPU 感測器不可用");

    public static string HardwareSensorsUnavailable => Text("Hardware sensors unavailable", "硬件传感器不可用", "ハードウェアセンサーは利用できません", "硬體感測器不可用");

    public static string InstallPawnIoRestartAdmin => Text(
        "Install PawnIO, then restart Remo System Profiler as administrator.",
        "安装 PawnIO，然后以管理员身份重启 Remo System Profiler。",
        "PawnIO をインストールしてから、Remo System Profiler を管理者として再起動してください。",
        "安裝 PawnIO，然後以系統管理員身分重新啟動 Remo System Profiler。");

    public static string ConnectedStatus(string source, string driverSummary) => Text(
        $"Connected\n{source}\n{driverSummary}",
        $"已连接\n{source}\n{driverSummary}",
        $"接続済み\n{source}\n{driverSummary}",
        $"已連線\n{source}\n{driverSummary}");

    public static string LimitedAccessStatus(string source) => Text(
        $"Limited access\n{source}\nRun as administrator",
        $"受限访问\n{source}\n请以管理员身份运行",
        $"制限付きアクセス\n{source}\n管理者として実行してください",
        $"受限存取\n{source}\n請以系統管理員身分執行");

    public static string HardwareSummary(int gpuCount, int storageCount) => Text(
        $"{gpuCount} GPU | {storageCount} storage",
        $"{gpuCount} 个 GPU | {storageCount} 个存储设备",
        $"{gpuCount} GPU | {storageCount} ストレージ",
        $"{gpuCount} 個 GPU | {storageCount} 個儲存裝置");

    public static string OverviewMemory => Text("Memory", "内存", "メモリ", "記憶體");

    public static string OverviewGpu(int index) => $"GPU {index}";

    public static string OverviewDisk(int index) => Text($"Disk {index}", $"磁盘 {index}", $"ディスク {index}", $"磁碟 {index}");

    public static string SensorGroupTitle(string key) => key switch
    {
        "temperature" => Text("Temperature", "温度", "温度", "溫度"),
        "power" => Text("Power", "功耗", "電力", "功耗"),
        "clock" => Text("Clock", "频率", "クロック", "頻率"),
        "voltage" => Text("Voltage", "电压", "電圧", "電壓"),
        _ => key
    };

    public static string SummaryLabel(string key) => key switch
    {
        "peak" => Text("Peak", "峰值", "ピーク", "峰值"),
        "avg" => Text("Avg", "平均", "平均", "平均"),
        "sensors" => Text("Sensors", "传感器", "センサー", "感測器"),
        "total" => Text("Total", "总计", "合計", "總計"),
        "package" => Text("Package", "封装", "パッケージ", "封裝"),
        "peakRail" => Text("Peak rail", "峰值轨", "ピークレール", "峰值軌"),
        "max" => Text("Max", "最大", "最大", "最大"),
        "primary" => Text("Primary", "主要", "メイン", "主要"),
        _ => key
    };

    public static string MetricLabel(string name, string kind)
    {
        string localizedName = LocalizeMetricName(name);
        string localizedKind = LocalizeMetricKind(kind);
        return $"{localizedName} ({localizedKind})";
    }

    public static string NoSensors => Text("no sensors", "无传感器", "センサーなし", "無感測器");

    public static string Normal => Text("normal", "正常", "正常", "正常");

    public static string SensorCount(int count) => Text($"{count} sensors", $"{count} 个传感器", $"{count} センサー", $"{count} 個感測器");

    public static string WarningCount(int count) => Text($"{count} warning", $"{count} 个警告", $"{count} 件の警告", $"{count} 個警告");

    public static string HotCount(int count) => Text($"{count} hot", $"{count} 个过热", $"{count} 高温", $"{count} 個過熱");

    public static string WarmCount(int count) => Text($"{count} warm", $"{count} 个偏热", $"{count} やや高温", $"{count} 個偏熱");

    public static string BenchmarkReady => Text("Ready", "就绪", "準備完了", "就緒");

    public static string BenchmarkRunButton => Text("Run", "运行", "実行", "執行");

    public static string BenchmarkRunningButton => Text("Running", "运行中", "実行中", "執行中");

    public static string BenchmarkCanceled => Text("Canceled", "已取消", "キャンセル済み", "已取消");

    public static string BenchmarkCanceling => Text("Canceling benchmark", "正在取消跑分", "ベンチマークをキャンセル中", "正在取消跑分");

    public static string BenchmarkCompletedAt(string timeText) => Text($"Completed at {timeText}", $"完成于 {timeText}", $"{timeText} に完了", $"完成於 {timeText}");

    public static string BenchmarkFailed(string message) => Text($"Benchmark failed: {message}", $"跑分失败：{message}", $"ベンチマーク失敗: {message}", $"跑分失敗：{message}");

    public static string BenchmarkRunningMode(string modeText) => Text(
        $"Running {modeText.ToLowerInvariant()} benchmark",
        $"正在运行{modeText}跑分",
        $"{modeText}ベンチマークを実行中",
        $"正在執行{modeText}跑分");

    public static string BenchmarkRunningProgress(double elapsedSeconds, double durationSeconds) => Text(
        $"Running {elapsedSeconds:0.0}s / {durationSeconds:0}s",
        $"运行中 {elapsedSeconds:0.0} 秒 / {durationSeconds:0} 秒",
        $"実行中 {elapsedSeconds:0.0} 秒 / {durationSeconds:0} 秒",
        $"執行中 {elapsedSeconds:0.0} 秒 / {durationSeconds:0} 秒");

    public static string BenchmarkThreadCount(int count) => Text(
        $"{count} thread{(count == 1 ? string.Empty : "s")}",
        $"{count} 线程",
        $"{count} スレッド",
        $"{count} 執行緒");

    public static string BenchmarkDurationRun(int seconds) => Text($"{seconds}s run", $"{seconds} 秒运行", $"{seconds} 秒実行", $"{seconds} 秒執行");

    public static string BenchmarkModeText(int modeIndex) => modeIndex switch
    {
        0 => Text("Single core", "单核", "シングルコア", "單核"),
        1 => Text("Multi core", "多核", "マルチコア", "多核"),
        _ => Text("Custom", "自定义", "カスタム", "自訂")
    };

    public static string DriverSummary(SensorDriverStatus status)
    {
        if (status.IsReady)
        {
            return string.IsNullOrWhiteSpace(status.Version)
                ? Text("PawnIO ready", "PawnIO 就绪", "PawnIO 準備完了", "PawnIO 就緒")
                : $"PawnIO {status.Version}";
        }

        return DriverMessage(status.Message);
    }

    public static string DriverMessage(string message)
    {
        if (message.Equals("PawnIO installed but not loaded; restart as administrator", StringComparison.OrdinalIgnoreCase))
        {
            return Text(
                message,
                "PawnIO 已安装但未加载，请以管理员身份重启",
                "PawnIO はインストール済みですが読み込まれていません。管理者として再起動してください",
                "PawnIO 已安裝但未載入，請以系統管理員身分重新啟動");
        }

        if (message.Equals("PawnIO missing; install it for motherboard, fan, and low-level sensors", StringComparison.OrdinalIgnoreCase))
        {
            return Text(
                message,
                "缺少 PawnIO；安装后可读取主板、风扇和底层传感器",
                "PawnIO がありません。マザーボード、ファン、低レベルセンサーにはインストールが必要です",
                "缺少 PawnIO；安裝後可讀取主機板、風扇和底層感測器");
        }

        if (message.StartsWith("PawnIO status unavailable:", StringComparison.OrdinalIgnoreCase))
        {
            string detail = message["PawnIO status unavailable:".Length..].Trim();
            return Text(
                "PawnIO status unavailable: " + detail,
                "无法读取 PawnIO 状态：" + detail,
                "PawnIO 状態を取得できません: " + detail,
                "無法讀取 PawnIO 狀態：" + detail);
        }

        return message;
    }

    public static string ResultMessage(string message)
    {
        if (message.Equals("Elevated permissions may be required for full hardware sensors", StringComparison.OrdinalIgnoreCase))
        {
            return Text(
                message,
                "可能需要提升权限才能读取完整硬件传感器",
                "完全なハードウェアセンサーには昇格権限が必要な場合があります",
                "可能需要提升權限才能讀取完整硬體感測器");
        }

        if (message.Equals("No supported hardware sensors found; try running as administrator", StringComparison.OrdinalIgnoreCase))
        {
            return Text(
                message,
                "未找到支持的硬件传感器，请尝试以管理员身份运行",
                "対応するハードウェアセンサーが見つかりません。管理者として実行してみてください",
                "未找到支援的硬體感測器，請嘗試以系統管理員身分執行");
        }

        if (message.StartsWith("Sensor read failed:", StringComparison.OrdinalIgnoreCase))
        {
            string detail = message["Sensor read failed:".Length..].Trim();
            return Text(
                "Sensor read failed: " + detail,
                "传感器读取失败：" + detail,
                "センサー読み取り失敗: " + detail,
                "感測器讀取失敗：" + detail);
        }

        return DriverMessage(message);
    }

    private static IReadOnlyDictionary<string, string> ActiveResources => ResourceSets[CurrentLanguage];

    private static string Text(string english, string simplifiedChinese, string japanese, string traditionalChinese) => CurrentLanguage switch
    {
        UiLanguage.SimplifiedChinese => simplifiedChinese,
        UiLanguage.Japanese => japanese,
        UiLanguage.TraditionalChinese => traditionalChinese,
        _ => english
    };

    private static string LocalizeMetricName(string name) => name switch
    {
        "Physical memory" => Text(name, "物理内存", "物理メモリ", "實體記憶體"),
        "Used" => Text(name, "已用", "使用中", "已用"),
        "Total" => Text(name, "总量", "合計", "總量"),
        "Available" => Text(name, "可用", "利用可能", "可用"),
        "Filesystem" => Text(name, "文件系统", "ファイルシステム", "檔案系統"),
        "Free" => Text(name, "空闲", "空き", "可用"),
        _ => name
    };

    private static string LocalizeMetricKind(string kind) => kind switch
    {
        "Load" => Text(kind, "负载", "負荷", "負載"),
        "Control" => Text(kind, "控制", "制御", "控制"),
        "Level" => Text(kind, "级别", "レベル", "等級"),
        "Temperature" => Text(kind, "温度", "温度", "溫度"),
        "Power" => Text(kind, "功耗", "電力", "功耗"),
        "Clock" => Text(kind, "频率", "クロック", "頻率"),
        "Voltage" => Text(kind, "电压", "電圧", "電壓"),
        "Data" => Text(kind, "数据", "データ", "資料"),
        "SmallData" => Text(kind, "小数据", "小データ", "小型資料"),
        "Fan" => Text(kind, "风扇", "ファン", "風扇"),
        "Throughput" => Text(kind, "吞吐", "スループット", "吞吐量"),
        _ => kind
    };
}
