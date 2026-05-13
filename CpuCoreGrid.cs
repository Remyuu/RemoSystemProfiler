using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;

namespace RemoSystemProfiler;

public sealed class CpuCoreGrid : Control
{
    public static readonly StyledProperty<IEnumerable?> CoresProperty =
        AvaloniaProperty.Register<CpuCoreGrid, IEnumerable?>(nameof(Cores));

    public static readonly StyledProperty<IBrush?> CellBackgroundProperty =
        AvaloniaProperty.Register<CpuCoreGrid, IBrush?>(nameof(CellBackground));

    public static readonly StyledProperty<IBrush?> CellBorderBrushProperty =
        AvaloniaProperty.Register<CpuCoreGrid, IBrush?>(nameof(CellBorderBrush));

    public static readonly StyledProperty<IBrush?> PillBackgroundProperty =
        AvaloniaProperty.Register<CpuCoreGrid, IBrush?>(nameof(PillBackground));

    public static readonly StyledProperty<IBrush?> TextBrushProperty =
        AvaloniaProperty.Register<CpuCoreGrid, IBrush?>(nameof(TextBrush));

    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<CpuCoreGrid, double>(nameof(Gap), 4d);

    private const double FallbackCellWidth = 86;
    private const double FallbackCellHeight = 58;
    private const double PillInsetX = 6;
    private const double PillInsetY = 4;
    private const double SparklineInset = 2;

    private readonly Dictionary<int, CoreState> _statesByKey = new();
    private readonly List<CoreItemViewModel> _cores = [];

    private INotifyCollectionChanged? _collectionChangedSource;
    private Rect[] _cellRects = [];
    private StreamGeometry? _cellGridGeometry;
    private Size _layoutSize;
    private int _layoutCount = -1;
    private double _layoutGap = -1;
    private bool _layoutDirty = true;

    private Pen? _cellBorderPen;
    private IBrush? _cellBorderPenBrush;

    static CpuCoreGrid()
    {
        AffectsRender<CpuCoreGrid>(
            CellBackgroundProperty,
            CellBorderBrushProperty,
            PillBackgroundProperty,
            TextBrushProperty,
            GapProperty);
        AffectsMeasure<CpuCoreGrid>(CoresProperty, GapProperty);
        CoresProperty.Changed.AddClassHandler<CpuCoreGrid>((control, _) => control.AttachCores());
        GapProperty.Changed.AddClassHandler<CpuCoreGrid>((control, _) => control.InvalidateLayoutCache());
    }

    public CpuCoreGrid()
    {
        DetachedFromVisualTree += (_, _) => DetachCores();
    }

    public IEnumerable? Cores
    {
        get => GetValue(CoresProperty);
        set => SetValue(CoresProperty, value);
    }

    public IBrush? CellBackground
    {
        get => GetValue(CellBackgroundProperty);
        set => SetValue(CellBackgroundProperty, value);
    }

    public IBrush? CellBorderBrush
    {
        get => GetValue(CellBorderBrushProperty);
        set => SetValue(CellBorderBrushProperty, value);
    }

    public IBrush? PillBackground
    {
        get => GetValue(PillBackgroundProperty);
        set => SetValue(PillBackgroundProperty, value);
    }

