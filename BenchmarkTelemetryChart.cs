using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using RemoSystemProfiler.Core;
using System.Collections;
using System.Collections.Specialized;

namespace RemoSystemProfiler;

public sealed class BenchmarkTelemetryChart : Control
{
    public static readonly StyledProperty<IEnumerable?> SamplesProperty =
        AvaloniaProperty.Register<BenchmarkTelemetryChart, IEnumerable?>(nameof(Samples));

    private INotifyCollectionChanged? _observedCollection;

    static BenchmarkTelemetryChart()
    {
        AffectsRender<BenchmarkTelemetryChart>(SamplesProperty);
        SamplesProperty.Changed.AddClassHandler<BenchmarkTelemetryChart>((control, args) => control.AttachCollection(args.NewValue as IEnumerable));
    }

    public IEnumerable? Samples
    {
        get => GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        Rect bounds = new(Bounds.Size);
        if (bounds.Width <= 2 || bounds.Height <= 2)
        {
            return;
        }

        IReadOnlyList<BenchmarkTelemetrySample> samples = MaterializeSamples(Samples);
        if (samples.Count < 2)
        {
            DrawEmptyFrame(context, bounds);
            return;
        }

        Rect chart = bounds.Deflate(new Thickness(5, 5));
        DrawGrid(context, chart);

        double maxElapsed = Math.Max(0.001, samples[^1].ElapsedSeconds);
        double maxPower = Math.Max(1, samples.Max(sample => sample.CpuPackagePowerW ?? 0));
        DrawSeries(context, chart, samples, maxElapsed, sample => sample.CpuLoadPercent, 100, DashboardBrushes.Blue);
        DrawSeries(context, chart, samples, maxElapsed, sample => sample.CpuMaxTemperatureC, 100, DashboardBrushes.Red);
        DrawSeries(context, chart, samples, maxElapsed, sample => sample.CpuPackagePowerW, maxPower, DashboardBrushes.Amber);
    }

    private void AttachCollection(IEnumerable? samples)
    {
        if (_observedCollection is not null)
        {
            _observedCollection.CollectionChanged -= OnSamplesChanged;
        }

        _observedCollection = samples as INotifyCollectionChanged;
        if (_observedCollection is not null)
        {
            _observedCollection.CollectionChanged += OnSamplesChanged;
        }

        InvalidateVisual();
    }

    private void OnSamplesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private static IReadOnlyList<BenchmarkTelemetrySample> MaterializeSamples(IEnumerable? samples)
    {
        if (samples is null)
        {
            return [];
        }

        List<BenchmarkTelemetrySample> materialized = [];
        foreach (object? item in samples)
        {
            if (item is BenchmarkTelemetrySample sample && double.IsFinite(sample.ElapsedSeconds) && sample.ElapsedSeconds >= 0)
            {
                materialized.Add(sample);
            }
        }

        materialized.Sort((left, right) => left.ElapsedSeconds.CompareTo(right.ElapsedSeconds));
        return materialized;
    }

    private static void DrawEmptyFrame(DrawingContext context, Rect bounds)
    {
        context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#334151")), 1), bounds.Deflate(0.5));
    }

    private static void DrawGrid(DrawingContext context, Rect chart)
    {
        Pen gridPen = new(new SolidColorBrush(Color.Parse("#304151")), 1);
        for (int i = 0; i <= 2; i++)
        {
            double y = chart.Y + chart.Height * i / 2d;
            context.DrawLine(gridPen, new Point(chart.X, y), new Point(chart.Right, y));
        }

        for (int i = 0; i <= 4; i++)
        {
            double x = chart.X + chart.Width * i / 4d;
            context.DrawLine(gridPen, new Point(x, chart.Y), new Point(x, chart.Bottom));
        }
    }

    private static void DrawSeries(
        DrawingContext context,
        Rect chart,
        IReadOnlyList<BenchmarkTelemetrySample> samples,
        double maxElapsed,
        Func<BenchmarkTelemetrySample, double?> selector,
        double maxValue,
        IBrush brush)
    {
        StreamGeometry geometry = new();
        bool hasFigure = false;
        bool hasAnyPoint = false;
        using (StreamGeometryContext stream = geometry.Open())
        {
            for (int i = 0; i < samples.Count; i++)
            {
                BenchmarkTelemetrySample sample = samples[i];
                double? rawValue = selector(sample);
                if (rawValue is null || !double.IsFinite(rawValue.Value))
                {
                    hasFigure = false;
                    continue;
                }

                double x = chart.X + Math.Clamp(sample.ElapsedSeconds / maxElapsed, 0, 1) * chart.Width;
                double normalized = Math.Clamp(rawValue.Value / Math.Max(0.001, maxValue), 0, 1);
                double y = chart.Bottom - normalized * chart.Height;
                Point point = new(x, y);
                if (!hasFigure)
                {
                    stream.BeginFigure(point, false);
                    hasFigure = true;
                }
                else
                {
                    stream.LineTo(point);
                }

                hasAnyPoint = true;
            }
        }

        if (hasAnyPoint)
        {
            context.DrawGeometry(null, new Pen(brush, 1.8), geometry);
        }
    }
}
