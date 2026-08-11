using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.IngredientParsing;

public enum MatchConfidence
{
    High,
    Medium,
    Low,
    None,
}

public class ParsedIngredientMatch
{
    public required ParsedIngredient ParsedIngredient { get; set; }
    public Food? MatchedFood { get; set; }
    public required double MatchScore { get; set; }
    public required int MatchDistance { get; set; }
    public string MatchMethod { get; set; } = string.Empty;
    public required bool IsMatched { get; set; }

    public MatchConfidence Confidence => IsMatched switch
    {
        false => MatchConfidence.None,
        true when MatchScore > 0.85 => MatchConfidence.High,
        true when MatchScore >= 0.60 => MatchConfidence.Medium,
        _ => MatchConfidence.Low,
    };

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

    private static string NormalizeFoodMeasure(string? measure)
    {
        if (string.IsNullOrWhiteSpace(measure))
        {
            return string.Empty;
        }

        string lower = measure.ToLowerInvariant().Trim();

        if (s_measureNormalizationMap.TryGetValue(lower, out string? exact))
        {
            return exact;
        }

        if (lower.StartsWith("tablespoon"))
        {
            return "tbsp";
        }

        if (lower.StartsWith("teaspoon"))
        {
            return "tsp";
        }

        if (lower.StartsWith("cup"))
        {
            return "cup";
        }

        if (lower.Contains("medium"))
        {
            return "medium";
        }

        if (lower.Contains("large"))
        {
            return "large";
        }

        if (lower.Contains("small"))
        {
            return "small";
        }

        return lower;
    }

    private static double? GetUnitConversionRatio(string parsedUnit, string foodMeasureUnit)
    {
        if (string.IsNullOrEmpty(parsedUnit) || string.IsNullOrEmpty(foodMeasureUnit))
        {
            return null;
        }

        if (string.Equals(parsedUnit, foodMeasureUnit, StringComparison.OrdinalIgnoreCase))
        {
            return 1.0;
        }

        return (parsedUnit, foodMeasureUnit) switch
        {
            ("cup",  "tbsp") => 16.0,
            ("cup",  "tsp")  => 48.0,
            ("tbsp", "cup")  => 1.0 / 16.0,
            ("tbsp", "tsp")  => 3.0,
            ("tsp",  "tbsp") => 1.0 / 3.0,
            ("tsp",  "cup")  => 1.0 / 48.0,
            ("kg",   "g")    => 1000.0,
            ("g",    "kg")   => 0.001,
            ("l",    "ml")   => 1000.0,
            ("ml",   "l")    => 0.001,
            _                => null,
        };
    }

    public double GetWeightInGrams()
    {
        if (!IsMatched || MatchedFood == null)
        {
            return 0;
        }

        string unit = ParsedIngredient.Unit?.ToLowerInvariant() ?? string.Empty;
        double qty  = ParsedIngredient.Quantity;

        if (unit == "g")
        {
            return qty;
        }

        if (unit == "kg")
        {
            return qty * 1000;
        }

        if (unit == "oz")
        {
            return qty * 28.35;
        }

        if (unit == "lb")
        {
            return qty * 453.59;
        }

        string foodMeasureUnit = NormalizeFoodMeasure(MatchedFood.Measure);
        double? ratio = GetUnitConversionRatio(unit, foodMeasureUnit);
        if (ratio.HasValue)
        {
            return qty * ratio.Value * MatchedFood.GramsPerMeasure;
        }

        if (unit == "ml")
        {
            return qty;
        }

        if (unit == "l")
        {
            return qty * 1000;
        }

        if (unit == "tsp")
        {
            return qty * 5;
        }

        if (unit == "tbsp")
        {
            return qty * 15;
        }

        if (unit == "cup")
        {
            return qty * 240;
        }

        if (unit == "drizzle")
        {
            return MatchedFood.GramsPerMeasure;
        }

        if (unit == "pinch")
        {
            return 1.5;
        }

        return qty * MatchedFood.GramsPerMeasure;
    }

    public string? GetQuantityHint()
    {
        if (!IsMatched || MatchedFood == null)
        {
            return null;
        }

        if (MatchedFood.GramsPerMeasure <= 1)
        {
            return null;
        }

        string normalizedMeasure = NormalizeFoodMeasure(MatchedFood.Measure);
        if (normalizedMeasure == "g" || normalizedMeasure == "kg" || normalizedMeasure == "ml" || normalizedMeasure == "l")
        {
            return null;
        }

        string canonicalUnit = (ParsedIngredient.CanonicalUnit ?? string.Empty).ToLowerInvariant();

        if (canonicalUnit == "g" || canonicalUnit == "kg")
        {
            double totalGrams = GetWeightInGrams();
            double count = totalGrams / MatchedFood.GramsPerMeasure;
            if (count < 0.5)
            {
                return null;
            }

            string measureLower = MatchedFood.Measure.ToLowerInvariant();
            return $"(~{count:0.#} {measureLower})";
        }

        double grams = GetWeightInGrams();
        if (grams <= 0)
        {
            return null;
        }

        return $"(~{grams:0.#}g)";
    }

    public string GetQuantityDisplay()
    {
        if (!IsMatched || MatchedFood == null)
        {
            return "-";
        }

        double grams    = GetWeightInGrams();
        string unit     = ParsedIngredient.Unit?.ToLowerInvariant() ?? string.Empty;
        string gramsStr = $"{grams:F1} g";

        if (unit == "g" || string.IsNullOrEmpty(unit))
        {
            return gramsStr;
        }

        return $"{gramsStr} ({ParsedIngredient.Quantity:F1} {ParsedIngredient.Unit})";
    }

    public double CalculateNutrient(Func<double, double> nutrientPer100gCalculator)
    {
        if (!IsMatched || MatchedFood == null)
        {
            return 0;
        }

        double gramsTotal = GetWeightInGrams();
        return (gramsTotal / 100.0) * nutrientPer100gCalculator(gramsTotal);
    }

    public double GetCalories() => CalculateNutrient(_ => MatchedFood?.CaloriesPer100g ?? 0);
    public double GetFat() => CalculateNutrient(_ => MatchedFood?.FatPer100g ?? 0);
    public double GetProtein() => CalculateNutrient(_ => MatchedFood?.ProteinPer100g ?? 0);
    public double GetCarbohydrates() => CalculateNutrient(_ => MatchedFood?.CarbohydratesPer100g ?? 0);
}
