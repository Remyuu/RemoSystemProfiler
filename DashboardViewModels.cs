using Avalonia;
using Avalonia.Media;
using RemoSystemProfiler.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RemoSystemProfiler;

public abstract class ObservableDashboardItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        RaisePropertyChanged(propertyName);
        return true;
    }
}

public sealed class MainWindowViewModel : ObservableDashboardItem
{
    private string _hardwareSummaryText = Localization.WaitingForHardwareSensors;
    private string _statusText = Localization.WaitingForSensors;
    private string? _statusToolTip;
    private IBrush _statusBrush = DashboardBrushes.Amber;
    private bool _isPawnIoDownloadVisible;
    private bool _isAdminRestartVisible;
    private bool _isPawnIoInstallRunning;
    private bool _isPawnIoPromptVisible;
    private string _pawnIoInstallButtonText = Localization.Resource("Ui_InstallPawnIo");
    private string _updatedText = "--:--:--";
    private string _cpuNameText = "--";
    private string _cpuLoadSummaryText = "--";
    private string _clockText = "--";
    private string _cpuPackagePowerText = "--";
    private string _cpuPeakTempText = "--";
    private string _coreCountText = "--";
    private bool _isCpuOverallView;
    private bool _isBenchmarkRunning;
    private string _benchmarkStatusText = Localization.BenchmarkReady;
    private string _benchmarkModeText = Localization.BenchmarkModeText(1);
    private string _benchmarkScoreText = "--";
    private string _benchmarkMixedScoreText = "--";
    private string _benchmarkSciMarkText = "--";
    private string _benchmarkZstdCompressionText = "--";
    private string _benchmarkZstdDecompressionText = "--";
    private string _benchmarkHashText = "--";
    private string _benchmarkThreadsText = Localization.BenchmarkThreadCount(BenchmarkRunner.MaxWorkerCount);
    private string _benchmarkCpuFrequencyText = "--";
    private string _benchmarkCpuTemperatureText = "--";
    private string _benchmarkCpuEnergyText = "--";
    private string _benchmarkPeakPowerText = "--";
    private string _benchmarkValidationText = "--";
    private string _benchmarkDisplayNameText = BenchmarkPayload.DefaultDisplayName;
    private bool _isBenchmarkUploadAvailable;
    private bool _isBenchmarkUploadRunning;
    private bool _isBenchmarkUploadCooldownRunning;
    private bool _isBenchmarkDeleteRunning;
    private string _benchmarkUploadStatusText = Localization.BenchmarkUploadNoResult;
    private bool _isLeaderboardLoading;
    private bool _isLeaderboardRefreshCooldownRunning;
    private string _leaderboardStatusText = Localization.LeaderboardReady;
    private string _benchmarkProgressText = FormatBenchmarkProgress(0, TimeSpan.Zero, BenchmarkRunner.GetPlan(BenchmarkRunProfile.Standard).TotalDuration);
    private double _benchmarkProgressValue;
    private int _selectedBenchmarkVersionIndex;
    private int _selectedBenchmarkModeIndex = 1;
    private int _selectedBenchmarkProfileIndex = 1;
    private int _selectedLeaderboardScoreIndex;
    private string _memoryUsageText = "--";
    private string _memoryCapacityText = "--";
    private string _memoryTempText = "";
    private bool _isStartupOverlayVisible = true;
    private double _startupOverlayOpacity = 1;
    private string _startupStatusText = Localization.OpeningSensorBackend;
    private int _selectedChartRangeIndex;
    private int _selectedUpdateIntervalIndex = 1;
    private int _selectedThemeIndex;
    private int _selectedLanguageIndex = Localization.CurrentLanguageIndex;
    private bool _isSidebarCompact;
    private bool _isSidebarExpanded = true;
    private string _sidebarToggleToolTip = Localization.CollapseSidebar;
    private Thickness _sidebarMargin = new(8, 10);
    private bool _isUpdateCheckRunning;
    private bool _isReleaseNotesVisible;
    private bool _isOpenReleaseVisible;
    private bool _isInstallUpdateVisible;
    private bool _isUpdateInstallRunning;
    private bool _isUpdateProgressVisible;
    private double _updateProgressValue;
    private string _updateProgressText = "0%";
    private string _updateStatusText = Localization.UpdateIdle;
    private string _releaseNotesText = string.Empty;
    private string _latestReleaseUrl = "https://github.com/Remyuu/RemoSystemProfiler/releases";
    private static readonly string ApplicationVersion = ResolveApplicationVersion();
    private readonly Dictionary<BenchmarkMetricCardKey, BenchmarkMetricCardViewModel> _benchmarkMetricCardsByKey = [];

    public MainWindowViewModel()
    {
        AddBenchmarkMetric(BenchmarkMetricCardKey.CpuCoreScore, "Ui_CpuCoreScore", BenchmarkScoreText, isPrimary: true);
        AddBenchmarkMetric(BenchmarkMetricCardKey.CpuMixedScore, "Ui_CpuMixedScore", BenchmarkMixedScoreText);
        AddBenchmarkMetric(BenchmarkMetricCardKey.Mode, "Ui_Mode", BenchmarkModeText);
        AddBenchmarkMetric(BenchmarkMetricCardKey.Validation, "Ui_Validation", BenchmarkValidationText);
        AddBenchmarkMetric(BenchmarkMetricCardKey.SciMark, "Ui_SciMark", BenchmarkSciMarkText);
        AddBenchmarkMetric(BenchmarkMetricCardKey.XxHash3, "Ui_XxHash3", BenchmarkHashText);
        AddBenchmarkMetric(BenchmarkMetricCardKey.ZstdCompression, "Ui_ZstdCompression", BenchmarkZstdCompressionText);
        AddBenchmarkMetric(BenchmarkMetricCardKey.ZstdDecompression, "Ui_ZstdDecompression", BenchmarkZstdDecompressionText);
        AddBenchmarkMetric(BenchmarkMetricCardKey.CpuAverageFrequency, "Ui_CpuAverageFrequency", BenchmarkCpuFrequencyText);
        AddBenchmarkMetric(BenchmarkMetricCardKey.CpuMaxTemperature, "Ui_CpuMaxTemperature", BenchmarkCpuTemperatureText);
        AddBenchmarkMetric(BenchmarkMetricCardKey.CpuEnergy, "Ui_CpuEnergy", BenchmarkCpuEnergyText);
        AddBenchmarkMetric(BenchmarkMetricCardKey.CpuPeakPower, "Ui_CpuPeakPower", BenchmarkPeakPowerText);
    }

    public ObservableCollection<OverviewItemViewModel> OverviewItems { get; } = [];

    public ObservableCollection<SensorGroupViewModel> CpuSensorGroups { get; } = [];

    public ObservableCollection<CoreItemViewModel> CpuCores { get; } = [];

    public ObservableCollection<CoreItemViewModel> CpuOverallUsage { get; } = [];

    public ObservableCollection<MetricItemViewModel> MemoryMetrics { get; } = [];

    public ObservableCollection<GpuDeviceViewModel> Gpus { get; } = [];

    public ObservableCollection<StorageDeviceViewModel> StorageDevices { get; } = [];

    public ObservableCollection<LeaderboardEntryViewModel> LeaderboardEntries { get; } = [];

    public ObservableCollection<BenchmarkMetricCardViewModel> BenchmarkMetricCards { get; } = [];

    public ObservableCollection<BenchmarkTelemetrySample> BenchmarkTelemetrySamples { get; } = [];

    public string HardwareSummaryText { get => _hardwareSummaryText; set => SetProperty(ref _hardwareSummaryText, value); }

    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    public string? StatusToolTip { get => _statusToolTip; set => SetProperty(ref _statusToolTip, value); }

    public IBrush StatusBrush { get => _statusBrush; set => SetProperty(ref _statusBrush, value); }

    public bool IsPawnIoDownloadVisible { get => _isPawnIoDownloadVisible; set => SetProperty(ref _isPawnIoDownloadVisible, value); }

    public bool IsAdminRestartVisible { get => _isAdminRestartVisible; set => SetProperty(ref _isAdminRestartVisible, value); }

    public string AdminRestartButtonText => Localization.RestartAsAdministrator;

    public bool IsPawnIoInstallRunning
    {
        get => _isPawnIoInstallRunning;
        set
        {
            if (SetProperty(ref _isPawnIoInstallRunning, value))
            {
                RaisePropertyChanged(nameof(IsPawnIoInstallAvailable));
                RaisePropertyChanged(nameof(IsPawnIoPromptCancelAvailable));
            }
        }
    }

    public bool IsPawnIoInstallAvailable => !IsPawnIoInstallRunning;

