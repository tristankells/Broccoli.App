using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Recipes;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

/// <summary>
/// Specifies the expected behavior of the Recipes search box (<see cref="RecipeListPageViewModel.SearchText"/>) wired       
/// up in the "Add a search bar" feature.
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
        RecipeListPageViewModel viewModel = CreateViewModel(MakeRecipe("Banana Bread"), MakeRecipe("Chicken Curry"));

        Assert.HasCount(2, viewModel.FilteredRecipes);
    }

    [TestMethod]
    public void SearchText_MatchingSubstring_ShowsOnlyMatchingRecipes()
    {
        RecipeListPageViewModel viewModel = CreateViewModel(MakeRecipe("Banana Bread"), MakeRecipe("Chicken Curry"));

        viewModel.SearchText = "chicken";

        Assert.HasCount(1, viewModel.FilteredRecipes);
        Assert.AreEqual("Chicken Curry", viewModel.FilteredRecipes[0].Name);
    }

    [TestMethod]
    public void SearchText_IsCaseInsensitive()
    {
        RecipeListPageViewModel viewModel = CreateViewModel(MakeRecipe("Banana Bread"));

        viewModel.SearchText = "BANANA";

        Assert.HasCount(1, viewModel.FilteredRecipes);
    }

    [TestMethod]
    public void SearchText_NoMatches_ResultsInEmptyList()
    {
        RecipeListPageViewModel viewModel = CreateViewModel(MakeRecipe("Banana Bread"), MakeRecipe("Chicken Curry"));

        viewModel.SearchText = "pizza";

        Assert.IsEmpty(viewModel.FilteredRecipes);
    }

    [TestMethod]
    public void SearchText_ClearedAfterFiltering_RestoresFullList()
    {
        RecipeListPageViewModel viewModel = CreateViewModel(MakeRecipe("Banana Bread"), MakeRecipe("Chicken Curry"));

        viewModel.SearchText = "chicken";
        viewModel.SearchText = string.Empty;

        Assert.HasCount(2, viewModel.FilteredRecipes);
    }
}
