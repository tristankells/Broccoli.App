using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Planning;

[TestClass]
public class MealPrepSeasonalityTests
{
    [TestMethod]
    public void RefreshSeasonality_SummarizesPlanByBand()
    {
        // Onion is in season all year; feijoa only in January (today is a non-January month).
        IngredientParserService parser = CreateParser();
        ISeasonalityService seasonality = CreateSeasonality(
            new ProduceItem { Id = "onion", Name = "Onion", Type = "vegetable", Months = AllMonths() },
            new ProduceItem { Id = "feijoa", Name = "Feijoa", Type = "fruit", Months = new Dictionary<int, SeasonalityState> { [1] = SeasonalityState.InSeason } });

        var planService = new Mock<IMealPrepPlanService>();
        planService.Setup(s => s.GetAll()).Returns(new List<MealPrepPlan>
        {
            new() { Id = "p1", Name = "Batch Cook", RecipeIds = ["r1", "r2"] },
        });

        var recipeService = new Mock<IRecipeService>();
        recipeService.Setup(s => s.GetAll()).Returns(new List<Recipe>
        {
            new() { Id = "r1", Name = "Onion Soup", Ingredients = "100 g onion" },
            new() { Id = "r2", Name = "Feijoa Salad", Ingredients = "100 g feijoa" },
        });

        var viewModel = new MealPrepViewModel(planService.Object, recipeService.Object, parser, seasonality);

        Assert.HasCount(1, viewModel.Plans);
        Assert.AreEqual("1 green · 1 red", viewModel.Plans[0].SeasonalitySummary);
        Assert.AreEqual("Mostly out of season", viewModel.Plans[0].SeasonalityWarning);
    }

    [TestMethod]
    public void RefreshSeasonality_NoMatches_ShowsNoSummary()
    {
        IngredientParserService parser = CreateParser();
        ISeasonalityService seasonality = CreateSeasonality();

        var planService = new Mock<IMealPrepPlanService>();
        planService.Setup(s => s.GetAll()).Returns(new List<MealPrepPlan> { new() { Id = "p1", RecipeIds = ["r1"] } });

        var recipeService = new Mock<IRecipeService>();
        recipeService.Setup(s => s.GetAll()).Returns(new List<Recipe>
        {
            new() { Id = "r1", Name = "Stew", Ingredients = "500 g chicken" },
        });

        var viewModel = new MealPrepViewModel(planService.Object, recipeService.Object, parser, seasonality);

        Assert.IsFalse(viewModel.Plans[0].HasSeasonality);
        Assert.IsFalse(viewModel.Plans[0].HasWarning);
    }

    private static IngredientParserService CreateParser()
    {
        var foodService = new Mock<IFoodService>();
        foodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns(new FoodMatchResult { Score = 0, Method = "None" });
        foodService.Setup(s => s.FindBestMatch("onion"))
            .Returns(new FoodMatchResult { Food = new Food { Name = "Onion" }, Score = 1, Method = "Exact" });
        foodService.Setup(s => s.FindBestMatch("feijoa"))
            .Returns(new FoodMatchResult { Food = new Food { Name = "Feijoa" }, Score = 1, Method = "Exact" });
        return new IngredientParserService(foodService.Object);
    }

    private static ISeasonalityService CreateSeasonality(params ProduceItem[] items)
    {
        var store = new Mock<ISeasonalityDataStore>();
        store.Setup(s => s.GetAll()).Returns(items.ToList());
        return new SeasonalityService(store.Object);
    }

    private static Dictionary<int, SeasonalityState> AllMonths()
    {
        var dict = new Dictionary<int, SeasonalityState>();
        for (int month = 1; month <= 12; month++)
        {
            dict[month] = SeasonalityState.InSeason;
        }

        return dict;
    }
}
