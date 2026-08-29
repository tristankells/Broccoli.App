using Avalonia.Controls;
using Avalonia.Threading;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Recipes;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

[TestClass]
public class RecipeListPageViewTests
{
    /// <summary>
    /// Regression test: the staggered entrance hides cards with <c>Opacity="0"</c> until they are
    /// revealed. Once the entrance has played (the pending flag is cleared), returning to the tab
    /// recreates the view and every fresh card container must be revealed immediately — otherwise
    /// the whole grid is invisible after navigating between pages.
    /// </summary>
    [TestMethod]
    public void Cards_RevealedInstantly_WhenEntranceAlreadyPlayed()
    {
        var recipeService = new Mock<IRecipeService>();
        recipeService.Setup(s => s.GetAll()).Returns(new List<Recipe>
        {
            new() { Name = "Banana Bread" },
            new() { Name = "Chicken Curry" },
            new() { Name = "Vegetable Soup" },
        });
        RecipeListPageViewModel viewModel = new(recipeService.Object);
        viewModel.Reload();
        viewModel.EntranceAnimationPending = false;

        HeadlessUiHost.Run(
            () => new RecipeListPageView { DataContext = viewModel },
            window =>
            {
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();

                List<Border> cards = HeadlessUiHost.FindVisualChildren<Button>(window)
                    .Where(button => button.Classes.Contains("recipeCard"))
                    .Select(button => button.Content as Border)
                    .Where(border => border is not null)
                    .Cast<Border>()
                    .ToList();

                Assert.IsTrue(cards.Count > 0, "The recipe cards should be realized.");
                Assert.IsTrue(
                    cards.All(card => card.Opacity == 1),
                    "Cards shown after the entrance already played must be fully visible, not stuck at Opacity 0.");
            });
    }
}
