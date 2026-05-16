using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using RemoSystemProfiler.Core;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RemoSystemProfiler;

public abstract class ObservableDashboardItem : ObservableObject
{
    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        OnPropertyChanged(propertyName);
    }
}

public sealed partial class MainWindowViewModel : ObservableDashboardItem
{
    [ObservableProperty]
    private string _hardwareSummaryText = Localization.WaitingForHardwareSensors;

    [ObservableProperty]
    private string _statusText = Localization.WaitingForSensors;

    [ObservableProperty]
    private string? _statusToolTip;

    [ObservableProperty]
    private IBrush _statusBrush = DashboardBrushes.Amber;

    [ObservableProperty]
    private bool _isPawnIoDownloadVisible;

    [ObservableProperty]
    private bool _isAdminRestartVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPawnIoInstallAvailable))]
    [NotifyPropertyChangedFor(nameof(IsPawnIoPromptCancelAvailable))]
    private bool _isPawnIoInstallRunning;

    [ObservableProperty]
    private bool _isPawnIoPromptVisible;

    [ObservableProperty]
    private string _pawnIoInstallButtonText = Localization.Resource("Ui_InstallPawnIo");

    [ObservableProperty]
    private string _updatedText = "--:--:--";

    [ObservableProperty]
    private string _cpuNameText = "--";

    [ObservableProperty]
    private string _cpuLoadSummaryText = "--";

    [ObservableProperty]
    private string _clockText = "--";

    [ObservableProperty]
    private string _cpuPackagePowerText = "--";

    [ObservableProperty]
    private string _cpuPeakTempText = "--";

    [ObservableProperty]
    private string _coreCountText = "--";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCpuLogicalProcessorView))]
    [NotifyPropertyChangedFor(nameof(CpuCoreGraphTitle))]
    [NotifyPropertyChangedFor(nameof(CpuCoreGraphToggleText))]
    [NotifyPropertyChangedFor(nameof(CpuCoreGraphToggleToolTip))]
    [NotifyPropertyChangedFor(nameof(CpuLogicalGraphOpacity))]
    [NotifyPropertyChangedFor(nameof(CpuOverallGraphOpacity))]
    [NotifyPropertyChangedFor(nameof(CpuLogicalGraphScale))]
    [NotifyPropertyChangedFor(nameof(CpuOverallGraphScale))]
    private bool _isCpuOverallView;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBenchmarkStartEnabled))]
    [NotifyPropertyChangedFor(nameof(IsBenchmarkCancelVisible))]
    [NotifyPropertyChangedFor(nameof(BenchmarkStartButtonText))]
    [NotifyPropertyChangedFor(nameof(AreBenchmarkSettingsEnabled))]
    [NotifyPropertyChangedFor(nameof(CanUploadBenchmarkResult))]
    [NotifyPropertyChangedFor(nameof(CanDeleteBenchmarkResult))]
    private bool _isBenchmarkRunning;

    [ObservableProperty]
    private string _benchmarkStatusText = Localization.BenchmarkReady;

    [ObservableProperty]
    private string _benchmarkModeText = Localization.BenchmarkModeText(1);

    [ObservableProperty]
    private string _benchmarkScoreText = "--";

    [ObservableProperty]
    private string _benchmarkMixedScoreText = "--";

    [ObservableProperty]
    private string _benchmarkSciMarkText = "--";

    [ObservableProperty]
    private string _benchmarkZstdCompressionText = "--";

    [ObservableProperty]
    private string _benchmarkZstdDecompressionText = "--";

    [ObservableProperty]
    private string _benchmarkHashText = "--";

    [ObservableProperty]
    private string _benchmarkThreadsText = Localization.BenchmarkThreadCount(BenchmarkRunner.MaxWorkerCount);

    [ObservableProperty]
    private string _benchmarkCpuFrequencyText = "--";

    [ObservableProperty]
    private string _benchmarkCpuTemperatureText = "--";

    [ObservableProperty]
    private string _benchmarkCpuEnergyText = "--";

    [ObservableProperty]
    private string _benchmarkPeakPowerText = "--";

    [ObservableProperty]
    private string _benchmarkValidationText = "--";

    [ObservableProperty]
    private string _benchmarkDisplayNameText = BenchmarkPayload.DefaultDisplayName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUploadBenchmarkResult))]
    [NotifyPropertyChangedFor(nameof(BenchmarkUploadButtonText))]
    [NotifyPropertyChangedFor(nameof(CanDeleteBenchmarkResult))]
    private bool _isBenchmarkUploadAvailable;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUploadBenchmarkResult))]
    [NotifyPropertyChangedFor(nameof(BenchmarkUploadButtonText))]
    [NotifyPropertyChangedFor(nameof(CanDeleteBenchmarkResult))]
    private bool _isBenchmarkUploadRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUploadBenchmarkResult))]
    [NotifyPropertyChangedFor(nameof(BenchmarkUploadButtonText))]
    private bool _isBenchmarkUploadCooldownRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUploadBenchmarkResult))]
    [NotifyPropertyChangedFor(nameof(CanDeleteBenchmarkResult))]
    [NotifyPropertyChangedFor(nameof(BenchmarkDeleteButtonText))]
    private bool _isBenchmarkDeleteRunning;

    [ObservableProperty]
    private string _benchmarkUploadStatusText = Localization.BenchmarkUploadNoResult;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRefreshLeaderboard))]
    [NotifyPropertyChangedFor(nameof(CanDeleteBenchmarkResult))]
    [NotifyPropertyChangedFor(nameof(LeaderboardButtonText))]
    private bool _isLeaderboardLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRefreshLeaderboard))]
    [NotifyPropertyChangedFor(nameof(LeaderboardButtonText))]
    private bool _isLeaderboardRefreshCooldownRunning;

    [ObservableProperty]
    private string _leaderboardStatusText = Localization.LeaderboardReady;

    [ObservableProperty]
    private string _benchmarkProgressText = FormatBenchmarkProgress(0, TimeSpan.Zero, BenchmarkRunner.GetPlan(BenchmarkRunProfile.Standard).TotalDuration);

    [ObservableProperty]
    private double _benchmarkProgressValue;

    private int _selectedBenchmarkModeIndex = 1;
    private int _selectedBenchmarkProfileIndex = 1;
    private int _selectedLeaderboardScoreIndex;

    [ObservableProperty]
    private string _memoryUsageText = "--";

    [ObservableProperty]
    private string _memoryCapacityText = "--";

    [ObservableProperty]
    private string _memoryTempText = "";

    [ObservableProperty]
    private bool _isStartupOverlayVisible = true;

    [ObservableProperty]
    private double _startupOverlayOpacity = 1;

    [ObservableProperty]
    private string _startupStatusText = Localization.OpeningSensorBackend;

    [ObservableProperty]
    private int _selectedChartRangeIndex;

    [ObservableProperty]
    private int _selectedUpdateIntervalIndex = 1;

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private int _selectedLanguageIndex = Localization.CurrentLanguageIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSidebarExpanded))]
    [NotifyPropertyChangedFor(nameof(SidebarToggleToolTip))]
    private bool _isSidebarCompact;

    [ObservableProperty]
    private Thickness _sidebarMargin = new(8, 10);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUpdateCheckAvailable))]
    [NotifyPropertyChangedFor(nameof(IsUpdateInstallAvailable))]
    private bool _isUpdateCheckRunning;

    [ObservableProperty]
    private bool _isReleaseNotesVisible;

    [ObservableProperty]
    private bool _isOpenReleaseVisible;

    [ObservableProperty]
    private bool _isInstallUpdateVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUpdateCheckAvailable))]
    [NotifyPropertyChangedFor(nameof(IsUpdateInstallAvailable))]
    private bool _isUpdateInstallRunning;

    [ObservableProperty]
    private bool _isUpdateProgressVisible;

    [ObservableProperty]
    private double _updateProgressValue;

    [ObservableProperty]
    private string _updateProgressText = "0%";

    [ObservableProperty]
    private string _updateStatusText = Localization.UpdateIdle;

    [ObservableProperty]
    private string _releaseNotesText = string.Empty;

    [ObservableProperty]
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

    public string AdminRestartButtonText => Localization.RestartAsAdministrator;

    public bool IsPawnIoInstallAvailable => !IsPawnIoInstallRunning;

    public bool IsPawnIoPromptCancelAvailable => !IsPawnIoInstallRunning;

    public string PawnIoPromptTitleText => Localization.PawnIoRequired;

    public string PawnIoPromptSubtitleText => Localization.InstallPawnIoRestartAdmin;

    public string VersionText => $"v{ApplicationVersion}";

    public string CurrentVersion => ApplicationVersion;

    public bool IsUpdateCheckAvailable => !IsUpdateCheckRunning && !IsUpdateInstallRunning;

    public bool IsUpdateInstallAvailable => !IsUpdateCheckRunning && !IsUpdateInstallRunning;

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

    public bool IsBenchmarkStartEnabled => !IsBenchmarkRunning;

    public bool AreBenchmarkSettingsEnabled => !IsBenchmarkRunning;

    public bool IsBenchmarkCancelVisible => IsBenchmarkRunning;

    public string BenchmarkStartButtonText => IsBenchmarkRunning ? Localization.BenchmarkRunningButton : Localization.BenchmarkRunButton;

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

    public bool CanRefreshLeaderboard => !IsLeaderboardLoading && !IsLeaderboardRefreshCooldownRunning;

    public string LeaderboardButtonText => IsLeaderboardLoading
        ? Localization.LeaderboardLoadingButton
        : IsLeaderboardRefreshCooldownRunning
        ? Localization.ActionCooldown
        : Localization.LeaderboardRefreshButton;

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

    public bool IsLeaderboardScorePickerEnabled => true;

    public bool IsLeaderboardCpuCoreSelected => ResolveLeaderboardScoreKind() == BenchmarkScoreKind.CpuCore;

    public string LeaderboardScoreKindText => IsLeaderboardCpuCoreSelected
        ? Localization.Resource("Ui_CpuCoreShort")
        : Localization.Resource("Ui_CpuMixedShort");

    public string LeaderboardSwitchToolTip => IsLeaderboardCpuCoreSelected
        ? Localization.Resource("Ui_CpuCoreScore")
        : Localization.Resource("Ui_CpuMixedScore");

    public double LeaderboardSwitchKnobOffset => IsLeaderboardCpuCoreSelected ? 26d : 0d;

    public IBrush LeaderboardSwitchBrush => DashboardBrushes.Teal;

    public bool IsSidebarExpanded => !IsSidebarCompact;

    public string SidebarToggleToolTip => IsSidebarCompact ? Localization.ExpandSidebar : Localization.CollapseSidebar;

    public void RefreshWaitingText()
    {
        HardwareSummaryText = Localization.WaitingForHardwareSensors;
        StatusText = Localization.WaitingForSensors;
        StartupStatusText = Localization.OpeningSensorBackend;
        RefreshLocalizedChrome();
    }

    public void RefreshLocalizedChrome()
    {
        RaisePropertyChanged(nameof(SidebarToggleToolTip));
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

    partial void OnBenchmarkModeTextChanged(string value) => SetBenchmarkMetric(BenchmarkMetricCardKey.Mode, value);

    partial void OnBenchmarkScoreTextChanged(string value) => SetBenchmarkMetric(BenchmarkMetricCardKey.CpuCoreScore, value);

    partial void OnBenchmarkMixedScoreTextChanged(string value) => SetBenchmarkMetric(BenchmarkMetricCardKey.CpuMixedScore, value);

    partial void OnBenchmarkSciMarkTextChanged(string value) => SetBenchmarkMetric(BenchmarkMetricCardKey.SciMark, value);

    partial void OnBenchmarkZstdCompressionTextChanged(string value) => SetBenchmarkMetric(BenchmarkMetricCardKey.ZstdCompression, value);

    partial void OnBenchmarkZstdDecompressionTextChanged(string value) => SetBenchmarkMetric(BenchmarkMetricCardKey.ZstdDecompression, value);

    partial void OnBenchmarkHashTextChanged(string value) => SetBenchmarkMetric(BenchmarkMetricCardKey.XxHash3, value);

    partial void OnBenchmarkCpuFrequencyTextChanged(string value) => SetBenchmarkMetric(BenchmarkMetricCardKey.CpuAverageFrequency, value);

    partial void OnBenchmarkCpuTemperatureTextChanged(string value) => SetBenchmarkMetric(BenchmarkMetricCardKey.CpuMaxTemperature, value);

    partial void OnBenchmarkCpuEnergyTextChanged(string value) => SetBenchmarkMetric(BenchmarkMetricCardKey.CpuEnergy, value);

    partial void OnBenchmarkPeakPowerTextChanged(string value) => SetBenchmarkMetric(BenchmarkMetricCardKey.CpuPeakPower, value);

    partial void OnBenchmarkValidationTextChanged(string value) => SetBenchmarkMetric(BenchmarkMetricCardKey.Validation, value);

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
        return BenchmarkRunner.GetPlan(ResolveBenchmarkProfile());
    }

    public BenchmarkScoreKind ResolveLeaderboardScoreKind()
    {
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

public sealed partial class BenchmarkMetricCardViewModel : ObservableDashboardItem
{
    [ObservableProperty]
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

public sealed partial class LeaderboardEntryViewModel : ObservableDashboardItem
{
    private bool _isExpanded;
    private bool _isDetailLoaded;
    private bool _isDetailLoading;
    private IReadOnlyList<BenchmarkTelemetrySample> _telemetrySamples = [];
    [ObservableProperty]
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

public sealed partial class MetricItemViewModel : ObservableDashboardItem, IDashboardItem<MetricReading, string>
{
    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private string _valueText = "--";

    public MetricItemViewModel(MetricReading reading)
    {
        Key = MetricKey(reading);
        Update(reading);
    }

    public string Key { get; }

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

public sealed partial class SensorGroupViewModel : ObservableDashboardItem, IDashboardItem<SensorGroupReading, string>
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _statusText = "--";

    [ObservableProperty]
    private string _summaryOneLabel = "--";

    [ObservableProperty]
    private string _summaryOneText = "--";

    [ObservableProperty]
    private string _summaryTwoLabel = "--";

    [ObservableProperty]
    private string _summaryTwoText = "--";

    [ObservableProperty]
    private string _summaryThreeLabel = "--";

    [ObservableProperty]
    private string _summaryThreeText = "--";

    [ObservableProperty]
    private bool _isExpanded;

    public SensorGroupViewModel(SensorGroupReading reading)
    {
        Key = reading.Key;
        _isExpanded = reading.IsExpandedByDefault;
        Update(reading);
    }

    public string Key { get; }

    public ObservableCollection<MetricItemViewModel> Metrics { get; } = [];

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

public sealed partial class OverviewItemViewModel : ObservableDashboardItem, IDashboardItem<OverviewReading, string>
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _primaryText = "--";

    [ObservableProperty]
    private string _secondaryText = "--";

    [ObservableProperty]
    private string _detailText = "--";

    [ObservableProperty]
    private IBrush _accentBrush = DashboardBrushes.Blue;

    [ObservableProperty]
    private double _gaugeValue;

    [ObservableProperty]
    private int _sampleVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExpandedView))]
    private bool _isCompact;

    public OverviewItemViewModel(OverviewReading reading)
    {
        Key = reading.Key;
        Update(reading);
    }

    public string Key { get; }

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

public sealed partial class CoreItemViewModel : ObservableDashboardItem, IDashboardItem<CoreReading, int>
{
    private static readonly IBrush[] LoadBrushCache = BuildLoadBrushCache();

    [ObservableProperty]
    private string _loadText = "--";

    [ObservableProperty]
    private IBrush _loadBrush = DashboardBrushes.Blue;

    [ObservableProperty]
    private double _loadPercent;

    [ObservableProperty]
    private int _sampleVersion;

    public CoreItemViewModel(CoreReading reading)
    {
        Key = reading.Index;
        Update(reading);
    }

    public int Key { get; }

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

public sealed partial class GpuDeviceViewModel : ObservableDashboardItem, IDashboardItem<GpuDeviceReading, string>
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _loadText = "--";

    [ObservableProperty]
    private string _temperatureText = "--";

    [ObservableProperty]
    private string _powerText = "--";

    [ObservableProperty]
    private string _statusText = "--";

    [ObservableProperty]
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

public sealed partial class StorageDeviceViewModel : ObservableDashboardItem, IDashboardItem<StorageDeviceReading, string>
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _usageText = "--";

    [ObservableProperty]
    private string _statusText = "--";

    [ObservableProperty]
    private bool _isExpanded;

    public StorageDeviceViewModel(StorageDeviceReading reading)
    {
        Key = reading.Name;
        Update(reading);
    }

    public string Key { get; }

    public ObservableCollection<MetricItemViewModel> Metrics { get; } = [];

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
