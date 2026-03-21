using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Services;

/// <summary>
/// Static utility for NZ Southern-Hemisphere season detection and scarcity weight lookup.
/// </summary>
public static class SeasonHelper
{
    /// <summary>All four seasons in calendar order.</summary>
    public static readonly IReadOnlyList<string> AllSeasons = ["spring", "summer", "autumn", "winter"];

    /// <summary>
    /// Returns the NZ season name for a given date (Southern Hemisphere).
    /// Spring = Sep/Oct/Nov · Summer = Dec/Jan/Feb · Autumn = Mar/Apr/May · Winter = Jun/Jul/Aug
    /// </summary>
    public static string GetCurrentSeason(DateTime date) => date.Month switch
    {
        9 or 10 or 11 => "spring",
        12 or 1 or 2  => "summer",
        3 or 4 or 5   => "autumn",
        6 or 7 or 8   => "winter",
        _             => "summer"   // unreachable — all months are covered
    };

    /// <summary>
    /// Returns the fixed scarcity weight for a produce item.
    /// <c>year_round</c> overrides to 0.25 regardless of the <see cref="ProduceItem.Seasons"/> count.
    /// </summary>
    public static double GetScarcityWeight(ProduceItem item)
    {
        if (item.YearRound) return 0.25;
        return item.Seasons.Count switch
        {
            1 => 1.00,
            2 => 0.75,
            3 => 0.50,
            _ => 0.25
        };
    }
}

