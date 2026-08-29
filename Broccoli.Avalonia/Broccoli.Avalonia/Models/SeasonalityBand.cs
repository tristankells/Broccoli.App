namespace Broccoli.Avalonia.Models;

/// <summary>
/// Traffic-light classification of a recipe's seasonality score. Green = strongly in season,
/// Red = mostly out of season, Yellow = mixed. Classified relative to the rest of the user's
/// recipe library (top/bottom third) with absolute threshold guards, or by absolute thresholds
/// alone when there isn't enough library data.
/// </summary>
public enum SeasonalityBand
{
    /// <summary>Score is strongly seasonal for the scored date.</summary>
    Green,

    /// <summary>Score is mixed - some produce in season, some not.</summary>
    Yellow,

    /// <summary>Score is strongly out of season for the scored date.</summary>
    Red,

    /// <summary>No produce ingredients were matched - score is null.</summary>
    Unavailable,
}
