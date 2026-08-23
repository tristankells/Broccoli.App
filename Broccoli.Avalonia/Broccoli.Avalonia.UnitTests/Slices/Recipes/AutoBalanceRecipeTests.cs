using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Recipes;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

/// <summary>
/// Exercises the auto-balance pipeline against the real "Crispy Gnocchi &amp; Beef Bolognese with
/// Parmesan" recipe (Crispy Gnocchi &amp; Beef Bolognese with Parmesan, 10 servings) stored locally at
/// <c>{LocalAppData}\Broccoli\Recipes\d221cfb7-fbed-4d5a-b1be-efeb843fa451\recipe.md</c>. The
/// ingredient block is embedded verbatim so the tests stay hermetic; food nutrition is a realistic
/// stand-in for the food database entries the matcher would resolve.
/// </summary>
[TestClass]
public class AutoBalanceRecipeTests
{
    private const string RecipeIngredients =
        "1 onion\n" +
        "8 cloves of garlic\n" +
        "500g carrot, grated\n" +
        "1 drizzle of oil\n" +
        "1000g lean beef mince\n" +
        "2 Tbsp Italian herbs\n" +
        "60g tomato paste\n" +
        "2 can crushed tomatoes\n" +
        "1 cup beef stock\n" +
        "150g baby spinach\n" +
        "2 Tbsp butter\n" +
        "1000g Gnocchi\n" +
        "70g grated Parmesan\n";

    private static readonly HashSet<AutoBalanceNutrient> s_allTargets = new()
    {
        AutoBalanceNutrient.Calories,
        AutoBalanceNutrient.Protein,
        AutoBalanceNutrient.Carbs,
        AutoBalanceNutrient.Fat,
    };

    private static readonly HashSet<AutoBalanceNutrient> s_noCalories = new()
    {
        AutoBalanceNutrient.Protein,
        AutoBalanceNutrient.Carbs,
        AutoBalanceNutrient.Fat,
    };

    private static readonly Dictionary<string, Food> s_foods = new(StringComparer.OrdinalIgnoreCase)
    {
        ["onion"] = MakeFood(1, "Onion", 40, 1.1, 9.3, 0.1),
        ["garlic"] = MakeFood(2, "Garlic", 149, 6.4, 33.0, 0.5),
        ["carrot"] = MakeFood(3, "Carrot", 41, 0.9, 10.0, 0.2),
        ["oil"] = MakeFood(4, "Olive Oil", 884, 0.0, 0.0, 100.0),
        ["lean beef mince"] = MakeFood(5, "Lean Beef Mince", 150, 21.0, 0.0, 7.0),
        ["italian herbs"] = MakeFood(6, "Italian Herbs", 0, 0.0, 0.0, 0.0),
        ["tomato paste"] = MakeFood(7, "Tomato Paste", 82, 4.3, 18.9, 0.5),
        ["crushed tomatoes"] = MakeFood(8, "Crushed Tomatoes", 32, 1.6, 7.0, 0.2),
        ["beef stock"] = MakeFood(9, "Beef Stock", 12, 1.7, 1.2, 0.1),
        ["baby spinach"] = MakeFood(10, "Baby Spinach", 23, 2.9, 3.6, 0.4),
        ["butter"] = MakeFood(11, "Butter", 717, 0.9, 0.1, 81.0),
        ["gnocchi"] = MakeFood(12, "Gnocchi", 180, 4.0, 38.0, 1.5),
        ["grated parmesan"] = MakeFood(13, "Grated Parmesan", 431, 38.0, 4.1, 29.0),
    };

    [TestMethod]
    public void BuildIngredients_FiltersToWeightBasedMatchedOnly()
    {
        List<AutoBalanceIngredient> ingredients = BuildIngredients(RecipeIngredients);

        Assert.AreEqual(6, ingredients.Count);
        CollectionAssert.AreEquivalent(
            new[] { "Carrot", "Lean Beef Mince", "Tomato Paste", "Baby Spinach", "Gnocchi", "Grated Parmesan" },
            ingredients.Select(i => i.FoodName).ToList());
        Assert.IsTrue(ingredients.All(i => i.CanonicalUnit is "g" or "kg"));
    }

    [TestMethod]
    public void SinglePass_AllTargets_RecipeShowsEachIngredientOnce()
    {
        AutoBalancePreview preview = Calculate(
            RecipeIngredients,
            Targets(3500, 260, 400, 90),
            s_allTargets,
            AutoBalanceStrategy.IndependentSinglePass);

        Assert.IsTrue(preview.HasChanges);
        Assert.AreEqual(2, preview.Adjustments.Count);
        Assert.AreEqual(
            preview.Adjustments.Count,
            preview.Adjustments.Select(a => a.Ingredient.FoodName).Distinct().Count());

        // Beef mince is the leading protein and fat contributor, so it must appear only once even
        // though it is adjusted for two different macros.
        AutoBalanceAdjustment beef = preview.Adjustments.Single(a => a.Ingredient.FoodName == "Lean Beef Mince");
        AutoBalanceAdjustment gnocchi = preview.Adjustments.Single(a => a.Ingredient.FoodName == "Gnocchi");
        Assert.IsTrue(beef.AfterGrams < beef.BeforeGrams);
        Assert.AreNotEqual(gnocchi.BeforeGrams, gnocchi.AfterGrams, 0.01);
    }

