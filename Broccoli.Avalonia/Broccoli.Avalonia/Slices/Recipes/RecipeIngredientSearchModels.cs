using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>
/// A single fuzzy match between one user search term and one recipe ingredient line.
/// </summary>
public class IngredientHit
{
    /// <summary>The user's search term that produced the match.</summary>
    public string SearchTerm { get; init; } = string.Empty;

    /// <summary>The recipe's raw ingredient line that matched.</summary>
    public string IngredientLine { get; init; } = string.Empty;

    /// <summary>The canonical food the ingredient line parsed to, when one was resolved.</summary>
    public string MatchedFoodName { get; init; } = string.Empty;

    /// <summary>Match confidence as a percentage (0-100).</summary>
    public double ScorePercent { get; init; }

    /// <summary>Scoring method that produced the match (Exact / Token / Fuzzy / FuzzySharp).</summary>
    public string Method { get; init; } = string.Empty;
}

/// <summary>
/// Aggregated search result for a single recipe: which of the user's terms it used, how many,
/// and how closely the ingredients matched.
/// </summary>
public class RecipeIngredientSearchResult
{
    public Recipe Recipe { get; init; } = null!;

    /// <summary>One hit per matched search term, best ingredient per term.</summary>
    public IReadOnlyList<IngredientHit> MatchedIngredients { get; init; } = [];

    /// <summary>Number of the user's search terms the recipe actually matched.</summary>
    public int MatchCount => MatchedIngredients.Count;

    /// <summary>Number of distinct search terms the user entered.</summary>
    public int TotalTerms { get; init; }

    /// <summary>Average fuzzy match percentage across the matched terms.</summary>
    public double AverageMatchPercent { get; init; }

    /// <summary>
    /// Coverage-weighted ranking score = average match percentage × (matched terms / total terms).
    /// Rewards recipes that use more of the user's list and match it well.
    /// </summary>
    public double MatchScore { get; init; }
}
