using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using RemoSystemProfiler.Core;
using System.Collections;
using System.ComponentModel;

namespace RemoSystemProfiler;

public sealed partial class BenchmarkPanel : UserControl
{
    private const int IconAnimationFrames = 12;
    private const int RefreshRotationMilliseconds = 360;
    private const int UploadLoopMilliseconds = 520;

    private CancellationTokenSource? _refreshRotationAnimation;
    private CancellationTokenSource? _uploadIconAnimation;
    private MainWindowViewModel? _viewModel;

    private TranslateTransform UploadIconTransform => UploadLeaderboardIcon.RenderTransform as TranslateTransform
        ?? new TranslateTransform();

    private RotateTransform RefreshIconTransform => RefreshLeaderboardIcon.RenderTransform as RotateTransform
        ?? new RotateTransform();

    public event EventHandler? RunRequested;
    public event EventHandler? CancelRequested;
    public event EventHandler? UploadRequested;
    public event EventHandler<LeaderboardDeleteRequestedEventArgs>? DeleteRequested;
    public event EventHandler? RefreshLeaderboardRequested;
    public event EventHandler? ToggleLeaderboardScoreRequested;
    public event EventHandler<BenchmarkTelemetryZoomRequestedEventArgs>? TelemetryZoomRequested;

    public BenchmarkPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        DetachedFromVisualTree += (_, _) =>
        {
            StopRefreshRotationAnimation();
            StopUploadIconAnimation();
            WatchViewModel(null);
        };
        RefreshLocalization();
    }

    public void RefreshLocalization()
    {
        BenchmarkVersionText.Text = BenchmarkRunner.Version;
        Dispatcher.UIThread.Post(RefreshLocalizedSelectionBoxes, DispatcherPriority.Render);
    }

    public void RefreshLocalizedSelectionBoxes()
    {
        RefreshSelectionBox(BenchmarkProfilePicker);
        RefreshSelectionBox(BenchmarkModePicker);
    }

    private void RunButton_Click(object? sender, RoutedEventArgs e) => RunRequested?.Invoke(this, EventArgs.Empty);

    private void CancelButton_Click(object? sender, RoutedEventArgs e) => CancelRequested?.Invoke(this, EventArgs.Empty);

    private void UploadButton_Click(object? sender, RoutedEventArgs e)
    {
        PulseButton(UploadLeaderboardButton);
        UploadRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshLeaderboardButton_Click(object? sender, RoutedEventArgs e)
    {
        StartRefreshRotationAnimation();
        RefreshLeaderboardRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ScoreToggleButton_Click(object? sender, RoutedEventArgs e) => ToggleLeaderboardScoreRequested?.Invoke(this, EventArgs.Empty);

    private void LeaderboardDeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: LeaderboardEntryViewModel entry })
        {
            DeleteRequested?.Invoke(this, new LeaderboardDeleteRequestedEventArgs(entry));
        }

        e.Handled = true;
    }

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

    private static void PulseButton(Control button)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            if (button.RenderTransform is not ScaleTransform scale)
            {
                return;
            }

            scale.ScaleX = 1.08;
            scale.ScaleY = 1.08;
            await Task.Delay(90).ConfigureAwait(true);
            scale.ScaleX = 1;
            scale.ScaleY = 1;
        }, DispatcherPriority.Input);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        WatchViewModel(DataContext as MainWindowViewModel);
    }

    private void WatchViewModel(MainWindowViewModel? viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        _viewModel = viewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            SyncUploadAnimation();
        }
        else
        {
            StopUploadIconAnimation();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsBenchmarkUploadRunning))
        {
            SyncUploadAnimation();
        }
    }

    private void SyncUploadAnimation()
    {
        if (_viewModel?.IsBenchmarkUploadRunning == true)
        {
            StartUploadIconAnimation();
        }
        else
        {
            StopUploadIconAnimation();
        }
    }

    private void StartRefreshRotationAnimation()
    {
        StopRefreshRotationAnimation();
        CancellationTokenSource animation = new();
        _refreshRotationAnimation = animation;
        _ = AnimateRefreshRotationAsync(animation);
    }

    private async Task AnimateRefreshRotationAsync(CancellationTokenSource animation)
    {
        CancellationToken token = animation.Token;
        try
        {
            RotateTransform transform = RefreshIconTransform;
            RefreshLeaderboardIcon.RenderTransform = transform;
            double from = NormalizeAngle(transform.Angle);
            double to = from + 360d;
            for (int frame = 1; frame <= IconAnimationFrames; frame++)
            {
                token.ThrowIfCancellationRequested();
                double t = frame / (double)IconAnimationFrames;
                transform.Angle = Lerp(from, to, EaseOutCubic(t));
                await Task.Delay(RefreshRotationMilliseconds / IconAnimationFrames, token).ConfigureAwait(true);
            }

            transform.Angle = NormalizeAngle(to);
        }
        catch (OperationCanceledException)
        {
            // Superseded by another click.
        }
        finally
        {
            if (ReferenceEquals(_refreshRotationAnimation, animation))
            {
                animation.Dispose();
                _refreshRotationAnimation = null;
            }
        }
    }

    private void StartUploadIconAnimation()
    {
        if (_uploadIconAnimation is not null)
        {
            return;
        }

        CancellationTokenSource animation = new();
        _uploadIconAnimation = animation;
        _ = AnimateUploadIconAsync(animation);
    }

    private async Task AnimateUploadIconAsync(CancellationTokenSource animation)
    {
        CancellationToken token = animation.Token;
        try
        {
            while (!token.IsCancellationRequested)
            {
                TranslateTransform transform = UploadIconTransform;
                UploadLeaderboardIcon.RenderTransform = transform;
                for (int frame = 0; frame <= IconAnimationFrames; frame++)
                {
                    token.ThrowIfCancellationRequested();
                    double t = frame / (double)IconAnimationFrames;
                    transform.Y = Lerp(2.5, -3.5, EaseOutCubic(t));
                    UploadLeaderboardIcon.Opacity = Lerp(0.55, 1, t);
                    await Task.Delay(UploadLoopMilliseconds / (IconAnimationFrames * 2), token).ConfigureAwait(true);
                }

                for (int frame = 0; frame <= IconAnimationFrames; frame++)
                {
                    token.ThrowIfCancellationRequested();
                    double t = frame / (double)IconAnimationFrames;
                    transform.Y = Lerp(-3.5, 2.5, t);
                    UploadLeaderboardIcon.Opacity = Lerp(1, 0.55, t);
                    await Task.Delay(UploadLoopMilliseconds / (IconAnimationFrames * 2), token).ConfigureAwait(true);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Upload finished or the control is closing.
        }
        finally
        {
            if (ReferenceEquals(_uploadIconAnimation, animation))
            {
                animation.Dispose();
                _uploadIconAnimation = null;
                UploadIconTransform.Y = 0;
                UploadLeaderboardIcon.Opacity = 1;
            }
        }
    }

    private void StopRefreshRotationAnimation()
    {
        _refreshRotationAnimation?.Cancel();
    }

    private void StopUploadIconAnimation()
    {
        _uploadIconAnimation?.Cancel();
        UploadIconTransform.Y = 0;
        UploadLeaderboardIcon.Opacity = 1;
    }

    private static double EaseOutCubic(double t)
    {
        double p = 1d - Math.Clamp(t, 0d, 1d);
        return 1d - (p * p * p);
    }

    private static double Lerp(double from, double to, double t) => from + ((to - from) * t);

    private static double NormalizeAngle(double angle)
    {
        double normalized = angle % 360d;
        return normalized < 0 ? normalized + 360d : normalized;
    }
}

public sealed class BenchmarkTelemetryZoomRequestedEventArgs(IEnumerable? samples) : EventArgs
{
    public IEnumerable? Samples { get; } = samples;
}

public sealed class LeaderboardDeleteRequestedEventArgs(LeaderboardEntryViewModel entry) : EventArgs
{
    public LeaderboardEntryViewModel Entry { get; } = entry;
}