    [TestMethod]
    public void SinglePass_WithoutCalories_CarbsAndFatHitExactly()
    {
        AutoBalancePreview preview = Calculate(
            RecipeIngredients,
            Targets(3500, 260, 400, 90),
            s_noCalories,
            AutoBalanceStrategy.IndependentSinglePass);

        Assert.AreEqual(400, preview.After.CarbsG, 0.5);
        Assert.AreEqual(90, preview.After.FatG, 0.5);
        Assert.AreEqual(2, preview.Adjustments.Count);
    }

    [TestMethod]
    public void SinglePass_CaloriesOnly_AdjustsLeadingCalorieContributor()
    {
        AutoBalancePreview preview = Calculate(
            RecipeIngredients,
            Targets(3500, 0, 0, 0),
            new HashSet<AutoBalanceNutrient> { AutoBalanceNutrient.Calories },
            AutoBalanceStrategy.IndependentSinglePass);

        Assert.AreEqual(1, preview.Adjustments.Count);
        Assert.AreEqual("Gnocchi", preview.Adjustments[0].Ingredient.FoodName);
        Assert.AreEqual(3500, preview.After.Calories, 1);
        Assert.IsTrue(preview.Adjustments[0].AfterGrams < preview.Adjustments[0].BeforeGrams);
    }

    [TestMethod]
    public void LinearSolve_WithoutCalories_HitsProteinCarbsFatExactly()
    {
        AutoBalancePreview preview = Calculate(
            RecipeIngredients,
            Targets(3500, 260, 400, 90),
            s_noCalories,
            AutoBalanceStrategy.LinearSolve);

        Assert.IsFalse(preview.UsedFallback);
        Assert.AreEqual(3, preview.Adjustments.Count);
        Assert.AreEqual(260, preview.After.ProteinG, 0.01);
        Assert.AreEqual(400, preview.After.CarbsG, 0.01);
        Assert.AreEqual(90, preview.After.FatG, 0.01);
    }

    [TestMethod]
    public void LinearSolve_AllTargets_RecipeFallsBackToSinglePass()
    {
        AutoBalancePreview preview = Calculate(
            RecipeIngredients,
            Targets(3500, 260, 400, 90),
            s_allTargets,
            AutoBalanceStrategy.LinearSolve);

        // Calories are nearly 4P + 4C + 9F for these foods, so the 4x4 system is near-singular and
        // the calculator correctly falls back to the single-pass heuristic.
        Assert.IsTrue(preview.UsedFallback);
        Assert.IsTrue(preview.HasChanges);
        Assert.AreEqual(
            preview.Adjustments.Count,
            preview.Adjustments.Select(a => a.Ingredient.FoodName).Distinct().Count());
    }

    [TestMethod]
    public void SinglePass_WithinTolerance_NoChanges()
    {
        AutoBalancePreview preview = Calculate(
            RecipeIngredients,
            Targets(4000, 260, 480, 95),
            s_allTargets,
            AutoBalanceStrategy.IndependentSinglePass,
            tolerancePercent: 15);

        Assert.IsFalse(preview.HasChanges);
        Assert.AreEqual(0, preview.Adjustments.Count);
    }

    private static AutoBalancePreview Calculate(
        string ingredientText,
        AutoBalanceTargets targets,
        HashSet<AutoBalanceNutrient> selected,
        AutoBalanceStrategy strategy,
        double tolerancePercent = 0) =>
        AutoBalanceCalculator.Calculate(BuildIngredients(ingredientText), targets, selected, strategy, tolerancePercent);

    private static List<AutoBalanceIngredient> BuildIngredients(string ingredientText)
    {
        var foodService = new Mock<IFoodService>();
        foodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns<string>(description => s_foods.TryGetValue(description, out Food? food)
                ? new FoodMatchResult { Food = food, Score = 1.0, Method = "Exact" }
                : new FoodMatchResult { Score = 0, Method = "None" });

        IngredientParserService parser = new(foodService.Object);
        return parser.ParseAndMatchIngredients(ingredientText)
            .Select(AutoBalanceIngredient.FromMatch)
            .Where(ingredient => ingredient is not null)
            .Select(ingredient => ingredient!)
            .ToList();
    }

    private static AutoBalanceTargets Targets(
        double calories,
        double protein,
        double carbs,
        double fat) => new()
    {
        Calories = calories,
        ProteinG = protein,
        CarbsG = carbs,
        FatG = fat,
    };

    private static Food MakeFood(int id, string name, double calories, double protein, double carbs, double fat) => new()
    {
        Id = id,
        Name = name,
        Measure = "100g",
        GramsPerMeasure = 100,
        CaloriesPer100g = calories,
        ProteinPer100g = protein,
        CarbohydratesPer100g = carbs,
        FatPer100g = fat,
    };
}
