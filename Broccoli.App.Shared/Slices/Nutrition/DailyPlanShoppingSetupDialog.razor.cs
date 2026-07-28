using Broccoli.App.Shared._Shared.IngredientParsing;
using Broccoli.App.Shared.Models;
using Broccoli.App.Shared.Slices.GroceryList;
using Microsoft.AspNetCore.Components;

namespace Broccoli.App.Shared.Slices.Nutrition;

/// <summary>Result passed to the parent when the user clicks "Preview Ingredients".</summary>
public sealed record ShoppingPreviewResult(string IngredientText, string Label);

public partial class DailyPlanShoppingSetupDialog
{
    // -- Parameters ------------------------------------------------------------

    [Parameter] public bool IsVisible { get; set; }

    /// <summary>The plan currently open in the editor — pre-selected on open.</summary>
    [Parameter] public DailyFoodPlan? CurrentPlan { get; set; }

    /// <summary>All plans owned by the user, for multi-plan selection.</summary>
    [Parameter] public List<DailyFoodPlan> AllPlans { get; set; } = new();

    /// <summary>All recipes, for scaling ingredient text.</summary>
    [Parameter] public List<Recipe> AllRecipes { get; set; } = new();

    /// <summary>All foods, for building non-recipe ingredient lines.</summary>
    [Parameter] public List<Food> AllFoods { get; set; } = new();

    /// <summary>Raised when the user cancels.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <summary>
    /// Raised when the user clicks "Preview Ingredients". Provides the built
    /// ingredient text (newline-separated) and a human-readable label for the dialog title.
    /// </summary>
    [Parameter] public EventCallback<ShoppingPreviewResult> OnPreview { get; set; }

    // -- Injected services -----------------------------------------------------

    [Inject] private IngredientParserService IngredientParserService { get; set; } = default!;

    // -- Inner types -----------------------------------------------------------

    /// <summary>Per-recipe options chosen in the setup dialog.</summary>
    private class RecipeShoppingSettings
    {
        /// <summary>
        /// False = plan-based: scale ingredients to exactly the total servings consumed
        /// across all selected plans over the chosen number of days
        /// (e.g. me=1.2 + wife=0.8 × 5 days = 10 servings).
        /// True  = full recipe: use the recipe exactly as written (recipe.Servings worth).
        /// </summary>
        public bool UseFullRecipe { get; set; } = false;

        /// <summary>
        /// Applies only when UseFullRecipe is true and the recipe appears in multiple selected plans.
        /// true  = buy one batch shared across all plans.
        /// false = buy one batch per plan.
        /// </summary>
        public bool ConsolidateAcrossPlans { get; set; } = true;
    }

    // -- UI state --------------------------------------------------------------

    private HashSet<string> _selectedPlanIds = new();
    private int _days = 1;
    private bool _customDays = false;
    private Dictionary<string, RecipeShoppingSettings> _recipeSettings = new();
    private bool _isBuilding = false;
    private bool _prevVisible = false;

    // -- Lifecycle -------------------------------------------------------------

    protected override void OnParametersSet()
    {
        bool becameVisible = IsVisible && !_prevVisible;
        _prevVisible = IsVisible;

        if (!becameVisible) return;

        // Reset to defaults each time the dialog opens.
        _days = 1;
        _customDays = false;
        _isBuilding = false;

        _selectedPlanIds.Clear();
        if (CurrentPlan is not null)
            _selectedPlanIds.Add(CurrentPlan.Id);

        RebuildRecipeSettings();
    }

    // -- Helpers ---------------------------------------------------------------

    private void RebuildRecipeSettings()
    {
        _recipeSettings.Clear();
        foreach (var recipe in GetUniqueRecipesAcrossAllPlans())
            _recipeSettings[recipe.Id] = new RecipeShoppingSettings();
    }

