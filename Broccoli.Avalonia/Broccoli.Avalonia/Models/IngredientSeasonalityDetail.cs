namespace Broccoli.Avalonia.Models;

/// <summary>
/// Seasonality detail for a single matched produce ingredient.
/// </summary>
public class IngredientSeasonalityDetail
{
    /// <summary>Display name from the produce dataset (e.g. "Strawberry").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Seasonality state of this ingredient for the scored date.</summary>
    public SeasonalityState State { get; init; }

    /// <summary>
    /// Scarcity weight for this ingredient: how much it counts toward the score relative to a
    /// year-round staple. 1.0 = available most of the year; up to ~3.5 = very short-season (rare).
    /// </summary>
    public double ScarcityWeight { get; init; }

    /// <summary>Number of effective in-season months per year (a partially-in-season month counts as half).</summary>
    public double EffectiveMonths { get; init; }

    /// <summary>Weight in grams used for this ingredient's contribution.</summary>
    public double WeightInGrams { get; init; }

    /// <summary>True when this ingredient is fully in season for the scored date.</summary>
    public bool IsInSeason => State == SeasonalityState.InSeason;

    /// <summary>True when this ingredient is partially in season for the scored date.</summary>
    public bool IsPartiallyInSeason => State == SeasonalityState.PartiallyInSeason;

    /// <summary>True when this ingredient is out of season for the scored date.</summary>
    public bool IsOutOfSeason => State == SeasonalityState.OutOfSeason;

    /// <summary>
    /// True when the ingredient has a short in-season window (scarcity &gt;= 2.0x, i.e. available
    /// roughly 3 months or fewer) AND is currently in season - i.e. a rare treat worth making the
    /// most of while it lasts. Surface this as a small "Limited season" callout rather than a warning.
    /// </summary>
    public bool IsLimitedSeason => ScarcityWeight >= 2.0 && IsInSeason;
}
