using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Seasonality;

public static class SeasonHelper
{
    public static readonly IReadOnlyList<string> AllSeasons = ["spring", "summer", "autumn", "winter"];

    public static string GetCurrentSeason(DateTime date) => date.Month switch
    {
        9 or 10 or 11 => "spring",
        12 or 1 or 2  => "summer",
        3 or 4 or 5   => "autumn",
        6 or 7 or 8   => "winter",
        _             => "summer"
    };

    public static double GetScarcityWeight(ProduceItem item)
    {
        if (item.YearRound)
        {
            return 0.25;
        }

        return item.Seasons.Count switch
        {
            1 => 1.00,
            2 => 0.75,
            3 => 0.50,
            _ => 0.25
        };
    }
}
