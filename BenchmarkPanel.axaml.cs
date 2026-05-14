using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RemoSystemProfiler.Core;
using System.Collections;

namespace RemoSystemProfiler;

public sealed partial class BenchmarkPanel : UserControl
{
    public event EventHandler? RunRequested;
    public event EventHandler? CancelRequested;
    public event EventHandler? UploadRequested;
    public event EventHandler? RefreshLeaderboardRequested;
    public event EventHandler? ToggleLeaderboardScoreRequested;
    public event EventHandler<BenchmarkTelemetryZoomRequestedEventArgs>? TelemetryZoomRequested;

    public BenchmarkPanel()
    {
        InitializeComponent();
        RefreshLocalization();
    }

    public void RefreshLocalization()
    {
        BenchmarkVersion21Item.Content = BenchmarkRunner.Version;
        BenchmarkVersion20Item.Content = BenchmarkRunner.LegacyVersion;
        Dispatcher.UIThread.Post(RefreshLocalizedSelectionBoxes, DispatcherPriority.Render);
    }

    public void RefreshLocalizedSelectionBoxes()
    {
        RefreshSelectionBox(BenchmarkVersionPicker);
        RefreshSelectionBox(BenchmarkProfilePicker);
        RefreshSelectionBox(BenchmarkModePicker);
    }

    private void RunButton_Click(object? sender, RoutedEventArgs e) => RunRequested?.Invoke(this, EventArgs.Empty);

    private void CancelButton_Click(object? sender, RoutedEventArgs e) => CancelRequested?.Invoke(this, EventArgs.Empty);

    private void UploadButton_Click(object? sender, RoutedEventArgs e) => UploadRequested?.Invoke(this, EventArgs.Empty);

    private void RefreshLeaderboardButton_Click(object? sender, RoutedEventArgs e) => RefreshLeaderboardRequested?.Invoke(this, EventArgs.Empty);

    private void ScoreToggleButton_Click(object? sender, RoutedEventArgs e) => ToggleLeaderboardScoreRequested?.Invoke(this, EventArgs.Empty);

    private void BenchmarkTelemetryPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        TelemetryZoomRequested?.Invoke(this, new BenchmarkTelemetryZoomRequestedEventArgs(MainBenchmarkTelemetryChart.Samples));
        e.Handled = true;
    }

    private void LeaderboardTelemetryPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control { DataContext: LeaderboardEntryViewModel entry })
        {
            TelemetryZoomRequested?.Invoke(this, new BenchmarkTelemetryZoomRequestedEventArgs(entry.TelemetrySamples));
        }

        e.Handled = true;
    }

    private static void RefreshSelectionBox(ComboBox comboBox)
    {
        int selectedIndex = comboBox.SelectedIndex;
        if (selectedIndex < 0)
        {
            return;
        }

        comboBox.SelectedIndex = -1;
        comboBox.SelectedIndex = selectedIndex;
    }
}

public sealed class BenchmarkTelemetryZoomRequestedEventArgs(IEnumerable? samples) : EventArgs
{
    public IEnumerable? Samples { get; } = samples;
}
