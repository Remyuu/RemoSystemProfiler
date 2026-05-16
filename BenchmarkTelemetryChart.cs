using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using RemoSystemProfiler.Core;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;

namespace RemoSystemProfiler;

public sealed class BenchmarkTelemetryChart : Control
{
    public static readonly StyledProperty<IEnumerable?> SamplesProperty =
        AvaloniaProperty.Register<BenchmarkTelemetryChart, IEnumerable?>(nameof(Samples));

    private const double CpuVoltageScaleMax = 2d;

    private INotifyCollectionChanged? _observedCollection;
    private bool _isPointerInside;
    private Point _pointerPosition;

    private static readonly IBrush HoverLineBrush = new SolidColorBrush(Color.Parse("#9AA9B8"));
    private static readonly IBrush HoverDotBrush = new SolidColorBrush(Color.Parse("#F8FAFC"));
    private static readonly IBrush TooltipBackgroundBrush = new SolidColorBrush(Color.Parse("#E8101620"));
    private static readonly IBrush TooltipBorderBrush = new SolidColorBrush(Color.Parse("#8A9AA9B8"));
    private static readonly IBrush TooltipTextBrush = new SolidColorBrush(Color.Parse("#F8FAFC"));
    private static readonly IBrush HitSurfaceBrush = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));

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

        context.DrawRectangle(HitSurfaceBrush, null, bounds);

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
        double maxClock = Math.Max(1, samples.Max(sample => sample.CpuClockGhz ?? 0));
        DrawSeries(context, chart, samples, maxElapsed, sample => sample.CpuLoadPercent, 100, DashboardBrushes.Blue);
        DrawSeries(context, chart, samples, maxElapsed, sample => sample.CpuMaxTemperatureC, 100, DashboardBrushes.Red);
        DrawSeries(context, chart, samples, maxElapsed, sample => sample.CpuPackagePowerW, maxPower, DashboardBrushes.Amber);
        DrawSeries(context, chart, samples, maxElapsed, sample => sample.CpuClockGhz, maxClock, DashboardBrushes.Green);
        DrawSeries(context, chart, samples, maxElapsed, sample => sample.CpuVoltageV, CpuVoltageScaleMax, DashboardBrushes.Purple);
        DrawHoverReadout(context, bounds, chart, samples, maxElapsed);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _isPointerInside = true;
        _pointerPosition = e.GetPosition(this);
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _isPointerInside = false;
        InvalidateVisual();
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

    private static Point ChartPoint(
        Rect chart,
        BenchmarkTelemetrySample sample,
        double maxElapsed,
        Func<BenchmarkTelemetrySample, double?> selector,
        double maxValue)
    {
        double x = chart.X + Math.Clamp(sample.ElapsedSeconds / maxElapsed, 0, 1) * chart.Width;
        double rawValue = selector(sample) ?? 0;
        double normalized = Math.Clamp(rawValue / Math.Max(0.001, maxValue), 0, 1);
        double y = chart.Bottom - normalized * chart.Height;
        return new Point(x, y);
    }

    private void DrawHoverReadout(
        DrawingContext context,
        Rect bounds,
        Rect chart,
        IReadOnlyList<BenchmarkTelemetrySample> samples,
        double maxElapsed)
    {
        if (!_isPointerInside || samples.Count == 0)
        {
            return;
        }

        double targetElapsed = Math.Clamp((_pointerPosition.X - chart.X) / Math.Max(1, chart.Width), 0, 1) * maxElapsed;
        BenchmarkTelemetrySample sample = samples[0];
        double bestDistance = Math.Abs(sample.ElapsedSeconds - targetElapsed);
        for (int i = 1; i < samples.Count; i++)
        {
            double distance = Math.Abs(samples[i].ElapsedSeconds - targetElapsed);
            if (distance < bestDistance)
            {
                sample = samples[i];
                bestDistance = distance;
            }
        }

        double maxPower = Math.Max(1, samples.Max(item => item.CpuPackagePowerW ?? 0));
        Point loadPoint = ChartPoint(chart, sample, maxElapsed, item => item.CpuLoadPercent, 100);
        context.DrawLine(new Pen(HoverLineBrush, 1), new Point(loadPoint.X, chart.Y), new Point(loadPoint.X, chart.Bottom));
        context.DrawEllipse(HoverDotBrush, null, loadPoint, 3, 3);

        if (sample.CpuMaxTemperatureC is not null)
        {
            Point temperaturePoint = ChartPoint(chart, sample, maxElapsed, item => item.CpuMaxTemperatureC, 100);
            context.DrawEllipse(DashboardBrushes.Red, null, temperaturePoint, 2.5, 2.5);
        }

        if (sample.CpuPackagePowerW is not null)
        {
            Point powerPoint = ChartPoint(chart, sample, maxElapsed, item => item.CpuPackagePowerW, maxPower);
            context.DrawEllipse(DashboardBrushes.Amber, null, powerPoint, 2.5, 2.5);
        }

        if (sample.CpuClockGhz is not null)
        {
            double maxClock = Math.Max(1, samples.Max(item => item.CpuClockGhz ?? 0));
            Point clockPoint = ChartPoint(chart, sample, maxElapsed, item => item.CpuClockGhz, maxClock);
            context.DrawEllipse(DashboardBrushes.Green, null, clockPoint, 2.5, 2.5);
        }

        if (sample.CpuVoltageV is not null)
        {
            Point voltagePoint = ChartPoint(chart, sample, maxElapsed, item => item.CpuVoltageV, CpuVoltageScaleMax);
            context.DrawEllipse(DashboardBrushes.Purple, null, voltagePoint, 2.5, 2.5);
        }

        DrawTooltip(context, bounds, sample);
    }

    private void DrawTooltip(DrawingContext context, Rect bounds, BenchmarkTelemetrySample sample)
    {
        string[] lines =
        [
            $"Time  {sample.ElapsedSeconds:0.0}s",
            $"{Localization.Resource("Ui_Load")}  {FormatPercent(sample.CpuLoadPercent)}",
            $"{Localization.Resource("Ui_Temp")}  {FormatTemperature(sample.CpuMaxTemperatureC)}",
            $"{Localization.Resource("Ui_Power")}  {FormatPower(sample.CpuPackagePowerW)}",
            $"{Localization.Resource("Ui_Speed")}  {FormatClock(sample.CpuClockGhz)}",
            $"{Localization.SensorGroupTitle("voltage")}  {FormatVoltage(sample.CpuVoltageV)}"
        ];

        FormattedText[] texts = lines
            .Select(line => new FormattedText(
                line,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold),
                12,
                TooltipTextBrush))
            .ToArray();

        double width = Math.Ceiling(texts.Max(text => text.Width)) + 18;
        double height = Math.Ceiling(texts.Sum(text => text.Height)) + 14;
        double x = _pointerPosition.X + 12;
        double y = _pointerPosition.Y + 12;
        if (x + width > bounds.Right - 4)
        {
            x = _pointerPosition.X - width - 12;
        }

        if (y + height > bounds.Bottom - 4)
        {
            y = _pointerPosition.Y - height - 12;
        }

        x = Math.Clamp(x, bounds.X + 4, Math.Max(bounds.X + 4, bounds.Right - width - 4));
        y = Math.Clamp(y, bounds.Y + 4, Math.Max(bounds.Y + 4, bounds.Bottom - height - 4));
        Rect tooltip = new(x, y, width, height);
        context.DrawRectangle(TooltipBackgroundBrush, new Pen(TooltipBorderBrush, 1), tooltip, 5, 5);

        double textY = tooltip.Y + 7;
        for (int i = 0; i < texts.Length; i++)
        {
            context.DrawText(texts[i], new Point(tooltip.X + 9, textY));
            textY += texts[i].Height;
        }
    }

    private static string FormatPercent(double? value) => value is { } number && double.IsFinite(number)
        ? $"{number:0.#}%"
        : "--";

    private static string FormatTemperature(double? value) => value is { } number && double.IsFinite(number)
        ? $"{number:0.#} C"
        : "--";

    private static string FormatPower(double? value) => value is { } number && double.IsFinite(number)
        ? $"{number:0.#} W"
        : "--";

    private static string FormatClock(double? value) => value is { } number && double.IsFinite(number)
        ? $"{number:0.00} GHz"
        : "--";

    private static string FormatVoltage(double? value) => value is { } number && double.IsFinite(number)
        ? $"{number:0.###} V"
        : "--";
}
