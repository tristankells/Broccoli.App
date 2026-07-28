using Broccoli.App.Shared._Shared.IngredientParsing;
using Broccoli.App.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Broccoli.App.Shared.Slices.Foods;

public partial class FoodEditDialog
{
    [Inject] private IFoodService FoodService { get; set; } = default!;
    [Inject] private IUsdaFoodSearchService UsdaService { get; set; } = default!;

    /// <summary>Food to edit. Null means create mode.</summary>
    [Parameter] public Food? Food { get; set; }

    /// <summary>Pre-fills Name when in create mode.</summary>
    [Parameter] public string? SuggestedName { get; set; }

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<Food> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    // ── Working copy ──────────────────────────────────────────────────────────
    private Food _editFood = new();
    private bool _isCreateMode;

    // ── Validation ────────────────────────────────────────────────────────────
    private bool _nameError;

    // ── Save state ────────────────────────────────────────────────────────────
    private bool _saving;
    private string? _saveError;

    // ── USDA inline search ────────────────────────────────────────────────────
    private string _usdaQuery = string.Empty;
    private UsdaSearchResult? _usdaResults;
    private bool _usdaSearching;
    private string? _usdaError;
    private int _usdaPage = 1;
    private const int UsdaPageSize = 8;
    private UsdaFoodItem? _appliedUsdaItem;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void OnParametersSet()
    {
        if (!IsOpen) return;

        _isCreateMode = Food is null;
        _editFood = _isCreateMode
            ? new Food { Name = Capitalise(SuggestedName ?? string.Empty), Measure = "100g", GramsPerMeasure = 100 }
            : CloneFood(Food!);

        _nameError  = false;
        _saveError  = null;
        _saving     = false;
        _usdaQuery  = SuggestedName ?? Food?.Name ?? string.Empty;
        _usdaResults = null;
        _usdaError   = null;
        _usdaPage    = 1;
        _appliedUsdaItem = null;
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    private async Task SaveAsync()
    {
        if (!Validate()) return;

        _saving = true;
        _saveError = null;
        try
        {
            Food saved;
            if (_isCreateMode)
                saved = await FoodService.AddAsync(_editFood);
            else
            {
                await FoodService.UpdateAsync(_editFood);
                saved = _editFood;
            }
            await OnSaved.InvokeAsync(saved);
        }
        catch (Exception ex)
        {
            _saveError = ex.Message;
        }
        finally
        {
            _saving = false;
        }
    }

    private bool Validate()
    {
        _nameError = string.IsNullOrWhiteSpace(_editFood.Name);
        // Apply default measure values if left blank
        if (string.IsNullOrWhiteSpace(_editFood.Measure)) _editFood.Measure = "100g";
        if (_editFood.GramsPerMeasure <= 0) _editFood.GramsPerMeasure = 100;
        return !_nameError;
    }

    private async Task Cancel()
    {
        await OnCancelled.InvokeAsync();
    }

    // ── USDA search ───────────────────────────────────────────────────────────

    private async Task UsdaSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(_usdaQuery)) return;
        _usdaSearching = true;
        _usdaError = null;
        _usdaPage = 1;
        try
        {
            _usdaResults = await UsdaService.SearchAsync(_usdaQuery, _usdaPage, UsdaPageSize);
        }
        catch (Exception ex)
        {
            _usdaError = $"Search failed: {ex.Message}";
            _usdaResults = null;
        }
        finally
        {
            _usdaSearching = false;
        }
    }

    private async Task UsdaGoToPageAsync(int page)
    {
        if (_usdaResults == null || page < 1 || page > _usdaResults.TotalPages) return;
        _usdaSearching = true;
        try
        {
            _usdaResults = await UsdaService.SearchAsync(_usdaQuery, page, UsdaPageSize);
            _usdaPage = page;
        }
        catch (Exception ex)
        {
            _usdaError = $"Search failed: {ex.Message}";
        }
        finally
        {
            _usdaSearching = false;
        }
    }

    private async Task HandleUsdaKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await UsdaSearchAsync();
    }

    private void ApplyUsdaItem(UsdaFoodItem item)
    {
        _editFood.CaloriesPer100g      = item.Calories;
        _editFood.ProteinPer100g       = item.Protein;
        _editFood.CarbohydratesPer100g = item.Carbohydrates;
        _editFood.FatPer100g           = item.Fat;
        _editFood.SaturatedFatPer100g  = item.SaturatedFat;
        _editFood.DietaryFiberPer100g  = item.DietaryFiber;
        _editFood.SugarsPer100g        = item.Sugars;
        _editFood.SodiumMgPer100g      = item.SodiumMg;

        // Capitalise the first letter of the existing name; never replace it with the USDA name.
        if (!string.IsNullOrWhiteSpace(_editFood.Name))
            _editFood.Name = Capitalise(_editFood.Name);

        // Remember which USDA item was applied so the user can optionally adopt its name.
        _appliedUsdaItem = item;
    }

    private void UseUsdaName()
    {
        if (_appliedUsdaItem != null)
            _editFood.Name = _appliedUsdaItem.Description;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Capitalise(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static Food CloneFood(Food src) => new()
    {
        Id                    = src.Id,
        Name                  = src.Name,
        Measure               = src.Measure,
        GramsPerMeasure       = src.GramsPerMeasure,
        Notes                 = src.Notes,
        CaloriesPer100g       = src.CaloriesPer100g,
        FatPer100g            = src.FatPer100g,
        SaturatedFatPer100g   = src.SaturatedFatPer100g,
        CarbohydratesPer100g  = src.CarbohydratesPer100g,
        DietaryFiberPer100g   = src.DietaryFiberPer100g,
        SugarsPer100g         = src.SugarsPer100g,
        ProteinPer100g        = src.ProteinPer100g,
        SodiumMgPer100g       = src.SodiumMgPer100g,
    };
}
