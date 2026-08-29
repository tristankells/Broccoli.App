using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;
using Broccoli.Avalonia.Slices.Groceries;
using Broccoli.Avalonia.Slices.Pantry;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Groceries;

[TestClass]
public class AddToCartSeasonalityTests
{
    [TestMethod]
    public void PreviewItem_OutOfSeasonProduce_IsFlagged()
    {
        AddToCartDialogViewModel viewModel = CreateViewModel(
            foods: new Dictionary<string, Food> { { "strawberries", Food("Strawberry") } },
            seasonalityStates: new Dictionary<string, SeasonalityState> { { "Strawberry", SeasonalityState.OutOfSeason } });

        Assert.HasCount(1, viewModel.PreviewItems);
        Assert.IsTrue(viewModel.PreviewItems[0].IsOutOfSeason);
    }

    [TestMethod]
    public void PreviewItem_InSeasonProduce_IsNotFlagged()
    {
        AddToCartDialogViewModel viewModel = CreateViewModel(
            foods: new Dictionary<string, Food> { { "onions", Food("Onion") } },
            seasonalityStates: new Dictionary<string, SeasonalityState> { { "Onion", SeasonalityState.InSeason } });

        Assert.IsFalse(viewModel.PreviewItems[0].IsOutOfSeason);
    }

    [TestMethod]
    public void PreviewItem_NonProduce_IsNotFlagged()
    {
        AddToCartDialogViewModel viewModel = CreateViewModel(
            foods: new Dictionary<string, Food> { { "chicken", Food("Chicken") } },
            seasonalityStates: new Dictionary<string, SeasonalityState>());

        Assert.IsFalse(viewModel.PreviewItems[0].IsOutOfSeason);
    }

    private static AddToCartDialogViewModel CreateViewModel(
        Dictionary<string, Food> foods,
        Dictionary<string, SeasonalityState> seasonalityStates)
    {
        var foodService = new Mock<IFoodService>();
        foodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns(new FoodMatchResult { Score = 0, Method = "None" });
        foreach ((string key, Food food) in foods)
        {
            foodService.Setup(s => s.FindBestMatch(key))
                .Returns(new FoodMatchResult { Food = food, Score = 1, Method = "Exact" });
        }

        var parser = new IngredientParserService(foodService.Object);
        var listService = new Mock<IGroceryListService>();
        listService.Setup(s => s.GetAll()).Returns([]);
        var cart = new IngredientCartService(parser, listService.Object);

        var pantry = new Mock<IPantryService>();
        pantry.Setup(s => s.FindByName(It.IsAny<string>())).Returns((PantryItem?)null);

        var seasonality = new Mock<ISeasonalityService>();
        seasonality.Setup(s => s.GetProduceStateForFood(It.IsAny<string>(), It.IsAny<DateTime?>()))
            .Returns((string name, DateTime? _) => seasonalityStates.TryGetValue(name, out SeasonalityState state) ? state : null);

        string[] lines = foods.Keys.Select(name => $"100 g {name}").ToArray();
        return new AddToCartDialogViewModel(cart, pantry.Object, "Recipe", lines.ToList(), seasonality.Object);
    }

    private static Food Food(string name) => new() { Name = name, Measure = "g", GramsPerMeasure = 1, CaloriesPer100g = 50 };
}