    public IBrush? TextBrush
    {
        get => GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    public double Gap
    {
        get => GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        int count = _cores.Count;
        if (count == 0)
        {
            return new Size(0, 0);
        }

        (int columns, int rows) = LayoutShape(count);
        double gap = Gap;
        double desiredWidth = double.IsInfinity(availableSize.Width)
            ? columns * FallbackCellWidth + Math.Max(0, columns - 1) * gap
            : availableSize.Width;
        double desiredHeight = double.IsInfinity(availableSize.Height)
            ? rows * FallbackCellHeight + Math.Max(0, rows - 1) * gap
            : availableSize.Height;
        return new Size(desiredWidth, desiredHeight);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        int count = _cores.Count;
        if (count == 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        EnsureLayout(Bounds.Size);
        IBrush cellBackground = CellBackground ?? DashboardBrushes.CoreCellBackground;
        IBrush pillBackground = PillBackground ?? DashboardBrushes.CorePillBackground;
        IBrush textBrush = TextBrush ?? DashboardBrushes.Blue;
        Pen cellBorder = GetCellBorderPen();
        if (_cellGridGeometry is not null)
        {
            context.DrawGeometry(cellBackground, cellBorder, _cellGridGeometry);
        }

        for (int i = 0; i < count; i++)
        {
            CoreItemViewModel core = _cores[i];
            Rect cell = _cellRects[i];
            DrawSparkline(context, core, cell);
            DrawLoadPill(context, core, cell, pillBackground, textBrush);
        }
    }

    private void DrawSparkline(DrawingContext context, CoreItemViewModel core, Rect cell)
    {
        if (!_statesByKey.TryGetValue(core.Key, out CoreState? state) || state.Count == 0)
        {
            return;
        }

        Rect chart = cell.Deflate(SparklineInset);
        if (chart.Width <= 0 || chart.Height <= 0)
        {
            return;
        }

        state.DrawAreaSegments(context, chart, DashboardBrushes.Blue);
        state.DrawLineSegments(context, chart, DashboardBrushes.Blue);
    }

    private void DrawLoadPill(DrawingContext context, CoreItemViewModel core, Rect cell, IBrush pillBackground, IBrush fallbackTextBrush)
    {
        IBrush loadBrush = core.LoadBrush ?? fallbackTextBrush;
        CoreState state = _statesByKey[core.Key];
        FormattedText text = state.GetLabelText(core.LoadText, loadBrush);

        Rect pill = new(
            cell.X + PillInsetX,
            cell.Y + PillInsetY,
            Math.Ceiling(text.Width) + 10,
            Math.Ceiling(text.Height) + 4);
        context.DrawRectangle(pillBackground, GetCellBorderPen(), pill, 2);
        context.DrawText(text, new Point(pill.X + 5, pill.Y + 2));
    }

    private void AttachCores()
    {
        DetachCores();
        if (Cores is INotifyCollectionChanged collectionChanged)
        {
            _collectionChangedSource = collectionChanged;
            collectionChanged.CollectionChanged += Cores_CollectionChanged;
        }

        SyncCoreList();
    }

    private void DetachCores()
    {
        if (_collectionChangedSource is not null)
        {
            _collectionChangedSource.CollectionChanged -= Cores_CollectionChanged;
            _collectionChangedSource = null;
        }

        foreach (CoreItemViewModel core in _cores)
        {
            core.PropertyChanged -= Core_PropertyChanged;
        }

        _cores.Clear();
    }

    private void Cores_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncCoreList();
    }

    private void SyncCoreList()
    {
        foreach (CoreItemViewModel core in _cores)
        {
            core.PropertyChanged -= Core_PropertyChanged;
        }

        _cores.Clear();
        HashSet<int> activeKeys = [];
        if (Cores is not null)
        {
            foreach (object? item in Cores)
            {
                if (item is not CoreItemViewModel core)
                {
                    continue;
                }

                _cores.Add(core);
                activeKeys.Add(core.Key);
                core.PropertyChanged += Core_PropertyChanged;
                if (!_statesByKey.TryGetValue(core.Key, out CoreState? state))
                {
                    state = new CoreState();
                    _statesByKey[core.Key] = state;
                }

                if (state.Count == 0)
                {
                    state.AddSample(core.LoadPercent);
                }
            }
        }

        foreach (int staleKey in _statesByKey.Keys.Where(key => !activeKeys.Contains(key)).ToArray())
        {
            _statesByKey.Remove(staleKey);
        }

        InvalidateLayoutCache();
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void Core_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not CoreItemViewModel core)
        {
            return;
        }

        if (e.PropertyName is nameof(CoreItemViewModel.SampleVersion))
        {
            if (!_statesByKey.TryGetValue(core.Key, out CoreState? state))
            {
                state = new CoreState();
                _statesByKey[core.Key] = state;
            }

            state.AddSample(core.LoadPercent);
            InvalidateVisual();
            return;
        }

        if (e.PropertyName is nameof(CoreItemViewModel.LoadPercent)
            or nameof(CoreItemViewModel.LoadText)
            or nameof(CoreItemViewModel.LoadBrush))
        {
            InvalidateVisual();
        }
    }

    private void EnsureLayout(Size size)
    {
        double gap = Gap;
        int count = _cores.Count;
        if (!_layoutDirty && _layoutSize == size && _layoutCount == count && Math.Abs(_layoutGap - gap) < double.Epsilon)
        {
            return;
        }

        _layoutDirty = false;
        _layoutSize = size;
        _layoutCount = count;
        _layoutGap = gap;
        if (_cellRects.Length < count)
        {
            _cellRects = new Rect[count];
        }

        (int columns, int rows) = LayoutShape(count);
        double cellWidth = CellSize(size.Width, columns, FallbackCellWidth, gap);
        double cellHeight = CellSize(size.Height, rows, FallbackCellHeight, gap);

        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int column = i % columns;
            double x = column * (cellWidth + gap);
            double y = row * (cellHeight + gap);
            _cellRects[i] = new Rect(x, y, cellWidth, cellHeight);
        }

