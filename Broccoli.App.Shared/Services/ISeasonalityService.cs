using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Services;

/// <summary>
/// Scores a recipe's seasonal suitability against the bundled NZ produce dataset.
/// </summary>
public interface ISeasonalityService
{
    /// <summary>
    /// Scores a recipe's seasonality from a list of already-parsed ingredient matches.
    /// <para>
    /// Only matched items with a grams weight ≥ 5 g that appear in the NZ produce dataset
    /// contribute to the score. All other ingredients (dry goods, dairy, meat, etc.)
    /// are silently ignored.
    /// </para>
    /// </summary>
    /// <param name="matches">
    ///   Output of <see cref="IngredientParserService.ParseAndMatchIngredientsAsync"/>.
    /// </param>
    /// <param name="asOf">Date to score against; defaults to <see cref="DateTime.Now"/>.</param>
    /// <returns>
    ///   A <see cref="SeasonalityResult"/> with a 0–100 score and per-ingredient breakdown,
    ///   or <see cref="SeasonalityLabel.Unavailable"/> when no produce was matched.
    /// </returns>
    SeasonalityResult Score(IEnumerable<ParsedIngredientMatch> matches, DateTime? asOf = null);
}

