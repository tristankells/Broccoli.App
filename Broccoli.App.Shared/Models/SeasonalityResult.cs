namespace Broccoli.Data.Models;

/// <summary>
/// Score classification label for a recipe's seasonality.
/// </summary>
public enum SeasonalityLabel
{
    /// <summary>Score 75–100: most produce is in season.</summary>
    PeakSeason,
    /// <summary>Score 40–74: mix of in- and out-of-season produce.</summary>
    PartiallyInSeason,
    /// <summary>Score 0–39: most produce is out of season.</summary>
    OffSeason,
    /// <summary>No produce ingredients were matched — score is null.</summary>
    Unavailable
}

/// <summary>
/// Top-level result of the seasonality scoring algorithm for a single recipe.
/// </summary>
public class SeasonalityResult
{
    /// <summary>
    /// Normalised score 0–100, or null when no produce ingredients were matched.
    /// </summary>
    public double? Score { get; init; }

    /// <summary>Score classification label.</summary>
    public SeasonalityLabel Label { get; init; }

    /// <summary>
    /// Per-ingredient breakdown — only for produce items found in the NZ dataset.
    /// </summary>
    public List<IngredientSeasonalityDetail> Breakdown { get; init; } = new();

    /// <summary>
    /// Human-readable best-seasons string, e.g. "Best in summer and autumn".
    /// Empty string when <see cref="Label"/> is <see cref="SeasonalityLabel.Unavailable"/>.
    /// </summary>
    public string BestSeasons { get; init; } = string.Empty;
}

/// <summary>
/// Seasonality detail for a single matched produce ingredient.
/// </summary>
public class IngredientSeasonalityDetail
{
    /// <summary>Display name from the produce dataset (e.g. "Strawberry").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Whether this ingredient is in season for the scored date.</summary>
    public bool IsInSeason { get; init; }

    /// <summary>
    /// Fixed scarcity weight: 1.0 (1 season) / 0.75 (2) / 0.5 (3) / 0.25 (4 or year-round).
    /// </summary>
    public double ScarcityWeight { get; init; }

    /// <summary>Weight in grams used for this ingredient's contribution.</summary>
    public double WeightInGrams { get; init; }

    /// <summary>
    /// True when <see cref="ScarcityWeight"/> >= 0.75.
    /// Used to surface the "Limited season — consider substituting" callout in the UI.
    /// </summary>
    public bool IsLimitedSeason => ScarcityWeight >= 0.75;
}

