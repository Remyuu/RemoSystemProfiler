using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace RemoSystemProfiler;

public sealed class LoadingPulseBars : Control
{
    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<LoadingPulseBars, IBrush?>(nameof(TrackBrush));

    public static readonly StyledProperty<IBrush?> PrimaryBrushProperty =
        AvaloniaProperty.Register<LoadingPulseBars, IBrush?>(nameof(PrimaryBrush));

    public static readonly StyledProperty<IBrush?> SecondaryBrushProperty =
        AvaloniaProperty.Register<LoadingPulseBars, IBrush?>(nameof(SecondaryBrush));

    public static readonly StyledProperty<IBrush?> TertiaryBrushProperty =
        AvaloniaProperty.Register<LoadingPulseBars, IBrush?>(nameof(TertiaryBrush));

    private readonly DispatcherTimer _timer = new(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;
    private bool _isAttached;

    static LoadingPulseBars()
    {
        AffectsRender<LoadingPulseBars>(
            TrackBrushProperty,
            PrimaryBrushProperty,
            SecondaryBrushProperty,
            TertiaryBrushProperty);
        IsVisibleProperty.Changed.AddClassHandler<LoadingPulseBars>((control, _) => control.UpdateTimerState());
    }

    public LoadingPulseBars()
    {
        _timer.Tick += Timer_Tick;
        AttachedToVisualTree += (_, _) =>
        {
            _isAttached = true;
            UpdateTimerState();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            _isAttached = false;
            _timer.Stop();
        };
    }

    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public IBrush? PrimaryBrush
    {
        get => GetValue(PrimaryBrushProperty);
        set => SetValue(PrimaryBrushProperty, value);
    }

    public IBrush? SecondaryBrush
    {
        get => GetValue(SecondaryBrushProperty);
        set => SetValue(SecondaryBrushProperty, value);
    }

    public IBrush? TertiaryBrush
    {
        get => GetValue(TertiaryBrushProperty);
        set => SetValue(TertiaryBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        double trackHeight = Math.Min(8, Math.Max(4, Bounds.Height / 7));
        double gap = Math.Min(10, trackHeight + 2);
        double totalHeight = trackHeight * 3 + gap * 2;
        double startY = Math.Max(0, (Bounds.Height - totalHeight) / 2);
        double width = Bounds.Width;
        double phase = (DateTimeOffset.UtcNow - _startedAt).TotalSeconds;

        DrawTrack(context, 0, startY, width, trackHeight, phase, PrimaryBrush ?? DashboardBrushes.Blue, 0.00, 0.32);
        DrawTrack(context, 1, startY + trackHeight + gap, width, trackHeight, phase, SecondaryBrush ?? DashboardBrushes.Green, 0.33, 0.44);
        DrawTrack(context, 2, startY + (trackHeight + gap) * 2, width, trackHeight, phase, TertiaryBrush ?? DashboardBrushes.Amber, 0.66, 0.26);
    }

    private void DrawTrack(
        DrawingContext context,
        int row,
        double y,
        double width,
        double height,
        double phaseSeconds,
        IBrush segmentBrush,
        double phaseOffset,
        double segmentWidthRatio)
    {
        Rect track = new(0, y, width, height);
        context.DrawRectangle(TrackBrush ?? DashboardBrushes.CorePillBackground, null, track, height / 2);

        double segmentWidth = Math.Clamp(width * segmentWidthRatio, 28, width);
        double cycle = (phaseSeconds * 0.62 + phaseOffset) % 1d;
        if (row == 1)
        {
            cycle = 1d - cycle;
        }

        double x = -segmentWidth + cycle * (width + segmentWidth);
        Rect segment = new(x, y, segmentWidth, height);
        using (context.PushClip(track))
        {
            context.DrawRectangle(segmentBrush, null, segment, height / 2);
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    private void UpdateTimerState()
    {
        if (_isAttached && IsVisible)
        {
            _timer.Start();
            return;
        }

        _timer.Stop();
    }
}
