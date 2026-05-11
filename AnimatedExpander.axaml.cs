using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace RemoSystemProfiler;

public sealed partial class AnimatedExpander : UserControl
{
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<AnimatedExpander, object?>(nameof(Header));

    public static readonly StyledProperty<object?> BodyProperty =
        AvaloniaProperty.Register<AnimatedExpander, object?>(nameof(Body));

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<AnimatedExpander, bool>(
            nameof(IsExpanded),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    private CancellationTokenSource? _animation;
    private bool _isHeaderPressed;

    public AnimatedExpander()
    {
        InitializeComponent();
        PropertyChanged += OnControlPropertyChanged;
        AttachedToVisualTree += (_, _) => ApplyExpandedState(IsExpanded, false);
        DetachedFromVisualTree += (_, _) => StopAnimation();
    }

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    private void OnControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsExpandedProperty)
        {
            ApplyExpandedState(IsExpanded, true);
            return;
        }

        if (e.Property == BodyProperty && IsExpanded)
        {
            Dispatcher.UIThread.Post(() => BodyHost.MaxHeight = MeasureBodyHeight(), DispatcherPriority.Loaded);
        }
    }

    private void ApplyExpandedState(bool expanded, bool animated)
    {
        StopAnimation();
        UpdateChevron(expanded);

        if (!animated)
        {
            BodyHost.IsVisible = expanded;
            BodyHost.Opacity = expanded ? 1 : 0;
            BodyHost.MaxHeight = expanded ? double.PositiveInfinity : 0;
            return;
        }

        _animation = new CancellationTokenSource();
        _ = AnimateBodyAsync(expanded, _animation.Token);
    }

    private async Task AnimateBodyAsync(bool expanded, CancellationToken token)
    {
        if (expanded)
        {
            BodyHost.IsVisible = true;
        }

        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        if (token.IsCancellationRequested)
        {
            return;
        }

        double currentHeight = Math.Max(0, BodyHost.Bounds.Height);
        double targetHeight = expanded ? MeasureBodyHeight() : 0;
        double startOpacity = BodyHost.Opacity;
        double targetOpacity = expanded ? 1 : 0;
        for (int frame = 1; frame <= DashboardAnimation.Frames; frame++)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            double t = frame / (double)DashboardAnimation.Frames;
            double eased = DashboardAnimation.EaseOutCubic(t);
            BodyHost.MaxHeight = DashboardAnimation.Lerp(currentHeight, targetHeight, eased);
            BodyHost.Opacity = DashboardAnimation.Lerp(startOpacity, targetOpacity, eased);
            await Task.Delay(DashboardAnimation.DurationMilliseconds / DashboardAnimation.Frames, token).ConfigureAwait(true);
        }

        if (expanded)
        {
            BodyHost.MaxHeight = double.PositiveInfinity;
            BodyHost.Opacity = 1;
        }
        else
        {
            BodyHost.MaxHeight = 0;
            BodyHost.Opacity = 0;
            BodyHost.IsVisible = false;
        }
    }

    private double MeasureBodyHeight()
    {
        double width = BodyHost.Bounds.Width;
        if (width <= 0)
        {
            width = Bounds.Width;
        }

        if (width <= 0)
        {
            width = 360;
        }

        BodyPresenter.Measure(new Size(width, double.PositiveInfinity));
        return Math.Ceiling(BodyPresenter.DesiredSize.Height);
    }

    private void UpdateChevron(bool expanded)
    {
        if (Chevron.RenderTransform is RotateTransform transform)
        {
            transform.Angle = expanded ? 180 : 0;
        }
    }

    private void StopAnimation()
    {
        _animation?.Cancel();
        _animation?.Dispose();
        _animation = null;
    }

    private void HeaderSurface_PointerEntered(object? sender, PointerEventArgs e)
    {
        ChevronButton.Classes.Add("header-hover");
    }

    private void HeaderSurface_PointerExited(object? sender, PointerEventArgs e)
    {
        _isHeaderPressed = false;
        ChevronButton.Classes.Remove("header-hover");
        ChevronButton.Classes.Remove("header-pressed");
    }

    private void HeaderSurface_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(HeaderSurface).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isHeaderPressed = true;
        ChevronButton.Classes.Add("header-pressed");
        e.Handled = true;
    }

    private void HeaderSurface_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isHeaderPressed)
        {
            return;
        }

        _isHeaderPressed = false;
        ChevronButton.Classes.Remove("header-pressed");
        IsExpanded = !IsExpanded;
        e.Handled = true;
    }
}
