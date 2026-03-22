using Broccoli.Data.Models;

namespace Broccoli.App.Shared.IngredientParsing;

/// <summary>
/// Confidence tier for a matched ingredient, suitable for UI colour-coding.
/// </summary>
public enum MatchConfidence
{
    /// <summary>Score &gt; 0.85 — auto-accepted (green).</summary>
    High,
    /// <summary>Score 0.60–0.85 — show match with option to change (yellow).</summary>
    Medium,
    /// <summary>Score &lt; 0.60 — flag as unmatched, require manual selection (red).</summary>
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
    /// Retained for backward compatibility — prefer <see cref="MatchScore"/>.
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
    /// ? High (&gt;0.85) · ?? Medium (0.60–0.85) · ?? Low (&lt;0.60) · ? None
    /// </summary>
    public MatchConfidence Confidence => IsMatched switch
    {
        false => MatchConfidence.None,
        true when MatchScore > 0.85 => MatchConfidence.High,
        true when MatchScore >= 0.60 => MatchConfidence.Medium,
        _ => MatchConfidence.Low
    };

    /// <summary>
    /// Calculates the actual weight in grams based on parsed quantity/unit and matched food metadata.
    /// </summary>
    public double GetWeightInGrams()
    {
        if (!IsMatched || MatchedFood == null)
        {
            return 0;
        }

        return ParsedIngredient.Unit?.ToLowerInvariant() switch
        {
            "g"       => ParsedIngredient.Quantity,
            "kg"      => ParsedIngredient.Quantity * 1000,
            "ml"      => ParsedIngredient.Quantity,          // approximate: 1 ml ˜ 1 g
            "l"       => ParsedIngredient.Quantity * 1000,
            "tsp"     => ParsedIngredient.Quantity * 5,
            "tbsp"    => ParsedIngredient.Quantity * 15,
            "cup"     => ParsedIngredient.Quantity * 240,
            "oz"      => ParsedIngredient.Quantity * 28.35,
            "lb"      => ParsedIngredient.Quantity * 453.59,
            "drizzle" => MatchedFood.GramsPerMeasure * 1,   // treat as 1 measure
            "pinch"   => 1.5,
            _         => ParsedIngredient.Quantity * MatchedFood.GramsPerMeasure
        };
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