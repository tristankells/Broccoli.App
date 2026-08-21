using System.ComponentModel;
using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

[TestClass]
public class RecipeEditViewModelComparisonTests
{
    [TestMethod]
    public void ChangingIngredients_RaisesPerServingAndDeviationPropertyChanged()
    {
        IngredientParserService parser = CreateParser();
        IMacroTargetService macroService = CreateMacroService();

        var recipeService = new Mock<IRecipeService>();
        var vm = new RecipeEditViewModel(recipeService.Object, null, parser, null, macroService)
        {
            Servings = 2,
        };

        var changed = new HashSet<string>();

        vm.Ingredients = "250g chicken breast";

        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);
        vm.Ingredients = "500g chicken breast";

        Assert.IsTrue(changed.Contains(nameof(vm.PerServingCalories)), $"PerServingCalories not raised. Raised: {string.Join(",", changed)}");
        Assert.IsTrue(changed.Contains(nameof(vm.CalDeviationPct)), $"CalDeviationPct not raised. Raised: {string.Join(",", changed)}");
    }

    private static IngredientParserService CreateParser()
    {
        var foodService = new Mock<IFoodService>();
        Food chicken = new()
        {
            Id = 1,
            Name = "chicken breast",
            Measure = "100g",
            GramsPerMeasure = 100,
            CaloriesPer100g = 165,
            ProteinPer100g = 31,
            CarbohydratesPer100g = 0,
            FatPer100g = 3.6,
        };
        foodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns(new FoodMatchResult { Food = chicken, Score = 1.0, Method = "Exact" });
        return new IngredientParserService(foodService.Object);
    }

    private static IMacroTargetService CreateMacroService()
    {
        MacroTargetSettings settings = new()
        {
            RecipeMealComparisonEnabled = true,
            RecipeMealComparisonPersonId = "person-1",
        };
        MacroTarget target = new()
        {
            Id = "person-1",
            Name = "Me",
            RecommendedCalories = 2400,
            RecommendedProteinG = 180,
            RecommendedCarbsG = 240,
            RecommendedFatG = 80,
        };
        var macroService = new Mock<IMacroTargetService>();
        macroService.Setup(s => s.GetSettings()).Returns(settings);
        macroService.Setup(s => s.GetAll()).Returns(new List<MacroTarget> { target });
        return macroService.Object;
    }
}
