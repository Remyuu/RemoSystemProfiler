using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace RemoSystemProfiler;

public sealed class Sparkline : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<Sparkline, double>(nameof(Value));

    public static readonly StyledProperty<int> SampleVersionProperty =
        AvaloniaProperty.Register<Sparkline, int>(nameof(SampleVersion));

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<Sparkline, IBrush?>(nameof(Stroke), DashboardBrushes.Blue);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Sparkline, double>(nameof(StrokeThickness), 2d);

    private float[] _samples = new float[ChartHistorySettings.MaxSamples];
    private int _start;
    private int _count;
    private int _samplesVersion;
    private int _geometryVersion = -1;
    private Size _geometrySize;
    private UtilizationChartSegment[] _areaSegments = [];
    private UtilizationChartSegment[] _lineSegments = [];
    private bool _usesSampleVersion;

    static Sparkline()
    {
        AffectsRender<Sparkline>(SampleVersionProperty, StrokeProperty, StrokeThicknessProperty);
        ValueProperty.Changed.AddClassHandler<Sparkline>((control, args) =>
        {
            if (args.NewValue is double value)
            {
                if (!control._usesSampleVersion || control._count <= 1)
                {
                    control.AddSample(value);
                    return;
                }

                control.InvalidateVisual();
            }
        });
        SampleVersionProperty.Changed.AddClassHandler<Sparkline>((control, args) =>
        {
            if (args.NewValue is int version && version > 0)
            {
                control._usesSampleVersion = true;
                control.AddSample(control.Value);
            }
        });
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int SampleVersion
    {
        get => GetValue(SampleVersionProperty);
        set => SetValue(SampleVersionProperty, value);
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

        EnsureGeometryCache(Bounds.Size);
        for (int i = 0; i < _areaSegments.Length; i++)
        {
            UtilizationChartSegment segment = _areaSegments[i];
            context.DrawGeometry(
                UtilizationChartBrushes.GetAreaBrush(segment.LoadBucket, Stroke),
                null,
                segment.Geometry);
        }

        for (int i = 0; i < _lineSegments.Length; i++)
        {
            UtilizationChartSegment segment = _lineSegments[i];
            context.DrawGeometry(
                null,
                UtilizationChartBrushes.GetStrokePen(segment.LoadBucket, Stroke, StrokeThickness),
                segment.Geometry);
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

        InvalidateVisual();
        _samplesVersion++;
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
    }

    private void EnsureGeometryCache(Size size)
    {
        if (_geometryVersion == _samplesVersion && _geometrySize == size)
        {
            return;
        }

        _geometryVersion = _samplesVersion;
        _geometrySize = size;
        BuildAreaSegments(size);
        BuildLineSegments(size);
    }

    private void BuildAreaSegments(Size size)
    {
        if (_count == 1)
        {
            float sample = SampleAt(0);
            _areaSegments =
            [
                new UtilizationChartSegment(
                    BuildAreaSegment(0, size.Width, size.Height, Y(sample, size.Height), Y(sample, size.Height)),
                    UtilizationChartBrushes.LoadBucket(sample))
            ];
            return;
        }

        _areaSegments = new UtilizationChartSegment[Math.Max(0, _count - 1)];
        double step = size.Width / (_count - 1);
        for (int i = 1; i < _count; i++)
        {
            float previous = SampleAt(i - 1);
            float current = SampleAt(i);
            double x0 = (i - 1) * step;
            double x1 = i * step;
            _areaSegments[i - 1] = new UtilizationChartSegment(
                BuildAreaSegment(x0, x1, size.Height, Y(previous, size.Height), Y(current, size.Height)),
                UtilizationChartBrushes.LoadBucket((previous + current) * 0.5));
        }
    }

    private void BuildLineSegments(Size size)
    {
        if (_count == 1)
        {
            float sample = SampleAt(0);
            double y = Y(sample, size.Height);
            _lineSegments =
            [
                new UtilizationChartSegment(
                    BuildLineSegment(0, y, size.Width, y),
                    UtilizationChartBrushes.LoadBucket(sample))
            ];
            return;
        }

        _lineSegments = new UtilizationChartSegment[Math.Max(0, _count - 1)];
        double step = size.Width / (_count - 1);
        for (int i = 1; i < _count; i++)
        {
            float previous = SampleAt(i - 1);
            float current = SampleAt(i);
            _lineSegments[i - 1] = new UtilizationChartSegment(
                BuildLineSegment((i - 1) * step, Y(previous, size.Height), i * step, Y(current, size.Height)),
                UtilizationChartBrushes.LoadBucket((previous + current) * 0.5));
        }
    }

    private static StreamGeometry BuildAreaSegment(double x0, double x1, double bottom, double y0, double y1)
    {
        StreamGeometry geometry = new();
        using StreamGeometryContext stream = geometry.Open();
        stream.BeginFigure(new Point(x0, bottom), true);
        stream.LineTo(new Point(x0, y0));
        stream.LineTo(new Point(x1, y1));
        stream.LineTo(new Point(x1, bottom));
        stream.EndFigure(true);
        return geometry;
    }

    private static StreamGeometry BuildLineSegment(double x0, double y0, double x1, double y1)
    {
        StreamGeometry geometry = new();
        using StreamGeometryContext stream = geometry.Open();
        stream.BeginFigure(new Point(x0, y0), false);
        stream.LineTo(new Point(x1, y1));
        return geometry;
    }

    private float SampleAt(int index) => _samples[(_start + index) % _samples.Length];

    private static double Y(float value, double height) => height - value / 100d * height;
}

internal readonly record struct UtilizationChartSegment(StreamGeometry Geometry, int LoadBucket);

internal static class UtilizationChartBrushes
{
    private static readonly Color FallbackLowColor = Color.Parse("#37B7E8");
    private static readonly Color HotColor = Color.Parse("#F97066");
    private static readonly Dictionary<BrushKey, IBrush> AreaBrushes = [];
    private static readonly Dictionary<BrushKey, IBrush> StrokeBrushes = [];
    private static readonly Dictionary<PenKey, Pen> StrokePens = [];

    public static int LoadBucket(double loadPercent)
    {
        return (int)Math.Round(Math.Clamp(loadPercent, 0, 100));
    }

    public static IBrush GetAreaBrush(int loadBucket, IBrush? lowBrush)
    {
        BrushKey key = new(ColorFromBrush(lowBrush, FallbackLowColor), Math.Clamp(loadBucket, 0, 100));
        if (!AreaBrushes.TryGetValue(key, out IBrush? brush))
        {
            Color color = InterpolateLoadColor(key.LoadBucket, key.LowColor);
            brush = new SolidColorBrush(WithAlpha(color, AreaAlpha(key.LoadBucket)));
            AreaBrushes[key] = brush;
        }

        return brush;
    }

    public static Pen GetStrokePen(int loadBucket, IBrush? lowBrush, double thickness)
    {
        PenKey key = new(ColorFromBrush(lowBrush, FallbackLowColor), Math.Clamp(loadBucket, 0, 100), thickness);
        if (!StrokePens.TryGetValue(key, out Pen? pen))
        {
            pen = new Pen(GetStrokeBrush(key.LowColor, key.LoadBucket), thickness);
            StrokePens[key] = pen;
        }

        return pen;
    }

    private static IBrush GetStrokeBrush(int loadBucket, IBrush? lowBrush)
    {
        return GetStrokeBrush(ColorFromBrush(lowBrush, FallbackLowColor), loadBucket);
    }

    private static IBrush GetStrokeBrush(Color lowColor, int loadBucket)
    {
        BrushKey key = new(lowColor, Math.Clamp(loadBucket, 0, 100));
        if (!StrokeBrushes.TryGetValue(key, out IBrush? brush))
        {
            brush = new SolidColorBrush(InterpolateLoadColor(key.LoadBucket, key.LowColor));
            StrokeBrushes[key] = brush;
        }

        return brush;
    }

    private static Color ColorFromBrush(IBrush? brush, Color fallback)
    {
        return brush is SolidColorBrush solid ? solid.Color : fallback;
    }

    private static Color InterpolateLoadColor(double loadPercent, Color low)
    {
        double amount = Math.Clamp((loadPercent - 58d) / 42d, 0, 1);
        return Color.FromRgb(
            Lerp(low.R, HotColor.R, amount),
            Lerp(low.G, HotColor.G, amount),
            Lerp(low.B, HotColor.B, amount));
    }

    private static byte AreaAlpha(double loadPercent)
    {
        double amount = Math.Clamp(loadPercent / 100d, 0, 1);
        return (byte)Math.Round(30 + amount * 80);
    }

    private static byte Lerp(byte start, byte end, double amount) => (byte)Math.Round(start + ((end - start) * amount));

    private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);

    private readonly record struct BrushKey(Color LowColor, int LoadBucket);

    private readonly record struct PenKey(Color LowColor, int LoadBucket, double Thickness);
}
