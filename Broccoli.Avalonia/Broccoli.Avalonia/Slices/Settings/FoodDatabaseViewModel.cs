using System.Text.Json;
using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Settings;

public partial class FoodDatabaseViewModel : ViewModelBase
{
    private readonly IFoodService _foodService;
    private readonly IFoodFileService? _fileService;
    private readonly IUsdaFoodSearchService? _usdaService;

    public ObservableCollection<Food> Foods { get; } = new();

    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private Food? _editingFood;
    [ObservableProperty] private Food? _newFood;

    public bool IsAddFormVisible => NewFood is not null;
    public bool IsEditFormVisible => EditingFood is not null;
    [ObservableProperty] private string _usdaQuery = string.Empty;
    [ObservableProperty] private bool _isUsdaSearchOpen;
    [ObservableProperty] private UsdaSearchResult? _usdaResult;
    [ObservableProperty] private bool _isUsdaSearching;
    [ObservableProperty] private int _usdaPage = 1;

    public bool IsUsdaResultVisible => UsdaResult?.Foods.Count > 0;

    public FoodDatabaseViewModel() : this(
        new LocalJsonFoodService(), null, null)
    {
    }

    public FoodDatabaseViewModel(
        IFoodService foodService,
        IFoodFileService? fileService = null,
        IUsdaFoodSearchService? usdaService = null)
    {
        _foodService = foodService;
        _fileService = fileService;
        _usdaService = usdaService;
        Load();
    }

    public void Load()
    {
        ErrorMessage = null;
        try
        {
            var foods = _foodService.GetAll().OrderBy(f => f.Id).ToList();
            Foods.Clear();
            foreach (var f in foods) Foods.Add(f);
        }
        catch (Exception ex) { ErrorMessage = $"Failed to load: {ex.Message}"; }
    }

    [RelayCommand]
    private void AddFood()
    {
        NewFood = new Food { Name = "", Measure = "100g", GramsPerMeasure = 100 };
    }

    [RelayCommand]
    private void SaveNewFood()
    {
        if (NewFood is null || string.IsNullOrWhiteSpace(NewFood.Name)) return;
        try
        {
            var added = _foodService.Add(NewFood);
            Foods.Add(added);
            NewFood = null;
        }
        catch (Exception ex) { ErrorMessage = $"Failed to add: {ex.Message}"; }
    }

    [RelayCommand]
    private void CancelNewFood()
    {
        NewFood = null;
    }

    [RelayCommand]
    private void StartEdit(Food food)
    {
        EditingFood = new Food
        {
            Id = food.Id,
            Name = food.Name,
            Measure = food.Measure,
            GramsPerMeasure = food.GramsPerMeasure,
            CaloriesPer100g = food.CaloriesPer100g,
            FatPer100g = food.FatPer100g,
            ProteinPer100g = food.ProteinPer100g,
            CarbohydratesPer100g = food.CarbohydratesPer100g,
            SaturatedFatPer100g = food.SaturatedFatPer100g,
            DietaryFiberPer100g = food.DietaryFiberPer100g,
            SugarsPer100g = food.SugarsPer100g,
            SodiumMgPer100g = food.SodiumMgPer100g,
            Notes = food.Notes
        };
    }

    [RelayCommand]
    private void SaveEdit()
    {
        if (EditingFood is null || string.IsNullOrWhiteSpace(EditingFood.Name)) return;
        try
        {
            _foodService.Update(EditingFood);
            var idx = Foods.IndexOf(Foods.First(f => f.Id == EditingFood.Id));
            if (idx >= 0) Foods[idx] = EditingFood;
            EditingFood = null;
        }
        catch (Exception ex) { ErrorMessage = $"Failed to save: {ex.Message}"; }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        EditingFood = null;
    }

