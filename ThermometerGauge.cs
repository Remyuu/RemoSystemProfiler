using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace RemoSystemProfiler;

public sealed class ThermometerGauge : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ThermometerGauge, double>(nameof(Value));

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<ThermometerGauge, IBrush?>(nameof(Fill), DashboardBrushes.Blue);

    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<ThermometerGauge, IBrush?>(nameof(TrackBrush));

    public static readonly StyledProperty<IBrush?> StrokeBrushProperty =
        AvaloniaProperty.Register<ThermometerGauge, IBrush?>(nameof(StrokeBrush));

    private Pen? _strokePen;
    private IBrush? _strokePenBrush;

    static ThermometerGauge()
    {
        AffectsRender<ThermometerGauge>(
            ValueProperty,
            FillProperty,
            TrackBrushProperty,
            StrokeBrushProperty);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public IBrush? StrokeBrush
    {
        get => GetValue(StrokeBrushProperty);
        set => SetValue(StrokeBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        double width = Bounds.Width;
        double height = Bounds.Height;
        double bulbSize = Math.Min(width, Math.Max(10, height * 0.24));
        double tubeWidth = Math.Min(Math.Max(7, width * 0.44), width - 2);
        double tubeX = (width - tubeWidth) / 2;
        double tubeTop = 1;
        double tubeBottom = Math.Max(tubeTop, height - bulbSize * 0.6);
        double tubeHeight = tubeBottom - tubeTop;
        double radius = tubeWidth / 2;

        IBrush track = TrackBrush ?? DashboardBrushes.CorePillBackground;
        IBrush fill = Fill ?? DashboardBrushes.Blue;
        Pen stroke = GetStrokePen();

        Rect tube = new(tubeX, tubeTop, tubeWidth, tubeHeight);
        Point bulbCenter = new(width / 2, height - bulbSize / 2);
        context.DrawRectangle(track, stroke, tube, radius);
        context.DrawEllipse(track, stroke, bulbCenter, bulbSize / 2, bulbSize / 2);

        double normalized = Math.Clamp(Value, 0, 100) / 100d;
        double fillHeight = Math.Max(radius, tubeHeight * normalized);
        Rect fillTube = new(tubeX + 2, tubeBottom - fillHeight, Math.Max(1, tubeWidth - 4), fillHeight);
        context.DrawRectangle(fill, null, fillTube, Math.Max(1, (tubeWidth - 4) / 2));
        context.DrawEllipse(fill, null, bulbCenter, Math.Max(1, bulbSize / 2 - 3), Math.Max(1, bulbSize / 2 - 3));
    }

    private Pen GetStrokePen()
    {
        IBrush brush = StrokeBrush ?? DashboardBrushes.CoreCellBorder;
        if (_strokePen is null || !ReferenceEquals(_strokePenBrush, brush))
        {
            _strokePenBrush = brush;
            _strokePen = new Pen(brush, 1);
        }

        return _strokePen;
    }
}
