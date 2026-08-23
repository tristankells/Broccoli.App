using Broccoli.Avalonia.IngredientParsing;

namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>A macro (or calorie) target that can be auto-balanced.</summary>
public enum AutoBalanceNutrient
{
    Calories,
    Protein,
    Carbs,
    Fat,
}

/// <summary>
/// A single matched, weight-based ingredient that the auto-balance feature is allowed to scale.
/// </summary>
public sealed class AutoBalanceIngredient
{
    public required string FoodName { get; set; }

    public required string FoodDescription { get; set; }

    public required string CanonicalUnit { get; set; }

    /// <summary>Merged quantity as typed in the recipe (sum of matching lines).</summary>
    public required double Quantity { get; set; }

    public required double Grams { get; set; }

    public required double KcalPerGram { get; set; }

    public required double ProteinPerGram { get; set; }

    public required double CarbsPerGram { get; set; }

    public required double FatPerGram { get; set; }

    public double Density(AutoBalanceNutrient nutrient) => nutrient switch
    {
        AutoBalanceNutrient.Calories => KcalPerGram,
        AutoBalanceNutrient.Protein => ProteinPerGram,
        AutoBalanceNutrient.Carbs => CarbsPerGram,
        AutoBalanceNutrient.Fat => FatPerGram,
        _ => 0,
    };

    public double Contribution(AutoBalanceNutrient nutrient) => Grams * Density(nutrient);

    /// <summary>
    /// Builds an eligible ingredient from a parsed match, or null when the match is unresolved,
    /// not weight-based (g/kg), or carries no grams.
    /// </summary>
    public static AutoBalanceIngredient? FromMatch(ParsedIngredientMatch match)
    {
        if (!match.IsMatched || match.MatchedFood is null)
        {
            return null;
        }

        string unit = match.ParsedIngredient.CanonicalUnit?.ToLowerInvariant() ?? string.Empty;
        if (unit is not ("g" or "kg"))
        {
            return null;
        }

        double grams = match.GetWeightInGrams();
        if (grams <= 0)
        {
            return null;
        }

        return new AutoBalanceIngredient
        {
            FoodName = match.MatchedFood.Name,
            FoodDescription = match.ParsedIngredient.FoodDescription,
            CanonicalUnit = unit,
            Quantity = match.ParsedIngredient.Quantity,
            Grams = grams,
            KcalPerGram = match.MatchedFood.CaloriesPer100g / 100.0,
            ProteinPerGram = match.MatchedFood.ProteinPer100g / 100.0,
            CarbsPerGram = match.MatchedFood.CarbohydratesPer100g / 100.0,
            FatPerGram = match.MatchedFood.FatPer100g / 100.0,
        };
    }
}

public sealed class AutoBalanceTotals
{
    public double Calories { get; set; }

    public double ProteinG { get; set; }

    public double CarbsG { get; set; }

    public double FatG { get; set; }
}

/// <summary>Recipe-total targets the auto-balance tries to reach.</summary>
public sealed class AutoBalanceTargets
{
    public double Calories { get; set; }

    public double ProteinG { get; set; }

    public double CarbsG { get; set; }

    public double FatG { get; set; }
}

/// <summary>A single ingredient whose quantity the auto-balance would change.</summary>
public sealed class AutoBalanceAdjustment
{
    public required AutoBalanceIngredient Ingredient { get; set; }

    public required double AfterGrams { get; set; }

    public double BeforeGrams => Ingredient.Grams;

    public double DeltaGrams => AfterGrams - BeforeGrams;
}

/// <summary>The computed outcome of an auto-balance run.</summary>
public sealed class AutoBalancePreview
{
    public required AutoBalanceTotals Before { get; set; }

    public required AutoBalanceTotals After { get; set; }

    public required IReadOnlyList<AutoBalanceAdjustment> Adjustments { get; set; }

    /// <summary>True when a requested linear solve fell back to the single-pass heuristic.</summary>
    public bool UsedFallback { get; set; }

    public bool HasChanges => Adjustments.Count > 0;
}
