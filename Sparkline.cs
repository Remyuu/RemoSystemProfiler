using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace RemoSystemProfiler;

public sealed class Sparkline : Control
{
    public static readonly StyledProperty<IReadOnlyList<double>?> ValuesProperty =
        AvaloniaProperty.Register<Sparkline, IReadOnlyList<double>?>(nameof(Values));

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<Sparkline, IBrush?>(nameof(Stroke), DashboardBrushes.Blue);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Sparkline, double>(nameof(StrokeThickness), 2d);

    static Sparkline()
    {
        AffectsRender<Sparkline>(ValuesProperty, StrokeProperty, StrokeThicknessProperty);
    }

    private Point[] _pointCache = [];
    private Pen? _penCache;
    private IBrush? _penBrush;
    private double _penThickness;

    public IReadOnlyList<double>? Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        IReadOnlyList<double>? values = Values;
        if (values is null || values.Count == 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        Pen pen = GetPen();
        if (values.Count == 1)
        {
            double y = Y(values[0]);
            context.DrawLine(pen, new Point(0, y), new Point(Bounds.Width, y));
            return;
        }

        EnsurePointCache(values.Count);
        double step = Bounds.Width / (values.Count - 1);
        for (int i = 0; i < values.Count; i++)
        {
            _pointCache[i] = new Point(i * step, Y(values[i]));
        }

        for (int i = 1; i < values.Count; i++)
        {
            context.DrawLine(pen, _pointCache[i - 1], _pointCache[i]);
        }

        double Y(double value)
        {
            double clamped = Math.Clamp(value, 0, 100);
            return Bounds.Height - clamped / 100d * Bounds.Height;
        }
    }

    private void EnsurePointCache(int count)
    {
        if (_pointCache.Length < count)
        {
            _pointCache = new Point[count];
        }
    }

    private Pen GetPen()
    {
        IBrush brush = Stroke ?? DashboardBrushes.Blue;
        double thickness = StrokeThickness;
        if (_penCache is null || !ReferenceEquals(_penBrush, brush) || Math.Abs(_penThickness - thickness) > double.Epsilon)
        {
            _penBrush = brush;
            _penThickness = thickness;
            _penCache = new Pen(brush, thickness);
        }

        return _penCache;
    }
}
