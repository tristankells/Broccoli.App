using System.Text.RegularExpressions;
using Broccoli.Data.Models;
using Broccoli.Shared.Services;

namespace Broccoli.App.Shared.Services;

/// <summary>
/// Service for parsing ingredient strings into structured components (quantity, unit, food name)
/// and matching them against a food database with fuzzy matching support.
/// </summary>
public static class IngredientParserService
{
    // Simplified parsing: try to extract quantity and unit, rest is food name
    private static readonly Regex QuantityUnitPattern = new(
        @"^([\d\.]+(?:\s*/\s*[\d\.]+)?\s*\d*\s*\d*/\d+|[\d\.]+\s*/\s*[\d\.]+|[\d\.]+|\d+\s+\d+/\d+)?\s*(\w+)?\s*(.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    // Normalized unit mapping: input unit -> (canonical unit, grams per unit for normalization)
    // Uses case-insensitive comparer, so only lowercase entries needed (case-insensitive lookup handles variations)
    private static readonly Dictionary<string, (string canonical, double gramsPerStandardUnit)> UnitNormalizationMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Grams
        { "g", ("g", 1.0) },
        { "gram", ("g", 1.0) },
        { "grams", ("g", 1.0) },

        // Kilograms
        { "kg", ("kg", 1000.0) },
        { "kilogram", ("kg", 1000.0) },
        { "kilograms", ("kg", 1000.0) },

        // Milliliters (case-insensitive, so no need for "mL")
        { "ml", ("ml", 1.0) },
        { "milliliter", ("ml", 1.0) },
        { "milliliters", ("ml", 1.0) },

        // Liters (case-insensitive, so no need for "L")
        { "l", ("l", 1000.0) },
        { "liter", ("l", 1000.0) },
        { "liters", ("l", 1000.0) },

        // Cups
        { "cup", ("cup", 240.0) },
        { "cups", ("cup", 240.0) },
        { "c", ("cup", 240.0) },

        // Tablespoons
        { "tbsp", ("tbsp", 15.0) },
        { "tablespoon", ("tbsp", 15.0) },
        { "tablespoons", ("tbsp", 15.0) },
        { "tbl", ("tbsp", 15.0) },
        { "t", ("tbsp", 15.0) },

        // Teaspoons
        { "tsp", ("tsp", 5.0) },
        { "teaspoon", ("tsp", 5.0) },
        { "teaspoons", ("tsp", 5.0) },

        // Ounces
        { "oz", ("oz", 28.35) },
        { "ounce", ("oz", 28.35) },
        { "ounces", ("oz", 28.35) },

        // Pounds
        { "lb", ("lb", 453.59) },
        { "lbs", ("lb", 453.59) },
        { "pound", ("lb", 453.59) },
        { "pounds", ("lb", 453.59) },
    };

