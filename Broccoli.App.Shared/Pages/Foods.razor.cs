using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Pages;

public partial class Foods
{
    // ── State ────────────────────────────────────────────────────────────────

    private List<Food>? _foods;
    private Food?       _editingFood;
    private Food?       _deletingFood;
    private bool        _usdaDialogVisible;
    private Food        _newFood = NewEmptyFood();

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        _foods = await LoadFoodsAsync();
    }

    // ── Edit ─────────────────────────────────────────────────────────────────

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

    // ── Delete ───────────────────────────────────────────────────────────────

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

    // ── Add ──────────────────────────────────────────────────────────────────

    private async Task AddFood()
    {
        if (string.IsNullOrWhiteSpace(_newFood.Name)) return;
        await FoodService.AddAsync(_newFood);
        _newFood = NewEmptyFood();
        _foods   = await LoadFoodsAsync();
    }

    // ── USDA import ──────────────────────────────────────────────────────────

    private async Task OnUsdaImport(List<Food> foods)
    {
        foreach (var food in foods)
        {
            await FoodService.AddAsync(food);
        }
        _usdaDialogVisible = false;
        _foods = await LoadFoodsAsync();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

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