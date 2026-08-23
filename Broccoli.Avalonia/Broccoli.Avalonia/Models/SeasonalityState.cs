namespace Broccoli.Avalonia.Models;

/// <summary>
/// Availability of a produce item for a single month.
/// </summary>
public enum SeasonalityState
{
    /// <summary>Not in season this month.</summary>
    OutOfSeason = 0,

    /// <summary>Partially in season this month (shoulder season / limited availability).</summary>
    PartiallyInSeason = 1,

    /// <summary>In season this month.</summary>
    InSeason = 2,
}
