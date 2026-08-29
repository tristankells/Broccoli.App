using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Seasonality;

/// <summary>Display colour and label helpers for a <see cref="SeasonalityBand"/>.</summary>
public static class SeasonalityBandVisuals
{
    public static string Color(SeasonalityBand band) => band switch
    {
        SeasonalityBand.Green => "#2ECC71",
        SeasonalityBand.Yellow => "#F39C12",
        SeasonalityBand.Red => "#E74C3C",
        _ => "Gray",
    };

    public static string Label(SeasonalityBand band) => band switch
    {
        SeasonalityBand.Green => "Green",
        SeasonalityBand.Yellow => "Yellow",
        SeasonalityBand.Red => "Red",
        _ => "Unavailable",
    };
}
