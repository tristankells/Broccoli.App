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
        Assert.AreEqual("Chicken Curry", viewModel.FilteredRecipes.First().Name);
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

    [TestMethod]
    public void SearchText_MultipleTokens_MatchesOnMultipleTokens()
    {
        RecipeListPageViewModel viewModel = CreateViewModel(MakeRecipe("Banana Bread"), MakeRecipe("Chicken Curry"), MakeRecipe("Chicken Stew"));

        viewModel.SearchText = "chicken cu";

        Assert.HasCount(1, viewModel.FilteredRecipes);
        Assert.AreEqual("Chicken Curry", viewModel.FilteredRecipes.First().Name);
    }

    [TestMethod]
    public void SearchText_MatchesTag()
    {
        RecipeListPageViewModel viewModel = CreateViewModel(
            new Recipe { Name = "Apple Pie", Tags = ["dessert", "baking"] },
            new Recipe { Name = "Chicken Breast", Tags = ["poultry"] },
            new Recipe { Name = "Banana Bread", Tags = [] });

        viewModel.SearchText = "dessert";

        Assert.HasCount(1, viewModel.FilteredRecipes);
        Assert.AreEqual("Apple Pie", viewModel.FilteredRecipes.First().Name);
    }

    [TestMethod]
    public void SearchText_MatchesIngredient()
    {
        RecipeListPageViewModel viewModel = CreateViewModel(
            new Recipe { Name = "Pasta", Ingredients = "tomatoes, garlic, basil" },
            new Recipe { Name = "Omelette", Ingredients = "eggs, butter, cheese" },
            new Recipe { Name = "Smoothie", Ingredients = "banana, yogurt, honey" });

        viewModel.SearchText = "garlic";

        Assert.HasCount(1, viewModel.FilteredRecipes);
        Assert.AreEqual("Pasta", viewModel.FilteredRecipes.First().Name);
    }

    [TestMethod]
    public void SearchText_MatchesCombinations()
    {
        RecipeListPageViewModel viewModel = CreateViewModel(
            new Recipe { Name = "Chicken Curry", Tags = ["indian", "spicy"], Ingredients = "chicken breast, curry powder, coconut milk" },
            new Recipe { Name = "Vegetable Soup", Tags = [], Ingredients = "carrots, celery, broth" },
            new Recipe { Name = "Banana Bread", Tags = ["dessert"], Ingredients = "banana, flour, sugar" });

        viewModel.SearchText = "curry";
        Assert.HasCount(1, viewModel.FilteredRecipes);
        Assert.AreEqual("Chicken Curry", viewModel.FilteredRecipes.First().Name);

        viewModel.SearchText = "indian";
        Assert.HasCount(1, viewModel.FilteredRecipes);
        Assert.AreEqual("Chicken Curry", viewModel.FilteredRecipes.First().Name);

        viewModel.SearchText = "celery";
        Assert.HasCount(1, viewModel.FilteredRecipes);
        Assert.AreEqual("Vegetable Soup", viewModel.FilteredRecipes.First().Name);
    }

    [TestMethod]
    public void SearchText_TitleSourceDisabled_DoesNotMatch()
    {
        RecipeListPageViewModel viewModel = CreateViewModel(
            new Recipe { Name = "Chicken Curry", Tags = [], Ingredients = "coconut milk, turmeric" },
            new Recipe { Name = "Banana Bread", Tags = ["dessert"], Ingredients = "banana, flour" });

        viewModel.IsTitleSearchEnabled = false;

        viewModel.SearchText = "chicken";
        Assert.IsEmpty(viewModel.FilteredRecipes);
    }

    [TestMethod]
    public void SearchText_TagSourceDisabled_DoesNotMatch()
    {
        RecipeListPageViewModel viewModel = CreateViewModel(
            new Recipe { Name = "Apple Pie", Tags = ["dessert", "baking"], Ingredients = "apples, cinnamon" },
            new Recipe { Name = "Plain Rice", Tags = [], Ingredients = "rice, water" });

        viewModel.IsTagSearchEnabled = false;

        viewModel.SearchText = "dessert";
        Assert.IsEmpty(viewModel.FilteredRecipes);
    }

    [TestMethod]
    public void SearchText_IngredientSourceDisabled_DoesNotMatch()
    {
        RecipeListPageViewModel viewModel = CreateViewModel(
            new Recipe { Name = "Green Smoothie", Tags = ["drink"], Ingredients = "spinach, banana, almond milk" },
            new Recipe { Name = "Omelette", Tags = ["breakfast"], Ingredients = "eggs, cheese, butter" });

        viewModel.IsIngredientSearchEnabled = false;

        viewModel.SearchText = "spinach";
        Assert.IsEmpty(viewModel.FilteredRecipes);
    }

    private static RecipeListPageViewModel CreateViewModel(params Recipe[] recipes)
    {
        var recipeService = new Mock<IRecipeService>();
        recipeService.Setup(service => service.GetAll()).Returns(recipes);
        var recipeListPageViewModel = new RecipeListPageViewModel(recipeService.Object);
        recipeListPageViewModel.Reload();
        return recipeListPageViewModel;
    }

    private static Recipe MakeRecipe(string name) => new() { Name = name };
}
