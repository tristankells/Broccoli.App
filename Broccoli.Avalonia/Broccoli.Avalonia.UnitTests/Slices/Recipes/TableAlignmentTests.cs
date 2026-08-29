using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

/// <summary>
/// Ensures the list-view table header and row cells stay aligned: each column's header text edge
/// must line up with the matching row value's edge (left edge for left-aligned columns, right edge
/// for right-aligned numeric columns).
/// </summary>
[TestClass]
public class TableAlignmentTests
{
    [TestMethod]
    public void HeaderText_AlignsWithRowText_ForEveryColumn()
    {
        RecipeListPageViewModel? viewModel = null;
        HeadlessUiHost.Run(
            () =>
            {
                viewModel = CreateViewModel();
                return new RecipeListPageView { DataContext = viewModel };
            },
            window =>
            {
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();

                List<TextBlock> headers = HeadlessUiHost.FindVisualChildren<TextBlock>(window)
                    .Where(t => t.DataContext is RecipeListColumnDefinition)
                    .ToList();
                List<TextBlock> cells = HeadlessUiHost.FindVisualChildren<TextBlock>(window)
                    .Where(t => t.DataContext is RecipeListCell)
                    .ToList();

                List<string> lines = [];
                for (int i = 0; i < headers.Count; i++)
                {
                    RecipeListColumnDefinition definition = (RecipeListColumnDefinition)headers[i].DataContext!;
                    TextBlock cell = cells.First(c => ((RecipeListCell)c.DataContext!).Column == i);

                    double headerEdge = TextEdge(headers[i], definition.Alignment, window);
                    double cellEdge = TextEdge(cell, definition.Alignment, window);
                    lines.Add(
                        $"col[{i}] {definition.Title} align={definition.Alignment}: " +
                        $"headerEdge={headerEdge.ToString("0.0", CultureInfo.InvariantCulture)} cellEdge={cellEdge.ToString("0.0", CultureInfo.InvariantCulture)}");

                    Assert.AreEqual(headerEdge, cellEdge, 1.0,
                        $"Column {i} header text isn't aligned with its row values.{Environment.NewLine}{string.Join(Environment.NewLine, lines)}");
                }
            });
    }

    private static double TextEdge(TextBlock text, TextAlignment alignment, Visual relativeTo)
    {
        double x = text.TranslatePoint(new Point(0, 0), relativeTo)?.X ?? 0;
        return alignment == TextAlignment.Left ? x : x + text.Bounds.Width;
    }

    private static RecipeListPageViewModel CreateViewModel()
    {
        var recipeService = new Mock<IRecipeService>();
        recipeService.Setup(s => s.GetAll()).Returns(new List<Recipe>
        {
            new() { Name = "Banana Bread", PrepTimeMinutes = 10, CookTimeMinutes = 20, Servings = 4, Source = "AllRecipes" },
            new() { Name = "Chicken Curry", PrepTimeMinutes = 15, CookTimeMinutes = 40, Servings = 6, Source = "BBC Food" },
        });
        var settings = new MacroTargetSettings { ShowRecipesAsList = true };
        var macro = new Mock<IMacroTargetService>();
        macro.Setup(s => s.GetSettings()).Returns(settings);
        RecipeListPageViewModel viewModel = new(recipeService.Object, null, null, macro.Object);
        viewModel.Reload();
        return viewModel;
    }
}
