using System.Text.Json;
using Broccoli.App.Shared._Shared.IngredientParsing;
using Broccoli.App.Shared._Shared.Platform;
using Broccoli.App.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace Broccoli.App.Shared.Slices.Foods;

public partial class Foods
{
    [Inject] private IFoodService FoodService { get; set; } = default!;
    [Inject] private IFoodFileService FoodFileService { get; set; } = default!;

    // -- State ----------------------------------------------------------------

    private List<Food>? _foods;
    private Food?       _editingFood;
    private Food?       _deletingFood;
    private bool        _usdaDialogVisible;
    private Food        _newFood = NewEmptyFood();

    // -- FoodEditDialog (standalone Add Food button) --------------------------
    private bool    _foodDialogOpen;
    private Food?   _foodDialogFood;
    private string? _foodDialogSuggestedName;

    // -- Import/Export --------------------------------------------------------
    private bool                         _importBusy;
    private bool                         _importDialogOpen;
    private List<ImportFoodPreviewItem>  _importItems = new();

    // -- Lifecycle ------------------------------------------------------------

    protected override async Task OnInitializedAsync()
    {
        _foods = await LoadFoodsAsync();
    }

    // -- FoodEditDialog -------------------------------------------------------

    private void OpenAddFoodDialog()
    {
        _foodDialogFood         = null;
        _foodDialogSuggestedName = null;
        _foodDialogOpen         = true;
    }

    private async Task HandleFoodDialogSaved(Food _)
    {
        _foodDialogOpen = false;
        _foodDialogFood = null;
        _foods = await LoadFoodsAsync();
    }

    // -- Edit -----------------------------------------------------------------

    private void StartEdit(Food food)
    {
        _deletingFood = null;
        _editingFood  = CloneFood(food);
    }

    private async Task SaveEdit()
    {
        if (_editingFood == null) return;
        await FoodService.UpdateAsync(_editingFood);
        _editingFood = null;
        _foods = await LoadFoodsAsync();
    }

    private void CancelEdit() => _editingFood = null;

    // -- Delete ---------------------------------------------------------------

    private void StartDelete(Food food)
    {
        _editingFood  = null;
        _deletingFood = food;
    }

    private async Task ConfirmDelete()
    {
        if (_deletingFood == null) return;
        await FoodService.DeleteAsync(_deletingFood.Id);
        _deletingFood = null;
        _foods = await LoadFoodsAsync();
    }

    private void CancelDelete() => _deletingFood = null;

    // -- Add (inline row) -----------------------------------------------------

    private async Task AddFood()
    {
        if (string.IsNullOrWhiteSpace(_newFood.Name)) return;
        await FoodService.AddAsync(_newFood);
        _newFood = NewEmptyFood();
        _foods   = await LoadFoodsAsync();
    }

    // -- USDA import ----------------------------------------------------------

    private async Task OnUsdaImport(List<Food> foods)
    {
        foreach (var food in foods)
        {
            await FoodService.AddAsync(food);
        }
        _usdaDialogVisible = false;
        _foods = await LoadFoodsAsync();
    }

    // -- Export ---------------------------------------------------------------

    private async Task ExportAsync()
    {
        var all = await FoodService.GetAllAsync();
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(all, options);
        await FoodFileService.ExportFoodsAsync("foods-export.json", json);
    }

    // -- Import ---------------------------------------------------------------

