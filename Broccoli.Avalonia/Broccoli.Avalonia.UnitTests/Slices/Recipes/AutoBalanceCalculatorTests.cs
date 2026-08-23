using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Recipes;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

[TestClass]
public class AutoBalanceCalculatorTests
{
    private static readonly HashSet<AutoBalanceNutrient> s_allTargets = new()
    {
        AutoBalanceNutrient.Calories,
        AutoBalanceNutrient.Protein,
        AutoBalanceNutrient.Carbs,
        AutoBalanceNutrient.Fat,
    };

    [TestMethod]
    public void Calculate_NoSelectedTargets_ReturnsNoChanges()
    {
        List<AutoBalanceIngredient> ingredients = Ingredients(
            Pure("Protein source", 100, protein: 1));

        AutoBalancePreview preview = AutoBalanceCalculator.Calculate(
            ingredients,
            Targets(calories: 100, protein: 150),
            new HashSet<AutoBalanceNutrient>(),
            AutoBalanceStrategy.IndependentSinglePass);

        Assert.IsFalse(preview.HasChanges);
        Assert.AreEqual(0, preview.Adjustments.Count);
    }

    [TestMethod]
    public void Calculate_NoIngredients_ReturnsNoChanges()
    {
        AutoBalancePreview preview = AutoBalanceCalculator.Calculate(
            new List<AutoBalanceIngredient>(),
            Targets(calories: 100, protein: 50),
            s_allTargets,
            AutoBalanceStrategy.IndependentSinglePass);

        Assert.IsFalse(preview.HasChanges);
    }

    [TestMethod]
    public void IndependentSinglePass_ScalesLeadingProteinContributor()
    {
        List<AutoBalanceIngredient> ingredients = Ingredients(
            Pure("Chicken", 100, protein: 1),
            Pure("Rice", 100, carbs: 1));

        AutoBalancePreview preview = AutoBalanceCalculator.Calculate(
            ingredients,
            Targets(protein: 120, carbs: 90),
            new HashSet<AutoBalanceNutrient> { AutoBalanceNutrient.Protein, AutoBalanceNutrient.Carbs },
            AutoBalanceStrategy.IndependentSinglePass);

        Assert.AreEqual(2, preview.Adjustments.Count);
        Assert.AreEqual(120, preview.After.ProteinG, 0.01);
        Assert.AreEqual(90, preview.After.CarbsG, 0.01);
        Assert.AreEqual(120, preview.Adjustments.Single(a => a.Ingredient.FoodName == "Chicken").AfterGrams);
        Assert.AreEqual(90, preview.Adjustments.Single(a => a.Ingredient.FoodName == "Rice").AfterGrams);
    }

    [TestMethod]
    public void IndependentSinglePass_AdjustsCaloriesViaLeadingContributor()
    {
        List<AutoBalanceIngredient> ingredients = Ingredients(
            Pure("Sauce", 100, calories: 1));

        AutoBalancePreview preview = AutoBalanceCalculator.Calculate(
            ingredients,
            Targets(calories: 150),
            new HashSet<AutoBalanceNutrient> { AutoBalanceNutrient.Calories },
            AutoBalanceStrategy.IndependentSinglePass);

        Assert.AreEqual(150, preview.After.Calories, 0.01);
        Assert.AreEqual(150, preview.Adjustments.Single().AfterGrams);
    }

    [TestMethod]
    public void IndependentSinglePass_WithinTolerance_SkipsAdjustment()
    {
        List<AutoBalanceIngredient> ingredients = Ingredients(
            Pure("Chicken", 100, protein: 1),
            Pure("Rice", 100, carbs: 1));

        AutoBalancePreview preview = AutoBalanceCalculator.Calculate(
            ingredients,
            Targets(protein: 110, carbs: 110),
            new HashSet<AutoBalanceNutrient> { AutoBalanceNutrient.Protein, AutoBalanceNutrient.Carbs },
            AutoBalanceStrategy.IndependentSinglePass,
            tolerancePercent: 15);

        Assert.IsFalse(preview.HasChanges);
        Assert.AreEqual(0, preview.Adjustments.Count);
    }

    [TestMethod]
    public void LinearSolve_HitsAllTargetsExactly()
    {
        List<AutoBalanceIngredient> ingredients = Ingredients(
            Pure("Protein source", 100, protein: 1),
            Pure("Carbs source", 100, carbs: 1),
            Pure("Fat source", 100, fat: 1),
            Pure("Calories source", 100, calories: 1));

        AutoBalanceTargets targets = Targets(calories: 130, protein: 120, carbs: 110, fat: 90);

        AutoBalancePreview preview = AutoBalanceCalculator.Calculate(
            ingredients,
            targets,
            s_allTargets,
            AutoBalanceStrategy.LinearSolve);

        Assert.IsFalse(preview.UsedFallback);
        Assert.AreEqual(targets.Calories, preview.After.Calories, 0.001);
        Assert.AreEqual(targets.ProteinG, preview.After.ProteinG, 0.001);
        Assert.AreEqual(targets.CarbsG, preview.After.CarbsG, 0.001);
        Assert.AreEqual(targets.FatG, preview.After.FatG, 0.001);
        Assert.AreEqual(4, preview.Adjustments.Count);
    }

