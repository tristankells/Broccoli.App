using Avalonia.Controls;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Recipes;
using Broccoli.Avalonia.Tests;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

[TestClass]
public class RecipeListRowViewModelTests
{
    [TestMethod]
    public void Cells_CarrySequentialColumnIndices_MatchingColumns()
    {
        RecipeListColumnDefinition[] columns =
        [
            new(RecipeListColumn.Name, 0),
            new(RecipeListColumn.Calories, 1),
            new(RecipeListColumn.DateAdded, 2),
        ];
        var card = new RecipeCardViewModel { Recipe = new Recipe { Name = "Banana Bread" }, Name = "Banana Bread" };

        var row = new RecipeListRowViewModel(card, columns);

        Assert.HasCount(3, row.Cells);
        Assert.AreEqual(0, row.Cells[0].Column);
        Assert.AreEqual("Banana Bread", row.Cells[0].Text);
        Assert.AreEqual(1, row.Cells[1].Column);
        Assert.AreEqual(2, row.Cells[2].Column);
    }

    [TestMethod]
    public void ColumnDefinitions_AreAllStarSized()
    {
        RecipeListColumnDefinition[] columns =
        [
            new(RecipeListColumn.Name, 0),
            new(RecipeListColumn.Calories, 1),
        ];
        var row = new RecipeListRowViewModel(
            new RecipeCardViewModel { Recipe = new Recipe { Name = "X" } },
            columns);

        Assert.HasCount(2, row.ColumnDefinitions);
        Assert.IsTrue(row.ColumnDefinitions[0].Width.IsStar);
        Assert.IsTrue(row.ColumnDefinitions[1].Width.IsStar);
    }

    [TestMethod]
    public void GridColumns_ForwardsBoundDefinitionsOntoGrid()
    {
        HeadlessUiHost.Run(
            () =>
            {
                var grid = new Grid();
                var definitions = new ColumnDefinitions();
                definitions.Add(new ColumnDefinition(GridLength.Parse("2*")));
                definitions.Add(new ColumnDefinition(GridLength.Parse("1*")));
                GridColumns.SetBoundColumnDefinitions(grid, definitions);
                return grid;
            },
            window =>
            {
                Grid grid = HeadlessUiHost.FindVisualChildren<Grid>(window)[0];

                Assert.HasCount(2, grid.ColumnDefinitions);
                Assert.IsTrue(grid.ColumnDefinitions[0].Width.IsStar);
                Assert.IsTrue(grid.ColumnDefinitions[1].Width.IsStar);
            });
    }
}