    private async Task ImportAsync()
    {
        _importBusy = true;
        try
        {
            string? json = await FoodFileService.ImportFoodsAsync();
            if (string.IsNullOrWhiteSpace(json)) return;

            List<Food>? incoming;
            try
            {
                incoming = JsonSerializer.Deserialize<List<Food>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return; // invalid JSON — silently ignore (could add a toast here)
            }

            if (incoming == null || incoming.Count == 0) return;

            var preview = new List<ImportFoodPreviewItem>();
            const double updateThreshold = 0.7;

            foreach (var food in incoming.Where(f => !string.IsNullOrWhiteSpace(f.Name)))
            {
                var match = FoodService.FindBestMatch(food.Name);
                if (match.IsMatch && match.Score >= updateThreshold)
                {
                    var changed = DetectChangedFields(food, match.Food!);
                    if (changed.Count > 0) // only add if there's actually something new
                    {
                        preview.Add(new ImportFoodPreviewItem
                        {
                            Incoming      = food,
                            Matched       = match.Food,
                            MatchScore    = match.Score,
                            ChangedFields = changed
                        });
                    }
                }
                else
                {
                    preview.Add(new ImportFoodPreviewItem { Incoming = food });
                }
            }

            if (preview.Count == 0) return;

            _importItems      = preview;
            _importDialogOpen = true;
        }
        finally
        {
            _importBusy = false;
        }
    }

    private async Task HandleImportConfirmed(List<Food> confirmed)
    {
        _importDialogOpen = false;

        // Map back each confirmed food to whether it's new or an update
        foreach (var food in confirmed)
        {
            var previewItem = _importItems.FirstOrDefault(i => i.Incoming == food);
            if (previewItem == null) continue;

            if (previewItem.IsNew)
            {
                await FoodService.AddAsync(food);
            }
            else
            {
                food.Id = previewItem.Matched!.Id;
                await FoodService.UpdateAsync(food);
            }
        }

        _importItems = new();
        _foods = await LoadFoodsAsync();
    }

    // -- Helpers --------------------------------------------------------------

    private static List<string> DetectChangedFields(Food incoming, Food existing)
    {
        var changed = new List<string>();
        if (!string.Equals(incoming.Measure, existing.Measure, StringComparison.OrdinalIgnoreCase)) changed.Add("Measure");
        if (Math.Abs(incoming.GramsPerMeasure      - existing.GramsPerMeasure)      > 0.001) changed.Add("g/Measure");
        if (Math.Abs(incoming.CaloriesPer100g      - existing.CaloriesPer100g)      > 0.001) changed.Add("Calories");
        if (Math.Abs(incoming.FatPer100g           - existing.FatPer100g)           > 0.001) changed.Add("Fat");
        if (Math.Abs(incoming.SaturatedFatPer100g  - existing.SaturatedFatPer100g)  > 0.001) changed.Add("SatFat");
        if (Math.Abs(incoming.CarbohydratesPer100g - existing.CarbohydratesPer100g) > 0.001) changed.Add("Carbs");
        if (Math.Abs(incoming.DietaryFiberPer100g  - existing.DietaryFiberPer100g)  > 0.001) changed.Add("Fiber");
        if (Math.Abs(incoming.SugarsPer100g        - existing.SugarsPer100g)        > 0.001) changed.Add("Sugars");
        if (Math.Abs(incoming.ProteinPer100g       - existing.ProteinPer100g)       > 0.001) changed.Add("Protein");
        if (Math.Abs(incoming.SodiumMgPer100g      - existing.SodiumMgPer100g)      > 0.001) changed.Add("Sodium");
        return changed;
    }

    private async Task<List<Food>> LoadFoodsAsync() =>
        (await FoodService.GetAllAsync()).OrderBy(f => f.Id).ToList();

    private static Food CloneFood(Food src) => new()
    {
        Id                   = src.Id,
        Name                 = src.Name,
        Measure              = src.Measure,
        GramsPerMeasure      = src.GramsPerMeasure,
        Notes                = src.Notes,
        CaloriesPer100g      = src.CaloriesPer100g,
        FatPer100g           = src.FatPer100g,
        SaturatedFatPer100g  = src.SaturatedFatPer100g,
        CarbohydratesPer100g = src.CarbohydratesPer100g,
        DietaryFiberPer100g  = src.DietaryFiberPer100g,
        SugarsPer100g        = src.SugarsPer100g,
        ProteinPer100g       = src.ProteinPer100g,
        SodiumMgPer100g      = src.SodiumMgPer100g
    };

    private static Food NewEmptyFood() => new()
    {
        Name    = string.Empty,
        Measure = string.Empty,
        Notes   = string.Empty
    };
}