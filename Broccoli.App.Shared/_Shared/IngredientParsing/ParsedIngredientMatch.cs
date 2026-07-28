using Broccoli.Data.Models;

namespace Broccoli.App.Shared.IngredientParsing;

/// <summary>
/// Confidence tier for a matched ingredient, suitable for UI colour-coding.
/// </summary>
public enum MatchConfidence
{
    /// <summary>Score &gt; 0.85 � auto-accepted (green).</summary>
    High,
    /// <summary>Score 0.60�0.85 � show match with option to change (yellow).</summary>
    Medium,
    /// <summary>Score &lt; 0.60 � flag as unmatched, require manual selection (red).</summary>
    Low,
    /// <summary>No match was found at all.</summary>
    None
}

/// <summary>
/// Represents a parsed ingredient with its matched (or unmatched) food from the database.
/// </summary>
public class ParsedIngredientMatch
{
    /// <summary>
    /// The parsed ingredient components.
    /// </summary>
    public required ParsedIngredient ParsedIngredient { get; set; }

    /// <summary>
    /// The matched food from the database, or null if no match found.
    /// </summary>
    public Food? MatchedFood { get; set; }

    /// <summary>
    /// Normalised match score in [0, 1] where 1.0 is a perfect match.
    /// Use this for confidence thresholds and UI colour-coding.
    /// </summary>
    public required double MatchScore { get; set; }

    /// <summary>
    /// The Levenshtein distance of the match (0 for exact, -1 for no match).
    /// Retained for backward compatibility � prefer <see cref="MatchScore"/>.
    /// </summary>
    public required int MatchDistance { get; set; }

    /// <summary>
    /// Describes which matching stage produced this result (e.g., "Exact", "Token", "Fuzzy", "FuzzySharp").
    /// </summary>
    public string MatchMethod { get; set; } = string.Empty;

    /// <summary>
    /// Whether a match was found.
    /// </summary>
    public required bool IsMatched { get; set; }

    /// <summary>
    /// Confidence tier based on <see cref="MatchScore"/> for UI colour-coding.
    /// ? High (&gt;0.85) � ?? Medium (0.60�0.85) � ?? Low (&lt;0.60) � ? None
    /// </summary>
    public MatchConfidence Confidence => IsMatched switch
    {
        false => MatchConfidence.None,
        true when MatchScore > 0.85 => MatchConfidence.High,
        true when MatchScore >= 0.60 => MatchConfidence.Medium,
        _ => MatchConfidence.Low
    };

