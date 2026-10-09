using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Groceries;

/// <summary>
/// Converts a matched grocery ingredient into a pure weight line (grams or pounds) when possible,
/// e.g. "1 onion" → "110g Onion" with a quantity hint that recalls the original item count
/// "(~1 medium onion)". Foods that are already measured by weight or volume, or that have no known
/// weight conversion, are returned unchanged.
/// </summary>
public static class GroceryUnitNormalizer
{
    private const double GramsPerPound = 453.59237;

    /// <summary>
    /// Returns the display name and quantity hint for a parsed ingredient under the given
    /// normalization mode. When the ingredient cannot be converted the original line and hint
    /// are returned untouched.
    /// </summary>
    public static (string Name, string? QuantityHint) Normalize(ParsedIngredientMatch match, GroceryUnitNormalization mode)
    {
        if (mode == GroceryUnitNormalization.Off || !match.IsMatched || match.MatchedFood is null)
        {
            return (Format(match), match.GetQuantityHint());
        }

        Food food = match.MatchedFood;
        double grams = match.GetWeightInGrams();
        if (grams <= 0 || food.GramsPerMeasure <= 1 || IsWeightOrVolumeMeasure(food.Measure))
        {
            return (Format(match), match.GetQuantityHint());
        }

        string unit = mode == GroceryUnitNormalization.Grams ? "g" : "lb";
        double quantity = mode == GroceryUnitNormalization.Grams ? grams : grams / GramsPerPound;
        string name = IngredientLineFormatter.Build(quantity, unit, food.Name);

        return (name, BuildCountHint(match, grams));
    }

    private static string Format(ParsedIngredientMatch match)
    {
        string unit = match.ParsedIngredient.CanonicalUnit ?? string.Empty;
        string food = match.IsMatched
            ? match.MatchedFood!.Name
            : match.ParsedIngredient.FoodDescription;
        return IngredientLineFormatter.Build(match.ParsedIngredient.Quantity, unit, food);
    }

    private static bool IsWeightOrVolumeMeasure(string? measure)
    {
        if (string.IsNullOrWhiteSpace(measure))
        {
            return true;
        }

        string normalized = NormalizeMeasure(measure);
        return normalized is "g" or "kg" or "ml" or "l" or "oz" or "lb" or "";
    }

    private static string? BuildCountHint(ParsedIngredientMatch match, double grams)
    {
        Food food = match.MatchedFood!;
        if (food.GramsPerMeasure <= 1)
        {
            return null;
        }

        double count = grams / food.GramsPerMeasure;
        if (count < 0.5)
        {
            return null;
        }

        string measure = food.Measure.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(measure))
        {
            return null;
        }

        // "each" adds no useful information to the hint, e.g. "(~1)" instead of "(~1 each)".
        return measure == "each"
            ? $"(~{count:0.#})"
            : $"(~{count:0.#} {measure})";
    }

    private static string NormalizeMeasure(string? measure)
    {
        string normalized = new string([.. measure!
            .Where(character => !char.IsDigit(character))
            .Select(character => char.ToLowerInvariant(character))])
            .Trim();

        return normalized switch
        {
            "gram" or "grams" => "g",
            "kilogram" or "kilograms" => "kg",
            "milliliter" or "milliliters" or "millilitre" or "millilitres" => "ml",
            "liter" or "liters" or "litre" or "litres" => "l",
            "ounce" or "ounces" => "oz",
            "pound" or "pounds" => "lb",
            "tablespoon" or "tablespoons" => "tbsp",
            "teaspoon" or "teaspoons" => "tsp",
            _ when normalized.StartsWith("cup", StringComparison.Ordinal) => "cup",
            _ when normalized.StartsWith("medium", StringComparison.Ordinal) => "medium",
            _ when normalized.StartsWith("large", StringComparison.Ordinal) => "large",
            _ when normalized.StartsWith("small", StringComparison.Ordinal) => "small",
            _ => normalized,
        };
    }
}
