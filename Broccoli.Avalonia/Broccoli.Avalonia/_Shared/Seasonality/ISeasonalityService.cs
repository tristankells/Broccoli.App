using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Seasonality;

public interface ISeasonalityService
{
    /// <summary>
    /// Scores a single recipe's parsed ingredient matches for the given date. The returned band is
    /// classified from absolute thresholds only (no library context).
    /// </summary>
    SeasonalityResult Score(IEnumerable<ParsedIngredientMatch> matches, DateTime? asOf = null, SeasonalityThresholds? thresholds = null);

    /// <summary>
    /// Scores a collection of recipes (one match-set each) and classifies each into a
    /// Green/Yellow/Red band relative to the whole collection's score distribution, so the top
    /// third is green and the bottom third red (guarded by the absolute thresholds).
    /// </summary>
    List<SeasonalityResult> ScoreAll(
        IEnumerable<IReadOnlyList<ParsedIngredientMatch>> matchSets,
        DateTime? asOf = null,
        SeasonalityThresholds? thresholds = null);

    /// <summary>
    /// Returns the seasonality state of a single food name for the given date, or null when the
    /// food doesn't map to a produce dataset entry. Used to flag out-of-season groceries.
    /// </summary>
    SeasonalityState? GetProduceStateForFood(string foodName, DateTime? asOf = null);

    /// <summary>
    /// Returns the seasonality detail (produce name, state for the date, scarcity weight, effective
    /// months) for a single food name, or null when it doesn't map to a produce dataset entry.
    /// Used by the recipe editor to annotate each ingredient.
    /// </summary>
    IngredientSeasonalityDetail? GetProduceDetailForFood(
        string foodName,
        DateTime? asOf = null,
        SeasonalityInfluenceMode influenceMode = SeasonalityInfluenceMode.Gentle);
}