    // Maps Food.Measure text to the same canonical unit vocabulary as ParsedIngredient.Unit.
    private static readonly Dictionary<string, string> s_measureNormalizationMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "tablespoon", "tbsp"  },
        { "teaspoon",   "tsp"   },
        { "cup",        "cup"   },
        { "gram",       "g"     },
        { "kilogram",   "kg"    },
        { "milliliter", "ml"    },
        { "millilitre", "ml"    },
        { "liter",      "l"     },
        { "litre",      "l"     },
        { "can",        "can"   },
        { "head",       "head"  },
        { "stalk",      "stalk" },
        { "clove",      "clove" },
        { "slice",      "slice" },
        { "piece",      "piece" },
        { "bunch",      "bunch" },
        { "sheet",      "sheet" },
    };

    /// <summary>
    /// Maps a <see cref="Food.Measure"/> string (e.g. "Tablespoon", "Cup, Chopped", "Medium Carrot")
    /// to the canonical unit used by <see cref="ParsedIngredient.Unit"/>.
    /// </summary>
    private static string NormalizeFoodMeasure(string? measure)
    {
        if (string.IsNullOrWhiteSpace(measure)) return string.Empty;
        string lower = measure.ToLowerInvariant().Trim();

        if (s_measureNormalizationMap.TryGetValue(lower, out string? exact)) return exact;

        // Prefix/contains matches for compound names like "Cup, Chopped", "Medium Carrot"
        if (lower.StartsWith("tablespoon")) return "tbsp";
        if (lower.StartsWith("teaspoon"))   return "tsp";
        if (lower.StartsWith("cup"))        return "cup";
        if (lower.Contains("medium"))       return "medium";
        if (lower.Contains("large"))        return "large";
        if (lower.Contains("small"))        return "small";

        return lower; // unknown measure — returned as-is so same-unit comparison still works
    }

    /// <summary>
    /// Returns the multiplier that converts 1 <paramref name="parsedUnit"/> into an equivalent number
    /// of <paramref name="foodMeasureUnit"/> units.
    /// For example, <c>("cup", "tbsp")</c> returns 16 because one cup contains 16 tablespoons.
    /// Returns <c>null</c> when no known relationship exists.
    /// </summary>
    private static double? GetUnitConversionRatio(string parsedUnit, string foodMeasureUnit)
    {
        if (string.IsNullOrEmpty(parsedUnit) || string.IsNullOrEmpty(foodMeasureUnit))
            return null;

        if (string.Equals(parsedUnit, foodMeasureUnit, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        return (parsedUnit, foodMeasureUnit) switch
        {
            // Volume cross-conversions
            ("cup",  "tbsp") => 16.0,
            ("cup",  "tsp")  => 48.0,
            ("tbsp", "cup")  => 1.0 / 16.0,
            ("tbsp", "tsp")  => 3.0,
            ("tsp",  "tbsp") => 1.0 / 3.0,
            ("tsp",  "cup")  => 1.0 / 48.0,
            // Mass cross-conversions
            ("kg",   "g")    => 1000.0,
            ("g",    "kg")   => 0.001,
            // Liquid cross-conversions
            ("l",    "ml")   => 1000.0,
            ("ml",   "l")    => 0.001,
            _                => null
        };
    }

    /// <summary>
    /// Calculates the actual weight in grams based on parsed quantity/unit and matched food metadata.
    /// Prefers food-specific <see cref="Food.GramsPerMeasure"/> density when the food's measure
    /// matches or is proportionally related to the parsed unit; falls back to static conversions
    /// for direct weight units (g/kg/oz/lb) and water-density approximations (ml/l).
    /// </summary>
    public double GetWeightInGrams()
    {
        if (!IsMatched || MatchedFood == null)
            return 0;

        string unit = ParsedIngredient.Unit?.ToLowerInvariant() ?? string.Empty;
        double qty  = ParsedIngredient.Quantity;

        // 1. Direct weight-unit conversions — food density is irrelevant
        if (unit == "g")  return qty;
        if (unit == "kg") return qty * 1000;
        if (unit == "oz") return qty * 28.35;
        if (unit == "lb") return qty * 453.59;

        // 2. Prefer food-specific GramsPerMeasure when we can relate the units
        string foodMeasureUnit = NormalizeFoodMeasure(MatchedFood.Measure);
        double? ratio = GetUnitConversionRatio(unit, foodMeasureUnit);
        if (ratio.HasValue)
            return qty * ratio.Value * MatchedFood.GramsPerMeasure;

        // 3. Volume fallbacks — water-density approximation (1 ml ≈ 1 g)
        if (unit == "ml") return qty;
        if (unit == "l")  return qty * 1000;

        // 4. Volumetric-unit static fallbacks (food has no matching measure)
        if (unit == "tsp")  return qty * 5;
        if (unit == "tbsp") return qty * 15;
        if (unit == "cup")  return qty * 240;

        // 5. Informal units
        if (unit == "drizzle") return MatchedFood.GramsPerMeasure; // treat as 1 measure
        if (unit == "pinch")   return 1.5;

        // 6. Count/size units and unknown — scale by GramsPerMeasure per item
        return qty * MatchedFood.GramsPerMeasure;
    }

    /// <summary>
    /// Returns the quantity formatted for display, always expressed in grams.
    /// Returns <c>"-"</c> for unmatched ingredients where weight cannot be determined.
    /// When the original unit was not grams the original quantity is appended in parentheses,
    /// e.g. <c>"27.0 g (2.0 tbsp)"</c> or <c>"104.0 g (1.0 cup)"</c>.
    /// </summary>
    public string GetQuantityDisplay()
    {
        if (!IsMatched || MatchedFood == null)
            return "-";

        double grams    = GetWeightInGrams();
        string unit     = ParsedIngredient.Unit?.ToLowerInvariant() ?? string.Empty;
        string gramsStr = $"{grams:F1} g";

        // Already in grams, or no explicit unit (count/fallback — repeating it adds no value)
        if (unit == "g" || string.IsNullOrEmpty(unit))
            return gramsStr;

        return $"{gramsStr} ({ParsedIngredient.Quantity:F1} {ParsedIngredient.Unit})";
    }

    /// <summary>
    /// Calculates nutritional value in the original unit's quantity.
    /// </summary>
    public double CalculateNutrient(Func<double, double> nutrientPer100gCalculator)
    {
        if (!IsMatched || MatchedFood == null)
        {
            return 0;
        }

        double gramsTotal = GetWeightInGrams();
        return (gramsTotal / 100.0) * nutrientPer100gCalculator(gramsTotal);
    }

    /// <summary>Gets calories for this ingredient.</summary>
    public double GetCalories() => CalculateNutrient(_ => MatchedFood?.CaloriesPer100g ?? 0);

    /// <summary>Gets fat in grams for this ingredient.</summary>
    public double GetFat() => CalculateNutrient(_ => MatchedFood?.FatPer100g ?? 0);

    /// <summary>Gets protein in grams for this ingredient.</summary>
    public double GetProtein() => CalculateNutrient(_ => MatchedFood?.ProteinPer100g ?? 0);

    /// <summary>Gets carbohydrates in grams for this ingredient.</summary>
    public double GetCarbohydrates() => CalculateNutrient(_ => MatchedFood?.CarbohydratesPer100g ?? 0);
}