using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Recipes;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

/// <summary>
/// Exercises the auto-balance pipeline against the real "Crispy Gnocchi &amp; Beef Bolognese with
/// Parmesan" recipe (10 servings) stored locally at
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
        ["cloves of garlic"] = MakeFood(2, "Garlic", 149, 6.4, 33.0, 0.5, gramsPerMeasure: 3),
        ["carrot"] = MakeFood(3, "Carrot", 41, 0.9, 10.0, 0.2),
        ["oil"] = MakeFood(4, "Olive Oil", 884, 0.0, 0.0, 100.0, gramsPerMeasure: 15),
        ["lean beef mince"] = MakeFood(5, "Lean Beef Mince", 150, 21.0, 0.0, 7.0),
        ["italian herbs"] = MakeFood(6, "Italian Herbs", 0, 0.0, 0.0, 0.0),
        ["tomato paste"] = MakeFood(7, "Tomato Paste", 82, 4.3, 18.9, 0.5),
        ["crushed tomatoes"] = MakeFood(8, "Crushed Tomatoes", 32, 1.6, 7.0, 0.2, gramsPerMeasure: 400),
        ["beef stock"] = MakeFood(9, "Beef Stock", 12, 1.7, 1.2, 0.1),
        ["baby spinach"] = MakeFood(10, "Baby Spinach", 23, 2.9, 3.6, 0.4),
        ["butter"] = MakeFood(11, "Butter", 717, 0.9, 0.1, 81.0),
        ["gnocchi"] = MakeFood(12, "Gnocchi", 180, 4.0, 38.0, 1.5),
        ["grated parmesan"] = MakeFood(13, "Grated Parmesan", 431, 38.0, 4.1, 29.0),
    };

    [TestMethod]
    public void BuildIngredients_IncludesAllMatched_OnlyWeightBasedAdjustable()
    {
        List<AutoBalanceIngredient> ingredients = BuildIngredients(RecipeIngredients);

        Assert.AreEqual(13, ingredients.Count);

        List<AutoBalanceIngredient> adjustable = ingredients.Where(i => i.IsAdjustable).ToList();
        CollectionAssert.AreEquivalent(
            new[] { "Carrot", "Lean Beef Mince", "Tomato Paste", "Baby Spinach", "Gnocchi", "Grated Parmesan" },
            adjustable.Select(i => i.FoodName).ToList());
        Assert.IsTrue(adjustable.All(i => i.CanonicalUnit is "g" or "kg"));

        // Volume/count ingredients (onion, garlic, oil, herbs, tomatoes, stock, butter) count toward
        // the totals but are never eligible to be scaled.
        Assert.IsTrue(ingredients.Where(i => !i.IsAdjustable).All(i => i.CanonicalUnit is not ("g" or "kg")));
        Assert.IsTrue(ingredients.Any(i => !i.IsAdjustable && i.FoodName == "Olive Oil"));
        Assert.IsTrue(ingredients.Any(i => !i.IsAdjustable && i.FoodName == "Butter"));
    }

    [TestMethod]
    public void Before_TotalsMatchRecipeEditorTotals()
    {
        List<ParsedIngredientMatch> matches = ParseRecipe(RecipeIngredients);
        List<AutoBalanceIngredient> ingredients = BuildIngredients(RecipeIngredients);

        AutoBalancePreview preview = AutoBalanceCalculator.Calculate(
            ingredients,
            Targets(3500, 260, 400, 90),
            s_allTargets,
            AutoBalanceStrategy.IndependentSinglePass);

        // The editor's Nutrition Summary sums match.GetCalories()/… over every matched ingredient;
        // the dialog's Before must agree so it never looks stale.
        double calories = matches.Where(m => m.IsMatched).Sum(m => m.GetCalories());
        double protein = matches.Where(m => m.IsMatched).Sum(m => m.GetProtein());
        double carbs = matches.Where(m => m.IsMatched).Sum(m => m.GetCarbohydrates());
        double fat = matches.Where(m => m.IsMatched).Sum(m => m.GetFat());

        Assert.AreEqual(calories, preview.Before.Calories, 0.001);
        Assert.AreEqual(protein, preview.Before.ProteinG, 0.001);
        Assert.AreEqual(carbs, preview.Before.CarbsG, 0.001);
        Assert.AreEqual(fat, preview.Before.FatG, 0.001);
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
            Targets(3500, 330, 560, 165),
            s_noCalories,
            AutoBalanceStrategy.LinearSolve);

        Assert.IsFalse(preview.UsedFallback);
        Assert.AreEqual(3, preview.Adjustments.Count);
        Assert.AreEqual(330, preview.After.ProteinG, 0.01);
        Assert.AreEqual(560, preview.After.CarbsG, 0.01);
        Assert.AreEqual(165, preview.After.FatG, 0.01);
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
        List<AutoBalanceIngredient> ingredients = BuildIngredients(RecipeIngredients);
        AutoBalanceTotals before = SumTotals(ingredients);

        AutoBalancePreview preview = AutoBalanceCalculator.Calculate(
            ingredients,
            new AutoBalanceTargets
            {
                Calories = before.Calories * 1.05,
                ProteinG = before.ProteinG * 1.05,
                CarbsG = before.CarbsG * 1.05,
                FatG = before.FatG * 1.05,
            },
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

    private static List<ParsedIngredientMatch> ParseRecipe(string ingredientText)
    {
        var foodService = new Mock<IFoodService>();
        foodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns<string>(description => s_foods.TryGetValue(description, out Food? food)
                ? new FoodMatchResult { Food = food, Score = 1.0, Method = "Exact" }
                : new FoodMatchResult { Score = 0, Method = "None" });

        IngredientParserService parser = new(foodService.Object);
        return parser.ParseAndMatchIngredients(ingredientText);
    }

    private static List<AutoBalanceIngredient> BuildIngredients(string ingredientText) =>
        ParseRecipe(ingredientText)
            .Select(AutoBalanceIngredient.FromMatch)
            .Where(ingredient => ingredient is not null)
            .Select(ingredient => ingredient!)
            .ToList();

    private static AutoBalanceTotals SumTotals(IEnumerable<AutoBalanceIngredient> ingredients)
    {
        double calories = 0, protein = 0, carbs = 0, fat = 0;
        foreach (AutoBalanceIngredient ingredient in ingredients)
        {
            calories += ingredient.Grams * ingredient.KcalPerGram;
            protein += ingredient.Grams * ingredient.ProteinPerGram;
            carbs += ingredient.Grams * ingredient.CarbsPerGram;
            fat += ingredient.Grams * ingredient.FatPerGram;
        }

        return new AutoBalanceTotals { Calories = calories, ProteinG = protein, CarbsG = carbs, FatG = fat };
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

    private static Food MakeFood(
        int id,
        string name,
        double calories,
        double protein,
        double carbs,
        double fat,
        double gramsPerMeasure = 100) => new()
    {
        Id = id,
        Name = name,
        Measure = "100g",
        GramsPerMeasure = gramsPerMeasure,
        CaloriesPer100g = calories,
        ProteinPer100g = protein,
        CarbohydratesPer100g = carbs,
        FatPer100g = fat,
    };
}