    /// <summary>Distinct recipes referenced across ALL plans (not just selected), so settings
    /// are pre-populated even before plans are checked.</summary>
    private List<Recipe> GetUniqueRecipesAcrossAllPlans()
    {
        var ids = AllPlans
            .SelectMany(p => p.Tabs)
            .SelectMany(t => t.Rows)
            .Where(r => r.RowType == DailyFoodPlanRowType.FoodEntry && r.IsRecipe && r.FoodOrRecipeId is not null)
            .Select(r => r.FoodOrRecipeId!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return ids
            .Select(id => AllRecipes.FirstOrDefault(r => r.Id == id))
            .Where(r => r is not null)
            .Select(r => r!)
            .OrderBy(r => r.Name)
            .ToList();
    }

    /// <summary>Distinct recipes referenced in the currently selected plans only.</summary>
    private List<Recipe> GetUniqueRecipesAcrossSelectedPlans()
    {
        var ids = AllPlans
            .Where(p => _selectedPlanIds.Contains(p.Id))
            .SelectMany(p => p.Tabs)
            .SelectMany(t => t.Rows)
            .Where(r => r.RowType == DailyFoodPlanRowType.FoodEntry && r.IsRecipe && r.FoodOrRecipeId is not null)
            .Select(r => r.FoodOrRecipeId!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return ids
            .Select(id => AllRecipes.FirstOrDefault(r => r.Id == id))
            .Where(r => r is not null)
            .Select(r => r!)
            .OrderBy(r => r.Name)
            .ToList();
    }

    /// <summary>How many of the selected plans contain this recipe.</summary>
    private int GetRecipeOccurrenceCount(string recipeId) =>
        AllPlans
            .Where(p => _selectedPlanIds.Contains(p.Id))
            .Count(p => p.Tabs.Any(t =>
                t.Rows.Any(r => r.IsRecipe && r.FoodOrRecipeId == recipeId)));

    /// <summary>Returns a human-friendly scale summary for display.</summary>
    private string ComputeScaleSummary(Recipe recipe)
    {
        if (!_recipeSettings.TryGetValue(recipe.Id, out var settings))
            return string.Empty;

        if (settings.UseFullRecipe)
        {
            int batches = settings.ConsolidateAcrossPlans
                ? 1
                : Math.Max(1, GetRecipeOccurrenceCount(recipe.Id));
            int servings = (recipe.Servings is > 0 ? recipe.Servings.Value : 1) * batches;
            return batches == 1
                ? $"1× recipe ({servings} servings)"
                : $"{batches}× recipe ({servings} servings)";
        }
        else
        {
            var recipeRows = AllPlans
                .Where(p => _selectedPlanIds.Contains(p.Id))
                .SelectMany(p => p.Tabs)
                .SelectMany(t => t.Rows)
                .Where(r => r.RowType == DailyFoodPlanRowType.FoodEntry
                            && r.IsRecipe
                            && r.FoodOrRecipeId == recipe.Id)
                .ToList();
            double totalServings = recipeRows.Sum(r => r.Quantity) * _days;
            double scaleFactor   = totalServings / (recipe.Servings is > 0 ? recipe.Servings.Value : 1);
            return $"{totalServings:0.##} servings ({scaleFactor:0.##}× recipe)";
        }
    }

    private double ComputeScaleFactor(Recipe recipe)
    {
        if (!_recipeSettings.TryGetValue(recipe.Id, out var settings))
            return 1;

        var recipeRows = AllPlans
            .Where(p => _selectedPlanIds.Contains(p.Id))
            .SelectMany(p => p.Tabs)
            .SelectMany(t => t.Rows)
            .Where(r => r.RowType == DailyFoodPlanRowType.FoodEntry
                        && r.IsRecipe
                        && r.FoodOrRecipeId == recipe.Id)
            .ToList();

        if (settings.UseFullRecipe)
        {
            // Full recipe: scale = 1 batch per plan (or 1 consolidated batch).
            // Does NOT multiply by _days — this gives you exactly recipe.Servings worth.
            int planCount = GetRecipeOccurrenceCount(recipe.Id);
            return settings.ConsolidateAcrossPlans ? 1.0 : Math.Max(1, planCount);
        }
        else
        {
            // Plan-based: scale to the total servings consumed across all selected plans × days.
            // e.g. me=1.2 + wife=0.8, 5 days → 10 servings; recipe.Servings=8 → scale=1.25
            double totalServingsWanted = recipeRows.Sum(r => r.Quantity) * _days;
            int servingsPerBatch = recipe.Servings is > 0 ? recipe.Servings.Value : 1;
            return totalServingsWanted / servingsPerBatch;
        }
    }

    private void TogglePlan(string planId)
    {
        if (_selectedPlanIds.Contains(planId))
        {
            // Keep at least one plan selected.
            if (_selectedPlanIds.Count > 1)
                _selectedPlanIds.Remove(planId);
        }
        else
        {
            _selectedPlanIds.Add(planId);
        }
    }

    private int GetPlanFoodCount(DailyFoodPlan plan) =>
        plan.Tabs.Sum(t => t.Rows.Count(r => r.RowType == DailyFoodPlanRowType.FoodEntry && !r.IsRecipe));

    private int GetPlanRecipeCount(DailyFoodPlan plan) =>
        plan.Tabs.Sum(t => t.Rows.Count(r => r.RowType == DailyFoodPlanRowType.FoodEntry && r.IsRecipe));

    private bool NoServingsWarning(Recipe recipe) =>
        recipe.Servings is null or <= 0;

    private void SetDays(int days)
    {
        _days = days;
        _customDays = false;
    }

    private void SetCustomDays()
    {
        _customDays = true;
    }

    // -- Actions ---------------------------------------------------------------

    private async Task Cancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task PreviewAsync()
    {
        _isBuilding = true;
        StateHasChanged();

        try
        {
            var lines = new List<string>();

            var selectedPlans = AllPlans.Where(p => _selectedPlanIds.Contains(p.Id)).ToList();

            // --- Non-recipe food rows -----------------------------------------
            foreach (var plan in selectedPlans)
            {
                foreach (var tab in plan.Tabs)
                {
                    foreach (var row in tab.Rows)
                    {
                        if (row.RowType != DailyFoodPlanRowType.FoodEntry) continue;
                        if (row.IsRecipe) continue;
                        if (string.IsNullOrEmpty(row.FoodOrRecipeId)) continue;

                        var food = AllFoods.FirstOrDefault(f => f.Id.ToString() == row.FoodOrRecipeId);
                        if (food is null) continue;

                        double scaledQty = row.Quantity * _days;
                        lines.Add(IngredientCartService.BuildLine(scaledQty, row.ServingName ?? string.Empty, food.Name));
                    }
                }
            }

            // --- Recipe rows (per unique recipe) ------------------------------
            var processedRecipeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var plan in selectedPlans)
            {
                foreach (var tab in plan.Tabs)
                {
                    foreach (var row in tab.Rows)
                    {
                        if (row.RowType != DailyFoodPlanRowType.FoodEntry) continue;
                        if (!row.IsRecipe) continue;
                        if (string.IsNullOrEmpty(row.FoodOrRecipeId)) continue;
                        if (processedRecipeIds.Contains(row.FoodOrRecipeId)) continue;

                        var recipe = AllRecipes.FirstOrDefault(r => r.Id == row.FoodOrRecipeId);
                        if (recipe is null) continue;

                        processedRecipeIds.Add(recipe.Id);

                        double scaleFactor = ComputeScaleFactor(recipe);
                        if (scaleFactor <= 0) continue;

                        var scaledLines = await BuildScaledRecipeLinesAsync(recipe, scaleFactor);
                        lines.AddRange(scaledLines);
                    }
                }
            }

            string text  = string.Join("\n", lines);
            string label = BuildLabel(selectedPlans);

            await OnPreview.InvokeAsync(new ShoppingPreviewResult(text, label));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error building shopping ingredient text: {ex.Message}");
        }
        finally
        {
            _isBuilding = false;
        }
    }

    private async Task<List<string>> BuildScaledRecipeLinesAsync(Recipe recipe, double scaleFactor)
    {
        if (string.IsNullOrWhiteSpace(recipe.Ingredients))
            return new List<string>();

        var matches = await IngredientParserService.ParseAndMatchIngredientsAsync(recipe.Ingredients);
        var result  = new List<string>();

        foreach (var match in matches)
        {
            double scaledQty = match.ParsedIngredient.Quantity * scaleFactor;
            string unit      = match.ParsedIngredient.CanonicalUnit ?? string.Empty;
            string food      = match.IsMatched
                ? match.MatchedFood!.Name
                : match.ParsedIngredient.FoodDescription;

            result.Add(IngredientCartService.BuildLine(scaledQty, unit, food));
        }

        return result;
    }

    private string BuildLabel(List<DailyFoodPlan> selectedPlans)
    {
        var names = selectedPlans.Select(p => p.Name);
        string planPart = string.Join(" + ", names);
        string dayPart  = _days == 1 ? "1 day" : $"{_days} days";
        return $"{planPart} ({dayPart})";
    }
}
