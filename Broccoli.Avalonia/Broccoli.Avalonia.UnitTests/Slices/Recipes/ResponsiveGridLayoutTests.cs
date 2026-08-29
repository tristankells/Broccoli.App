using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Broccoli.Avalonia.Slices.Recipes;
using Broccoli.Avalonia.Tests;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

[TestClass]
public class ResponsiveGridLayoutTests
{
    private const double ItemWidth = 220;
    private const double ColumnSpacing = 16;

    [TestMethod]
    public void ComputeMetrics_FullWidthExactFit_HasNoEdgeGap()
    {
        // 2 columns, min gap between, no leftover space.
        ResponsiveGridLayout.ColumnLayoutMetrics metrics = Compute(2, 456);

        Assert.AreEqual(2, metrics.ItemsPerLine);
        Assert.AreEqual(0, metrics.EdgeGap);
        Assert.AreEqual(ColumnSpacing, metrics.InterItemGap);
        Assert.AreEqual(456, metrics.ExtentWidth);
    }

    [TestMethod]
    public void ComputeMetrics_LeftoverWidth_IsSharedEvenlyBetweenAndAroundItems()
    {
        // 2 columns at 480px -> 24px leftover shared 3 ways (before, between, after).
        ResponsiveGridLayout.ColumnLayoutMetrics metrics = Compute(2, 480);

        Assert.AreEqual(2, metrics.ItemsPerLine);
        Assert.AreEqual(8, metrics.EdgeGap);
        Assert.AreEqual(24, metrics.InterItemGap);
    }

    [TestMethod]
    public void ComputeMetrics_GapGrowsAsWidthGrows_UntilNextColumnFits()
    {
        double narrowGap = Compute(2, 480).InterItemGap;
        double wideGap = Compute(2, 600).InterItemGap;

        Assert.IsTrue(wideGap > narrowGap, "The gap between cards should grow as the window widens.");
    }

    [TestMethod]
    public void ComputeMetrics_MovesToNextRowAtBreakpoints()
    {
        // 3 columns need 3*220 + 2*16 = 692px of width.
        ResponsiveGridLayout.ColumnLayoutMetrics justBelow = Compute(10, 691);
        ResponsiveGridLayout.ColumnLayoutMetrics justAt = Compute(10, 692);

        Assert.AreEqual(2, justBelow.ItemsPerLine);
        Assert.AreEqual(3, justAt.ItemsPerLine);
    }

    [TestMethod]
    public void ComputeMetrics_SameColumnCountRegardlessOfItemCount()
    {
        ResponsiveGridLayout.ColumnLayoutMetrics withSix = Compute(6, 1000);
        ResponsiveGridLayout.ColumnLayoutMetrics withNine = Compute(9, 1000);

        Assert.AreEqual(withSix.ItemsPerLine, withNine.ItemsPerLine);
    }

    [TestMethod]
    public void ComputeMetrics_PartialRow_FillsFromLeftAlignedWithFirstRow()
    {
        // 5 items, 3 columns -> row 0 has 3, row 1 is a partial row of 2.
        ResponsiveGridLayout.ColumnLayoutMetrics metrics = Compute(5, 900);

        Assert.AreEqual(3, metrics.ItemsPerLine);

        // Items 3 and 4 (the partial row) must sit exactly on the first two column slots of the grid.
        Assert.AreEqual(ItemX(0, metrics), ItemX(3, metrics));
        Assert.AreEqual(ItemX(1, metrics), ItemX(4, metrics));
    }

    [TestMethod]
    public void ComputeMetrics_PartialRow_XPositionsLeftAligned()
    {
        // Partial row of 2 must be at the left, not spread across the full width.
        ResponsiveGridLayout.ColumnLayoutMetrics metrics = Compute(5, 900);

        double secondColumnX = X(1, metrics);
        Assert.IsTrue(ItemX(3, metrics) < metrics.ExtentWidth / 2, "Partial row items should hug the left of the grid.");
        Assert.IsTrue(secondColumnX < metrics.ExtentWidth / 2);
    }

    [TestMethod]
    public void ComputeMetrics_MaxColumns_CapsColumnCount()
    {
        ResponsiveGridLayout.ColumnLayoutMetrics metrics = Compute(8, 2000, maxColumns: 4);

        Assert.AreEqual(4, metrics.ItemsPerLine);
    }

