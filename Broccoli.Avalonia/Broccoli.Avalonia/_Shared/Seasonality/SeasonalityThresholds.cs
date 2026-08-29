using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Seasonality;

/// <summary>
/// The configuration that drives the seasonality score and its Green/Yellow/Red classification:
/// the four absolute score bounds, the scarcity influence mode (how much short-season ingredients
/// count), and the top/bottom ranking percentiles used when classifying a recipe library.
/// </summary>
public sealed record SeasonalityThresholds(
    double GreenAlwaysHigh,
    double GreenFloor,
    double RedCeiling,
    double RedAlwaysLow,
    SeasonalityInfluenceMode InfluenceMode = SeasonalityInfluenceMode.Gentle,
    double TopPercentile = 0.67,
    double BottomPercentile = 0.33)
{
    /// <summary>Defaults used when the user hasn't customized the settings.</summary>
    public static SeasonalityThresholds Default { get; } = new(80, 55, 45, 25);

    public static SeasonalityThresholds FromSettings(MacroTargetSettings settings) => new(
        settings.SeasonalityGreenAlwaysHigh,
        settings.SeasonalityGreenFloor,
        settings.SeasonalityRedCeiling,
        settings.SeasonalityRedAlwaysLow,
        settings.SeasonalityInfluenceMode,
        settings.SeasonalityTopPercentile,
        settings.SeasonalityBottomPercentile);
}
