using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

[TestClass]
public class RecipeSeasonalityTests
{
    [TestMethod]
    public void Card_ExposesBandLabelScoreAndCoverage()
    {
        RecipeCardViewModel card = RecipeCardViewModel.FromRecipe(
            new Recipe { Name = "Curry" },
            imagePath: null,
            calPerServing: 0,
            proPerServing: 0,
            carbPerServing: 0,
            fatPerServing: 0,
            seasonality: new SeasonalityResult
            {
                Score = 90,
                Band = SeasonalityBand.Green,
                MatchedProduceCount = 2,
                TotalIngredientCount = 5,
            });

        Assert.AreEqual(SeasonalityBand.Green, card.SeasonBand);
        Assert.AreEqual("Green", card.SeasonBandLabel);
        Assert.AreEqual("#2ECC71", card.SeasonColor);
        Assert.AreEqual(90, card.SeasonScoreValue);
        Assert.AreEqual("90", card.SeasonScore);
        Assert.AreEqual("2 of 5 ingredients matched", card.SeasonCoverageText);
        Assert.AreEqual("Green", card.ColumnText(RecipeListColumn.Seasonality));
        Assert.IsTrue(card.HasSeasonality);
    }

    [TestMethod]
    public void Card_Unavailable_ShowsNoSeasonality()
    {
        RecipeCardViewModel card = RecipeCardViewModel.FromRecipe(
            new Recipe { Name = "Curry" },
            imagePath: null,
            0, 0, 0, 0,
            seasonality: new SeasonalityResult { Score = null, Band = SeasonalityBand.Unavailable });

        Assert.IsFalse(card.HasSeasonality);
        Assert.AreEqual(string.Empty, card.SeasonBandLabel);
        Assert.AreEqual("Gray", card.SeasonColor);
        Assert.AreEqual(string.Empty, card.SeasonCoverageText);
    }

    [TestMethod]
    public void Filter_ByGreenBand_KeepsOnlyGreenCards()
    {
        RecipeListPageViewModel viewModel = CreateViewModel(
            ("Green Stew", SeasonalityBand.Green, 90),
            ("Red Salad", SeasonalityBand.Red, 10),
            ("Yellow Soup", SeasonalityBand.Yellow, 50));
        viewModel.Reload();

        viewModel.SeasonalityFilter = SeasonalityFilter.Green;

        Assert.HasCount(1, viewModel.FilteredRecipes);
        Assert.AreEqual("Green Stew", viewModel.FilteredRecipes.First().Name);
    }

    [TestMethod]
    public void Filter_ByRedBand_KeepsOnlyRedCards()
    {
        RecipeListPageViewModel viewModel = CreateViewModel(
            ("Green Stew", SeasonalityBand.Green, 90),
            ("Red Salad", SeasonalityBand.Red, 10),
            ("Yellow Soup", SeasonalityBand.Yellow, 50));
        viewModel.Reload();

        viewModel.SeasonalityFilter = SeasonalityFilter.Red;

        Assert.HasCount(1, viewModel.FilteredRecipes);
        Assert.AreEqual("Red Salad", viewModel.FilteredRecipes.First().Name);
    }

    [TestMethod]
    public void Sort_BySeasonality_PutsGreenBeforeRedBeforeUnavailable()
    {
        RecipeListPageViewModel viewModel = CreateViewModel(
            ("Unavailable Curry", SeasonalityBand.Unavailable, null),
            ("Red Salad", SeasonalityBand.Red, 10),
            ("Green Stew", SeasonalityBand.Green, 90),
            ("Yellow Soup", SeasonalityBand.Yellow, 50));
        viewModel.Reload();

        viewModel.SortCommand.Execute(RecipeListColumn.Seasonality);

        Assert.AreEqual("Green Stew", viewModel.SortedListItems[0].Card.Name);
        Assert.AreEqual("Yellow Soup", viewModel.SortedListItems[1].Card.Name);
        Assert.AreEqual("Red Salad", viewModel.SortedListItems[2].Card.Name);
        Assert.AreEqual("Unavailable Curry", viewModel.SortedListItems[3].Card.Name);
    }

    [TestMethod]
    public void AsOfDate_ScopesSeasonalityToChosenMonth()
    {
        IngredientParserService parser = CreateFeijoaParser();
        ISeasonalityService seasonality = CreateFeijoaSeasonality();

        // January: feijoa is in season -> Green.
        RecipeListPageViewModel january = CreateRealViewModel(parser, seasonality);
        january.AsOfDate = new DateTime(2000, 1, 15);
        january.Reload();
        Assert.AreEqual(SeasonalityBand.Green, january.FilteredRecipes.First().SeasonBand);

        // July: feijoa is out of season -> Red.
        RecipeListPageViewModel july = CreateRealViewModel(parser, seasonality);
        july.AsOfDate = new DateTime(2000, 7, 15);
        july.Reload();
        Assert.AreEqual(SeasonalityBand.Red, july.FilteredRecipes.First().SeasonBand);
    }

    private static IngredientParserService CreateFeijoaParser()
    {
        var foodService = new Mock<IFoodService>();
        foodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns(new FoodMatchResult { Score = 0, Method = "None" });
        foodService.Setup(s => s.FindBestMatch("feijoa"))
            .Returns(new FoodMatchResult { Food = new Food { Name = "Feijoa" }, Score = 1, Method = "Exact" });
        return new IngredientParserService(foodService.Object);
    }

    private static ISeasonalityService CreateFeijoaSeasonality()
    {
        var store = new Mock<ISeasonalityDataStore>();
        store.Setup(s => s.GetAll()).Returns(new List<ProduceItem>
        {
            new() { Id = "feijoa", Name = "Feijoa", Type = "fruit", Months = new Dictionary<int, SeasonalityState> { [1] = SeasonalityState.InSeason } },
        });
        return new SeasonalityService(store.Object);
    }

    private static RecipeListPageViewModel CreateRealViewModel(IngredientParserService parser, ISeasonalityService seasonality)
    {
        var recipeService = new Mock<IRecipeService>();
        recipeService.Setup(s => s.GetAll()).Returns(new List<Recipe> { new() { Name = "Feijoa Salad", Ingredients = "100 g feijoa" } });

        var macroService = new Mock<IMacroTargetService>();
        macroService.Setup(s => s.GetSettings()).Returns(new MacroTargetSettings());

        return new RecipeListPageViewModel(recipeService.Object, parser, seasonality, macroService.Object);
    }

    private static RecipeListPageViewModel CreateViewModel(params (string Name, SeasonalityBand Band, double? Score)[] recipes)
    {
        // BuildCards sorts recipes by name, so align the mocked score results with that order.
        (string Name, SeasonalityBand Band, double? Score)[] ordered = recipes
            .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var recipeService = new Mock<IRecipeService>();
        recipeService.Setup(s => s.GetAll()).Returns(ordered.Select(r => new Recipe { Name = r.Name }).ToList());

        var seasonality = new Mock<ISeasonalityService>();
        seasonality.Setup(s => s.ScoreAll(
                It.IsAny<IEnumerable<IReadOnlyList<ParsedIngredientMatch>>>(),
                It.IsAny<DateTime?>(),
                It.IsAny<SeasonalityThresholds?>()))
            .Returns(ordered
                .Select(r => new SeasonalityResult { Score = r.Score, Band = r.Band })
                .ToList());

        var macroService = new Mock<IMacroTargetService>();
        macroService.Setup(s => s.GetSettings()).Returns(new MacroTargetSettings());

        return new RecipeListPageViewModel(recipeService.Object, null, seasonality.Object, macroService.Object);
    }
}
