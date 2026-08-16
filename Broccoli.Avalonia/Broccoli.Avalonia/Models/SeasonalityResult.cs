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
