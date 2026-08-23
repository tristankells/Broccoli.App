using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>
/// Finds recipes whose ingredients fuzzy-match a list of foods the user wants to use up.
/// </summary>
public interface IRecipeIngredientSearchService
{
    /// <summary>
    /// Ranks all recipes by how many of the given search terms they use and how closely their
    /// ingredients match, best match first. Returns an empty list when no recipes match.
    /// </summary>
    IReadOnlyList<RecipeIngredientSearchResult> Search(IReadOnlyList<string> searchTerms);
}