    public bool IsPawnIoPromptCancelAvailable => !IsPawnIoInstallRunning;

    public bool IsPawnIoPromptVisible { get => _isPawnIoPromptVisible; set => SetProperty(ref _isPawnIoPromptVisible, value); }

    public string PawnIoInstallButtonText { get => _pawnIoInstallButtonText; set => SetProperty(ref _pawnIoInstallButtonText, value); }

    public string PawnIoPromptTitleText => Localization.PawnIoRequired;

    public string PawnIoPromptSubtitleText => Localization.InstallPawnIoRestartAdmin;

    public string UpdatedText { get => _updatedText; set => SetProperty(ref _updatedText, value); }

    public string VersionText => $"v{ApplicationVersion}";

    public string CurrentVersion => ApplicationVersion;

    public bool IsUpdateCheckRunning
    {
        get => _isUpdateCheckRunning;
        set
        {
            if (SetProperty(ref _isUpdateCheckRunning, value))
            {
                RaisePropertyChanged(nameof(IsUpdateCheckAvailable));
                RaisePropertyChanged(nameof(IsUpdateInstallAvailable));
            }
        }
    }

    public bool IsUpdateCheckAvailable => !IsUpdateCheckRunning && !IsUpdateInstallRunning;

    public bool IsUpdateInstallRunning
    {
        get => _isUpdateInstallRunning;
        set
        {
            if (SetProperty(ref _isUpdateInstallRunning, value))
            {
                RaisePropertyChanged(nameof(IsUpdateCheckAvailable));
                RaisePropertyChanged(nameof(IsUpdateInstallAvailable));
            }
        }
    }

    public bool IsUpdateInstallAvailable => !IsUpdateCheckRunning && !IsUpdateInstallRunning;

    public bool IsReleaseNotesVisible { get => _isReleaseNotesVisible; set => SetProperty(ref _isReleaseNotesVisible, value); }

    public bool IsOpenReleaseVisible { get => _isOpenReleaseVisible; set => SetProperty(ref _isOpenReleaseVisible, value); }

    public bool IsInstallUpdateVisible { get => _isInstallUpdateVisible; set => SetProperty(ref _isInstallUpdateVisible, value); }

    public bool IsUpdateProgressVisible { get => _isUpdateProgressVisible; set => SetProperty(ref _isUpdateProgressVisible, value); }

    public double UpdateProgressValue { get => _updateProgressValue; set => SetProperty(ref _updateProgressValue, value); }

    public string UpdateProgressText { get => _updateProgressText; set => SetProperty(ref _updateProgressText, value); }

    public string UpdateStatusText { get => _updateStatusText; set => SetProperty(ref _updateStatusText, value); }

    public string ReleaseNotesText { get => _releaseNotesText; set => SetProperty(ref _releaseNotesText, value); }

    public string LatestReleaseUrl { get => _latestReleaseUrl; set => SetProperty(ref _latestReleaseUrl, value); }

    public string CpuNameText { get => _cpuNameText; set => SetProperty(ref _cpuNameText, value); }

    public string CpuLoadSummaryText { get => _cpuLoadSummaryText; set => SetProperty(ref _cpuLoadSummaryText, value); }

    public string ClockText { get => _clockText; set => SetProperty(ref _clockText, value); }

    public string CpuPackagePowerText { get => _cpuPackagePowerText; set => SetProperty(ref _cpuPackagePowerText, value); }

    public string CpuPeakTempText { get => _cpuPeakTempText; set => SetProperty(ref _cpuPeakTempText, value); }

    public string CoreCountText { get => _coreCountText; set => SetProperty(ref _coreCountText, value); }

    public bool IsCpuOverallView
    {
        get => _isCpuOverallView;
        set
        {
            if (SetProperty(ref _isCpuOverallView, value))
            {
                RaisePropertyChanged(nameof(IsCpuLogicalProcessorView));
                RefreshCpuCoreGraphChrome();
            }
        }
    }

    public bool IsCpuLogicalProcessorView => !IsCpuOverallView;

    public string CpuCoreGraphTitle => IsCpuOverallView
        ? Localization.Resource("Ui_OverallUtilization")
        : Localization.Resource("Ui_LogicalProcessors");

    public string CpuCoreGraphToggleText => IsCpuOverallView
        ? Localization.Resource("Ui_LogicalProcessorsShort")
        : Localization.Resource("Ui_OverallUtilizationShort");

    public string CpuCoreGraphToggleToolTip => IsCpuOverallView
        ? Localization.Resource("Ui_ShowLogicalProcessors")
        : Localization.Resource("Ui_ShowOverallUtilization");

    public double CpuLogicalGraphOpacity => IsCpuOverallView ? 0d : 1d;

    public double CpuOverallGraphOpacity => IsCpuOverallView ? 1d : 0d;

    public double CpuLogicalGraphScale => IsCpuOverallView ? 0.985d : 1d;

    public double CpuOverallGraphScale => IsCpuOverallView ? 1d : 0.985d;

    public bool IsBenchmarkRunning
    {
        get => _isBenchmarkRunning;
        set
        {
            if (SetProperty(ref _isBenchmarkRunning, value))
            {
                RaisePropertyChanged(nameof(IsBenchmarkStartEnabled));
                RaisePropertyChanged(nameof(IsBenchmarkCancelVisible));
                RaisePropertyChanged(nameof(BenchmarkStartButtonText));
                RaisePropertyChanged(nameof(AreBenchmarkSettingsEnabled));
                RaisePropertyChanged(nameof(CanUploadBenchmarkResult));
                RaisePropertyChanged(nameof(CanDeleteBenchmarkResult));
            }
        }
    }

    public bool IsBenchmarkStartEnabled => !IsBenchmarkRunning;

    public bool AreBenchmarkSettingsEnabled => !IsBenchmarkRunning;

    public bool IsBenchmarkCancelVisible => IsBenchmarkRunning;

    public string BenchmarkStartButtonText => IsBenchmarkRunning ? Localization.BenchmarkRunningButton : Localization.BenchmarkRunButton;

    public string BenchmarkStatusText { get => _benchmarkStatusText; set => SetProperty(ref _benchmarkStatusText, value); }

    public string BenchmarkModeText { get => _benchmarkModeText; set => SetBenchmarkText(ref _benchmarkModeText, value, BenchmarkMetricCardKey.Mode); }

    public string BenchmarkScoreText { get => _benchmarkScoreText; set => SetBenchmarkText(ref _benchmarkScoreText, value, BenchmarkMetricCardKey.CpuCoreScore); }

    public string BenchmarkMixedScoreText { get => _benchmarkMixedScoreText; set => SetBenchmarkText(ref _benchmarkMixedScoreText, value, BenchmarkMetricCardKey.CpuMixedScore); }

    public string BenchmarkSciMarkText { get => _benchmarkSciMarkText; set => SetBenchmarkText(ref _benchmarkSciMarkText, value, BenchmarkMetricCardKey.SciMark); }

    public string BenchmarkZstdCompressionText { get => _benchmarkZstdCompressionText; set => SetBenchmarkText(ref _benchmarkZstdCompressionText, value, BenchmarkMetricCardKey.ZstdCompression); }

    public string BenchmarkZstdDecompressionText { get => _benchmarkZstdDecompressionText; set => SetBenchmarkText(ref _benchmarkZstdDecompressionText, value, BenchmarkMetricCardKey.ZstdDecompression); }

    public string BenchmarkHashText { get => _benchmarkHashText; set => SetBenchmarkText(ref _benchmarkHashText, value, BenchmarkMetricCardKey.XxHash3); }

    public string BenchmarkThreadsText { get => _benchmarkThreadsText; set => SetProperty(ref _benchmarkThreadsText, value); }

    public string BenchmarkCpuFrequencyText { get => _benchmarkCpuFrequencyText; set => SetBenchmarkText(ref _benchmarkCpuFrequencyText, value, BenchmarkMetricCardKey.CpuAverageFrequency); }

    public string BenchmarkCpuTemperatureText { get => _benchmarkCpuTemperatureText; set => SetBenchmarkText(ref _benchmarkCpuTemperatureText, value, BenchmarkMetricCardKey.CpuMaxTemperature); }

    public string BenchmarkCpuEnergyText { get => _benchmarkCpuEnergyText; set => SetBenchmarkText(ref _benchmarkCpuEnergyText, value, BenchmarkMetricCardKey.CpuEnergy); }

    public string BenchmarkPeakPowerText { get => _benchmarkPeakPowerText; set => SetBenchmarkText(ref _benchmarkPeakPowerText, value, BenchmarkMetricCardKey.CpuPeakPower); }

