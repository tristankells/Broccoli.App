using Avalonia;
using Avalonia.Layout;

namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>
/// A wrapping, responsive uniform grid.
/// <para>
/// All rows share the same number of columns, computed from the available width. The columns are
/// distributed to fill the full width edge-to-edge, so the gap between cards grows as the window
/// widens. Every row (including a partial final row) is filled from the left so cards always align
/// with the columns of the first row.
/// </para>
/// </summary>
public class ResponsiveGridLayout : NonVirtualizingLayout
{
    /// <summary>Defines the <see cref="ItemWidth"/> property.</summary>
    public static readonly StyledProperty<double> ItemWidthProperty =
        AvaloniaProperty.Register<ResponsiveGridLayout, double>(nameof(ItemWidth), 220);

    /// <summary>Defines the <see cref="ItemHeight"/> property.</summary>
    public static readonly StyledProperty<double> ItemHeightProperty =
        AvaloniaProperty.Register<ResponsiveGridLayout, double>(nameof(ItemHeight), double.NaN);

    /// <summary>Defines the <see cref="ColumnSpacing"/> property.</summary>
    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<ResponsiveGridLayout, double>(nameof(ColumnSpacing), 16);

    /// <summary>Defines the <see cref="RowSpacing"/> property.</summary>
    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<ResponsiveGridLayout, double>(nameof(RowSpacing), 16);

    /// <summary>Defines the <see cref="MaxColumns"/> property.</summary>
    public static readonly StyledProperty<int> MaxColumnsProperty =
        AvaloniaProperty.Register<ResponsiveGridLayout, int>(nameof(MaxColumns), int.MaxValue);

    /// <summary>Gets or sets the width of each card. The default is 220.</summary>
    public double ItemWidth
    {
        get => GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    /// <summary>Gets or sets the height of each card. When NaN (the default), the tallest card is used.</summary>
    public double ItemHeight
    {
        get => GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <summary>Gets or sets the minimum space between columns. The actual gap grows to fill the width.</summary>
    public double ColumnSpacing
    {
        get => GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    /// <summary>Gets or sets the space between rows.</summary>
    public double RowSpacing
    {
        get => GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    /// <summary>Gets or sets the maximum number of columns allowed.</summary>
    public int MaxColumns
    {
        get => GetValue(MaxColumnsProperty);
        set => SetValue(MaxColumnsProperty, value);
    }

    protected override Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
    {
        IReadOnlyList<Layoutable> children = context.Children;
        int count = children.Count;
        if (count == 0)
        {
            context.LayoutState = new LayoutParameters();
            return new Size();
        }

        double itemWidth = double.IsNaN(ItemWidth) || ItemWidth <= 0 ? 220 : ItemWidth;
        double columnSpacing = Math.Max(0, ColumnSpacing);
        double rowSpacing = Math.Max(0, RowSpacing);
        int maxColumns = Math.Max(1, MaxColumns);

        bool hasFixedHeight = !double.IsNaN(ItemHeight) && ItemHeight > 0;
        double rowHeight = hasFixedHeight ? ItemHeight : 0;

        Size childMeasureSize = new(itemWidth, hasFixedHeight ? ItemHeight : double.PositiveInfinity);
        foreach (Layoutable child in children)
        {
            child.Measure(childMeasureSize);
            if (!hasFixedHeight)
            {
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            }
        }

        ColumnLayoutMetrics metrics = ComputeMetrics(
            count,
            availableSize.Width,
            itemWidth,
            columnSpacing,
            maxColumns);

        int rows = (int)Math.Ceiling((double)count / metrics.ItemsPerLine);
        double extentHeight = rows * rowHeight + (rows - 1) * rowSpacing;

        context.LayoutState = new LayoutParameters
        {
            Metrics = metrics,
            ItemWidth = itemWidth,
            RowHeight = rowHeight,
        };

        return new Size(metrics.ExtentWidth, extentHeight);
    }

    protected override Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
    {
        if (context.LayoutState is not LayoutParameters parameters || parameters.Metrics.ItemsPerLine <= 0)
        {
            return finalSize;
        }

        IReadOnlyList<Layoutable> children = context.Children;
        double rowSpacing = Math.Max(0, RowSpacing);
        ColumnLayoutMetrics metrics = parameters.Metrics;

        for (int index = 0; index < children.Count; index++)
        {
            int row = index / metrics.ItemsPerLine;
            int slot = index % metrics.ItemsPerLine;
            double x = metrics.EdgeGap + slot * (parameters.ItemWidth + metrics.InterItemGap);
            double y = row * (parameters.RowHeight + rowSpacing);
            children[index].Arrange(new Rect(x, y, parameters.ItemWidth, parameters.RowHeight));
        }

        return finalSize;
    }

    /// <summary>
    /// Computes the uniform column grid for the given width, replicating "space even" justification:
    /// leftover width is shared equally between and around the items, so every full row stretches
    /// edge-to-edge while the gap never drops below <paramref name="columnSpacing"/>. Every row uses
    /// the same grid, so a partial final row is simply filled from the left of that grid.
    /// </summary>
    internal static ColumnLayoutMetrics ComputeMetrics(
        int itemCount,
        double width,
        double itemWidth,
        double columnSpacing,
        int maxColumns)
    {
        bool isWidthInfinite = double.IsInfinity(width);

        int itemsPerLine;
        if (isWidthInfinite)
        {
            itemsPerLine = itemCount;
        }
        else
        {
            itemsPerLine = (int)Math.Floor((width + columnSpacing) / (itemWidth + columnSpacing));
            itemsPerLine = Math.Clamp(itemsPerLine, 1, maxColumns);
        }

        double edgeGap;
        double interItemGap;
        double extentWidth;
        if (isWidthInfinite)
        {
            edgeGap = columnSpacing;
            interItemGap = columnSpacing;
            extentWidth = itemCount * itemWidth + (itemCount - 1) * columnSpacing;
        }
        else
        {
            double leftover = width - itemsPerLine * itemWidth - (itemsPerLine - 1) * columnSpacing;
            if (leftover < 0)
            {
                edgeGap = 0;
                interItemGap = columnSpacing;
            }
            else
            {
                double share = leftover / (itemsPerLine + 1);
                edgeGap = share;
                interItemGap = columnSpacing + share;
            }

            extentWidth = width;
        }

        return new ColumnLayoutMetrics(itemsPerLine, edgeGap, interItemGap, extentWidth);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemWidthProperty
            || change.Property == ItemHeightProperty
            || change.Property == ColumnSpacingProperty
            || change.Property == RowSpacingProperty
            || change.Property == MaxColumnsProperty)
        {
            InvalidateMeasure();
        }
    }

    internal readonly record struct ColumnLayoutMetrics(int ItemsPerLine, double EdgeGap, double InterItemGap, double ExtentWidth);

    private sealed class LayoutParameters
    {
        public ColumnLayoutMetrics Metrics;
        public double ItemWidth;
        public double RowHeight;
    }
}
