namespace Broccoli.Avalonia.Models;

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
    Unavailable,
}