    public string BenchmarkValidationText { get => _benchmarkValidationText; set => SetBenchmarkText(ref _benchmarkValidationText, value, BenchmarkMetricCardKey.Validation); }

    public string BenchmarkDisplayNameText
    {
        get => _benchmarkDisplayNameText;
        set => SetProperty(ref _benchmarkDisplayNameText, value);
    }

    public bool IsBenchmarkUploadAvailable
    {
        get => _isBenchmarkUploadAvailable;
        set
        {
            if (SetProperty(ref _isBenchmarkUploadAvailable, value))
            {
                RaisePropertyChanged(nameof(CanUploadBenchmarkResult));
                RaisePropertyChanged(nameof(BenchmarkUploadButtonText));
                RaisePropertyChanged(nameof(CanDeleteBenchmarkResult));
            }
        }
    }

    public bool IsBenchmarkUploadRunning
    {
        get => _isBenchmarkUploadRunning;
        set
        {
            if (SetProperty(ref _isBenchmarkUploadRunning, value))
            {
                RaisePropertyChanged(nameof(CanUploadBenchmarkResult));
                RaisePropertyChanged(nameof(BenchmarkUploadButtonText));
                RaisePropertyChanged(nameof(CanDeleteBenchmarkResult));
            }
        }
    }

    public bool IsBenchmarkUploadCooldownRunning
    {
        get => _isBenchmarkUploadCooldownRunning;
        set
        {
            if (SetProperty(ref _isBenchmarkUploadCooldownRunning, value))
            {
                RaisePropertyChanged(nameof(CanUploadBenchmarkResult));
                RaisePropertyChanged(nameof(BenchmarkUploadButtonText));
            }
        }
    }

    public bool IsBenchmarkDeleteRunning
    {
        get => _isBenchmarkDeleteRunning;
        set
        {
            if (SetProperty(ref _isBenchmarkDeleteRunning, value))
            {
                RaisePropertyChanged(nameof(CanUploadBenchmarkResult));
                RaisePropertyChanged(nameof(CanDeleteBenchmarkResult));
                RaisePropertyChanged(nameof(BenchmarkDeleteButtonText));
            }
        }
    }

    public bool CanUploadBenchmarkResult => IsBenchmarkUploadAvailable && !IsBenchmarkRunning && !IsBenchmarkUploadRunning && !IsBenchmarkUploadCooldownRunning && !IsBenchmarkDeleteRunning;

    public string BenchmarkUploadButtonText => IsBenchmarkUploadRunning
        ? Localization.BenchmarkUploading
        : IsBenchmarkUploadCooldownRunning
        ? Localization.ActionCooldown
        : Localization.BenchmarkUploadButton;

    public bool CanDeleteBenchmarkResult => !IsBenchmarkRunning && !IsBenchmarkUploadRunning && !IsBenchmarkDeleteRunning && !IsLeaderboardLoading;

    public string BenchmarkDeleteButtonText => IsBenchmarkDeleteRunning
        ? Localization.BenchmarkDeleting
        : Localization.BenchmarkDeleteButton;

    public string BenchmarkUploadStatusText { get => _benchmarkUploadStatusText; set => SetProperty(ref _benchmarkUploadStatusText, value); }

    public bool IsLeaderboardLoading
    {
        get => _isLeaderboardLoading;
        set
        {
            if (SetProperty(ref _isLeaderboardLoading, value))
            {
                RaisePropertyChanged(nameof(CanRefreshLeaderboard));
                RaisePropertyChanged(nameof(CanDeleteBenchmarkResult));
                RaisePropertyChanged(nameof(LeaderboardButtonText));
            }
        }
    }

    public bool IsLeaderboardRefreshCooldownRunning
    {
        get => _isLeaderboardRefreshCooldownRunning;
        set
        {
            if (SetProperty(ref _isLeaderboardRefreshCooldownRunning, value))
            {
                RaisePropertyChanged(nameof(CanRefreshLeaderboard));
                RaisePropertyChanged(nameof(LeaderboardButtonText));
            }
        }
    }

    public bool CanRefreshLeaderboard => !IsLeaderboardLoading && !IsLeaderboardRefreshCooldownRunning;

    public string LeaderboardButtonText => IsLeaderboardLoading
        ? Localization.LeaderboardLoadingButton
        : IsLeaderboardRefreshCooldownRunning
        ? Localization.ActionCooldown
        : Localization.LeaderboardRefreshButton;

    public string LeaderboardStatusText { get => _leaderboardStatusText; set => SetProperty(ref _leaderboardStatusText, value); }

    public string BenchmarkProgressText { get => _benchmarkProgressText; set => SetProperty(ref _benchmarkProgressText, value); }

    public double BenchmarkProgressValue { get => _benchmarkProgressValue; set => SetProperty(ref _benchmarkProgressValue, value); }

    public int SelectedBenchmarkVersionIndex
    {
        get => _selectedBenchmarkVersionIndex;
        set
        {
            if (SetProperty(ref _selectedBenchmarkVersionIndex, Math.Clamp(value, 0, 1)))
            {
                SelectedLeaderboardScoreIndex = BenchmarkRunner.SupportsCpuCoreScore(ResolveBenchmarkVersion()) ? 0 : 1;
                RefreshBenchmarkDisplay();
                RefreshLeaderboardScoreChrome();
            }
        }
    }

    public int SelectedBenchmarkModeIndex
    {
        get => _selectedBenchmarkModeIndex;
        set
        {
            int modeIndex = Math.Clamp(value, 0, 1);
            if (SetProperty(ref _selectedBenchmarkModeIndex, modeIndex))
            {
                RefreshBenchmarkDisplay();
            }
        }
    }

    public int SelectedBenchmarkProfileIndex
    {
        get => _selectedBenchmarkProfileIndex;
        set
        {
            if (SetProperty(ref _selectedBenchmarkProfileIndex, Math.Clamp(value, 0, 2)))
            {
                RefreshBenchmarkDisplay();
            }
        }
    }

    public int SelectedLeaderboardScoreIndex
    {
        get => _selectedLeaderboardScoreIndex;
        set
        {
            if (SetProperty(ref _selectedLeaderboardScoreIndex, Math.Clamp(value, 0, 1)))
            {
                RefreshLeaderboardScoreChrome();
            }
        }
    }

    public bool IsLeaderboardScorePickerEnabled => BenchmarkRunner.SupportsCpuCoreScore(ResolveBenchmarkVersion());

    public bool IsLeaderboardCpuCoreSelected => ResolveLeaderboardScoreKind() == BenchmarkScoreKind.CpuCore;

    public string LeaderboardScoreKindText => IsLeaderboardCpuCoreSelected
        ? Localization.Resource("Ui_CpuCoreShort")
        : Localization.Resource("Ui_CpuMixedShort");

    public string LeaderboardSwitchToolTip => IsLeaderboardCpuCoreSelected
        ? Localization.Resource("Ui_CpuCoreScore")
        : Localization.Resource("Ui_CpuMixedScore");

    public double LeaderboardSwitchKnobOffset => IsLeaderboardCpuCoreSelected ? 26d : 0d;

    public IBrush LeaderboardSwitchBrush => DashboardBrushes.Teal;

    public string MemoryUsageText { get => _memoryUsageText; set => SetProperty(ref _memoryUsageText, value); }

    public string MemoryCapacityText { get => _memoryCapacityText; set => SetProperty(ref _memoryCapacityText, value); }

    public string MemoryTempText { get => _memoryTempText; set => SetProperty(ref _memoryTempText, value); }

    public bool IsStartupOverlayVisible { get => _isStartupOverlayVisible; set => SetProperty(ref _isStartupOverlayVisible, value); }

    public double StartupOverlayOpacity { get => _startupOverlayOpacity; set => SetProperty(ref _startupOverlayOpacity, value); }

    public string StartupStatusText { get => _startupStatusText; set => SetProperty(ref _startupStatusText, value); }

    public int SelectedChartRangeIndex { get => _selectedChartRangeIndex; set => SetProperty(ref _selectedChartRangeIndex, value); }

    public int SelectedUpdateIntervalIndex { get => _selectedUpdateIntervalIndex; set => SetProperty(ref _selectedUpdateIntervalIndex, value); }

    public int SelectedThemeIndex { get => _selectedThemeIndex; set => SetProperty(ref _selectedThemeIndex, value); }

    public int SelectedLanguageIndex { get => _selectedLanguageIndex; set => SetProperty(ref _selectedLanguageIndex, value); }

    public bool IsSidebarCompact
    {
        get => _isSidebarCompact;
        set
        {
            if (SetProperty(ref _isSidebarCompact, value))
            {
                IsSidebarExpanded = !value;
                SidebarToggleToolTip = value ? Localization.ExpandSidebar : Localization.CollapseSidebar;
            }
        }
    }

