using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Seasonality;

/// <summary>
/// The four absolute score bounds that drive the Green/Yellow/Red traffic-light classification.
/// Green and red are "guaranteed" above/below the outer bounds, and the inner floor/ceiling bound
/// which scores are ever allowed to be green/red (the middle is percentile-driven for a library).
/// </summary>
public sealed record SeasonalityThresholds(double GreenAlwaysHigh, double GreenFloor, double RedCeiling, double RedAlwaysLow)
{
    /// <summary>Defaults used when the user hasn't customized the thresholds in Settings.</summary>
    public static SeasonalityThresholds Default { get; } = new(80, 55, 45, 25);

    public static SeasonalityThresholds FromSettings(MacroTargetSettings settings) => new(
        settings.SeasonalityGreenAlwaysHigh,
        settings.SeasonalityGreenFloor,
        settings.SeasonalityRedCeiling,
        settings.SeasonalityRedAlwaysLow);
}