        foreach (CoreState state in _statesByKey.Values)
        {
            state.InvalidateGeometry();
        }
        _cellGridGeometry = BuildCellGridGeometry(count);
    }

    private void InvalidateLayoutCache()
    {
        _layoutDirty = true;
        foreach (CoreState state in _statesByKey.Values)
        {
            state.InvalidateGeometry();
        }
    }

    private static (int Columns, int Rows) LayoutShape(int count)
    {
        if (count <= 0)
        {
            return (1, 1);
        }

        int root = (int)Math.Floor(Math.Sqrt(count));
        for (int rows = root; rows >= 1; rows--)
        {
            if (count % rows == 0)
            {
                return (count / rows, rows);
            }
        }

        int columns = (int)Math.Ceiling(Math.Sqrt(count));
        return (columns, (int)Math.Ceiling(count / (double)columns));
    }

    private static double CellSize(double available, int count, double fallback, double gap)
    {
        if (count <= 0 || double.IsInfinity(available))
        {
            return fallback;
        }

        return Math.Max(30, (available - Math.Max(0, count - 1) * gap) / count);
    }

    private Pen GetCellBorderPen()
    {
        IBrush brush = CellBorderBrush ?? DashboardBrushes.CoreCellBorder;
        if (_cellBorderPen is null || !ReferenceEquals(_cellBorderPenBrush, brush))
        {
            _cellBorderPenBrush = brush;
            _cellBorderPen = new Pen(brush, 1);
        }

        return _cellBorderPen;
    }

    private StreamGeometry BuildCellGridGeometry(int count)
    {
        StreamGeometry geometry = new();
        using StreamGeometryContext stream = geometry.Open();
        for (int i = 0; i < count; i++)
        {
            Rect cell = _cellRects[i];
            stream.BeginFigure(cell.TopLeft, true);
            stream.LineTo(cell.TopRight);
            stream.LineTo(cell.BottomRight);
            stream.LineTo(cell.BottomLeft);
            stream.EndFigure(true);
        }

        return geometry;
    }

    private sealed class CoreState
    {
        private float[] _samples = new float[ChartHistorySettings.MaxSamples];
        private int _start;
        private string? _labelTextValue;
        private IBrush? _labelTextBrush;
        private FormattedText? _labelText;

        public int Count { get; private set; }

        public void AddSample(double value)
        {
            EnsureCapacity(ChartHistorySettings.MaxSamples);
            float sample = (float)Math.Clamp(value, 0, 100);
            if (Count < _samples.Length)
            {
                _samples[(_start + Count) % _samples.Length] = sample;
                Count++;
            }
            else
            {
                _samples[_start] = sample;
                _start = (_start + 1) % _samples.Length;
            }

        }

        public void InvalidateGeometry()
        {
        }

        public void DrawAreaSegments(DrawingContext context, Rect bounds, IBrush lowBrush)
        {
            if (Count <= 1)
            {
                float sample = SampleAt(0);
                StreamGeometry geometry = BuildAreaSegment(bounds.Left, bounds.Right, bounds.Bottom, Y(sample, bounds), Y(sample, bounds));
                context.DrawGeometry(UtilizationChartBrushes.CreateAreaBrush(sample, lowBrush), null, geometry);
                return;
            }

            double step = bounds.Width / (Count - 1);
            for (int i = 1; i < Count; i++)
            {
                float previous = SampleAt(i - 1);
                float current = SampleAt(i);
                double x0 = bounds.X + (i - 1) * step;
                double x1 = bounds.X + i * step;
                StreamGeometry geometry = BuildAreaSegment(x0, x1, bounds.Bottom, Y(previous, bounds), Y(current, bounds));
                context.DrawGeometry(UtilizationChartBrushes.CreateAreaBrush((previous + current) * 0.5, lowBrush), null, geometry);
            }
        }

        public void DrawLineSegments(DrawingContext context, Rect bounds, IBrush lowBrush)
        {
            if (Count <= 1)
            {
                float sample = SampleAt(0);
                StreamGeometry geometry = new();
                using StreamGeometryContext stream = geometry.Open();
                double y = Y(sample, bounds);
                stream.BeginFigure(new Point(bounds.Left, y), false);
                stream.LineTo(new Point(bounds.Right, y));
                context.DrawGeometry(null, new Pen(UtilizationChartBrushes.CreateStrokeBrush(sample, lowBrush), 2.2), geometry);
                return;
            }

            double step = bounds.Width / (Count - 1);
            for (int i = 1; i < Count; i++)
            {
                float previous = SampleAt(i - 1);
                float current = SampleAt(i);
                StreamGeometry geometry = new();
                using StreamGeometryContext stream = geometry.Open();
                stream.BeginFigure(new Point(bounds.X + (i - 1) * step, Y(previous, bounds)), false);
                stream.LineTo(new Point(bounds.X + i * step, Y(current, bounds)));
                context.DrawGeometry(null, new Pen(UtilizationChartBrushes.CreateStrokeBrush((previous + current) * 0.5, lowBrush), 2.2), geometry);
            }
        }

        public FormattedText GetLabelText(string label, IBrush brush)
        {
            if (_labelText is null || _labelTextValue != label || !ReferenceEquals(_labelTextBrush, brush))
            {
                _labelTextValue = label;
                _labelTextBrush = brush;
                _labelText = new FormattedText(
                    label,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold),
                    14,
                    brush);
            }

            return _labelText;
        }

        private void EnsureCapacity(int capacity)
        {
            capacity = Math.Max(2, capacity);
            if (_samples.Length == capacity)
            {
                return;
            }

            float[] resized = new float[capacity];
            int copyCount = Math.Min(Count, capacity);
            int skip = Math.Max(0, Count - copyCount);
            for (int i = 0; i < copyCount; i++)
            {
                resized[i] = SampleAt(skip + i);
            }

            _samples = resized;
            _start = 0;
            Count = copyCount;
        }

        private float SampleAt(int index) => _samples[(_start + index) % _samples.Length];

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

        private static double Y(float value, Rect bounds) => bounds.Bottom - value / 100d * bounds.Height;
    }
}
