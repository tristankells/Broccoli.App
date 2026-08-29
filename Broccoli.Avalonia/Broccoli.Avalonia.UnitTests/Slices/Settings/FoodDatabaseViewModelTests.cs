using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Settings;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Settings;

[TestClass]
public class FoodDatabaseViewModelTests
{
    [TestMethod]
    public void Load_PopulatesAllFoods()
    {
        FoodDatabaseViewModel viewModel = CreateViewModel(MakeFood(1, "Chicken Breast"), MakeFood(2, "Banana"));

        Assert.HasCount(2, viewModel.Foods);
        Assert.HasCount(2, viewModel.FilteredFoods);
    }

    [TestMethod]
    public void SearchText_MatchingSubstring_ShowsOnlyMatchingFoods()
    {
        FoodDatabaseViewModel viewModel = CreateViewModel(MakeFood(1, "Chicken Breast"), MakeFood(2, "Banana"));

        viewModel.SearchText = "chicken";

        Assert.HasCount(1, viewModel.FilteredFoods);
        Assert.AreEqual("Chicken Breast", viewModel.FilteredFoods.First().Name);
    }

    [TestMethod]
    public void SearchText_IsCaseInsensitive()
    {
        FoodDatabaseViewModel viewModel = CreateViewModel(MakeFood(1, "Chicken Breast"));

        viewModel.SearchText = "CHICKEN";

        Assert.HasCount(1, viewModel.FilteredFoods);
    }

    [TestMethod]
    public void SearchText_NoMatches_ResultsInEmptyList()
    {
        FoodDatabaseViewModel viewModel = CreateViewModel(MakeFood(1, "Chicken Breast"), MakeFood(2, "Banana"));

        viewModel.SearchText = "pizza";

        Assert.IsEmpty(viewModel.FilteredFoods);
    }

    [TestMethod]
    public void SearchText_ClearedAfterFiltering_RestoresFullList()
    {
        FoodDatabaseViewModel viewModel = CreateViewModel(MakeFood(1, "Chicken Breast"), MakeFood(2, "Banana"));

        viewModel.SearchText = "banana";
        viewModel.SearchText = string.Empty;

        Assert.HasCount(2, viewModel.FilteredFoods);
    }

    private static FoodDatabaseViewModel CreateViewModel(params Food[] foods)
    {
        var foodService = new Mock<IFoodService>();
        foodService.Setup(service => service.GetAll()).Returns(foods.ToList());
        return new FoodDatabaseViewModel(foodService.Object);
    }

    private static Food MakeFood(int id, string name) => new() { Id = id, Name = name };
}