    public bool IsSidebarExpanded { get => _isSidebarExpanded; private set => SetProperty(ref _isSidebarExpanded, value); }

    public string SidebarToggleToolTip { get => _sidebarToggleToolTip; private set => SetProperty(ref _sidebarToggleToolTip, value); }

    public Thickness SidebarMargin { get => _sidebarMargin; set => SetProperty(ref _sidebarMargin, value); }

    public void RefreshWaitingText()
    {
        HardwareSummaryText = Localization.WaitingForHardwareSensors;
        StatusText = Localization.WaitingForSensors;
        StartupStatusText = Localization.OpeningSensorBackend;
        RefreshLocalizedChrome();
    }

    public void RefreshLocalizedChrome()
    {
        SidebarToggleToolTip = IsSidebarCompact ? Localization.ExpandSidebar : Localization.CollapseSidebar;
        if (!IsPawnIoInstallRunning)
        {
            PawnIoInstallButtonText = Localization.Resource("Ui_InstallPawnIo");
        }

        RaisePropertyChanged(nameof(AdminRestartButtonText));
        RaisePropertyChanged(nameof(PawnIoPromptTitleText));
        RaisePropertyChanged(nameof(PawnIoPromptSubtitleText));
        RefreshCpuCoreGraphChrome();
        RaisePropertyChanged(nameof(BenchmarkStartButtonText));
        RaisePropertyChanged(nameof(CanUploadBenchmarkResult));
        RaisePropertyChanged(nameof(BenchmarkUploadButtonText));
        RaisePropertyChanged(nameof(BenchmarkDeleteButtonText));
        RaisePropertyChanged(nameof(CanRefreshLeaderboard));
        RaisePropertyChanged(nameof(LeaderboardButtonText));
        RefreshBenchmarkMetricCards();
        RefreshLeaderboardScoreChrome();
        RefreshBenchmarkDisplay();
        if (!IsBenchmarkRunning && BenchmarkProgressValue <= 0 && BenchmarkScoreText == "--")
        {
            BenchmarkStatusText = Localization.BenchmarkReady;
            BenchmarkUploadStatusText = Localization.BenchmarkUploadNoResult;
        }

        if (LeaderboardEntries.Count == 0 && !IsLeaderboardLoading)
        {
            LeaderboardStatusText = Localization.LeaderboardReady;
        }
    }

    private void RefreshCpuCoreGraphChrome()
    {
        RaisePropertyChanged(nameof(CpuCoreGraphTitle));
        RaisePropertyChanged(nameof(CpuCoreGraphToggleText));
        RaisePropertyChanged(nameof(CpuCoreGraphToggleToolTip));
        RaisePropertyChanged(nameof(CpuLogicalGraphOpacity));
        RaisePropertyChanged(nameof(CpuOverallGraphOpacity));
        RaisePropertyChanged(nameof(CpuLogicalGraphScale));
        RaisePropertyChanged(nameof(CpuOverallGraphScale));
    }

    private void AddBenchmarkMetric(BenchmarkMetricCardKey key, string titleResourceKey, string valueText, bool isPrimary = false)
    {
        BenchmarkMetricCardViewModel card = new(titleResourceKey, valueText, isPrimary);
        _benchmarkMetricCardsByKey[key] = card;
        BenchmarkMetricCards.Add(card);
    }

    private void SetBenchmarkMetric(BenchmarkMetricCardKey key, string valueText)
    {
        if (_benchmarkMetricCardsByKey.TryGetValue(key, out BenchmarkMetricCardViewModel? card))
        {
            card.ValueText = valueText;
        }
    }

    private void SetBenchmarkText(ref string field, string value, BenchmarkMetricCardKey key, [CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref field, value, propertyName))
        {
            SetBenchmarkMetric(key, value);
        }
    }

    private void RefreshBenchmarkMetricCards()
    {
        foreach (BenchmarkMetricCardViewModel card in BenchmarkMetricCards)
        {
            card.RefreshLocalization();
        }
    }

    private void RefreshLeaderboardScoreChrome()
    {
        RaisePropertyChanged(nameof(IsLeaderboardScorePickerEnabled));
        RaisePropertyChanged(nameof(IsLeaderboardCpuCoreSelected));
        RaisePropertyChanged(nameof(LeaderboardScoreKindText));
        RaisePropertyChanged(nameof(LeaderboardSwitchToolTip));
        RaisePropertyChanged(nameof(LeaderboardSwitchKnobOffset));
        RaisePropertyChanged(nameof(LeaderboardSwitchBrush));
    }

    public int ResolveBenchmarkWorkerCount()
    {
        return SelectedBenchmarkModeIndex == 0 ? 1 : BenchmarkRunner.MaxWorkerCount;
    }

    public BenchmarkRunProfile ResolveBenchmarkProfile()
    {
        return SelectedBenchmarkProfileIndex switch
        {
            0 => BenchmarkRunProfile.Quick,
            2 => BenchmarkRunProfile.Sustained,
            _ => BenchmarkRunProfile.Standard
        };
    }

    public BenchmarkProfilePlan ResolveBenchmarkPlan()
    {
        return BenchmarkRunner.GetPlan(ResolveBenchmarkProfile(), ResolveBenchmarkVersion());
    }

    public string ResolveBenchmarkVersion()
    {
        return SelectedBenchmarkVersionIndex == 1 ? BenchmarkRunner.LegacyVersion : BenchmarkRunner.Version;
    }

    public BenchmarkScoreKind ResolveLeaderboardScoreKind()
    {
        if (!BenchmarkRunner.SupportsCpuCoreScore(ResolveBenchmarkVersion()))
        {
            return BenchmarkScoreKind.CpuMixed;
        }

        return SelectedLeaderboardScoreIndex == 1
            ? BenchmarkScoreKind.CpuMixed
            : BenchmarkScoreKind.CpuCore;
    }

    public string ResolveBenchmarkModeText()
    {
        return Localization.BenchmarkModeText(SelectedBenchmarkModeIndex);
    }

    private void RefreshBenchmarkDisplay()
    {
        int workers = ResolveBenchmarkWorkerCount();
        BenchmarkProfilePlan plan = ResolveBenchmarkPlan();
        BenchmarkModeText = ResolveBenchmarkModeText();
        BenchmarkThreadsText = Localization.BenchmarkThreadCount(workers);
        if (!IsBenchmarkRunning && BenchmarkProgressValue <= 0)
        {
            BenchmarkProgressText = FormatBenchmarkProgress(0, TimeSpan.Zero, plan.TotalDuration);
        }
    }

    public static string FormatBenchmarkProgress(double percent, TimeSpan elapsed, TimeSpan duration)
    {
        return $"{percent:0}% | {FormatBenchmarkDuration(elapsed)} / {FormatBenchmarkDuration(duration)}";
    }

    public static string FormatBenchmarkDuration(TimeSpan duration)
    {
        int totalSeconds = Math.Max(0, (int)Math.Round(duration.TotalSeconds));
        return totalSeconds >= 60
            ? $"{totalSeconds / 60}m {totalSeconds % 60:00}s"
            : $"{totalSeconds}s";
    }

    private static string ResolveApplicationVersion()
    {
        Assembly assembly = typeof(MainWindowViewModel).Assembly;
        string? informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        string? version = string.IsNullOrWhiteSpace(informationalVersion)
            ? assembly.GetName().Version?.ToString(3)
            : informationalVersion.Split('+')[0];

        return string.IsNullOrWhiteSpace(version) ? "1.0.0" : version;
    }

    private enum BenchmarkMetricCardKey
    {
        CpuCoreScore,
        CpuMixedScore,
        Mode,
        Validation,
        SciMark,
        XxHash3,
        ZstdCompression,
        ZstdDecompression,
        CpuAverageFrequency,
        CpuMaxTemperature,
        CpuEnergy,
        CpuPeakPower
    }
}

public sealed class BenchmarkMetricCardViewModel : ObservableDashboardItem
{
    private string _valueText;

    public BenchmarkMetricCardViewModel(string titleResourceKey, string valueText, bool isPrimary)
    {
        TitleResourceKey = titleResourceKey;
        _valueText = valueText;
        IsPrimary = isPrimary;
    }

    public string TitleResourceKey { get; }

    public string TitleText => Localization.Resource(TitleResourceKey);

    public bool IsPrimary { get; }

    public double ValueFontSize => IsPrimary ? 22d : 13d;

    public FontWeight TitleFontWeight => IsPrimary ? FontWeight.SemiBold : FontWeight.Normal;

    public FontWeight ValueFontWeight => IsPrimary ? FontWeight.Bold : FontWeight.SemiBold;

