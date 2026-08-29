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
    /// Scarcity weight derived from the number of in-season months: lower when the ingredient is
    /// available for most of the year.
    /// </summary>
    public double ScarcityWeight { get; init; }

    /// <summary>Weight in grams used for this ingredient's contribution.</summary>
    public double WeightInGrams { get; init; }

    /// <summary>True when this ingredient is fully in season for the scored date.</summary>
    public bool IsInSeason => State == SeasonalityState.InSeason;

    /// <summary>True when this ingredient is partially in season for the scored date.</summary>
    public bool IsPartiallyInSeason => State == SeasonalityState.PartiallyInSeason;

    /// <summary>True when this ingredient is out of season for the scored date.</summary>
    public bool IsOutOfSeason => State == SeasonalityState.OutOfSeason;

    /// <summary>
    /// True when the ingredient has a short in-season window (scarce) AND is currently in season -
    /// i.e. a rare treat worth making the most of while it lasts. Surface this as a small
    /// "Limited season" callout rather than a warning.
    /// </summary>
    public bool IsLimitedSeason => ScarcityWeight >= 0.75 && IsInSeason;
}
