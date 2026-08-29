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
    /// Scarcity weight used as the per-ingredient influence multiplier in the seasonality score,
    /// using the user's chosen influence mode (defaults to the gentle curve). Derived from how
    /// many effective in-season months the item has across the year (a partially-in-season month
    /// counts as half), so year-round staples count 1x and short-season items count more - meaning
    /// a scarce ingredient that is out of season has a bigger effect on the score.
    /// </summary>
    public static double GetScarcityWeight(ProduceItem item) => GetScarcityWeight(item, SeasonalityInfluenceMode.Gentle);

    public static double GetScarcityWeight(ProduceItem item, SeasonalityInfluenceMode mode)
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

        effectiveMonths = Math.Max(effectiveMonths, 1.0);

        return mode switch
        {
            SeasonalityInfluenceMode.None => 1.0,
            SeasonalityInfluenceMode.Moderate => Math.Min(12.0 / effectiveMonths, 4.0),
            SeasonalityInfluenceMode.Strong => Math.Min(Math.Pow(12.0 / effectiveMonths, 1.5), 8.0),
            _ => Math.Min(Math.Sqrt(12.0 / effectiveMonths), 4.0),
        };
    }
}
