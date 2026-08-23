using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Recipes;

public class RecipeIngredientSearchService : IRecipeIngredientSearchService
{
    /// <summary>
    /// Minimum fuzzy match score (0-1) for a search term / ingredient pair to count as a hit.
    /// Slightly looser than <see cref="FoodService"/>'s matching threshold because this is a
    /// discovery feature - we'd rather surface a recipe that mostly fits than hide it.
    /// </summary>
    public const double MatchThreshold = 0.5;

    private readonly IRecipeService _recipeService;
    private readonly IngredientParserService _parser;
    private readonly IFoodService _foodService;

    public RecipeIngredientSearchService(
        IRecipeService recipeService,
        IngredientParserService parser,
        IFoodService foodService)
    {
        _recipeService = recipeService;
        _parser = parser;
        _foodService = foodService;
    }

    public IReadOnlyList<RecipeIngredientSearchResult> Search(IReadOnlyList<string> searchTerms)
    {
        List<string> terms = searchTerms
            .Select(term => term.Trim())
            .Where(term => term.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (terms.Count == 0)
        {
            return [];
        }

        var results = new List<RecipeIngredientSearchResult>();

        foreach (Recipe recipe in _recipeService.GetAll())
        {
            List<ParsedIngredientMatch> ingredients = _parser.ParseAndMatchIngredients(recipe.Ingredients);
            if (ingredients.Count == 0)
            {
                continue;
            }

            List<IngredientHit> hits = [];

            foreach (string term in terms)
            {
                IngredientHit? hit = FindBestHit(term, ingredients);
                if (hit is not null)
                {
                    hits.Add(hit);
                }
            }

            if (hits.Count == 0)
            {
                continue;
            }

            double averageMatchPercent = hits.Average(hit => hit.ScorePercent);
            double coverage = (double)hits.Count / terms.Count;

            results.Add(new RecipeIngredientSearchResult
            {
                Recipe = recipe,
                MatchedIngredients = hits,
                TotalTerms = terms.Count,
                AverageMatchPercent = averageMatchPercent,
                MatchScore = averageMatchPercent * coverage,
            });
        }

        return results
            .OrderByDescending(result => result.MatchScore)
            .ThenByDescending(result => result.MatchCount)
            .ThenBy(result => result.Recipe.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IngredientHit? FindBestHit(string term, List<ParsedIngredientMatch> ingredients)
    {
        IngredientHit? best = null;
        double bestScore = MatchThreshold;

        foreach (ParsedIngredientMatch ingredient in ingredients)
        {
            FoodMatchResult descriptionResult = _foodService.ScoreMatch(term, ingredient.ParsedIngredient.FoodDescription);
            FoodMatchResult foodNameResult = ingredient.MatchedFood is not null
                ? _foodService.ScoreMatch(term, ingredient.MatchedFood.Name)
                : descriptionResult;

            FoodMatchResult result = descriptionResult.Score >= foodNameResult.Score
                ? descriptionResult
                : foodNameResult;

            if (result.Score < MatchThreshold || result.Score <= bestScore)
            {
                continue;
            }

            bestScore = result.Score;
            best = new IngredientHit
            {
                SearchTerm = term,
                IngredientLine = ingredient.ParsedIngredient.RawLine,
                MatchedFoodName = ingredient.MatchedFood?.Name ?? string.Empty,
                ScorePercent = result.Score * 100,
                Method = result.Method,
            };
        }

        return best;
    }
}