    [RelayCommand]
    private void DeleteFood(Food food)
    {
        try
        {
            _foodService.Delete(food.Id);
            Foods.Remove(food);
        }
        catch (Exception ex) { ErrorMessage = $"Failed to delete: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (_fileService is null) return;
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Foods.OrderBy(f => f.Id), options);
            await _fileService.ExportFoodsAsync("foods-export.json", json);
        }
        catch (Exception ex) { ErrorMessage = $"Export failed: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (_fileService is null) return;
        try
        {
            string? json = await _fileService.ImportFoodsAsync();
            if (string.IsNullOrWhiteSpace(json)) return;

            var incoming = JsonSerializer.Deserialize<List<Food>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (incoming == null || incoming.Count == 0) return;

            int added = 0, updated = 0;
            foreach (var food in incoming.Where(f => !string.IsNullOrWhiteSpace(f.Name)))
            {
                var match = _foodService.FindBestMatch(food.Name);
                if (match.IsMatch && match.Score >= 0.7)
                {
                    food.Id = match.Food!.Id;
                    _foodService.Update(food);
                    updated++;
                }
                else
                {
                    _foodService.Add(food);
                    added++;
                }
            }

            ErrorMessage = $"Imported: {added} added, {updated} updated.";
            Load();
        }
        catch (Exception ex) { ErrorMessage = $"Import failed: {ex.Message}"; }
    }

    [RelayCommand]
    private void OpenUsdaSearch()
    {
        IsUsdaSearchOpen = true;
        UsdaQuery = string.Empty;
        UsdaResult = null;
    }

    [RelayCommand]
    private void CloseUsdaSearch()
    {
        IsUsdaSearchOpen = false;
    }

    [RelayCommand]
    private async Task SearchUsdaAsync()
    {
        if (_usdaService is not { IsAvailable: true } || string.IsNullOrWhiteSpace(UsdaQuery)) return;
        IsUsdaSearching = true;
        UsdaPage = 1;
        try
        {
            UsdaResult = await _usdaService.SearchAsync(UsdaQuery, 1, 10);
        }
        catch (Exception ex) { ErrorMessage = $"USDA search failed: {ex.Message}"; }
        finally { IsUsdaSearching = false; }
    }

    [RelayCommand]
    private async Task UsdaNextPageAsync()
    {
        if (_usdaService is not { IsAvailable: true } || UsdaResult is null) return;
        int nextPage = UsdaPage + 1;
        if (nextPage > UsdaResult.TotalPages) return;
        IsUsdaSearching = true;
        try
        {
            UsdaResult = await _usdaService.SearchAsync(UsdaQuery, nextPage, 10);
            UsdaPage = nextPage;
        }
        catch (Exception ex) { ErrorMessage = $"USDA search failed: {ex.Message}"; }
        finally { IsUsdaSearching = false; }
    }

    [RelayCommand]
    private async Task UsdaPrevPageAsync()
    {
        if (_usdaService is not { IsAvailable: true } || UsdaResult is null) return;
        int prevPage = UsdaPage - 1;
        if (prevPage < 1) return;
        IsUsdaSearching = true;
        try
        {
            UsdaResult = await _usdaService.SearchAsync(UsdaQuery, prevPage, 10);
            UsdaPage = prevPage;
        }
        catch (Exception ex) { ErrorMessage = $"USDA search failed: {ex.Message}"; }
        finally { IsUsdaSearching = false; }
    }

    [RelayCommand]
    private async Task ImportUsdaFoodAsync(UsdaFoodItem item)
    {
        var food = new Food
        {
            Id = 0,
            Name = item.Description,
            Measure = "100g",
            GramsPerMeasure = 100.0,
            Notes = $"Imported from USDA FDC (fdcId: {item.FdcId})",
            CaloriesPer100g = item.Calories,
            FatPer100g = item.Fat,
            SaturatedFatPer100g = item.SaturatedFat,
            CarbohydratesPer100g = item.Carbohydrates,
            DietaryFiberPer100g = item.DietaryFiber,
            SugarsPer100g = item.Sugars,
            ProteinPer100g = item.Protein,
            SodiumMgPer100g = item.SodiumMg
        };

        try
        {
            var added = _foodService.Add(food);
            Foods.Add(added);
        }
        catch (Exception ex) { ErrorMessage = $"Failed to import: {ex.Message}"; }
    }

    partial void OnNewFoodChanged(Food? value) => OnPropertyChanged(nameof(IsAddFormVisible));
    partial void OnEditingFoodChanged(Food? value) => OnPropertyChanged(nameof(IsEditFormVisible));
    partial void OnUsdaResultChanged(UsdaSearchResult? value) => OnPropertyChanged(nameof(IsUsdaResultVisible));
}