    public string ValueText
    {
        get => _valueText;
        set => SetProperty(ref _valueText, value);
    }

    public void RefreshLocalization()
    {
        RaisePropertyChanged(nameof(TitleText));
    }
}

public interface IDashboardItem<in TData, out TKey>
    where TKey : notnull
{
    TKey Key { get; }

    void Update(TData data);
}

public sealed class LeaderboardEntryViewModel : ObservableDashboardItem
{
    private bool _isExpanded;
    private bool _isDetailLoaded;
    private bool _isDetailLoading;
    private IReadOnlyList<BenchmarkTelemetrySample> _telemetrySamples = [];
    private bool _hasTelemetrySamples;
    private BenchmarkLeaderboardEntry _entry;
    private readonly int _rank;

    public LeaderboardEntryViewModel(int rank, BenchmarkLeaderboardEntry entry, BenchmarkScoreKind scoreKind)
    {
        double score = ResolveScore(entry, scoreKind);
        int displayRank = entry.Rank is > 0 ? entry.Rank.Value : rank;
        _entry = entry;
        _rank = displayRank;
        RunId = entry.RunId;
        RankText = $"#{displayRank}";
        CountryCodeText = NormalizeCountryCode(entry.CountryCode);
        DisplayNameText = string.IsNullOrWhiteSpace(entry.DisplayName)
            ? BenchmarkPayload.DefaultDisplayName
            : entry.DisplayName;
        CpuNameText = ResolveCpuName(entry);
        ScoreText = score > 0 ? score.ToString("N0", CultureInfo.InvariantCulture) : "--";
        ProfileModeText = $"{entry.Profile} / {entry.Mode}";
        VersionText = $"suite {entry.SuiteVersion} | app {entry.AppVersion}";
        CanDelete = BenchmarkOwnershipStore.HasCredential(entry.RunId);
        OwnerFrameBrush = CanDelete ? DashboardBrushes.Teal : Brushes.Transparent;
        OwnerFrameThickness = CanDelete ? new Thickness(2) : new Thickness(0);
        OwnerFramePadding = CanDelete ? new Thickness(2) : new Thickness(0);
        DetailText = BuildDetailText(entry);
        TelemetrySamples = entry.TelemetrySamples is { Count: > 0 } samples ? samples : [];
        HasTelemetrySamples = TelemetrySamples.Count > 1;
        ReplaceDetailItems(BuildDetailItems(displayRank, entry, DisplayNameText, CpuNameText, ScoreText, ProfileModeText, VersionText));
    }

    public event EventHandler? DetailRequested;

    public string RankText { get; }

    public string CountryCodeText { get; }

    public string DisplayNameText { get; }

    public string CpuNameText { get; }

    public string ScoreText { get; }

    public string ProfileModeText { get; }

    public string VersionText { get; }

    public string DetailText { get; }

    public string RunId { get; }

    public bool CanDelete { get; }

    public string DeleteButtonText => Localization.BenchmarkDeleteButton;

    public IBrush OwnerFrameBrush { get; }

    public Thickness OwnerFrameThickness { get; }

    public Thickness OwnerFramePadding { get; }

    public ObservableCollection<LeaderboardDetailItemViewModel> DetailItems { get; } = [];

    public IReadOnlyList<BenchmarkTelemetrySample> TelemetrySamples
    {
        get => _telemetrySamples;
        private set
        {
            if (!ReferenceEquals(_telemetrySamples, value))
            {
                _telemetrySamples = value;
                RaisePropertyChanged();
                HasTelemetrySamples = _telemetrySamples.Count > 1;
            }
        }
    }

    public bool HasTelemetrySamples
    {
        get => _hasTelemetrySamples;
        private set => SetProperty(ref _hasTelemetrySamples, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value)
                && value
                && !_isDetailLoaded
                && !_isDetailLoading
                && !string.IsNullOrWhiteSpace(RunId))
            {
                DetailRequested?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void MarkDetailLoading()
    {
        _isDetailLoading = true;
    }

    public void MarkDetailFailed()
    {
        _isDetailLoading = false;
    }

    public void ApplyDetail(BenchmarkRunDetail? detail, IReadOnlyList<BenchmarkTelemetrySample> telemetrySamples)
    {
        if (detail is not null)
        {
            _entry = MergeDetail(_entry, detail, telemetrySamples);
            ReplaceDetailItems(BuildDetailItems(_rank, _entry, DisplayNameText, CpuNameText, ScoreText, ProfileModeText, VersionText));
        }

        if (telemetrySamples.Count > 0)
        {
            TelemetrySamples = telemetrySamples;
        }

        _isDetailLoaded = true;
        _isDetailLoading = false;
    }

    private void ReplaceDetailItems(IReadOnlyList<LeaderboardDetailItemViewModel> items)
    {
        DetailItems.Clear();
        foreach (LeaderboardDetailItemViewModel item in items)
        {
            DetailItems.Add(item);
        }
    }

    private static BenchmarkLeaderboardEntry MergeDetail(
        BenchmarkLeaderboardEntry current,
        BenchmarkRunDetail detail,
        IReadOnlyList<BenchmarkTelemetrySample> telemetrySamples)
    {
        return new BenchmarkLeaderboardEntry
        {
            RunId = string.IsNullOrWhiteSpace(detail.RunId) ? current.RunId : detail.RunId,
            Rank = current.Rank,
            DisplayName = string.IsNullOrWhiteSpace(detail.DisplayName) ? current.DisplayName : detail.DisplayName,
            CountryCode = current.CountryCode,
            RankingKey = current.RankingKey,
            RankingScore = current.RankingScore,
            AppVersion = current.AppVersion,
            SuiteId = string.IsNullOrWhiteSpace(detail.SuiteId) ? current.SuiteId : detail.SuiteId,
            SuiteVersion = string.IsNullOrWhiteSpace(detail.SuiteVersion) ? current.SuiteVersion : detail.SuiteVersion,
            Profile = string.IsNullOrWhiteSpace(detail.Profile) ? current.Profile : detail.Profile,
            Mode = string.IsNullOrWhiteSpace(detail.Mode) ? current.Mode : detail.Mode,
            CpuName = current.CpuName,
            CpuCores = current.CpuCores,
            CpuThreads = current.CpuThreads,
            Hardware = detail.Hardware ?? current.Hardware,
            Scores = detail.Scores.Count == 0 ? current.Scores : detail.Scores,
            Metrics = detail.Metrics.Count == 0 ? current.Metrics : detail.Metrics,
            HasDetail = true,
            HasTelemetry = current.HasTelemetry || telemetrySamples.Count > 0,
            ClientCreatedAt = current.ClientCreatedAt,
            ServerCreatedAt = current.ServerCreatedAt,
            TelemetrySamples = telemetrySamples.Count == 0 ? current.TelemetrySamples : telemetrySamples
        };
    }

    private static string BuildDetailText(BenchmarkLeaderboardEntry entry)
    {
        string cpu = ResolveCpuName(entry);
        (int? coresValue, int? threadsValue) = ResolveCpuTopology(entry);
        string cores = coresValue is { } coreCount && threadsValue is { } threadCount
            ? $"{coreCount}c/{threadCount}t"
            : "--";
        return $"{cpu} | {cores}";
    }

    private static string NormalizeCountryCode(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return "--";
        }

        string code = countryCode.Trim().ToUpperInvariant();
        if (code.Length != 2 || code.Any(ch => ch is < 'A' or > 'Z') || code is "XX" or "T1")
        {
            return code;
        }

        return code;
    }

