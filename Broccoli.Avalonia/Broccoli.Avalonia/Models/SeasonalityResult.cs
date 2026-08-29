namespace Broccoli.Avalonia.Models;

/// <summary>
/// Top-level result of the seasonality scoring algorithm for a single recipe.
/// </summary>
public class SeasonalityResult
{
    /// <summary>
    /// Normalised score 0–100, or null when no produce ingredients were matched.
    /// </summary>
    public double? Score { get; init; }

    /// <summary>
    /// Traffic-light classification of <see cref="Score"/>. Set by absolute thresholds for a
    /// single-recipe score, or by library percentiles when scored as part of a collection via
    /// <c>ScoreAll</c>.
    /// </summary>
    public SeasonalityBand Band { get; set; } = SeasonalityBand.Unavailable;

    /// <summary>
    /// Per-ingredient breakdown — only for produce items found in the NZ dataset.
    /// </summary>
    public List<IngredientSeasonalityDetail> Breakdown { get; init; } = new();

    /// <summary>
    /// Human-readable best-seasons string, e.g. "Best in summer and autumn".
    /// Empty string when <see cref="Band"/> is <see cref="SeasonalityBand.Unavailable"/>.
    /// </summary>
    public string BestSeasons { get; init; } = string.Empty;

    /// <summary>Number of ingredients that matched the produce dataset (Breakdown count).</summary>
    public int MatchedProduceCount { get; init; }

    /// <summary>Total number of parsed ingredient lines the score was based on.</summary>
    public int TotalIngredientCount { get; init; }

    /// <summary>
    /// Coverage label such as "3 of 12 ingredients matched", so users can tell when the score is
    /// based on only a couple of ingredients. Empty when nothing matched.
    /// </summary>
    public string CoverageText => MatchedProduceCount == 0
        ? string.Empty
        : $"{MatchedProduceCount} of {TotalIngredientCount} ingredients matched";
}
