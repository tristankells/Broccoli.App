namespace Broccoli.Avalonia.Models;

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