    private static IReadOnlyList<LeaderboardDetailItemViewModel> BuildDetailItems(
        int rank,
        BenchmarkLeaderboardEntry entry,
        string displayName,
        string cpuName,
        string score,
        string profileMode,
        string version)
    {
        (int? coresValue, int? threadsValue) = ResolveCpuTopology(entry);
        string cores = coresValue is { } coreCount && threadsValue is { } threadCount
            ? $"{coreCount}c / {threadCount}t"
            : "--";
        string energyText = BenchmarkTelemetrySummaries.FormatEnergy(entry.TelemetrySamples);
        string peakPowerText = BenchmarkTelemetrySummaries.FormatPeakPower(entry.TelemetrySamples);

        return
        [
            new("Rank", $"#{rank}"),
            new("Name", displayName),
            new("Country", NormalizeCountryCode(entry.CountryCode)),
            new("Ranking Score", score),
            new("CPU Core Score", FormatNumber(FindScore(entry, BenchmarkPayload.CpuCoreScoreKey))),
            new("CPU Mixed Score", FormatNumber(FindScore(entry, BenchmarkPayload.CpuMixedScoreKey))),
            new("Profile / Mode", profileMode),
            new("Version", version),
            new("CPU", cpuName),
            new("Cores / Threads", cores),
            new("SciMark", FormatNumber(FindMetric(entry, BenchmarkPayload.SciMarkMetricKey))),
            new("zstd compression", FormatMetric(entry, BenchmarkPayload.ZstdCompressMetricKey, "GB/s")),
            new("zstd decompression", FormatMetric(entry, BenchmarkPayload.ZstdDecompressMetricKey, "GB/s")),
            new("XxHash3", FormatMetric(entry, BenchmarkPayload.XxHash3MetricKey, "GB/s")),
            new("Avg frequency", FormatMetric(entry, BenchmarkPayload.CpuAverageFrequencyMetricKey, "GHz")),
            new("Max temp", FormatMetric(entry, BenchmarkPayload.CpuMaxTemperatureMetricKey, "C")),
            new("CPU energy", energyText),
            new("Peak power", peakPowerText),
            new("Client time", string.IsNullOrWhiteSpace(entry.ClientCreatedAt) ? "--" : entry.ClientCreatedAt),
            new("Server time", string.IsNullOrWhiteSpace(entry.ServerCreatedAt) ? "--" : entry.ServerCreatedAt),
            new("ID", string.IsNullOrWhiteSpace(entry.RunId) ? "--" : entry.RunId)
        ];
    }

    private static string FormatNumber(double? value) => value is { } number
        ? number.ToString("N0", CultureInfo.InvariantCulture)
        : "--";

    private static string FormatMetric(BenchmarkLeaderboardEntry entry, string key, string fallbackUnit)
    {
        BenchmarkValueDto? metric = FindValue(entry.Metrics, key);
        if (metric is null)
        {
            return "--";
        }

        string unit = string.IsNullOrWhiteSpace(metric.Unit) ? fallbackUnit : metric.Unit;
        return unit switch
        {
            "GB/s" => $"{metric.Value:0.00} GB/s",
            "GHz" => $"{metric.Value:0.00} GHz",
            "C" => $"{metric.Value:0.#} C",
            _ => string.IsNullOrWhiteSpace(unit)
                ? metric.Value.ToString("N0", CultureInfo.InvariantCulture)
                : $"{metric.Value:0.##} {unit}"
        };
    }

    private static double ResolveScore(BenchmarkLeaderboardEntry entry, BenchmarkScoreKind scoreKind)
    {
        if (entry.RankingScore is { } rankingScore && double.IsFinite(rankingScore) && rankingScore > 0)
        {
            return rankingScore;
        }

        string scoreKey = BenchmarkPayload.ScoreKindToApiValue(scoreKind);
        if (FindScore(entry, scoreKey) is { } score)
        {
            return score;
        }

        return FindScore(entry, BenchmarkPayload.CpuMixedScoreKey) ?? 0;
    }

    private static string ResolveCpuName(BenchmarkLeaderboardEntry entry)
    {
        string? cpuName = string.IsNullOrWhiteSpace(entry.CpuName)
            ? entry.Hardware?.Cpu?.Name
            : entry.CpuName;
        return string.IsNullOrWhiteSpace(cpuName) ? "--" : cpuName;
    }

    private static (int? Cores, int? Threads) ResolveCpuTopology(BenchmarkLeaderboardEntry entry)
    {
        int? cores = entry.CpuCores ?? entry.Hardware?.Cpu?.Cores;
        int? threads = entry.CpuThreads ?? entry.Hardware?.Cpu?.Threads;
        return (cores, threads);
    }

    private static double? FindScore(BenchmarkLeaderboardEntry entry, string key) => FindValue(entry.Scores, key)?.Value;

    private static double? FindMetric(BenchmarkLeaderboardEntry entry, string key) => FindValue(entry.Metrics, key)?.Value;

    private static BenchmarkValueDto? FindValue(IReadOnlyList<BenchmarkValueDto>? values, string key)
    {
        return values?.FirstOrDefault(value => string.Equals(value.Key, key, StringComparison.Ordinal));
    }
}

internal static class BenchmarkTelemetrySummaries
{
    public static string FormatEnergy(IReadOnlyList<BenchmarkTelemetrySample>? samples)
    {
        if (samples is null || samples.Count < 2)
        {
            return "--";
        }

        double wattSeconds = 0;
        BenchmarkTelemetrySample? previous = null;
        for (int i = 0; i < samples.Count; i++)
        {
            BenchmarkTelemetrySample current = samples[i];
            if (current.CpuPackagePowerW is not { } currentPower || !double.IsFinite(currentPower) || currentPower < 0)
            {
                previous = null;
                continue;
            }

            if (previous?.CpuPackagePowerW is { } previousPower && double.IsFinite(previousPower) && previousPower >= 0)
            {
                double deltaSeconds = current.ElapsedSeconds - previous.ElapsedSeconds;
                if (double.IsFinite(deltaSeconds) && deltaSeconds > 0)
                {
                    wattSeconds += ((previousPower + currentPower) * 0.5d) * deltaSeconds;
                }
            }

            previous = current;
        }

        return FormatEnergy(wattSeconds);
    }

    public static string FormatEnergy(double wattSeconds)
    {
        if (!double.IsFinite(wattSeconds) || wattSeconds <= 0)
        {
            return "--";
        }

        double wattHours = wattSeconds / 3600d;
        return wattHours < 1
            ? $"{wattHours * 1000d:0.#} mWh"
            : $"{wattHours:0.00} Wh";
    }

    public static string FormatPeakPower(IReadOnlyList<BenchmarkTelemetrySample>? samples)
    {
        double? peakPower = null;
        if (samples is not null)
        {
            for (int i = 0; i < samples.Count; i++)
            {
                double? value = samples[i].CpuPackagePowerW;
                if (value is { } power && double.IsFinite(power) && power >= 0)
                {
                    peakPower = peakPower is null ? power : Math.Max(peakPower.Value, power);
                }
            }
        }

        return FormatPeakPower(peakPower);
    }

    public static string FormatPeakPower(double? peakPower)
    {
        return peakPower is { } power && double.IsFinite(power) && power >= 0
            ? $"{power:0.#} W"
            : "--";
    }
}

public sealed record LeaderboardDetailItemViewModel(string LabelText, string ValueText);

public sealed class MetricItemViewModel : ObservableDashboardItem, IDashboardItem<MetricReading, string>
{
    private string _label = string.Empty;
    private string _valueText = "--";

    public MetricItemViewModel(MetricReading reading)
    {
        Key = MetricKey(reading);
        Update(reading);
    }

    public string Key { get; }

    public string Label { get => _label; private set => SetProperty(ref _label, value); }

    public string ValueText { get => _valueText; private set => SetProperty(ref _valueText, value); }

    public void Update(MetricReading reading)
    {
        Label = Localization.MetricLabel(reading.Name, reading.Kind);
        ValueText = reading.ValueText;
    }

    public static string MetricKey(MetricReading reading) => $"{reading.Kind}:{reading.Name}";
}

public sealed record SensorGroupReading(
    string Key,
    string Title,
    IReadOnlyList<MetricReading> Metrics,
    bool IsExpandedByDefault);

public sealed class SensorGroupViewModel : ObservableDashboardItem, IDashboardItem<SensorGroupReading, string>
{
    private string _title = string.Empty;
    private string _statusText = "--";
    private string _summaryOneLabel = "--";
    private string _summaryOneText = "--";
    private string _summaryTwoLabel = "--";
    private string _summaryTwoText = "--";
    private string _summaryThreeLabel = "--";
    private string _summaryThreeText = "--";
    private bool _isExpanded;

    public SensorGroupViewModel(SensorGroupReading reading)
    {
        Key = reading.Key;
        _isExpanded = reading.IsExpandedByDefault;
        Update(reading);
    }

    public string Key { get; }

    public ObservableCollection<MetricItemViewModel> Metrics { get; } = [];

    public string Title { get => _title; private set => SetProperty(ref _title, value); }

    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

    public string SummaryOneLabel { get => _summaryOneLabel; private set => SetProperty(ref _summaryOneLabel, value); }

    public string SummaryOneText { get => _summaryOneText; private set => SetProperty(ref _summaryOneText, value); }

    public string SummaryTwoLabel { get => _summaryTwoLabel; private set => SetProperty(ref _summaryTwoLabel, value); }

    public string SummaryTwoText { get => _summaryTwoText; private set => SetProperty(ref _summaryTwoText, value); }

    public string SummaryThreeLabel { get => _summaryThreeLabel; private set => SetProperty(ref _summaryThreeLabel, value); }

