using Broccoli.App.Shared.Services;
using Broccoli.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Broccoli.App.Shared.Components;

public partial class UsdaSearchDialog
{
    [Inject] private IUsdaFoodSearchService UsdaService { get; set; } = default!;

    [Parameter] public bool IsVisible   { get; set; }
    [Parameter] public EventCallback<List<Food>> OnImport { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private string _query = string.Empty;
    private UsdaSearchResult? _result;
    private readonly HashSet<int> _selectedIds = new();
    private bool _isSearching;
    private string? _error;
    private int _currentPage = 1;
    private const int PageSize = 10;

    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(_query)) return;
        _isSearching = true;
        _error = null;
        _selectedIds.Clear();
        _currentPage = 1;
        try
        {
            _result = await UsdaService.SearchAsync(_query, _currentPage, PageSize);
        }
        catch (Exception ex)
        {
            _error = $"Search failed: {ex.Message}";
            _result = null;
        }
        finally
        {
            _isSearching = false;
        }
    }

    private async Task GoToPage(int page)
    {
        if (_result == null || page < 1 || page > _result.TotalPages) return;
        _isSearching = true;
        _selectedIds.Clear();
        try
        {
            _result = await UsdaService.SearchAsync(_query, page, PageSize);
            _currentPage = page;
        }
        catch (Exception ex)
        {
            _error = $"Search failed: {ex.Message}";
        }
        finally
        {
            _isSearching = false;
        }
    }

    private void ToggleSelection(int fdcId, bool selected)
    {
        if (selected) _selectedIds.Add(fdcId);
        else _selectedIds.Remove(fdcId);
    }

    private async Task Import()
    {
        if (_result == null) return;

        var foods = _result.Foods
            .Where(f => _selectedIds.Contains(f.FdcId))
            .Select(f => new Food
            {
                Id                  = 0,
                Name                = f.Description,
                Measure             = "100g",
                GramsPerMeasure     = 100.0,
                Notes               = $"Imported from USDA FDC (fdcId: {f.FdcId})",
                CaloriesPer100g     = f.Calories,
                FatPer100g          = f.Fat,
                SaturatedFatPer100g = f.SaturatedFat,
                CarbohydratesPer100g = f.Carbohydrates,
                DietaryFiberPer100g = f.DietaryFiber,
                SugarsPer100g       = f.Sugars,
                ProteinPer100g      = f.Protein,
                SodiumMgPer100g     = f.SodiumMg
            })
            .ToList();

        await OnImport.InvokeAsync(foods);
        Reset();
    }

    private async Task Cancel()
    {
        Reset();
        await OnCancel.InvokeAsync();
    }

    private void Reset()
    {
        _query = string.Empty;
        _result = null;
        _selectedIds.Clear();
        _error = null;
        _currentPage = 1;
    }

    private async Task HandleSearchKeyPress(KeyboardEventArgs args)
    {
        if (args.Key == "Enter") await Search();
    }
}

