using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace RemoSystemProfiler;

public sealed class CoreTopologyPanel : Panel
{
    private const double Gap = 4;
    private const double FallbackCellWidth = 86;
    private const double FallbackCellHeight = 58;

    protected override Size MeasureOverride(Size availableSize)
    {
        int count = Children.Count;
        if (count == 0)
        {
            return new Size(0, 0);
        }

        (int columns, int rows) = LayoutShape(count);
        double cellWidth = CellSize(availableSize.Width, columns, FallbackCellWidth);
        double cellHeight = CellSize(availableSize.Height, rows, FallbackCellHeight);
        Size cellSize = new(cellWidth, cellHeight);

        foreach (UIElement child in Children)
        {
            child.Measure(cellSize);
        }

        double desiredWidth = double.IsInfinity(availableSize.Width)
            ? columns * FallbackCellWidth + Math.Max(0, columns - 1) * Gap
            : availableSize.Width;
        double desiredHeight = double.IsInfinity(availableSize.Height)
            ? rows * FallbackCellHeight + Math.Max(0, rows - 1) * Gap
            : availableSize.Height;

        return new Size(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int count = Children.Count;
        if (count == 0)
        {
            return finalSize;
        }

        (int columns, int rows) = LayoutShape(count);
        double cellWidth = CellSize(finalSize.Width, columns, FallbackCellWidth);
        double cellHeight = CellSize(finalSize.Height, rows, FallbackCellHeight);

        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int column = i % columns;
            double x = column * (cellWidth + Gap);
            double y = row * (cellHeight + Gap);
            Children[i].Arrange(new Rect(x, y, cellWidth, cellHeight));
        }

        return finalSize;
    }

    private static (int Columns, int Rows) LayoutShape(int count)
    {
        int columns = count switch
        {
            <= 0 => 1,
            <= 8 => count,
            <= 32 => 8,
            <= 64 => 16,
            _ => 24
        };
        int rows = (int)Math.Ceiling(count / (double)columns);
        return (columns, Math.Max(1, rows));
    }

    private static double CellSize(double available, int count, double fallback)
    {
        if (count <= 0)
        {
            return fallback;
        }

        if (double.IsInfinity(available))
        {
            return fallback;
        }

        return Math.Max(30, (available - Math.Max(0, count - 1) * Gap) / count);
    }
}