    [TestMethod]
    public void ComputeMetrics_FewerItemsThanColumns_KeepsWidthBasedColumns()
    {
        // 3 columns fit at 900px, even though only 2 items exist.
        ResponsiveGridLayout.ColumnLayoutMetrics metrics = Compute(2, 900);

        Assert.AreEqual(3, metrics.ItemsPerLine);
    }

    [TestMethod]
    public void ComputeMetrics_FewItems_AreLeftAligned()
    {
        // 2 items in a 3-column grid must hug the left grid columns, not spread/centre.
        ResponsiveGridLayout.ColumnLayoutMetrics metrics = Compute(2, 900);

        Assert.IsTrue(ItemX(0, metrics) < metrics.ExtentWidth / 2);
        Assert.IsTrue(ItemX(1, metrics) < metrics.ExtentWidth / 2);
    }

    [TestMethod]
    public void ComputeMetrics_SingleItem_IsLeftAligned()
    {
        ResponsiveGridLayout.ColumnLayoutMetrics metrics = Compute(1, 900);

        Assert.AreEqual(3, metrics.ItemsPerLine);
        Assert.IsTrue(ItemX(0, metrics) < metrics.ExtentWidth / 2);
    }

    [TestMethod]
    public void ComputeMetrics_NarrowerThanItemWidth_ForcesSingleColumn()
    {
        ResponsiveGridLayout.ColumnLayoutMetrics metrics = Compute(4, 200);

        Assert.AreEqual(1, metrics.ItemsPerLine);
    }

    private static ResponsiveGridLayout.ColumnLayoutMetrics Compute(int itemCount, double width, int maxColumns = int.MaxValue)
        => ResponsiveGridLayout.ComputeMetrics(itemCount, width, ItemWidth, ColumnSpacing, maxColumns);

    private static double ItemX(int itemIndex, ResponsiveGridLayout.ColumnLayoutMetrics metrics)
        => X(itemIndex % metrics.ItemsPerLine, metrics);

    private static double X(int slot, ResponsiveGridLayout.ColumnLayoutMetrics metrics)
        => metrics.EdgeGap + slot * (ItemWidth + metrics.InterItemGap);

    [TestMethod]
    public void ItemsRepeater_PartialRow_AlignsToFirstRowColumns()
    {
        HeadlessUiHost.Run(
            () =>
            {
                var repeater = new ItemsRepeater
                {
                    Layout = new ResponsiveGridLayout { ItemWidth = 220, ColumnSpacing = 16, RowSpacing = 16 },
                    ItemTemplate = new FuncDataTemplate<object>((item, _) => new TextBlock { Text = item?.ToString() }),
                    ItemsSource = Enumerable.Range(0, 5).Select(i => (object)$"Item {i}").ToList(),
                };
                return new Border { Width = 700, Child = repeater };
            },
            window =>
            {
                ItemsRepeater repeater = HeadlessUiHost.FindVisualChildren<ItemsRepeater>(window)[0];

                Assert.IsNotNull(repeater.TryGetElement(0), "All items should be realized for a non-virtualizing layout.");
                Assert.IsNotNull(repeater.TryGetElement(4));

                Rect row0Col0 = repeater.TryGetElement(0)!.Bounds;
                Rect row0Col1 = repeater.TryGetElement(1)!.Bounds;
                Rect row0Col2 = repeater.TryGetElement(2)!.Bounds;
                Rect row1Col0 = repeater.TryGetElement(3)!.Bounds;
                Rect row1Col1 = repeater.TryGetElement(4)!.Bounds;

                // First row spreads its three columns across the width.
                Assert.IsTrue(row0Col0.X < row0Col1.X && row0Col1.X < row0Col2.X);
                Assert.IsTrue(row0Col2.X > 400, "A full row should stretch toward the right edge.");

                // Partial second row is filled from the left, aligned to the first row's columns.
                Assert.AreEqual(row0Col0.X, row1Col0.X, 0.5);
                Assert.AreEqual(row0Col1.X, row1Col1.X, 0.5);
                Assert.IsTrue(row1Col1.X < row1Col0.X + 300, "Partial row should not spread across the full width.");

                // Partial row sits on a new row.
                Assert.IsTrue(row1Col0.Y > row0Col0.Y);
            });
    }
}
