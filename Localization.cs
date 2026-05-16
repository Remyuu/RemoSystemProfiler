using Avalonia.Controls;
using RemoSystemProfiler.Core;

namespace RemoSystemProfiler;

public enum UiLanguage
{
    English,
    SimplifiedChinese,
    Japanese,
    TraditionalChinese,
    Spanish,
    German,
    French
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
        ["Ui_Spanish"] = "Español",
        ["Ui_German"] = "Deutsch",
        ["Ui_French"] = "Français",
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
        ["Ui_CheckForUpdates"] = "Check for updates",
        ["Ui_DownloadAndInstall"] = "Download and install",
        ["Ui_OpenRelease"] = "Open release",
        ["Ui_ReleaseNotes"] = "Release notes",
        ["Ui_InstallPawnIo"] = "Download and install PawnIO",
        ["Ui_OpenDataFolder"] = "Open cache folder",
        ["Ui_Load"] = "Load",
        ["Ui_Temp"] = "Temp",
        ["Ui_Power"] = "Power",
        ["Ui_Voltage"] = "Voltage",
        ["Ui_LogicalProcessors"] = "Logical processors",
        ["Ui_OverallUtilization"] = "Overall utilization",
        ["Ui_LogicalProcessorsShort"] = "Cores",
        ["Ui_OverallUtilizationShort"] = "Total",
        ["Ui_ShowLogicalProcessors"] = "Show logical processors",
        ["Ui_ShowOverallUtilization"] = "Show overall utilization",
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
        ["Ui_Profile"] = "Profile",
        ["Ui_Quick"] = "Quick",
        ["Ui_Standard"] = "Standard",
        ["Ui_Sustained"] = "Sustained",
        ["Ui_BenchmarkVersion"] = "Benchmark version",
        ["Ui_Mode"] = "Mode",
        ["Ui_Cores"] = "Cores",
        ["Ui_Score"] = "Score",
        ["Ui_CpuCoreScore"] = "CPU Core Score",
        ["Ui_CpuMixedScore"] = "CPU Mixed Score",
        ["Ui_CpuCoreShort"] = "CPU Core",
        ["Ui_CpuMixedShort"] = "CPU Mixed",
        ["Ui_LeaderboardScore"] = "Ranking",
        ["Ui_BenchmarkTelemetry"] = "CPU telemetry",
        ["Ui_SciMark"] = "SciMark",
        ["Ui_Zstd"] = "zstd",
        ["Ui_ZstdCompression"] = "zstd compression",
        ["Ui_ZstdDecompression"] = "zstd decompression",
        ["Ui_XxHash3"] = "XxHash3",
        ["Ui_CpuAverageFrequency"] = "CPU avg frequency",
        ["Ui_CpuMaxTemperature"] = "CPU max temp",
        ["Ui_CpuEnergy"] = "CPU energy",
        ["Ui_CpuPeakPower"] = "Peak power",
        ["Ui_PowerThermal"] = "Power / throttle",
        ["Ui_Validation"] = "Validation",
        ["Ui_Leaderboard"] = "Leaderboard",
        ["Ui_DisplayName"] = "Display name",
        ["Ui_Progress"] = "Progress",
        ["Ui_Cancel"] = "Cancel",
        ["Ui_SingleCore"] = "Single core",
        ["Ui_MultiCore"] = "Multi core",
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
        ["Ui_Spanish"] = "Español",
        ["Ui_German"] = "Deutsch",
        ["Ui_French"] = "Français",
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
        ["Ui_CheckForUpdates"] = "检查更新",
        ["Ui_DownloadAndInstall"] = "下载并安装",
        ["Ui_OpenRelease"] = "打开发布页",
        ["Ui_ReleaseNotes"] = "更新日志",
        ["Ui_InstallPawnIo"] = "下载并安装 PawnIO",
        ["Ui_OpenDataFolder"] = "打开缓存文件夹",
        ["Ui_Load"] = "负载",
        ["Ui_Temp"] = "温度",
        ["Ui_Power"] = "功耗",
        ["Ui_Voltage"] = "电压",
        ["Ui_LogicalProcessors"] = "逻辑处理器",
        ["Ui_OverallUtilization"] = "总体利用率",
        ["Ui_LogicalProcessorsShort"] = "逻辑",
        ["Ui_OverallUtilizationShort"] = "总体",
        ["Ui_ShowLogicalProcessors"] = "切换到逻辑处理器",
        ["Ui_ShowOverallUtilization"] = "切换到总体利用率",
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
        ["Ui_Profile"] = "档位",
        ["Ui_Quick"] = "快速",
        ["Ui_Standard"] = "标准",
        ["Ui_Sustained"] = "持续",
        ["Ui_BenchmarkVersion"] = "跑分版本",
        ["Ui_Mode"] = "模式",
        ["Ui_Cores"] = "核心",
        ["Ui_Score"] = "分数",
        ["Ui_CpuCoreScore"] = "CPU 核心分数",
        ["Ui_CpuMixedScore"] = "CPU Mixed 分数",
        ["Ui_CpuCoreShort"] = "CPU Core",
        ["Ui_CpuMixedShort"] = "CPU Mixed",
        ["Ui_LeaderboardScore"] = "排行方式",
        ["Ui_BenchmarkTelemetry"] = "CPU 监控曲线",
        ["Ui_SciMark"] = "SciMark",
        ["Ui_Zstd"] = "zstd",
        ["Ui_ZstdCompression"] = "zstd 压缩",
        ["Ui_ZstdDecompression"] = "zstd 解压",
        ["Ui_XxHash3"] = "XxHash3",
        ["Ui_CpuAverageFrequency"] = "CPU 平均频率",
        ["Ui_CpuMaxTemperature"] = "CPU 最高温度",
        ["Ui_CpuEnergy"] = "CPU 累计能耗",
        ["Ui_CpuPeakPower"] = "最高功率",
        ["Ui_PowerThermal"] = "功耗 / 降频",
        ["Ui_Validation"] = "校验",
        ["Ui_Leaderboard"] = "排行榜",
        ["Ui_DisplayName"] = "显示名",
        ["Ui_Progress"] = "进度",
        ["Ui_Cancel"] = "取消",
        ["Ui_SingleCore"] = "单核",
        ["Ui_MultiCore"] = "多核",
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
        ["Ui_Spanish"] = "Español",
        ["Ui_German"] = "Deutsch",
        ["Ui_French"] = "Français",
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
        ["Ui_CheckForUpdates"] = "更新を確認",
        ["Ui_DownloadAndInstall"] = "ダウンロードしてインストール",
        ["Ui_OpenRelease"] = "リリースを開く",
        ["Ui_ReleaseNotes"] = "リリースノート",
        ["Ui_InstallPawnIo"] = "PawnIO をダウンロードしてインストール",
        ["Ui_OpenDataFolder"] = "キャッシュフォルダーを開く",
        ["Ui_Load"] = "負荷",
        ["Ui_Temp"] = "温度",
        ["Ui_Power"] = "電力",
        ["Ui_Voltage"] = "電圧",
        ["Ui_LogicalProcessors"] = "論理プロセッサ",
        ["Ui_OverallUtilization"] = "全体使用率",
        ["Ui_LogicalProcessorsShort"] = "論理",
        ["Ui_OverallUtilizationShort"] = "全体",
        ["Ui_ShowLogicalProcessors"] = "論理プロセッサを表示",
        ["Ui_ShowOverallUtilization"] = "全体使用率を表示",
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
        ["Ui_Profile"] = "プロファイル",
        ["Ui_Quick"] = "クイック",
        ["Ui_Standard"] = "標準",
        ["Ui_Sustained"] = "持続",
        ["Ui_BenchmarkVersion"] = "Benchmark version",
        ["Ui_Mode"] = "モード",
        ["Ui_Cores"] = "コア",
        ["Ui_Score"] = "スコア",
        ["Ui_CpuCoreScore"] = "CPU Core Score",
        ["Ui_CpuMixedScore"] = "CPU Mixed Score",
        ["Ui_CpuCoreShort"] = "CPU Core",
        ["Ui_CpuMixedShort"] = "CPU Mixed",
        ["Ui_LeaderboardScore"] = "Ranking",
        ["Ui_BenchmarkTelemetry"] = "CPU telemetry",
        ["Ui_SciMark"] = "SciMark",
        ["Ui_Zstd"] = "zstd",
        ["Ui_ZstdCompression"] = "zstd compression",
        ["Ui_ZstdDecompression"] = "zstd decompression",
        ["Ui_XxHash3"] = "XxHash3",
        ["Ui_CpuAverageFrequency"] = "CPU avg frequency",
        ["Ui_CpuMaxTemperature"] = "CPU max temp",
        ["Ui_CpuEnergy"] = "CPU energy",
        ["Ui_CpuPeakPower"] = "Peak power",
        ["Ui_PowerThermal"] = "Power / throttle",
        ["Ui_Validation"] = "Validation",
        ["Ui_Leaderboard"] = "ランキング",
        ["Ui_DisplayName"] = "表示名",
        ["Ui_Progress"] = "進捗",
        ["Ui_Cancel"] = "キャンセル",
        ["Ui_SingleCore"] = "シングルコア",
        ["Ui_MultiCore"] = "マルチコア",
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
        ["Ui_Spanish"] = "Español",
        ["Ui_German"] = "Deutsch",
        ["Ui_French"] = "Français",
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
        ["Ui_CheckForUpdates"] = "檢查更新",
        ["Ui_DownloadAndInstall"] = "下載並安裝",
        ["Ui_OpenRelease"] = "開啟發布頁",
        ["Ui_ReleaseNotes"] = "更新日誌",
        ["Ui_InstallPawnIo"] = "下載並安裝 PawnIO",
        ["Ui_OpenDataFolder"] = "開啟快取資料夾",
        ["Ui_Load"] = "負載",
        ["Ui_Temp"] = "溫度",
        ["Ui_Power"] = "功耗",
        ["Ui_Voltage"] = "電壓",
        ["Ui_LogicalProcessors"] = "邏輯處理器",
        ["Ui_OverallUtilization"] = "總體使用率",
        ["Ui_LogicalProcessorsShort"] = "邏輯",
        ["Ui_OverallUtilizationShort"] = "總體",
        ["Ui_ShowLogicalProcessors"] = "切換到邏輯處理器",
        ["Ui_ShowOverallUtilization"] = "切換到總體使用率",
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
        ["Ui_Profile"] = "檔位",
        ["Ui_Quick"] = "快速",
        ["Ui_Standard"] = "標準",
        ["Ui_Sustained"] = "持續",
        ["Ui_BenchmarkVersion"] = "跑分版本",
        ["Ui_Mode"] = "模式",
        ["Ui_Cores"] = "核心",
        ["Ui_Score"] = "分數",
        ["Ui_CpuCoreScore"] = "CPU 核心分數",
        ["Ui_CpuMixedScore"] = "CPU Mixed 分數",
        ["Ui_CpuCoreShort"] = "CPU Core",
        ["Ui_CpuMixedShort"] = "CPU Mixed",
        ["Ui_LeaderboardScore"] = "排行方式",
        ["Ui_BenchmarkTelemetry"] = "CPU 監控曲線",
        ["Ui_SciMark"] = "SciMark",
        ["Ui_Zstd"] = "zstd",
        ["Ui_ZstdCompression"] = "zstd 壓縮",
        ["Ui_ZstdDecompression"] = "zstd 解壓",
        ["Ui_XxHash3"] = "XxHash3",
        ["Ui_CpuAverageFrequency"] = "CPU 平均頻率",
        ["Ui_CpuMaxTemperature"] = "CPU 最高溫度",
        ["Ui_CpuEnergy"] = "CPU 累計能耗",
        ["Ui_CpuPeakPower"] = "最高功率",
        ["Ui_PowerThermal"] = "功耗 / 降頻",
        ["Ui_Validation"] = "校驗",
        ["Ui_Leaderboard"] = "排行榜",
        ["Ui_DisplayName"] = "顯示名稱",
        ["Ui_Progress"] = "進度",
        ["Ui_Cancel"] = "取消",
        ["Ui_SingleCore"] = "單核",
        ["Ui_MultiCore"] = "多核",
        ["Ui_StartupTitle"] = "正在連線到硬體後端",
        ["Ui_StartupSubtitle"] = "正在初始化 LibreHardwareMonitor 並列舉裝置"
    };

    private static readonly IReadOnlyDictionary<string, string> SpanishResources = new Dictionary<string, string>(EnglishResources)
    {
        ["Ui_Performance"] = "Rendimiento",
        ["Ui_Settings"] = "Configuración",
        ["Ui_Dashboard"] = "Panel",
        ["Ui_ChartRange"] = "Rango del gráfico",
        ["Ui_UpdateInterval"] = "Intervalo de actualización",
        ["Ui_Theme"] = "Tema",
        ["Ui_Language"] = "Idioma",
        ["Ui_RangeTenSeconds"] = "10 segundos",
        ["Ui_RangeThirtySeconds"] = "30 segundos",
        ["Ui_RangeOneMinute"] = "1 minuto",
        ["Ui_RangeFiveMinutes"] = "5 minutos",
        ["Ui_IntervalHalfSecond"] = "0.5 segundos",
        ["Ui_IntervalOneSecond"] = "1 segundo",
        ["Ui_IntervalTwoSeconds"] = "2 segundos",
        ["Ui_IntervalFiveSeconds"] = "5 segundos",
        ["Ui_ThemeSystem"] = "Sistema",
        ["Ui_ThemeLight"] = "Claro",
        ["Ui_ThemeDark"] = "Oscuro",
        ["Ui_About"] = "Acerca de",
        ["Ui_AppSubtitle"] = "Monitor de hardware Avalonia",
        ["Ui_Version"] = "Versión",
        ["Ui_Website"] = "Sitio web",
        ["Ui_BuiltWith"] = "Creado con Avalonia 12 y LibreHardwareMonitor.",
        ["Ui_LicenseNotice"] = "Los componentes de terceros conservan sus licencias originales.",
        ["Ui_CheckForUpdates"] = "Buscar actualizaciones",
        ["Ui_DownloadAndInstall"] = "Descargar e instalar",
        ["Ui_OpenRelease"] = "Abrir versión",
        ["Ui_ReleaseNotes"] = "Notas de la versión",
        ["Ui_InstallPawnIo"] = "Descargar e instalar PawnIO",
        ["Ui_OpenDataFolder"] = "Abrir carpeta de caché",
        ["Ui_Load"] = "Carga",
        ["Ui_Temp"] = "Temp.",
        ["Ui_Power"] = "Potencia",
        ["Ui_Voltage"] = "Voltaje",
        ["Ui_LogicalProcessors"] = "Procesadores lógicos",
        ["Ui_OverallUtilization"] = "Uso total",
        ["Ui_LogicalProcessorsShort"] = "Núcleos",
        ["Ui_OverallUtilizationShort"] = "Total",
        ["Ui_ShowLogicalProcessors"] = "Mostrar procesadores lógicos",
        ["Ui_ShowOverallUtilization"] = "Mostrar uso total",
        ["Ui_Utilization"] = "Uso",
        ["Ui_Speed"] = "Velocidad",
        ["Ui_PackagePower"] = "Potencia del paquete",
        ["Ui_PeakTemp"] = "Temp. máxima",
        ["Ui_CoresThreads"] = "Núcleos / hilos",
        ["Ui_Memory"] = "Memoria",
        ["Ui_CpuSensors"] = "Sensores de CPU",
        ["Ui_GpuDevices"] = "Dispositivos GPU",
        ["Ui_StorageDevices"] = "Dispositivos de almacenamiento",
        ["Ui_Benchmark"] = "Prueba",
        ["Ui_Profile"] = "Perfil",
        ["Ui_Quick"] = "Rápida",
        ["Ui_Standard"] = "Estándar",
        ["Ui_Sustained"] = "Sostenida",
        ["Ui_BenchmarkVersion"] = "Benchmark version",
        ["Ui_Mode"] = "Modo",
        ["Ui_Cores"] = "Núcleos",
        ["Ui_Score"] = "Puntuación",
        ["Ui_CpuCoreScore"] = "CPU Core Score",
        ["Ui_CpuMixedScore"] = "CPU Mixed Score",
        ["Ui_CpuCoreShort"] = "CPU Core",
        ["Ui_CpuMixedShort"] = "CPU Mixed",
        ["Ui_LeaderboardScore"] = "Ranking",
        ["Ui_BenchmarkTelemetry"] = "CPU telemetry",
        ["Ui_SciMark"] = "SciMark",
        ["Ui_Zstd"] = "zstd",
        ["Ui_ZstdCompression"] = "zstd compression",
        ["Ui_ZstdDecompression"] = "zstd decompression",
        ["Ui_XxHash3"] = "XxHash3",
        ["Ui_CpuAverageFrequency"] = "CPU avg frequency",
        ["Ui_CpuMaxTemperature"] = "CPU max temp",
        ["Ui_CpuEnergy"] = "CPU energy",
        ["Ui_CpuPeakPower"] = "Peak power",
        ["Ui_PowerThermal"] = "Power / throttle",
        ["Ui_Validation"] = "Validation",
        ["Ui_Leaderboard"] = "Clasificación",
        ["Ui_DisplayName"] = "Nombre visible",
        ["Ui_Progress"] = "Progreso",
        ["Ui_Cancel"] = "Cancelar",
        ["Ui_SingleCore"] = "Un núcleo",
        ["Ui_MultiCore"] = "Varios núcleos",
        ["Ui_StartupTitle"] = "Conectando con el backend de hardware",
        ["Ui_StartupSubtitle"] = "Inicializando LibreHardwareMonitor y enumerando dispositivos"
    };

    private static readonly IReadOnlyDictionary<string, string> GermanResources = new Dictionary<string, string>(EnglishResources)
    {
        ["Ui_Performance"] = "Leistung",
        ["Ui_Settings"] = "Einstellungen",
        ["Ui_Dashboard"] = "Dashboard",
        ["Ui_ChartRange"] = "Diagrammbereich",
        ["Ui_UpdateInterval"] = "Aktualisierungsintervall",
        ["Ui_Theme"] = "Design",
        ["Ui_Language"] = "Sprache",
        ["Ui_RangeTenSeconds"] = "10 Sekunden",
        ["Ui_RangeThirtySeconds"] = "30 Sekunden",
        ["Ui_RangeOneMinute"] = "1 Minute",
        ["Ui_RangeFiveMinutes"] = "5 Minuten",
        ["Ui_IntervalHalfSecond"] = "0.5 Sekunden",
        ["Ui_IntervalOneSecond"] = "1 Sekunde",
        ["Ui_IntervalTwoSeconds"] = "2 Sekunden",
        ["Ui_IntervalFiveSeconds"] = "5 Sekunden",
        ["Ui_ThemeSystem"] = "System",
        ["Ui_ThemeLight"] = "Hell",
        ["Ui_ThemeDark"] = "Dunkel",
        ["Ui_About"] = "Info",
        ["Ui_AppSubtitle"] = "Avalonia-Hardwaremonitor",
        ["Ui_Version"] = "Version",
        ["Ui_Website"] = "Website",
        ["Ui_BuiltWith"] = "Erstellt mit Avalonia 12 und LibreHardwareMonitor.",
        ["Ui_LicenseNotice"] = "Drittanbieterkomponenten behalten ihre ursprünglichen Lizenzen.",
        ["Ui_CheckForUpdates"] = "Nach Updates suchen",
        ["Ui_DownloadAndInstall"] = "Herunterladen und installieren",
        ["Ui_OpenRelease"] = "Release öffnen",
        ["Ui_ReleaseNotes"] = "Versionshinweise",
        ["Ui_InstallPawnIo"] = "PawnIO herunterladen und installieren",
        ["Ui_OpenDataFolder"] = "Cache-Ordner öffnen",
        ["Ui_Load"] = "Last",
        ["Ui_Temp"] = "Temp.",
        ["Ui_Power"] = "Leistung",
        ["Ui_Voltage"] = "Spannung",
        ["Ui_LogicalProcessors"] = "Logische Prozessoren",
        ["Ui_OverallUtilization"] = "Gesamtauslastung",
        ["Ui_LogicalProcessorsShort"] = "Kerne",
        ["Ui_OverallUtilizationShort"] = "Gesamt",
        ["Ui_ShowLogicalProcessors"] = "Logische Prozessoren anzeigen",
        ["Ui_ShowOverallUtilization"] = "Gesamtauslastung anzeigen",
        ["Ui_Utilization"] = "Auslastung",
        ["Ui_Speed"] = "Geschwindigkeit",
        ["Ui_PackagePower"] = "Package-Leistung",
        ["Ui_PeakTemp"] = "Spitzentemp.",
        ["Ui_CoresThreads"] = "Kerne / Threads",
        ["Ui_Memory"] = "Speicher",
        ["Ui_CpuSensors"] = "CPU-Sensoren",
        ["Ui_GpuDevices"] = "GPU-Geräte",
        ["Ui_StorageDevices"] = "Speichergeräte",
        ["Ui_Benchmark"] = "Benchmark",
        ["Ui_Profile"] = "Profil",
        ["Ui_Quick"] = "Kurz",
        ["Ui_Standard"] = "Standard",
        ["Ui_Sustained"] = "Dauer",
        ["Ui_BenchmarkVersion"] = "Benchmark version",
        ["Ui_Mode"] = "Modus",
        ["Ui_Cores"] = "Kerne",
        ["Ui_Score"] = "Punktzahl",
        ["Ui_CpuCoreScore"] = "CPU Core Score",
        ["Ui_CpuMixedScore"] = "CPU Mixed Score",
        ["Ui_CpuCoreShort"] = "CPU Core",
        ["Ui_CpuMixedShort"] = "CPU Mixed",
        ["Ui_LeaderboardScore"] = "Ranking",
        ["Ui_BenchmarkTelemetry"] = "CPU telemetry",
        ["Ui_SciMark"] = "SciMark",
        ["Ui_Zstd"] = "zstd",
        ["Ui_ZstdCompression"] = "zstd compression",
        ["Ui_ZstdDecompression"] = "zstd decompression",
        ["Ui_XxHash3"] = "XxHash3",
        ["Ui_CpuAverageFrequency"] = "CPU avg frequency",
        ["Ui_CpuMaxTemperature"] = "CPU max temp",
        ["Ui_CpuEnergy"] = "CPU energy",
        ["Ui_CpuPeakPower"] = "Peak power",
        ["Ui_PowerThermal"] = "Power / throttle",
        ["Ui_Validation"] = "Validation",
        ["Ui_Leaderboard"] = "Bestenliste",
        ["Ui_DisplayName"] = "Anzeigename",
        ["Ui_Progress"] = "Fortschritt",
        ["Ui_Cancel"] = "Abbrechen",
        ["Ui_SingleCore"] = "Ein Kern",
        ["Ui_MultiCore"] = "Mehrere Kerne",
        ["Ui_StartupTitle"] = "Verbindung zum Hardware-Backend",
        ["Ui_StartupSubtitle"] = "LibreHardwareMonitor wird initialisiert und Geräte werden aufgelistet"
    };

    private static readonly IReadOnlyDictionary<string, string> FrenchResources = new Dictionary<string, string>(EnglishResources)
    {
        ["Ui_Performance"] = "Performances",
        ["Ui_Settings"] = "Paramètres",
        ["Ui_Dashboard"] = "Tableau de bord",
        ["Ui_ChartRange"] = "Plage du graphique",
        ["Ui_UpdateInterval"] = "Intervalle d'actualisation",
        ["Ui_Theme"] = "Thème",
        ["Ui_Language"] = "Langue",
        ["Ui_RangeTenSeconds"] = "10 secondes",
        ["Ui_RangeThirtySeconds"] = "30 secondes",
        ["Ui_RangeOneMinute"] = "1 minute",
        ["Ui_RangeFiveMinutes"] = "5 minutes",
        ["Ui_IntervalHalfSecond"] = "0.5 seconde",
        ["Ui_IntervalOneSecond"] = "1 seconde",
        ["Ui_IntervalTwoSeconds"] = "2 secondes",
        ["Ui_IntervalFiveSeconds"] = "5 secondes",
        ["Ui_ThemeSystem"] = "Système",
        ["Ui_ThemeLight"] = "Clair",
        ["Ui_ThemeDark"] = "Sombre",
        ["Ui_About"] = "À propos",
        ["Ui_AppSubtitle"] = "Moniteur matériel Avalonia",
        ["Ui_Version"] = "Version",
        ["Ui_Website"] = "Site web",
        ["Ui_BuiltWith"] = "Créé avec Avalonia 12 et LibreHardwareMonitor.",
        ["Ui_LicenseNotice"] = "Les composants tiers conservent leurs licences d'origine.",
        ["Ui_CheckForUpdates"] = "Rechercher des mises à jour",
        ["Ui_DownloadAndInstall"] = "Télécharger et installer",
        ["Ui_OpenRelease"] = "Ouvrir la version",
        ["Ui_ReleaseNotes"] = "Notes de version",
        ["Ui_InstallPawnIo"] = "Télécharger et installer PawnIO",
        ["Ui_OpenDataFolder"] = "Ouvrir le dossier de cache",
        ["Ui_Load"] = "Charge",
        ["Ui_Temp"] = "Temp.",
        ["Ui_Power"] = "Puissance",
        ["Ui_Voltage"] = "Tension",
        ["Ui_LogicalProcessors"] = "Processeurs logiques",
        ["Ui_OverallUtilization"] = "Utilisation totale",
        ["Ui_LogicalProcessorsShort"] = "Coeurs",
        ["Ui_OverallUtilizationShort"] = "Total",
        ["Ui_ShowLogicalProcessors"] = "Afficher les processeurs logiques",
        ["Ui_ShowOverallUtilization"] = "Afficher l'utilisation totale",
        ["Ui_Utilization"] = "Utilisation",
        ["Ui_Speed"] = "Vitesse",
        ["Ui_PackagePower"] = "Puissance du package",
        ["Ui_PeakTemp"] = "Temp. max",
        ["Ui_CoresThreads"] = "Coeurs / threads",
        ["Ui_Memory"] = "Mémoire",
        ["Ui_CpuSensors"] = "Capteurs CPU",
        ["Ui_GpuDevices"] = "Périphériques GPU",
        ["Ui_StorageDevices"] = "Périphériques de stockage",
        ["Ui_Benchmark"] = "Benchmark",
        ["Ui_Profile"] = "Profil",
        ["Ui_Quick"] = "Rapide",
        ["Ui_Standard"] = "Standard",
        ["Ui_Sustained"] = "Soutenu",
        ["Ui_BenchmarkVersion"] = "Benchmark version",
        ["Ui_Mode"] = "Mode",
        ["Ui_Cores"] = "Coeurs",
        ["Ui_Score"] = "Score",
        ["Ui_CpuCoreScore"] = "CPU Core Score",
        ["Ui_CpuMixedScore"] = "CPU Mixed Score",
        ["Ui_CpuCoreShort"] = "CPU Core",
        ["Ui_CpuMixedShort"] = "CPU Mixed",
        ["Ui_LeaderboardScore"] = "Ranking",
        ["Ui_BenchmarkTelemetry"] = "CPU telemetry",
        ["Ui_SciMark"] = "SciMark",
        ["Ui_Zstd"] = "zstd",
        ["Ui_ZstdCompression"] = "zstd compression",
        ["Ui_ZstdDecompression"] = "zstd decompression",
        ["Ui_XxHash3"] = "XxHash3",
        ["Ui_CpuAverageFrequency"] = "CPU avg frequency",
        ["Ui_CpuMaxTemperature"] = "CPU max temp",
        ["Ui_CpuEnergy"] = "CPU energy",
        ["Ui_CpuPeakPower"] = "Peak power",
        ["Ui_PowerThermal"] = "Power / throttle",
        ["Ui_Validation"] = "Validation",
        ["Ui_Leaderboard"] = "Classement",
        ["Ui_DisplayName"] = "Nom affiché",
        ["Ui_Progress"] = "Progression",
        ["Ui_Cancel"] = "Annuler",
        ["Ui_SingleCore"] = "Coeur unique",
        ["Ui_MultiCore"] = "Multi-coeur",
        ["Ui_StartupTitle"] = "Connexion au backend matériel",
        ["Ui_StartupSubtitle"] = "Initialisation de LibreHardwareMonitor et énumération des périphériques"
    };

    private static readonly IReadOnlyDictionary<UiLanguage, IReadOnlyDictionary<string, string>> ResourceSets =
        new Dictionary<UiLanguage, IReadOnlyDictionary<string, string>>
        {
            [UiLanguage.English] = EnglishResources,
            [UiLanguage.SimplifiedChinese] = SimplifiedChineseResources,
            [UiLanguage.Japanese] = JapaneseResources,
            [UiLanguage.TraditionalChinese] = TraditionalChineseResources,
            [UiLanguage.Spanish] = SpanishResources,
            [UiLanguage.German] = GermanResources,
            [UiLanguage.French] = FrenchResources
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
            4 => UiLanguage.Spanish,
            5 => UiLanguage.German,
            6 => UiLanguage.French,
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

    public static string WaitingForHardwareSensors => Text("Waiting for hardware sensors", "等待硬件传感器", "ハードウェアセンサーを待機中", "等待硬體感測器", "Esperando sensores de hardware", "Warten auf Hardwaresensoren", "En attente des capteurs matériels");

    public static string WaitingForSensors => Text("Waiting for sensors", "等待传感器", "センサーを待機中", "等待感測器", "Esperando sensores", "Warten auf Sensoren", "En attente des capteurs");

    public static string OpeningSensorBackend => Text("Opening sensor backend", "正在打开传感器后端", "センサーバックエンドを開いています", "正在開啟感測器後端", "Abriendo backend de sensores", "Sensor-Backend wird geöffnet", "Ouverture du backend des capteurs");

    public static string ExpandSidebar => Text("Expand sidebar", "展开侧边栏", "サイドバーを展開", "展開側邊欄", "Expandir barra lateral", "Seitenleiste erweitern", "Développer la barre latérale");

    public static string CollapseSidebar => Text("Collapse sidebar", "收起侧边栏", "サイドバーを折りたたむ", "收合側邊欄", "Contraer barra lateral", "Seitenleiste einklappen", "Réduire la barre latérale");

    public static string PawnIoRequired => Text(
        "PawnIO is required for full sensor access",
        "完整传感器访问需要 PawnIO",
        "完全なセンサーアクセスには PawnIO が必要です",
        "完整感測器存取需要 PawnIO",
        "PawnIO es necesario para acceder a todos los sensores",
        "PawnIO ist für vollständigen Sensorzugriff erforderlich",
        "PawnIO est requis pour l'accès complet aux capteurs");

    public static string SensorBackendUnavailable => Text("Sensor backend unavailable", "传感器后端不可用", "センサーバックエンドは利用できません", "感測器後端不可用", "Backend de sensores no disponible", "Sensor-Backend nicht verfügbar", "Backend des capteurs indisponible");

    public static string ConnectedLimited => Text("Connected with limited sensor access", "已连接，但传感器访问受限", "接続済み、センサーアクセスは制限されています", "已連線，但感測器存取受限", "Conectado con acceso limitado a sensores", "Verbunden mit eingeschränktem Sensorzugriff", "Connecté avec un accès limité aux capteurs");

    public static string ConnectedBackend => Text("Connected to hardware backend", "已连接到硬件后端", "ハードウェアバックエンドに接続済み", "已連線到硬體後端", "Conectado al backend de hardware", "Mit Hardware-Backend verbunden", "Connecté au backend matériel");

    public static string CpuSensorsUnavailable => Text("CPU sensors unavailable", "CPU 传感器不可用", "CPU センサーは利用できません", "CPU 感測器不可用", "Sensores de CPU no disponibles", "CPU-Sensoren nicht verfügbar", "Capteurs CPU indisponibles");

    public static string HardwareSensorsUnavailable => Text("Hardware sensors unavailable", "硬件传感器不可用", "ハードウェアセンサーは利用できません", "硬體感測器不可用", "Sensores de hardware no disponibles", "Hardwaresensoren nicht verfügbar", "Capteurs matériels indisponibles");

    public static string InstallPawnIoRestartAdmin => Text(
        "Install PawnIO, then restart Remo System Profiler as administrator.",
        "安装 PawnIO，然后以管理员身份重启 Remo System Profiler。",
        "PawnIO をインストールしてから、Remo System Profiler を管理者として再起動してください。",
        "安裝 PawnIO，然後以系統管理員身分重新啟動 Remo System Profiler。",
        "Instala PawnIO y reinicia Remo System Profiler como administrador.",
        "Installiere PawnIO und starte Remo System Profiler als Administrator neu.",
        "Installez PawnIO, puis redémarrez Remo System Profiler en tant qu'administrateur.");

    public static string RestartAsAdministrator => Text(
        "Restart as administrator",
        "以管理员身份重启",
        "管理者として再起動",
        "以系統管理員身分重新啟動",
        "Reiniciar como administrador",
        "Als Administrator neu starten",
        "Redémarrer en administrateur");

    public static string AdminRestartCannotFindExecutable => Text(
        "Cannot find the current executable to restart as administrator.",
        "无法找到当前程序，不能以管理员身份重启。",
        "管理者として再起動するための現在の実行ファイルが見つかりません。",
        "找不到目前程式，無法以系統管理員身分重新啟動。",
        "No se encuentra el ejecutable actual para reiniciar como administrador.",
        "Die aktuelle Programmdatei für den Administrator-Neustart wurde nicht gefunden.",
        "Impossible de trouver l'exécutable actuel pour redémarrer en administrateur.");

    public static string AdminRestartCanceled => Text(
        "Administrator restart was canceled.",
        "已取消管理员重启。",
        "管理者としての再起動はキャンセルされました。",
        "已取消系統管理員重新啟動。",
        "Se canceló el reinicio como administrador.",
        "Der Neustart als Administrator wurde abgebrochen.",
        "Le redémarrage en administrateur a été annulé.");

    public static string AdminRestartFailed(string detail) => Text(
        "Administrator restart failed: " + detail,
        "管理员重启失败：" + detail,
        "管理者としての再起動に失敗しました: " + detail,
        "系統管理員重新啟動失敗：" + detail,
        "Error al reiniciar como administrador: " + detail,
        "Neustart als Administrator fehlgeschlagen: " + detail,
        "Échec du redémarrage en administrateur : " + detail);

    public static string PawnIoInstallerSource => Text(
        "Downloads the official PawnIO installer from GitHub Releases.",
        "从 GitHub Releases 下载官方 PawnIO 安装器。",
        "GitHub Releases から公式 PawnIO インストーラーをダウンロードします。",
        "從 GitHub Releases 下載官方 PawnIO 安裝程式。",
        "Descarga el instalador oficial de PawnIO desde GitHub Releases.",
        "Lädt den offiziellen PawnIO-Installer von GitHub Releases herunter.",
        "Télécharge l'installateur officiel de PawnIO depuis GitHub Releases.");

    public static string PawnIoInstallDownloading => Text(
        "Downloading PawnIO...",
        "正在下载 PawnIO...",
        "PawnIO をダウンロード中...",
        "正在下載 PawnIO...",
        "Descargando PawnIO...",
        "PawnIO wird heruntergeladen...",
        "Téléchargement de PawnIO...");

    public static string PawnIoInstallDownloadingProgress(double percent) => Text(
        $"Downloading PawnIO {percent:0}%",
        $"正在下载 PawnIO {percent:0}%",
        $"PawnIO をダウンロード中 {percent:0}%",
        $"正在下載 PawnIO {percent:0}%",
        $"Descargando PawnIO {percent:0}%",
        $"PawnIO wird heruntergeladen {percent:0}%",
        $"Téléchargement de PawnIO {percent:0}%");

    public static string PawnIoInstallInstalling => Text(
        "Installing PawnIO...",
        "正在安装 PawnIO...",
        "PawnIO をインストール中...",
        "正在安裝 PawnIO...",
        "Instalando PawnIO...",
        "PawnIO wird installiert...",
        "Installation de PawnIO...");

    public static string PawnIoInstallCompleted => Text(
        "PawnIO installer finished. Restart Remo System Profiler as administrator if sensors are still limited.",
        "PawnIO 安装器已完成。如果传感器仍受限，请以管理员身份重启 Remo System Profiler。",
        "PawnIO インストーラーが完了しました。センサーがまだ制限される場合は、Remo System Profiler を管理者として再起動してください。",
        "PawnIO 安裝程式已完成。如果感測器仍受限，請以系統管理員身分重新啟動 Remo System Profiler。",
        "El instalador de PawnIO terminó. Reinicia Remo System Profiler como administrador si los sensores siguen limitados.",
        "Der PawnIO-Installer ist fertig. Starte Remo System Profiler als Administrator neu, falls Sensoren weiter eingeschränkt sind.",
        "L'installateur PawnIO est terminé. Redémarrez Remo System Profiler en tant qu'administrateur si les capteurs restent limités.");

    public static string PawnIoInstallRestartRequired => Text(
        "PawnIO installed. Restart Windows, then open Remo System Profiler as administrator.",
        "PawnIO 已安装。请重启 Windows，然后以管理员身份打开 Remo System Profiler。",
        "PawnIO をインストールしました。Windows を再起動してから、Remo System Profiler を管理者として開いてください。",
        "PawnIO 已安裝。請重新啟動 Windows，然後以系統管理員身分開啟 Remo System Profiler。",
        "PawnIO instalado. Reinicia Windows y abre Remo System Profiler como administrador.",
        "PawnIO wurde installiert. Starte Windows neu und öffne Remo System Profiler als Administrator.",
        "PawnIO est installé. Redémarrez Windows, puis ouvrez Remo System Profiler en tant qu'administrateur.");

    public static string PawnIoOldInstallRemovedRestart => Text(
        "Old PawnIO was removed. Restart Windows, then click Download and install PawnIO again.",
        "旧版 PawnIO 已移除。请重启 Windows，然后再次点击下载并安装 PawnIO。",
        "古い PawnIO を削除しました。Windows を再起動してから、もう一度 PawnIO のダウンロードとインストールを実行してください。",
        "舊版 PawnIO 已移除。請重新啟動 Windows，然後再次點擊下載並安裝 PawnIO。",
        "Se quitó el PawnIO antiguo. Reinicia Windows y vuelve a descargar e instalar PawnIO.",
        "Die alte PawnIO-Version wurde entfernt. Starte Windows neu und klicke erneut auf PawnIO herunterladen und installieren.",
        "L'ancien PawnIO a été retiré. Redémarrez Windows, puis relancez le téléchargement et l'installation de PawnIO.");

    public static string PawnIoOldInstallRemovalFailed(string detail) => Text(
        "Could not remove the old PawnIO install automatically: " + detail,
        "无法自动移除旧版 PawnIO：" + detail,
        "古い PawnIO を自動削除できませんでした: " + detail,
        "無法自動移除舊版 PawnIO：" + detail,
        "No se pudo quitar automáticamente el PawnIO antiguo: " + detail,
        "Die alte PawnIO-Version konnte nicht automatisch entfernt werden: " + detail,
        "Impossible de retirer automatiquement l'ancien PawnIO : " + detail);

    public static string PawnIoInstallFailed(string detail) => Text(
        "PawnIO install failed: " + detail,
        "PawnIO 安装失败：" + detail,
        "PawnIO のインストールに失敗しました: " + detail,
        "PawnIO 安裝失敗：" + detail,
        "Error al instalar PawnIO: " + detail,
        "PawnIO-Installation fehlgeschlagen: " + detail,
        "Échec de l'installation de PawnIO : " + detail);

    public static string PawnIoInstallCanceled => Text(
        "PawnIO installation was canceled.",
        "PawnIO 安装已取消。",
        "PawnIO のインストールをキャンセルしました。",
        "PawnIO 安裝已取消。",
        "La instalación de PawnIO fue cancelada.",
        "Die PawnIO-Installation wurde abgebrochen.",
        "L'installation de PawnIO a été annulée.");

    public static string ConnectedStatus(string source, string driverSummary) => Text(
        $"Connected\n{source}\n{driverSummary}",
        $"已连接\n{source}\n{driverSummary}",
        $"接続済み\n{source}\n{driverSummary}",
        $"已連線\n{source}\n{driverSummary}",
        $"Conectado\n{source}\n{driverSummary}",
        $"Verbunden\n{source}\n{driverSummary}",
        $"Connecté\n{source}\n{driverSummary}");

    public static string LimitedAccessStatus(string source) => Text(
        $"Limited access\n{source}\nRun as administrator",
        $"受限访问\n{source}\n请以管理员身份运行",
        $"制限付きアクセス\n{source}\n管理者として実行してください",
        $"受限存取\n{source}\n請以系統管理員身分執行",
        $"Acceso limitado\n{source}\nEjecutar como administrador",
        $"Eingeschränkter Zugriff\n{source}\nAls Administrator ausführen",
        $"Accès limité\n{source}\nExécuter en tant qu'administrateur");

    public static string HardwareSummary(int gpuCount, int storageCount) => Text(
        $"{gpuCount} GPU | {storageCount} storage",
        $"{gpuCount} 个 GPU | {storageCount} 个存储设备",
        $"{gpuCount} GPU | {storageCount} ストレージ",
        $"{gpuCount} 個 GPU | {storageCount} 個儲存裝置",
        $"{gpuCount} GPU | {storageCount} almacenamiento",
        $"{gpuCount} GPU | {storageCount} Speicher",
        $"{gpuCount} GPU | {storageCount} stockage");

    public static string OverviewMemory => Text("Memory", "内存", "メモリ", "記憶體", "Memoria", "Speicher", "Mémoire");

    public static string OverviewGpu(int index) => $"GPU {index}";

    public static string OverviewDisk(int index) => Text($"Disk {index}", $"磁盘 {index}", $"ディスク {index}", $"磁碟 {index}", $"Disco {index}", $"Datenträger {index}", $"Disque {index}");

    public static string SensorGroupTitle(string key) => key switch
    {
        "temperature" => Text("Temperature", "温度", "温度", "溫度", "Temperatura", "Temperatur", "Température"),
        "power" => Text("Power", "功耗", "電力", "功耗", "Potencia", "Leistung", "Puissance"),
        "clock" => Text("Clock", "频率", "クロック", "頻率", "Frecuencia", "Takt", "Fréquence"),
        "voltage" => Text("Voltage", "电压", "電圧", "電壓", "Voltaje", "Spannung", "Tension"),
        _ => key
    };

    public static string SummaryLabel(string key) => key switch
    {
        "peak" => Text("Peak", "峰值", "ピーク", "峰值", "Pico", "Spitze", "Pic"),
        "avg" => Text("Avg", "平均", "平均", "平均", "Prom.", "Durchschn.", "Moy."),
        "sensors" => Text("Sensors", "传感器", "センサー", "感測器", "Sensores", "Sensoren", "Capteurs"),
        "total" => Text("Total", "总计", "合計", "總計", "Total", "Gesamt", "Total"),
        "package" => Text("Package", "封装", "パッケージ", "封裝", "Paquete", "Package", "Package"),
        "peakRail" => Text("Peak rail", "峰值轨", "ピークレール", "峰值軌", "Riel pico", "Spitzenschiene", "Rail max"),
        "max" => Text("Max", "最大", "最大", "最大", "Máx.", "Max.", "Max"),
        "primary" => Text("Primary", "主要", "メイン", "主要", "Principal", "Primär", "Principal"),
        _ => key
    };

    public static string MetricLabel(string name, string kind)
    {
        string localizedName = LocalizeMetricName(name);
        string localizedKind = LocalizeMetricKind(kind);
        return $"{localizedName} ({localizedKind})";
    }

    public static string NoSensors => Text("no sensors", "无传感器", "センサーなし", "無感測器", "sin sensores", "keine Sensoren", "aucun capteur");

    public static string NotReported => Text("Not reported", "未报告", "未報告", "未回報", "No reportado", "Nicht gemeldet", "Non signalé");

    public static string Normal => Text("normal", "正常", "正常", "正常", "normal", "normal", "normal");

    public static string SensorCount(int count) => Text($"{count} sensors", $"{count} 个传感器", $"{count} センサー", $"{count} 個感測器", $"{count} sensores", $"{count} Sensoren", $"{count} capteurs");

    public static string WarningCount(int count) => Text($"{count} warning", $"{count} 个警告", $"{count} 件の警告", $"{count} 個警告", $"{count} advertencia", $"{count} Warnung", $"{count} avertissement");

    public static string HotCount(int count) => Text($"{count} hot", $"{count} 个过热", $"{count} 高温", $"{count} 個過熱", $"{count} caliente", $"{count} heiß", $"{count} chaud");

    public static string WarmCount(int count) => Text($"{count} warm", $"{count} 个偏热", $"{count} やや高温", $"{count} 個偏熱", $"{count} tibio", $"{count} warm", $"{count} tiède");

    public static string BenchmarkReady => Text("Ready", "就绪", "準備完了", "就緒", "Listo", "Bereit", "Prêt");

    public static string BenchmarkRunButton => Text("Run", "运行", "実行", "執行", "Ejecutar", "Starten", "Lancer");

    public static string BenchmarkRunningButton => Text("Running", "运行中", "実行中", "執行中", "Ejecutando", "Läuft", "En cours");

    public static string BenchmarkCanceled => Text("Canceled", "已取消", "キャンセル済み", "已取消", "Cancelado", "Abgebrochen", "Annulé");

    public static string BenchmarkCanceling => Text("Canceling benchmark", "正在取消跑分", "ベンチマークをキャンセル中", "正在取消跑分", "Cancelando prueba", "Benchmark wird abgebrochen", "Annulation du benchmark");

    public static string BenchmarkValidationOk => Text("OK", "通过", "OK", "通過", "OK", "OK", "OK");

    public static string BenchmarkValidationFailed => Text("Failed", "失败", "失敗", "失敗", "Error", "Fehlgeschlagen", "Échec");

    public static string BenchmarkUploadNoResult => Text("Run a valid result first", "请先运行一次有效跑分", "先に有効な結果を実行", "請先執行一次有效跑分", "Ejecuta primero un resultado válido", "Zuerst ein gültiges Ergebnis ausführen", "Exécutez d'abord un résultat valide");

    public static string BenchmarkUploadButton => Text("Upload result", "上传成绩", "結果をアップロード", "上傳成績", "Subir resultado", "Ergebnis hochladen", "Téléverser le résultat");

    public static string BenchmarkUploading => Text("Uploading...", "正在上传...", "アップロード中...", "正在上傳...", "Subiendo...", "Wird hochgeladen...", "Téléversement...");

    public static string ActionCooldown => Text("Wait 5s", "等待 5 秒", "5秒待機", "等待 5 秒", "Espera 5 s", "5 s warten", "Attendre 5 s");

    public static string BenchmarkDeleteButton => Text("Delete matching result", "删除匹配成绩", "一致する結果を削除", "刪除相符成績", "Eliminar resultado coincidente", "Passendes Ergebnis löschen", "Supprimer le résultat correspondant");

    public static string BenchmarkDeleting => Text("Deleting...", "正在删除...", "削除中...", "正在刪除...", "Eliminando...", "Wird gelöscht...", "Suppression...");

    public static string BenchmarkUploadSuccess => Text("Uploaded", "上传成功", "アップロード済み", "已上傳", "Subido", "Hochgeladen", "Téléversé");

    public static string BenchmarkUploadUpdated => Text("Updated", "已更新", "更新済み", "已更新", "Actualizado", "Aktualisiert", "Mis à jour");

    public static string BenchmarkDeleteSuccess => Text("Deleted", "删除成功", "削除済み", "已刪除", "Eliminado", "Gelöscht", "Supprimé");

    public static string BenchmarkDeleteNotFound => Text("No matching result", "没有匹配成绩", "一致する結果がありません", "沒有相符成績", "Sin resultado coincidente", "Kein passendes Ergebnis", "Aucun résultat correspondant");

    public static string BenchmarkUploadDuplicate => Text("Already uploaded", "该成绩已经上传过", "すでにアップロード済み", "該成績已上傳過", "Ya subido", "Bereits hochgeladen", "Déjà téléversé");

    public static string BenchmarkUploadRateLimited => Text("Try again later", "上传过于频繁，请稍后再试", "後でもう一度お試しください", "上傳過於頻繁，請稍後再試", "Inténtalo más tarde", "Später erneut versuchen", "Réessayez plus tard");

    public static string BenchmarkUploadFailed(string detail) => Text($"Upload failed: {detail}", $"上传失败：{detail}", $"アップロード失敗: {detail}", $"上傳失敗：{detail}", $"Error al subir: {detail}", $"Upload fehlgeschlagen: {detail}", $"Échec du téléversement : {detail}");

    public static string BenchmarkDeleteFailed(string detail) => Text($"Delete failed: {detail}", $"删除失败：{detail}", $"削除失敗: {detail}", $"刪除失敗：{detail}", $"Error al eliminar: {detail}", $"Löschen fehlgeschlagen: {detail}", $"Échec de la suppression : {detail}");

    public static string LeaderboardReady => Text("Leaderboard not loaded", "排行榜未加载", "ランキング未読み込み", "排行榜未載入", "Clasificación sin cargar", "Bestenliste nicht geladen", "Classement non chargé");

    public static string LeaderboardRefreshButton => Text("Refresh leaderboard", "刷新排行榜", "ランキングを更新", "重新整理排行榜", "Actualizar clasificación", "Bestenliste aktualisieren", "Actualiser le classement");

    public static string LeaderboardLoadingButton => Text("Loading...", "正在加载...", "読み込み中...", "正在載入...", "Cargando...", "Wird geladen...", "Chargement...");

    public static string LeaderboardLoaded(int count) => Text($"{count} entries loaded", $"已加载 {count} 条", $"{count} 件を読み込み", $"已載入 {count} 筆", $"{count} entradas cargadas", $"{count} Einträge geladen", $"{count} entrées chargées");

    public static string LeaderboardEmpty => Text("No entries yet", "暂无成绩", "まだ結果がありません", "暫無成績", "Sin entradas", "Noch keine Einträge", "Aucune entrée");

    public static string LeaderboardFailed(string detail) => Text($"Leaderboard failed: {detail}", $"排行榜加载失败：{detail}", $"ランキング取得失敗: {detail}", $"排行榜載入失敗：{detail}", $"Error de clasificación: {detail}", $"Bestenliste fehlgeschlagen: {detail}", $"Échec du classement : {detail}");

    public static string BenchmarkCompletedAt(string timeText) => Text($"Completed at {timeText}", $"完成于 {timeText}", $"{timeText} に完了", $"完成於 {timeText}", $"Completado a las {timeText}", $"Abgeschlossen um {timeText}", $"Terminé à {timeText}");

    public static string BenchmarkFailed(string message) => Text($"Benchmark failed: {message}", $"跑分失败：{message}", $"ベンチマーク失敗: {message}", $"跑分失敗：{message}", $"Prueba fallida: {message}", $"Benchmark fehlgeschlagen: {message}", $"Échec du benchmark : {message}");

    public static string BenchmarkRunningMode(string modeText) => Text(
        $"Running {modeText.ToLowerInvariant()} benchmark",
        $"正在运行{modeText}跑分",
        $"{modeText}ベンチマークを実行中",
        $"正在執行{modeText}跑分",
        $"Ejecutando prueba {modeText.ToLowerInvariant()}",
        $"{modeText}-Benchmark läuft",
        $"Benchmark {modeText.ToLowerInvariant()} en cours");

    public static string BenchmarkRunningProgress(string workload, double elapsedSeconds, double durationSeconds) => Text(
        $"Running {workload} {elapsedSeconds:0.0}s / {durationSeconds:0}s",
        $"正在运行 {workload} {elapsedSeconds:0.0} 秒 / {durationSeconds:0} 秒",
        $"{workload} 実行中 {elapsedSeconds:0.0} 秒 / {durationSeconds:0} 秒",
        $"正在執行 {workload} {elapsedSeconds:0.0} 秒 / {durationSeconds:0} 秒",
        $"Ejecutando {workload} {elapsedSeconds:0.0}s / {durationSeconds:0}s",
        $"{workload} läuft {elapsedSeconds:0.0}s / {durationSeconds:0}s",
        $"{workload} en cours {elapsedSeconds:0.0}s / {durationSeconds:0}s");

    public static string BenchmarkThreadCount(int count) => Text(
        $"{count} thread{(count == 1 ? string.Empty : "s")}",
        $"{count} 线程",
        $"{count} スレッド",
        $"{count} 執行緒",
        $"{count} hilo{(count == 1 ? string.Empty : "s")}",
        $"{count} Thread{(count == 1 ? string.Empty : "s")}",
        $"{count} thread{(count == 1 ? string.Empty : "s")}");

    public static string UpdateIdle => Text(
        "Use GitHub Releases to check for newer builds.",
        "使用 GitHub Releases 检查是否有新版本。",
        "GitHub Releases で新しいビルドを確認します。",
        "使用 GitHub Releases 檢查是否有新版本。",
        "Usa GitHub Releases para buscar compilaciones nuevas.",
        "Nutze GitHub Releases, um nach neuen Builds zu suchen.",
        "Utilisez GitHub Releases pour rechercher de nouvelles versions.");

    public static string UpdateChecking => Text("Checking GitHub Releases...", "正在检查 GitHub Releases...", "GitHub Releases を確認中...", "正在檢查 GitHub Releases...", "Buscando en GitHub Releases...", "GitHub Releases werden geprüft...", "Vérification de GitHub Releases...");

    public static string UpdateDownloading => Text("Downloading update...", "正在下载更新...", "更新をダウンロード中...", "正在下載更新...", "Descargando actualización...", "Update wird heruntergeladen...", "Téléchargement de la mise à jour...");

    public static string UpdatePreparing => Text("Preparing update...", "正在准备更新...", "更新を準備中...", "正在準備更新...", "Preparando actualización...", "Update wird vorbereitet...", "Préparation de la mise à jour...");

    public static string UpdateNoReleaseNotes => Text("No release notes were published.", "此版本没有发布日志。", "このリリースにはノートがありません。", "此版本沒有發布日誌。");

    public static string UpdateCheckCanceled => Text("Update check canceled.", "更新检查已取消。", "更新確認をキャンセルしました。", "更新檢查已取消。");

    public static string UpdateNoReleases => Text(
        "No GitHub Releases were found.",
        "没有找到 GitHub Releases。",
        "GitHub Releases が見つかりません。",
        "沒有找到 GitHub Releases。");

    public static string UpdateNoDownloadAsset => Text(
        "New version found, but this release has no downloadable update package.",
        "发现新版本，但这个 release 没有可下载的更新包。",
        "新しいバージョンがありますが、このリリースには更新パッケージがありません。",
        "發現新版本，但這個 release 沒有可下載的更新包。");

    public static string UpdateAvailable(string version) => Text(
        $"New version available: {version}",
        $"发现新版本：{version}",
        $"新しいバージョンがあります: {version}",
        $"發現新版本：{version}");

    public static string UpdateAlreadyLatest(string version) => Text(
        $"Up to date: {version}",
        $"已是最新版本：{version}",
        $"最新です: {version}",
        $"已是最新版本：{version}");

    public static string UpdateCheckFailed(string detail) => Text(
        "Update check failed: " + detail,
        "更新检查失败：" + detail,
        "更新確認に失敗しました: " + detail,
        "更新檢查失敗：" + detail);

    public static string UpdateInstallFailed(string detail) => Text(
        "Update failed: " + detail,
        "更新失败：" + detail,
        "更新に失敗しました: " + detail,
        "更新失敗：" + detail);

    public static string UpdateUnsupportedAsset(string name) => Text(
        "Unsupported update package: " + name,
        "不支持的更新包：" + name,
        "サポートされていない更新パッケージ: " + name,
        "不支援的更新包：" + name);

    public static string UpdateInvalidPackage => Text(
        "Downloaded update package is not a valid zip file.",
        "下载的更新包不是有效的 zip 文件。",
        "ダウンロードした更新パッケージは有効な zip ファイルではありません。",
        "下載的更新包不是有效的 zip 檔案。");

    public static string UpdateCannotFindExecutable => Text(
        "Cannot find the current executable to restart after update.",
        "无法找到当前程序，更新后不能自动重启。",
        "更新後に再起動する現在の実行ファイルが見つかりません。",
        "無法找到目前程式，更新後不能自動重新啟動。");

    public static string UpdateWillRestart => Text(
        "Update downloaded. The app will close, replace files, and restart.",
        "更新已下载。程序将关闭、替换文件并重新启动。",
        "更新をダウンロードしました。アプリを閉じてファイルを置き換え、再起動します。",
        "更新已下載。程式將關閉、替換檔案並重新啟動。");

    public static string UpdateInstallerStarted => Text(
        "Installer downloaded. The app will close while the installer runs.",
        "安装器已下载。安装器运行时程序将关闭。",
        "インストーラーをダウンロードしました。インストーラー実行中はアプリを閉じます。",
        "安裝程式已下載。安裝程式執行時程式將關閉。");

    public static string BenchmarkModeText(int modeIndex) => modeIndex switch
    {
        0 => Text("Single core", "单核", "シングルコア", "單核", "Un núcleo", "Ein Kern", "Coeur unique"),
        1 => Text("Multi core", "多核", "マルチコア", "多核", "Varios núcleos", "Mehrere Kerne", "Multi-coeur"),
        _ => Text("Custom", "自定义", "カスタム", "自訂", "Personalizado", "Benutzerdefiniert", "Personnalisé")
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

        if (message.Equals("PawnIO installation found but the driver is unavailable; uninstall PawnIO, then install again", StringComparison.OrdinalIgnoreCase))
        {
            return Text(
                message,
                "检测到 PawnIO 安装记录，但驱动不可用；请先卸载 PawnIO，然后重新安装。",
                "PawnIO のインストール記録はありますが、ドライバーを利用できません。先に PawnIO をアンインストールしてから再インストールしてください。",
                "偵測到 PawnIO 安裝記錄，但驅動程式不可用；請先解除安裝 PawnIO，然後重新安裝。");
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

    private static string Text(
        string english,
        string simplifiedChinese,
        string japanese,
        string traditionalChinese,
        string? spanish = null,
        string? german = null,
        string? french = null) => CurrentLanguage switch
    {
        UiLanguage.SimplifiedChinese => simplifiedChinese,
        UiLanguage.Japanese => japanese,
        UiLanguage.TraditionalChinese => traditionalChinese,
        UiLanguage.Spanish => spanish ?? english,
        UiLanguage.German => german ?? english,
        UiLanguage.French => french ?? english,
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
