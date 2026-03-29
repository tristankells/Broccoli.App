using Microsoft.AspNetCore.Components;

namespace Broccoli.App.Shared.IngredientParsing;

public partial class ParsedIngredientsTable(IngredientParserService ingredientParserService, IFoodService foodService)
{
    [Parameter] public string? IngredientsText { get; set; }

    [Parameter] public int? Servings { get; set; }

    /// <summary>
    /// Optional content rendered inside the pinned nutrition header, immediately after
    /// the "Per Serving" row. Intended for the meal macro comparison panel.
    /// </summary>
    [Parameter] public RenderFragment? MealComparisonContent { get; set; }

    /// <summary>
    /// When set, the Per Serving values are colour-coded against these per-meal targets.
    /// Pass null (default) to disable colour coding.
    /// </summary>
    [Parameter] public double? MealTargetCalories { get; set; }
    [Parameter] public double? MealTargetProteinG  { get; set; }
    [Parameter] public double? MealTargetCarbsG    { get; set; }
    [Parameter] public double? MealTargetFatG      { get; set; }

    /// <summary>Returns the CSS deviation class for a per-serving value vs its per-meal target.</summary>
    private static string DeviationClass(double actual, double? target)
    {
        if (target is null || target.Value <= 0) return string.Empty;
        var pct = Math.Abs(actual - target.Value) / target.Value * 100.0;
        return pct <= 15 ? "macro-ok" : pct <= 25 ? "macro-warn" : "macro-over";
    }

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