    public string SummaryThreeText { get => _summaryThreeText; private set => SetProperty(ref _summaryThreeText, value); }

    public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }

    public void Update(SensorGroupReading reading)
    {
        Title = reading.Title;
        StatusText = DashboardStatus.SensorGroupStatus(reading.Metrics);
        ApplySummary(reading);
        DashboardCollection.SyncItems(
            Metrics,
            reading.Metrics,
            MetricItemViewModel.MetricKey,
            metric => new MetricItemViewModel(metric));
    }

    private void ApplySummary(SensorGroupReading reading)
    {
        IReadOnlyList<MetricReading> metrics = reading.Metrics;
        if (metrics.Count == 0)
        {
            SetSummary("--", "--", "--", "--", Localization.SummaryLabel("sensors"), "0");
            return;
        }

        switch (reading.Key)
        {
            case "temperature":
                SetSummary(
                    Localization.SummaryLabel("peak"),
                    MetricFormatter.FormatTemperature(MaxMetric(metrics)),
                    Localization.SummaryLabel("avg"),
                    MetricFormatter.FormatTemperature(AverageMetric(metrics)),
                    Localization.SummaryLabel("sensors"),
                    metrics.Count.ToString());
                break;
            case "power":
                MetricReading? package = metrics.FirstOrDefault(metric => metric.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
                    ?? metrics.FirstOrDefault(metric => metric.Name.Contains("Total", StringComparison.OrdinalIgnoreCase));
                SetSummary(
                    package is null ? Localization.SummaryLabel("total") : Localization.SummaryLabel("package"),
                    package?.ValueText ?? MetricFormatter.FormatPower(SumMetric(metrics)),
                    Localization.SummaryLabel("peakRail"),
                    MetricFormatter.FormatPower(MaxMetric(metrics)),
                    Localization.SummaryLabel("sensors"),
                    metrics.Count.ToString());
                break;
            case "clock":
                SetSummary(
                    Localization.SummaryLabel("avg"),
                    FormatClock(AverageMetric(metrics)),
                    Localization.SummaryLabel("max"),
                    FormatClock(MaxMetric(metrics)),
                    Localization.SummaryLabel("sensors"),
                    metrics.Count.ToString());
                break;
            case "voltage":
                SetSummary(
                    Localization.SummaryLabel("max"),
                    FormatVoltage(MaxMetric(metrics)),
                    Localization.SummaryLabel("avg"),
                    FormatVoltage(AverageMetric(metrics)),
                    Localization.SummaryLabel("sensors"),
                    metrics.Count.ToString());
                break;
            default:
                SetSummary(Localization.SummaryLabel("primary"), metrics[0].ValueText, Localization.SummaryLabel("sensors"), metrics.Count.ToString(), "--", "--");
                break;
        }
    }

    private void SetSummary(
        string oneLabel,
        string oneText,
        string twoLabel,
        string twoText,
        string threeLabel,
        string threeText)
    {
        SummaryOneLabel = oneLabel;
        SummaryOneText = oneText;
        SummaryTwoLabel = twoLabel;
        SummaryTwoText = twoText;
        SummaryThreeLabel = threeLabel;
        SummaryThreeText = threeText;
    }

    private static string FormatClock(double valueMHz) => valueMHz >= 1000
        ? $"{valueMHz / 1000d:0.00} GHz"
        : $"{valueMHz:0} MHz";

    private static string FormatVoltage(double value) => $"{value:0.###} V";

    private static float MaxMetric(IReadOnlyList<MetricReading> metrics)
    {
        float max = metrics[0].Value;
        for (int i = 1; i < metrics.Count; i++)
        {
            max = Math.Max(max, metrics[i].Value);
        }

        return max;
    }

    private static float SumMetric(IReadOnlyList<MetricReading> metrics)
    {
        float sum = 0;
        for (int i = 0; i < metrics.Count; i++)
        {
            sum += metrics[i].Value;
        }

        return sum;
    }

    private static float AverageMetric(IReadOnlyList<MetricReading> metrics) => SumMetric(metrics) / metrics.Count;
}

internal static class DashboardStatus
{
    public static string SensorGroupStatus(IReadOnlyList<MetricReading> metrics)
    {
        if (metrics.Count == 0)
        {
            return Localization.NoSensors;
        }

        string thermal = ThermalStatus(metrics, 85, 75);
        if (!string.IsNullOrEmpty(thermal))
        {
            return thermal;
        }

        int warnings = metrics.Count(IsGaugeWarning);
        return warnings > 0 ? Localization.WarningCount(warnings) : Localization.SensorCount(metrics.Count);
    }

    public static string DeviceStatus(
        IReadOnlyList<MetricReading> primaryMetrics,
        IReadOnlyList<MetricReading> secondaryMetrics,
        float hotThreshold,
        float warmThreshold)
    {
        int count = primaryMetrics.Count + secondaryMetrics.Count;
        if (count == 0)
        {
            return Localization.NoSensors;
        }

        string thermal = ThermalStatus(primaryMetrics, secondaryMetrics, hotThreshold, warmThreshold);
        if (!string.IsNullOrEmpty(thermal))
        {
            return thermal;
        }

        int warnings = CountGaugeWarnings(primaryMetrics) + CountGaugeWarnings(secondaryMetrics);
        return warnings > 0 ? Localization.WarningCount(warnings) : Localization.Normal;
    }

    private static string ThermalStatus(IReadOnlyList<MetricReading> metrics, float hotThreshold, float warmThreshold)
    {
        int hot = CountTemperaturesAtOrAbove(metrics, hotThreshold);
        if (hot > 0)
        {
            return Localization.HotCount(hot);
        }

        int warm = CountTemperaturesAtOrAbove(metrics, warmThreshold);
        return warm > 0 ? Localization.WarmCount(warm) : string.Empty;
    }

    private static string ThermalStatus(
        IReadOnlyList<MetricReading> primaryMetrics,
        IReadOnlyList<MetricReading> secondaryMetrics,
        float hotThreshold,
        float warmThreshold)
    {
        int hot = CountTemperaturesAtOrAbove(primaryMetrics, hotThreshold) + CountTemperaturesAtOrAbove(secondaryMetrics, hotThreshold);
        if (hot > 0)
        {
            return Localization.HotCount(hot);
        }

        int warm = CountTemperaturesAtOrAbove(primaryMetrics, warmThreshold) + CountTemperaturesAtOrAbove(secondaryMetrics, warmThreshold);
        return warm > 0 ? Localization.WarmCount(warm) : string.Empty;
    }