    /// <summary>
    /// Parses a single ingredient string into its components: quantity, unit, and food name.
    /// </summary>
    /// <param name="ingredientLine">Raw ingredient text (e.g., "1.5 cups flour")</param>
    /// <returns>Parsed ingredient with quantity, unit, and food name; or null if parsing fails</returns>
    public static ParsedIngredient? ParseIngredient(string ingredientLine)
    {
        if (string.IsNullOrWhiteSpace(ingredientLine))
        {
            return null;
        }

        string trimmed = ingredientLine.Trim();
        
        // Split by spaces and try to identify: quantity, unit, and food name
        var tokens = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        
        if (tokens.Count == 0)
        {
            return null;
        }

        double quantity = 1.0;
        int quantityTokens = 0;
        string unit = string.Empty;
        int unitTokens = 0;

        // Try to parse quantity from first token(s)
        if (tokens.Count > 0 && TryParseQuantity(tokens[0], out var parsedQty))
        {
            quantity = parsedQty;
            quantityTokens = 1;

            // Handle mixed fractions like "1 1/2"
            if (tokens.Count > 1 && tokens[1].Contains('/'))
            {
                var fractionParts = tokens[1].Split('/');
                if (fractionParts.Length == 2 &&
                    double.TryParse(fractionParts[0], out var numerator) &&
                    double.TryParse(fractionParts[1], out var denominator) &&
                    denominator != 0)
                {
                    quantity += numerator / denominator;
                    quantityTokens = 2;
                }
            }
        }

        // Try to parse unit from next token(s)
        int unitStartIdx = quantityTokens;
        if (unitStartIdx < tokens.Count)
        {
            string potentialUnit = tokens[unitStartIdx];
            if (IsUnit(potentialUnit))
            {
                unit = potentialUnit;
                unitTokens = 1;
            }
        }

        // Everything else is the food name
        int nameStartIdx = quantityTokens + unitTokens;
        if (nameStartIdx >= tokens.Count)
        {
            // No food name found
            return null;
        }

        string foodName = string.Join(" ", tokens.Skip(nameStartIdx));

        if (string.IsNullOrWhiteSpace(foodName))
        {
            return null;
        }

        string normalizedUnit = NormalizeUnit(unit, out string canonicalUnit);

        return new ParsedIngredient
        {
            RawLine = trimmed,
            Quantity = quantity,
            Unit = normalizedUnit,
            CanonicalUnit = canonicalUnit,
            FoodName = foodName
        };
    }

