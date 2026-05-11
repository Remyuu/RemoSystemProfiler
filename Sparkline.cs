using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace RemoSystemProfiler;

public sealed class Sparkline : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<Sparkline, double>(nameof(Value));

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<Sparkline, IBrush?>(nameof(Stroke), DashboardBrushes.Blue);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Sparkline, double>(nameof(StrokeThickness), 2d);

    private float[] _samples = new float[ChartHistorySettings.MaxSamples];
    private int _start;
    private int _count;
    private StreamGeometry? _geometry;
    private Size _geometrySize;
    private Pen? _penCache;
    private IBrush? _penBrush;
    private double _penThickness;
    private bool _geometryDirty = true;

    static Sparkline()
    {
        AffectsRender<Sparkline>(StrokeProperty, StrokeThicknessProperty);
        ValueProperty.Changed.AddClassHandler<Sparkline>((control, args) =>
        {
            if (args.NewValue is double value)
            {
                control.AddSample(value);
            }
        });
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
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

        if (_count == 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        StreamGeometry? geometry = GetGeometry(Bounds.Size);
        if (geometry is not null)
        {
            context.DrawGeometry(null, GetPen(), geometry);
        }
    }

    private void AddSample(double value)
    {
        EnsureSampleCapacity(ChartHistorySettings.MaxSamples);

        float sample = (float)Math.Clamp(value, 0, 100);
        if (_count < _samples.Length)
        {
            _samples[(_start + _count) % _samples.Length] = sample;
            _count++;
        }
        else
        {
            _samples[_start] = sample;
            _start = (_start + 1) % _samples.Length;
        }

        _geometryDirty = true;
        InvalidateVisual();
    }

    private void EnsureSampleCapacity(int capacity)
    {
        capacity = Math.Max(2, capacity);
        if (_samples.Length == capacity)
        {
            return;
        }

        float[] resized = new float[capacity];
        int copyCount = Math.Min(_count, capacity);
        int skip = Math.Max(0, _count - copyCount);
        for (int i = 0; i < copyCount; i++)
        {
            resized[i] = SampleAt(skip + i);
        }

        _samples = resized;
        _start = 0;
        _count = copyCount;
        _geometryDirty = true;
    }

    private StreamGeometry? GetGeometry(Size size)
    {
        if (!_geometryDirty && _geometry is not null && _geometrySize == size)
        {
            return _geometry;
        }

        _geometrySize = size;
        _geometryDirty = false;

        StreamGeometry geometry = new();
        using StreamGeometryContext stream = geometry.Open();
        if (_count == 1)
        {
            double y = Y(SampleAt(0), size.Height);
            stream.BeginFigure(new Point(0, y), false);
            stream.LineTo(new Point(size.Width, y));
        }
        else
        {
            double step = size.Width / (_count - 1);
            stream.BeginFigure(new Point(0, Y(SampleAt(0), size.Height)), false);
            for (int i = 1; i < _count; i++)
            {
                stream.LineTo(new Point(i * step, Y(SampleAt(i), size.Height)));
            }
        }

        _geometry = geometry;
        return _geometry;
    }

    private float SampleAt(int index) => _samples[(_start + index) % _samples.Length];

    private static double Y(float value, double height) => height - value / 100d * height;

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
