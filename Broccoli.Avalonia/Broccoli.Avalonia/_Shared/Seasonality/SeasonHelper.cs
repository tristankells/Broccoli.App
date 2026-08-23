using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Seasonality;

public static class SeasonHelper
{
    public static readonly IReadOnlyList<string> AllSeasons = ["spring", "summer", "autumn", "winter"];

    public static string GetCurrentSeason(DateTime date) => date.Month switch
    {
        9 or 10 or 11 => "spring",
        12 or 1 or 2 => "summer",
        3 or 4 or 5 => "autumn",
        6 or 7 or 8 => "winter",
        _ => "summer",
    };

    /// <summary>Month numbers (1..12) belonging to each Southern-Hemisphere season.</summary>
    public static IReadOnlyList<int> GetSeasonMonths(string season) => season switch
    {
        "spring" => [9, 10, 11],
        "summer" => [12, 1, 2],
        "autumn" => [3, 4, 5],
        "winter" => [6, 7, 8],
        _ => [12, 1, 2],
    };

    /// <summary>
    /// Scarcity weight derived from how many effective in-season months the item has across the
    /// year (a partially-in-season month counts as half). Items available for most of the year are
    /// common (low weight); short-season items are scarce (high weight).
    /// </summary>
    public static double GetScarcityWeight(ProduceItem item)
    {
        double effectiveMonths = 0;
        for (int month = 1; month <= 12; month++)
        {
            effectiveMonths += item.GetStateForMonth(month) switch
            {
                SeasonalityState.InSeason => 1.0,
                SeasonalityState.PartiallyInSeason => 0.5,
                _ => 0.0,
            };
        }

        return effectiveMonths switch
        {
            < 4 => 1.00,
            < 7 => 0.75,
            < 10 => 0.50,
            _ => 0.25,
        };
    }
}
