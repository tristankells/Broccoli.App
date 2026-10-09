using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

[TestClass]
public class RecipeEditViewModelSeasonalityTests
{
    [TestMethod]
    public void Edit_AnnotatesSeasonalIngredients()
    {
        var macroService = new Mock<IMacroTargetService>();
        macroService.Setup(s => s.GetSettings()).Returns(new MacroTargetSettings { ShowRecipeEditorSeasonality = true });
        var viewModel = new RecipeEditViewModel(
            new Mock<IRecipeService>().Object,
            existingRecipe: null,
            CreateFeijoaParser(),
            foodService: null,
            macroService.Object,
            CreateFeijoaSeasonality());

        viewModel.Ingredients = "100 g feijoa";

        Assert.IsTrue(viewModel.ShowSeasonality);
        Assert.HasCount(1, viewModel.ParsedMatches);
        Assert.IsTrue(viewModel.ParsedMatches[0].HasSeasonality);
        StringAssert.Contains(viewModel.ParsedMatches[0].SeasonalityHint, "season");
        StringAssert.Contains(viewModel.ParsedMatches[0].SeasonalityHint, "pts");
        StringAssert.Contains(viewModel.ParsedMatches[0].SeasonalityTooltip, "Feijoa");
        StringAssert.Contains(viewModel.ParsedMatches[0].SeasonalityTooltip, "months/year");
        StringAssert.Contains(viewModel.ParsedMatches[0].TooltipText, "100 g feijoa");
    }

    [TestMethod]
    public void Edit_NonProduceIngredient_NoSeasonalityHint()
    {
        var macroService = new Mock<IMacroTargetService>();
        macroService.Setup(s => s.GetSettings()).Returns(new MacroTargetSettings { ShowRecipeEditorSeasonality = true });
        var viewModel = new RecipeEditViewModel(
            new Mock<IRecipeService>().Object,
            existingRecipe: null,
            CreateFeijoaParser(),
            foodService: null,
            macroService.Object,
            CreateFeijoaSeasonality());

        viewModel.Ingredients = "500 g chicken breast";

        Assert.HasCount(1, viewModel.ParsedMatches);
        Assert.IsFalse(viewModel.ParsedMatches[0].HasSeasonality);
        Assert.IsFalse(viewModel.HasSeasonality);
    }

    [TestMethod]
    public void Edit_ExposesRecipeLevelSeasonalitySummary()
    {
        var macroService = new Mock<IMacroTargetService>();
        macroService.Setup(s => s.GetSettings()).Returns(new MacroTargetSettings { ShowRecipeEditorSeasonality = true });
        var viewModel = new RecipeEditViewModel(
            new Mock<IRecipeService>().Object,
            existingRecipe: null,
            CreateFeijoaParser(),
            foodService: null,
            macroService.Object,
            CreateFeijoaSeasonality());

        viewModel.Ingredients = "100 g feijoa";

        Assert.IsTrue(viewModel.HasSeasonality);
        Assert.IsNotNull(viewModel.Seasonality);
        Assert.IsFalse(string.IsNullOrEmpty(viewModel.SeasonScoreText));
        Assert.IsFalse(string.IsNullOrEmpty(viewModel.SeasonalityBandLabel));
        StringAssert.Contains(viewModel.SeasonalityMonthLabel, "Seasonality for");
    }

    [TestMethod]
    public void Edit_SeasonalityDisabledInSettings_NoHints()
    {
        var macroService = new Mock<IMacroTargetService>();
        macroService.Setup(s => s.GetSettings()).Returns(new MacroTargetSettings { ShowRecipeEditorSeasonality = false });
        var viewModel = new RecipeEditViewModel(
            new Mock<IRecipeService>().Object,
            existingRecipe: null,
            CreateFeijoaParser(),
            foodService: null,
            macroService.Object,
            CreateFeijoaSeasonality());

        viewModel.Ingredients = "100 g feijoa";

        Assert.IsFalse(viewModel.ShowSeasonality);
        Assert.IsFalse(viewModel.ParsedMatches[0].HasSeasonality);
        Assert.AreEqual(string.Empty, viewModel.ParsedMatches[0].SeasonalityHint);
    }

    [TestMethod]
    public void ParsedIngredientRow_ApplySeasonality_SetsHintAndTooltip()
    {
        ParsedIngredientRow row = ParsedIngredientRow.FromMatch(new ParsedIngredientMatch
        {
            ParsedIngredient = new ParsedIngredient { RawLine = "100 g feijoa", Quantity = 100, Unit = "g", CanonicalUnit = "g", FoodDescription = "feijoa" },
            MatchedFood = new Food { Name = "Feijoa" },
            MatchScore = 1, MatchDistance = 0, MatchMethod = "Exact", IsMatched = true,
        });

        row.ApplySeasonality(
            new IngredientSeasonalityDetail { Name = "Feijoa", State = SeasonalityState.InSeason, ScarcityWeight = 2.0, EffectiveMonths = 3 },
            impactPoints: 15);

        Assert.IsTrue(row.HasSeasonality);
        Assert.AreEqual("#2ECC71", row.SeasonalityColor);
        StringAssert.Contains(row.SeasonalityHint, "In season");
        StringAssert.Contains(row.SeasonalityHint, "+15 pts");
        StringAssert.Contains(row.SeasonalityTooltip, "Contributes +15 pts");
        StringAssert.Contains(row.SeasonalityTooltip, "Available ~3 months/year");
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
}