    [TestMethod]
    public void LinearSolve_SingularSystem_FallsBackToSinglePass()
    {
        List<AutoBalanceIngredient> ingredients = Ingredients(
            Pure("Lean", 100, protein: 1, calories: 4),
            Pure("Rich", 100, protein: 2, calories: 8));

        AutoBalancePreview preview = AutoBalanceCalculator.Calculate(
            ingredients,
            Targets(calories: 1200, protein: 360),
            new HashSet<AutoBalanceNutrient> { AutoBalanceNutrient.Protein, AutoBalanceNutrient.Calories },
            AutoBalanceStrategy.LinearSolve);

        Assert.IsTrue(preview.UsedFallback);
        Assert.IsTrue(preview.HasChanges);
    }

    [TestMethod]
    public void LinearSolve_NoEligiblePivot_FallsBackToSinglePass()
    {
        List<AutoBalanceIngredient> ingredients = Ingredients(
            Pure("Protein source", 100, protein: 1),
            Pure("Carbs source", 100, carbs: 1));

        AutoBalancePreview preview = AutoBalanceCalculator.Calculate(
            ingredients,
            Targets(calories: 300, protein: 120, carbs: 90),
            s_allTargets,
            AutoBalanceStrategy.LinearSolve);

        Assert.IsTrue(preview.UsedFallback);
        Assert.AreEqual(120, preview.After.ProteinG, 0.01);
        Assert.AreEqual(90, preview.After.CarbsG, 0.01);
    }

    [TestMethod]
    public void LinearSolve_InfeasibleQuantity_FallsBackToSinglePass()
    {
        List<AutoBalanceIngredient> ingredients = Ingredients(
            Pure("Protein source", 100, protein: 1),
            Pure("Carbs source", 100, carbs: 1),
            Pure("Fat source", 100, fat: 1),
            Pure("Calories source", 100, calories: 1));

        AutoBalancePreview preview = AutoBalanceCalculator.Calculate(
            ingredients,
            Targets(calories: 130, protein: 1000, carbs: 110, fat: 90),
            s_allTargets,
            AutoBalanceStrategy.LinearSolve);

        Assert.IsTrue(preview.UsedFallback);
    }

    [TestMethod]
    public void FromMatch_WeightUnit_BuildsIngredient()
    {
        var parsed = new ParsedIngredient
        {
            RawLine = "100g chicken breast",
            Quantity = 100,
            Unit = "g",
            CanonicalUnit = "g",
            FoodDescription = "chicken breast",
        };
        var food = new Food
        {
            Name = "Chicken Breast",
            GramsPerMeasure = 100,
            Measure = "100g",
            CaloriesPer100g = 120,
            ProteinPer100g = 24,
            CarbohydratesPer100g = 1,
            FatPer100g = 2,
        };
        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = food,
            MatchScore = 1,
            MatchDistance = 0,
            MatchMethod = "Exact",
            IsMatched = true,
        };

        AutoBalanceIngredient? ingredient = AutoBalanceIngredient.FromMatch(match);

        Assert.IsNotNull(ingredient);
        Assert.AreEqual(100, ingredient.Grams, 0.001);
        Assert.AreEqual(1.2, ingredient.KcalPerGram, 0.001);
        Assert.AreEqual(0.24, ingredient.ProteinPerGram, 0.001);
    }

    [TestMethod]
    public void FromMatch_NonWeightUnit_IsNotAdjustable()
    {
        var parsed = new ParsedIngredient
        {
            RawLine = "1 cup flour",
            Quantity = 1,
            Unit = "cup",
            CanonicalUnit = "cup",
            FoodDescription = "flour",
        };
        var food = new Food { Name = "Flour", GramsPerMeasure = 120, Measure = "cup" };
        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = food,
            MatchScore = 1,
            MatchDistance = 0,
            MatchMethod = "Exact",
            IsMatched = true,
        };

        AutoBalanceIngredient? ingredient = AutoBalanceIngredient.FromMatch(match);

        Assert.IsNotNull(ingredient);
        Assert.IsFalse(ingredient.IsAdjustable);
    }

    [TestMethod]
    public void FromMatch_Unmatched_ReturnsNull()
    {
        var parsed = new ParsedIngredient
        {
            RawLine = "100g mystery",
            Quantity = 100,
            Unit = "g",
            CanonicalUnit = "g",
            FoodDescription = "mystery",
        };
        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = null,
            MatchScore = 0,
            MatchDistance = -1,
            MatchMethod = string.Empty,
            IsMatched = false,
        };

        Assert.IsNull(AutoBalanceIngredient.FromMatch(match));
    }

    private static List<AutoBalanceIngredient> Ingredients(params AutoBalanceIngredient[] ingredients) =>
        ingredients.ToList();

    private static AutoBalanceTargets Targets(
        double calories = 0,
        double protein = 0,
        double carbs = 0,
        double fat = 0) => new()
    {
        Calories = calories,
        ProteinG = protein,
        CarbsG = carbs,
        FatG = fat,
    };

    private static AutoBalanceIngredient Pure(
        string name,
        double grams,
        double calories = 0,
        double protein = 0,
        double carbs = 0,
        double fat = 0) => new()
    {
        FoodName = name,
        FoodDescription = name.ToLowerInvariant(),
        CanonicalUnit = "g",
        Quantity = grams,
        Grams = grams,
        KcalPerGram = calories,
        ProteinPerGram = protein,
        CarbsPerGram = carbs,
        FatPerGram = fat,
    };
}
