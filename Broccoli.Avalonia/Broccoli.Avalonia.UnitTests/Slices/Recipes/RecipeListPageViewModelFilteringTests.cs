using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Recipes;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

/// <summary>
/// Specifies the expected behavior of the Recipes search box (<see cref="RecipeListPageViewModel.SearchText"/>)
/// wired up in the "Add a search bar" feature. The underlying filtering logic is currently an
/// empty stub, so all of these are expected to fail until that filtering is implemented.
/// </summary>
[TestClass]
public class RecipeListPageViewModelFilteringTests
{
    private static RecipeListPageViewModel CreateViewModel(params Recipe[] recipes)
    {
        var recipeService = new Mock<IRecipeService>();
        recipeService.Setup(s => s.GetAll()).Returns(recipes);
        return new RecipeListPageViewModel(recipeService.Object);
    }

    private static Recipe MakeRecipe(string name) => new() { Name = name };

    [TestMethod]
    public void SearchText_EmptyByDefault_ShowsAllRecipes()
    {
        var viewModel = CreateViewModel(MakeRecipe("Banana Bread"), MakeRecipe("Chicken Curry"));

        Assert.AreEqual(2, viewModel.Recipes.Count);
    }

    [TestMethod]
    public void SearchText_MatchingSubstring_ShowsOnlyMatchingRecipes()
    {
        var viewModel = CreateViewModel(MakeRecipe("Banana Bread"), MakeRecipe("Chicken Curry"));

        viewModel.SearchText = "chicken";

        Assert.AreEqual(1, viewModel.Recipes.Count);
        Assert.AreEqual("Chicken Curry", viewModel.Recipes[0].Name);
    }

    [TestMethod]
    public void SearchText_IsCaseInsensitive()
    {
        var viewModel = CreateViewModel(MakeRecipe("Banana Bread"));

        viewModel.SearchText = "BANANA";

        Assert.AreEqual(1, viewModel.Recipes.Count);
    }

    [TestMethod]
    public void SearchText_NoMatches_ResultsInEmptyList()
    {
        var viewModel = CreateViewModel(MakeRecipe("Banana Bread"), MakeRecipe("Chicken Curry"));

        viewModel.SearchText = "pizza";

        Assert.AreEqual(0, viewModel.Recipes.Count);
    }

    [TestMethod]
    public void SearchText_ClearedAfterFiltering_RestoresFullList()
    {
        var viewModel = CreateViewModel(MakeRecipe("Banana Bread"), MakeRecipe("Chicken Curry"));

        viewModel.SearchText = "chicken";
        viewModel.SearchText = string.Empty;

        Assert.AreEqual(2, viewModel.Recipes.Count);
    }
}