    /// <summary>
    /// Tries to parse a quantity from a string that may contain a number or fraction.
    /// </summary>
    private static bool TryParseQuantity(string token, out double result)
    {
        result = 0;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        // Try simple double parse
        if (double.TryParse(token, out var doubleResult))
        {
            result = doubleResult;
            return true;
        }

        // Try fraction format "numerator/denominator"
        if (token.Contains('/'))
        {
            var parts = token.Split('/');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], out var numerator) &&
                double.TryParse(parts[1], out var denominator) &&
                denominator != 0)
            {
                result = numerator / denominator;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a token is likely a unit of measurement.
    /// </summary>
    private static bool IsUnit(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        // Check if it's in our unit map
        return UnitNormalizationMap.ContainsKey(token);
    }

    /// <summary>
    /// Parses multiple ingredient lines and matches them against the food database.
    /// </summary>
    /// <param name="ingredientLines">Raw ingredient text with newline separators</param>
    /// <param name="foodService">Food service for fuzzy matching</param>
    /// <returns>List of matched or unmatched parsed ingredients</returns>
    public static async Task<List<ParsedIngredientMatch>> ParseAndMatchIngredientsAsync(
        string ingredientLines,
        IFoodService foodService)
    {
        var results = new List<ParsedIngredientMatch>();

        if (string.IsNullOrWhiteSpace(ingredientLines))
        {
            return results;
        }

        var lines = ingredientLines.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parsed = ParseIngredient(line);
            if (parsed == null)
            {
                continue;
            }

            // Try exact match first
            bool foundExact = foodService.TryGetFood(parsed.FoodName, out var exactFood);
            
            if (foundExact && exactFood != null)
            {
                results.Add(new ParsedIngredientMatch
                {
                    ParsedIngredient = parsed,
                    MatchedFood = exactFood,
                    MatchDistance = 0,
                    IsMatched = true
                });
            }
            else
            {
                // Try fuzzy match with Levenshtein distance threshold of 3
                bool foundFuzzy = foodService.TryGetFoodFuzzy(parsed.FoodName, maxDistance: 3, out var fuzzyFood);
                
                if (foundFuzzy && fuzzyFood != null)
                {
                    // Calculate actual distance for reporting
                    int distance = CalculateLevenshteinDistance(parsed.FoodName.ToLowerInvariant(), fuzzyFood.Name.ToLowerInvariant());
                    
                    results.Add(new ParsedIngredientMatch
                    {
                        ParsedIngredient = parsed,
                        MatchedFood = fuzzyFood,
                        MatchDistance = distance,
                        IsMatched = true
                    });
                }
                else
                {
                    results.Add(new ParsedIngredientMatch
                    {
                        ParsedIngredient = parsed,
                        MatchedFood = null,
                        MatchDistance = -1,
                        IsMatched = false
                    });
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Normalizes a unit string to one of the standard units.
    /// </summary>
    /// <param name="unit">Raw unit string</param>
    /// <param name="canonicalUnit">Output: the canonical form of the unit</param>
    /// <returns>Normalized unit or original if no mapping found</returns>
    public static string NormalizeUnit(string unit, out string canonicalUnit)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            canonicalUnit = string.Empty;
            return string.Empty;
        }

        string trimmedUnit = unit.Trim();

        if (UnitNormalizationMap.TryGetValue(trimmedUnit, out var normalization))
        {
            canonicalUnit = normalization.canonical;
            return normalization.canonical;
        }

        canonicalUnit = trimmedUnit;
        return trimmedUnit;
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// </summary>
    private static int CalculateLevenshteinDistance(string source, string target)
    {
        if (source == target)
        {
            return 0;
        }

        int sourceLength = source.Length;
        int targetLength = target.Length;

        if (sourceLength == 0)
        {
            return targetLength;
        }

        if (targetLength == 0)
        {
            return sourceLength;
        }

        int[] previousRow = new int[targetLength + 1];
        int[] currentRow = new int[targetLength + 1];

        for (int j = 0; j <= targetLength; j++)
        {
            previousRow[j] = j;
        }

        for (int i = 1; i <= sourceLength; i++)
        {
            currentRow[0] = i;

            for (int j = 1; j <= targetLength; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;

                currentRow[j] = Math.Min(
                    Math.Min(
                        previousRow[j] + 1,
                        currentRow[j - 1] + 1),
                    previousRow[j - 1] + cost);
            }

            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[targetLength];
    }
}

/// <summary>
/// Represents a parsed ingredient with quantity, unit, and food name.
/// </summary>
public class ParsedIngredient
{
    /// <summary>
    /// The original ingredient line as entered by the user.
    /// </summary>
    public required string RawLine { get; set; }

    /// <summary>
    /// Parsed quantity as a number (e.g., 1.5 for "1 1/2 cups")
    /// </summary>
    public required double Quantity { get; set; }

    /// <summary>
    /// The canonical unit after normalization (e.g., "cup" for "c", "cups", "Cup")
    /// </summary>
    public required string Unit { get; set; }

    /// <summary>
    /// The canonical unit form for internal calculations.
    /// </summary>
    public required string CanonicalUnit { get; set; }

    /// <summary>
    /// The extracted food name/ingredient.
    /// </summary>
    public required string FoodName { get; set; }
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
    /// The Levenshtein distance of the match (0 for exact, -1 for no match).
    /// </summary>
    public required int MatchDistance { get; set; }

    /// <summary>
    /// Whether a match was found.
    /// </summary>
    public required bool IsMatched { get; set; }

    /// <summary>
    /// Calculates the actual weight in grams based on parsed quantity and matched food's GramsPerMeasure.
    /// </summary>
    public double GetWeightInGrams()
    {
        if (!IsMatched || MatchedFood == null)
        {
            return 0;
        }

        return ParsedIngredient.Quantity * MatchedFood.GramsPerMeasure;
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

    /// <summary>
    /// Gets calories for this ingredient.
    /// </summary>
    public double GetCalories()
    {
        return CalculateNutrient(_ => MatchedFood?.CaloriesPer100g ?? 0);
    }

    /// <summary>
    /// Gets fat in grams for this ingredient.
    /// </summary>
    public double GetFat()
    {
        return CalculateNutrient(_ => MatchedFood?.FatPer100g ?? 0);
    }

    /// <summary>
    /// Gets protein in grams for this ingredient.
    /// </summary>
    public double GetProtein()
    {
        return CalculateNutrient(_ => MatchedFood?.ProteinPer100g ?? 0);
    }

    /// <summary>
    /// Gets carbohydrates in grams for this ingredient.
    /// </summary>
    public double GetCarbohydrates()
    {
        return CalculateNutrient(_ => MatchedFood?.CarbohydratesPer100g ?? 0);
    }
}




