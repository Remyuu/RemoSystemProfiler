using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace RemoSystemProfiler;

public sealed class ScrollFadeHost : Panel
{
    public static readonly StyledProperty<IBrush?> TopFadeProperty =
        AvaloniaProperty.Register<ScrollFadeHost, IBrush?>(nameof(TopFade));

    public static readonly StyledProperty<IBrush?> BottomFadeProperty =
        AvaloniaProperty.Register<ScrollFadeHost, IBrush?>(nameof(BottomFade));

    public static readonly StyledProperty<double> FadeHeightProperty =
        AvaloniaProperty.Register<ScrollFadeHost, double>(nameof(FadeHeight), 10d);

    private readonly Border _topFade = new()
    {
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
        IsHitTestVisible = false
    };

    private readonly Border _bottomFade = new()
    {
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
        IsHitTestVisible = false
    };

    private ScrollViewer? _scrollViewer;

    static ScrollFadeHost()
    {
        TopFadeProperty.Changed.AddClassHandler<ScrollFadeHost>((control, _) => control.UpdateFadePresenters());
        BottomFadeProperty.Changed.AddClassHandler<ScrollFadeHost>((control, _) => control.UpdateFadePresenters());
        FadeHeightProperty.Changed.AddClassHandler<ScrollFadeHost>((control, _) => control.UpdateFadePresenters());
    }

    public ScrollFadeHost()
    {
        Children.Add(_topFade);
        Children.Add(_bottomFade);
        DetachedFromVisualTree += (_, _) => DetachScrollViewer();
    }

    public IBrush? TopFade
    {
        get => GetValue(TopFadeProperty);
        set => SetValue(TopFadeProperty, value);
    }

    public IBrush? BottomFade
    {
        get => GetValue(BottomFadeProperty);
        set => SetValue(BottomFadeProperty, value);
    }

    public double FadeHeight
    {
        get => GetValue(FadeHeightProperty);
        set => SetValue(FadeHeightProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachScrollViewer();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachScrollViewer();
        base.OnDetachedFromVisualTree(e);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        EnsureOverlayOrder();
        foreach (Control child in Children)
        {
            child.Measure(availableSize);
        }

        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        EnsureOverlayOrder();
        Rect contentRect = new(finalSize);
        foreach (Control child in Children)
        {
            if (!ReferenceEquals(child, _topFade) && !ReferenceEquals(child, _bottomFade))
            {
                child.Arrange(contentRect);
            }
        }

        double fadeHeight = Math.Clamp(FadeHeight, 0, finalSize.Height / 2);
        _topFade.Arrange(new Rect(0, 0, finalSize.Width, fadeHeight));
        _bottomFade.Arrange(new Rect(0, finalSize.Height - fadeHeight, finalSize.Width, fadeHeight));
        AttachScrollViewer();
        return finalSize;
    }

    private void EnsureOverlayOrder()
    {
        if (Children.Count >= 2
            && ReferenceEquals(Children[^2], _topFade)
            && ReferenceEquals(Children[^1], _bottomFade))
        {
            return;
        }

        Children.Remove(_topFade);
        Children.Remove(_bottomFade);
        Children.Add(_topFade);
        Children.Add(_bottomFade);
    }

    private void AttachScrollViewer()
    {
        ScrollViewer? scrollViewer = this.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault();
        if (ReferenceEquals(_scrollViewer, scrollViewer))
        {
            UpdateFadeState();
            return;
        }

        DetachScrollViewer();
        _scrollViewer = scrollViewer;
        if (_scrollViewer is null)
        {
            UpdateFadeState();
            return;
        }

        _scrollViewer.PropertyChanged += ScrollViewer_PropertyChanged;
        UpdateFadeState();
    }

    private void DetachScrollViewer()
    {
        if (_scrollViewer is not null)
        {
            _scrollViewer.PropertyChanged -= ScrollViewer_PropertyChanged;
        }

        _scrollViewer = null;
    }

    private void ScrollViewer_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.OffsetProperty
            || e.Property == ScrollViewer.ExtentProperty
            || e.Property == ScrollViewer.ViewportProperty)
        {
            UpdateFadeState();
        }
    }

    private void UpdateFadeState()
    {
        bool showTop = false;
        bool showBottom = false;
        if (_scrollViewer is { } scrollViewer)
        {
            const double epsilon = 0.5;
            showTop = scrollViewer.Offset.Y > epsilon;
            showBottom = scrollViewer.Offset.Y + scrollViewer.Viewport.Height < scrollViewer.Extent.Height - epsilon;
        }

        _topFade.IsVisible = showTop;
        _bottomFade.IsVisible = showBottom;
        UpdateFadePresenters();
    }

    private void UpdateFadePresenters()
    {
        double fadeHeight = Math.Max(0, FadeHeight);
        _topFade.Height = fadeHeight;
        _topFade.Background = TopFade;
        _bottomFade.Height = fadeHeight;
        _bottomFade.Background = BottomFade;
    }
}