    private static int CountTemperaturesAtOrAbove(IReadOnlyList<MetricReading> metrics, float threshold)
    {
        int count = 0;
        for (int i = 0; i < metrics.Count; i++)
        {
            MetricReading metric = metrics[i];
            if (metric.Kind.Equals("Temperature", StringComparison.OrdinalIgnoreCase) && metric.Value >= threshold)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountGaugeWarnings(IReadOnlyList<MetricReading> metrics)
    {
        int count = 0;
        for (int i = 0; i < metrics.Count; i++)
        {
            if (IsGaugeWarning(metrics[i]))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsGaugeWarning(MetricReading metric)
    {
        if (!metric.Kind.Equals("Load", StringComparison.OrdinalIgnoreCase)
            && !metric.Kind.Equals("Control", StringComparison.OrdinalIgnoreCase)
            && !metric.Kind.Equals("Level", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return metric.GaugeValue >= 95;
    }
}

public sealed record OverviewReading(
    string Key,
    string Title,
    string PrimaryText,
    string SecondaryText,
    string DetailText,
    double GaugeValue,
    IBrush AccentBrush);

public static class ChartHistorySettings
{
    private static int _displaySeconds = 10;
    private static double _sampleIntervalSeconds = 1;

    public static int DisplaySeconds { get => _displaySeconds; set => _displaySeconds = Math.Clamp(value, 10, 300); }

    public static double SampleIntervalSeconds { get => _sampleIntervalSeconds; set => _sampleIntervalSeconds = Math.Clamp(value, 0.5, 10); }

    public static int MaxSamples => Math.Max(2, (int)Math.Ceiling(DisplaySeconds / SampleIntervalSeconds));
}

public sealed class OverviewItemViewModel : ObservableDashboardItem, IDashboardItem<OverviewReading, string>
{
    private string _title = string.Empty;
    private string _primaryText = "--";
    private string _secondaryText = "--";
    private string _detailText = "--";
    private IBrush _accentBrush = DashboardBrushes.Blue;
    private double _gaugeValue;
    private int _sampleVersion;
    private bool _isCompact;

    public OverviewItemViewModel(OverviewReading reading)
    {
        Key = reading.Key;
        Update(reading);
    }

    public string Key { get; }

    public string Title { get => _title; private set => SetProperty(ref _title, value); }

    public string PrimaryText { get => _primaryText; private set => SetProperty(ref _primaryText, value); }

    public string SecondaryText { get => _secondaryText; private set => SetProperty(ref _secondaryText, value); }

    public string DetailText { get => _detailText; private set => SetProperty(ref _detailText, value); }

    public IBrush AccentBrush { get => _accentBrush; private set => SetProperty(ref _accentBrush, value); }

    public double GaugeValue { get => _gaugeValue; private set => SetProperty(ref _gaugeValue, value); }

    public int SampleVersion { get => _sampleVersion; private set => SetProperty(ref _sampleVersion, value); }

    public bool IsCompact
    {
        get => _isCompact;
        set
        {
            if (SetProperty(ref _isCompact, value))
            {
                RaisePropertyChanged(nameof(IsExpandedView));
            }
        }
    }

    public bool IsExpandedView => !IsCompact;

    public void Update(OverviewReading reading)
    {
        Title = reading.Title;
        PrimaryText = reading.PrimaryText;
        SecondaryText = reading.SecondaryText;
        DetailText = reading.DetailText;
        AccentBrush = reading.AccentBrush;
        GaugeValue = reading.GaugeValue;
        SampleVersion = unchecked(SampleVersion + 1);
    }
}

public sealed class CoreItemViewModel : ObservableDashboardItem, IDashboardItem<CoreReading, int>
{
    private static readonly IBrush[] LoadBrushCache = BuildLoadBrushCache();

    private string _loadText = "--";
    private IBrush _loadBrush = DashboardBrushes.Blue;
    private double _loadPercent;
    private int _sampleVersion;

    public CoreItemViewModel(CoreReading reading)
    {
        Key = reading.Index;
        Update(reading);
    }

    public int Key { get; }

    public string LoadText { get => _loadText; private set => SetProperty(ref _loadText, value); }

    public IBrush LoadBrush { get => _loadBrush; private set => SetProperty(ref _loadBrush, value); }

    public double LoadPercent { get => _loadPercent; private set => SetProperty(ref _loadPercent, value); }

    public int SampleVersion { get => _sampleVersion; private set => SetProperty(ref _sampleVersion, value); }

    public void Update(CoreReading reading)
    {
        LoadText = reading.LoadText;
        LoadBrush = BuildLoadBrush(reading.LoadPercent);
        LoadPercent = reading.LoadPercent;
        SampleVersion = unchecked(SampleVersion + 1);
    }

    private static IBrush BuildLoadBrush(int loadPercent) => LoadBrushCache[Math.Clamp(loadPercent, 0, LoadBrushCache.Length - 1)];

    private static IBrush[] BuildLoadBrushCache()
    {
        IBrush[] brushes = new IBrush[101];
        for (int loadPercent = 0; loadPercent < brushes.Length; loadPercent++)
        {
            brushes[loadPercent] = BuildLoadBrushCore(loadPercent);
        }

        return brushes;
    }

    private static IBrush BuildLoadBrushCore(int loadPercent)
    {
        double t = Math.Clamp(loadPercent, 0, 100) / 100d;
        byte red = (byte)Math.Round(Lerp(0x37, 0xF9, t));
        byte green = (byte)Math.Round(Lerp(0xB7, 0x70, t));
        byte blue = (byte)Math.Round(Lerp(0xE8, 0x66, t));
        return new SolidColorBrush(Color.FromArgb(255, red, green, blue));
    }

    private static double Lerp(double start, double end, double amount) => start + ((end - start) * amount);
}

public sealed class GpuDeviceViewModel : ObservableDashboardItem, IDashboardItem<GpuDeviceReading, string>
{
    private string _name = string.Empty;
    private string _loadText = "--";
    private string _temperatureText = "--";
    private string _powerText = "--";
    private string _statusText = "--";
    private bool _isExpanded;

    public GpuDeviceViewModel(GpuDeviceReading reading)
    {
        Key = reading.Name;
        Update(reading);
    }

    public string Key { get; }

    public ObservableCollection<MetricItemViewModel> PowerSensors { get; } = [];

    public ObservableCollection<MetricItemViewModel> TemperatureSensors { get; } = [];

    public ObservableCollection<MetricItemViewModel> MemorySensors { get; } = [];

    public string Name { get => _name; private set => SetProperty(ref _name, value); }

    public string LoadText { get => _loadText; private set => SetProperty(ref _loadText, value); }

    public string TemperatureText { get => _temperatureText; private set => SetProperty(ref _temperatureText, value); }

    public string PowerText { get => _powerText; private set => SetProperty(ref _powerText, value); }

    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

    public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }

    public void Update(GpuDeviceReading reading)
    {
        Name = reading.Name;
        LoadText = reading.LoadText;
        TemperatureText = reading.TemperatureText;
        PowerText = reading.PowerText;
        StatusText = DashboardStatus.DeviceStatus(
            reading.TemperatureSensors,
            reading.LoadSensors,
            85,
            75);
        DashboardCollection.SyncItems(
            PowerSensors,
            reading.PowerSensors,
            MetricItemViewModel.MetricKey,
            reading => new MetricItemViewModel(reading));
        DashboardCollection.SyncItems(
            TemperatureSensors,
            reading.TemperatureSensors,
            MetricItemViewModel.MetricKey,
            reading => new MetricItemViewModel(reading));
        DashboardCollection.SyncItems(
            MemorySensors,
            reading.MemorySensors,
            MetricItemViewModel.MetricKey,
            reading => new MetricItemViewModel(reading));
    }
}

public sealed class StorageDeviceViewModel : ObservableDashboardItem, IDashboardItem<StorageDeviceReading, string>
{
    private string _name = string.Empty;
    private string _usageText = "--";
    private string _statusText = "--";
    private bool _isExpanded;

    public StorageDeviceViewModel(StorageDeviceReading reading)
    {
        Key = reading.Name;
        Update(reading);
    }

    public string Key { get; }

    public ObservableCollection<MetricItemViewModel> Metrics { get; } = [];

    public string Name { get => _name; private set => SetProperty(ref _name, value); }

    public string UsageText { get => _usageText; private set => SetProperty(ref _usageText, value); }

    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

    public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }

    public void Update(StorageDeviceReading reading)
    {
        Name = reading.Name;
        UsageText = reading.UsageText;
        StatusText = DashboardStatus.DeviceStatus(
            reading.TemperatureSensors,
            reading.UsageSensors,
            60,
            50);
        DashboardCollection.SyncItems(
            Metrics,
            reading.Metrics,
            MetricItemViewModel.MetricKey,
            reading => new MetricItemViewModel(reading));
    }
}

public static class DashboardCollection
{
    public static void SyncItems<TView, TData, TKey>(
        ObservableCollection<TView> collection,
        IEnumerable<TData> data,
        Func<TData, TKey> dataKey,
        Func<TData, TView> create)
        where TView : IDashboardItem<TData, TKey>
        where TKey : notnull
    {
        Sync(collection, data, item => item.Key, dataKey, create, (item, reading) => item.Update(reading));
    }

    public static void Sync<TView, TData, TKey>(
        ObservableCollection<TView> collection,
        IEnumerable<TData> data,
        Func<TView, TKey> viewKey,
        Func<TData, TKey> dataKey,
        Func<TData, TView> create,
        Action<TView, TData> update)
        where TKey : notnull
    {
        IReadOnlyList<TData> incoming = data as IReadOnlyList<TData> ?? data.ToArray();
        bool sameShape = collection.Count == incoming.Count;

        if (sameShape)
        {
            for (int i = 0; i < incoming.Count; i++)
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

        for (int i = 0; i < incoming.Count; i++)
        {
            update(collection[i], incoming[i]);
        }
    }
}

internal static class DashboardBrushes
{
    public static readonly IBrush Blue = Solid("#37B7E8");
    public static readonly IBrush Red = Solid("#F97066");
    public static readonly IBrush Green = Solid("#32D583");
    public static readonly IBrush Amber = Solid("#FDB022");
    public static readonly IBrush Teal = Solid("#087F8C");
    public static readonly IBrush SwitchOffTrack = Solid("#202A36");
    public static readonly IBrush Purple = Solid("#C77DFF");
    public static readonly IBrush OrangeRed = Solid("#FF5A4F");
    public static readonly IBrush LimeGreen = Solid("#32D583");
    public static readonly IBrush CoreCellBackground = Solid("#202733");
    public static readonly IBrush CoreCellBorder = Solid("#455060");
    public static readonly IBrush CorePillBackground = Solid("#28303C");

    private static IBrush Solid(string color) => new SolidColorBrush(Color.Parse(color));
}
