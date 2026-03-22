using Microsoft.AspNetCore.Components;

namespace Broccoli.App.Shared.IngredientParsing;

public partial class ParsedIngredientsTable(IngredientParserService ingredientParserService, IFoodService foodService)
{
    [Parameter] public string? IngredientsText { get; set; }

    [Parameter] public int? Servings { get; set; }

    private List<ParsedIngredientMatch> _matches = new();
    private NutritionTotals _totals = new();
    private bool _isLoading = false;
    private string? _lastProcessedIngredients;
    private int _lastIngredientHash = 0;
    private Dictionary<(string, int), ParsedIngredientMatch?> _ingredientCache = new();

    protected override async Task OnInitializedAsync()
    {
        await ProcessIngredientsAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        // Reprocess whenever IngredientsText or Servings parameters change
        await ProcessIngredientsAsync();
    }

    private async Task ProcessIngredientsAsync()
    {
        if (string.IsNullOrWhiteSpace(IngredientsText))
        {
            _matches.Clear();
            _totals = new NutritionTotals();
            _lastProcessedIngredients = IngredientsText;
            _lastIngredientHash = (IngredientsText ?? string.Empty).GetHashCode();
            return;
        }

        // Skip if we've already processed this exact ingredients text
        if (IngredientsText == _lastProcessedIngredients)
        {
            return;
        }

        _isLoading = true;
        _lastIngredientHash = IngredientsText.GetHashCode();

        try
        {
            // Parse and match ingredients
            _matches = await ingredientParserService.ParseAndMatchIngredientsAsync(
                IngredientsText);

            _lastProcessedIngredients = IngredientsText;

            // Calculate totals
            CalculateTotals();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void CalculateTotals()
    {
        _totals = new NutritionTotals
        {
            Calories = _matches
                .Where(ingredientMatch => ingredientMatch.IsMatched)
                .Sum(ingredientMatch => ingredientMatch.GetCalories()),
            Fat = _matches
                .Where(ingredientMatch => ingredientMatch.IsMatched)
                .Sum(ingredientMatch => ingredientMatch.GetFat()),
            Protein = _matches
                .Where(ingredientMatch => ingredientMatch.IsMatched)
                .Sum(ingredientMatch => ingredientMatch.GetProtein()),
            Carbohydrates = _matches
                .Where(ingredientMatch => ingredientMatch.IsMatched)
                .Sum(ingredientMatch => ingredientMatch.GetCarbohydrates())
        };
    }

    private class NutritionTotals
    {
        public double Calories { get; set; }

        public double Fat { get; set; }

        public double Protein { get; set; }

        public double Carbohydrates { get; set; }
    }
}