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

        DrawAreaSegments(context, Bounds.Size);
        DrawLineSegments(context, Bounds.Size);
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

    private void DrawAreaSegments(DrawingContext context, Size size)
    {
        if (_count == 1)
        {
            float sample = SampleAt(0);
            StreamGeometry geometry = new();
            using StreamGeometryContext stream = geometry.Open();
            stream.BeginFigure(new Point(0, size.Height), true);
            stream.LineTo(new Point(0, Y(sample, size.Height)));
            stream.LineTo(new Point(size.Width, Y(sample, size.Height)));
            stream.LineTo(new Point(size.Width, size.Height));
            stream.EndFigure(true);
            context.DrawGeometry(UtilizationChartBrushes.CreateAreaBrush(sample, Stroke), null, geometry);
            return;
        }

        double step = size.Width / (_count - 1);
        for (int i = 1; i < _count; i++)
        {
            float previous = SampleAt(i - 1);
            float current = SampleAt(i);
            double x0 = (i - 1) * step;
            double x1 = i * step;
            StreamGeometry geometry = new();
            using StreamGeometryContext stream = geometry.Open();
            stream.BeginFigure(new Point(x0, size.Height), true);
            stream.LineTo(new Point(x0, Y(previous, size.Height)));
            stream.LineTo(new Point(x1, Y(current, size.Height)));
            stream.LineTo(new Point(x1, size.Height));
            stream.EndFigure(true);
            context.DrawGeometry(UtilizationChartBrushes.CreateAreaBrush((previous + current) * 0.5, Stroke), null, geometry);
        }
    }

    private void DrawLineSegments(DrawingContext context, Size size)
    {
        if (_count == 1)
        {
            float sample = SampleAt(0);
            double y = Y(sample, size.Height);
            StreamGeometry geometry = new();
            using StreamGeometryContext stream = geometry.Open();
            stream.BeginFigure(new Point(0, y), false);
            stream.LineTo(new Point(size.Width, y));
            context.DrawGeometry(null, new Pen(UtilizationChartBrushes.CreateStrokeBrush(sample, Stroke), StrokeThickness), geometry);
            return;
        }

        double step = size.Width / (_count - 1);
        for (int i = 1; i < _count; i++)
        {
            float previous = SampleAt(i - 1);
            float current = SampleAt(i);
            StreamGeometry geometry = new();
            using StreamGeometryContext stream = geometry.Open();
            stream.BeginFigure(new Point((i - 1) * step, Y(previous, size.Height)), false);
            stream.LineTo(new Point(i * step, Y(current, size.Height)));
            context.DrawGeometry(
                null,
                new Pen(UtilizationChartBrushes.CreateStrokeBrush((previous + current) * 0.5, Stroke), StrokeThickness),
                geometry);
        }
    }

    private float SampleAt(int index) => _samples[(_start + index) % _samples.Length];

    private static double Y(float value, double height) => height - value / 100d * height;
}

internal static class UtilizationChartBrushes
{
    private static readonly Color FallbackLowColor = Color.Parse("#37B7E8");
    private static readonly Color HotColor = Color.Parse("#F97066");

    public static IBrush CreateAreaBrush(double loadPercent, IBrush? lowBrush)
    {
        Color color = InterpolateLoadColor(loadPercent, ColorFromBrush(lowBrush, FallbackLowColor));
        return new SolidColorBrush(WithAlpha(color, AreaAlpha(loadPercent)));
    }

    public static IBrush CreateStrokeBrush(double loadPercent, IBrush? lowBrush)
    {
        return new SolidColorBrush(InterpolateLoadColor(loadPercent, ColorFromBrush(lowBrush, FallbackLowColor)));
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
}
