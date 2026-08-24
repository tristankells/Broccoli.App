using System.Collections.ObjectModel;
using System.Text.Json;
using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Settings;

public partial class FoodDatabaseViewModel : ViewModelBase
{
    private readonly IFoodService _foodService;
    private readonly IFoodFileService? _fileService;
    private readonly IUsdaFoodSearchService? _usdaService;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _usdaQuery = string.Empty;

    [ObservableProperty]
    private bool _isUsdaSearchOpen;

    [ObservableProperty]
    private UsdaSearchResult? _usdaResult;

    [ObservableProperty]
    private bool _isUsdaSearching;

    [ObservableProperty]
    private int _usdaPage = 1;

    [ObservableProperty]
    private bool _isConfirmingReset;

    public FoodDatabaseViewModel()
        : this(
        new FoodService(), null, null)
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

    public ObservableCollection<Food> Foods { get; } = new();

    public ObservableCollection<Food> FilteredFoods { get; } = new();

    public bool IsUsdaResultVisible => UsdaResult?.Foods.Count > 0;

    public void Load()
    {
        ErrorMessage = null;
        try
        {
            var foods = _foodService.GetAll().OrderBy(f => f.Id).ToList();
            Foods.Clear();
            foreach (Food? f in foods)
            {
                Foods.Add(f);
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load: {ex.Message}";
        }
    }

    private void ApplyFilter()
    {
        string filter = SearchText.Trim();

        FilteredFoods.Clear();
        if (string.IsNullOrEmpty(filter))
        {
            foreach (Food food in Foods)
            {
                FilteredFoods.Add(food);
            }

            return;
        }

        foreach (Food food in Foods)
        {
            if (food.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                FilteredFoods.Add(food);
            }
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void AddFood()
    {
        OpenEditDialog(new Food { Name = string.Empty, Measure = "100g", GramsPerMeasure = 100 }, isNew: true);
    }

    [RelayCommand]
    private void StartEdit(Food food)
    {
        OpenEditDialog(CloneForEdit(food), isNew: false);
    }

    private void OpenEditDialog(Food food, bool isNew)
    {
        var dialogViewModel = new FoodEditDialogViewModel();
        dialogViewModel.Open(food, isNew, saved =>
        {
            try
            {
                if (isNew)
                {
                    Food added = _foodService.Add(saved);
                    Foods.Add(added);
                }
                else
                {
                    _foodService.Update(saved);
                    int idx = Foods.IndexOf(Foods.First(f => f.Id == saved.Id));
                    if (idx >= 0)
                    {
                        Foods[idx] = saved;
                    }
                }

                ApplyFilter();
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to save: {ex.Message}";
            }
        });

        var dialog = new FoodEditDialog { DataContext = dialogViewModel };
        dialog.Show();
    }

    private static Food CloneForEdit(Food food) => new()
    {
        Id = food.Id,
        Name = food.Name,
        Measure = food.Measure,
        GramsPerMeasure = food.GramsPerMeasure,
        IsCustom = food.IsCustom,
        CaloriesPer100g = food.CaloriesPer100g,
        FatPer100g = food.FatPer100g,
        ProteinPer100g = food.ProteinPer100g,
        CarbohydratesPer100g = food.CarbohydratesPer100g,
        SaturatedFatPer100g = food.SaturatedFatPer100g,
        DietaryFiberPer100g = food.DietaryFiberPer100g,
        SugarsPer100g = food.SugarsPer100g,
        SodiumMgPer100g = food.SodiumMgPer100g,
        Notes = food.Notes,
    };

    [RelayCommand]
    private void DeleteFood(Food food)
    {
        try
        {
            _foodService.Delete(food.Id);
            Foods.Remove(food);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RequestReset() => IsConfirmingReset = true;

    [RelayCommand]
    private void CancelReset() => IsConfirmingReset = false;

    [RelayCommand]
    private void ConfirmReset()
    {
        try
        {
            _foodService.ResetToSeed();
            Load();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Reset failed: {ex.Message}";
        }
        finally
        {
            IsConfirmingReset = false;
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (_fileService is null)
        {
            return;
        }

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Foods.OrderBy(f => f.Id), options);
            await _fileService.ExportFoodsAsync("foods-export.json", json);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (_fileService is null)
        {
            return;
        }

        try
        {
            string? json = await _fileService.ImportFoodsAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            List<Food>? incoming = JsonSerializer.Deserialize<List<Food>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (incoming == null || incoming.Count == 0)
            {
                return;
            }

            int added = 0, updated = 0;
            foreach (Food? food in incoming.Where(f => !string.IsNullOrWhiteSpace(f.Name)))
            {
                FoodMatchResult match = _foodService.FindBestMatch(food.Name);
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
        catch (Exception ex)
        {
            ErrorMessage = $"Import failed: {ex.Message}";
        }
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
        if (_usdaService is not { IsAvailable: true } || string.IsNullOrWhiteSpace(UsdaQuery))
        {
            return;
        }

        IsUsdaSearching = true;
        UsdaPage = 1;
        try
        {
            UsdaResult = await _usdaService.SearchAsync(UsdaQuery, 1, 10);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"USDA search failed: {ex.Message}";
        }
        finally
        {
            IsUsdaSearching = false;
        }
    }

    [RelayCommand]
    private async Task UsdaNextPageAsync()
    {
        if (_usdaService is not { IsAvailable: true } || UsdaResult is null)
        {
            return;
        }

        int nextPage = UsdaPage + 1;
        if (nextPage > UsdaResult.TotalPages)
        {
            return;
        }

        IsUsdaSearching = true;
        try
        {
            UsdaResult = await _usdaService.SearchAsync(UsdaQuery, nextPage, 10);
            UsdaPage = nextPage;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"USDA search failed: {ex.Message}";
        }
        finally
        {
            IsUsdaSearching = false;
        }
    }

    [RelayCommand]
    private async Task UsdaPrevPageAsync()
    {
        if (_usdaService is not { IsAvailable: true } || UsdaResult is null)
        {
            return;
        }

        int prevPage = UsdaPage - 1;
        if (prevPage < 1)
        {
            return;
        }

        IsUsdaSearching = true;
        try
        {
            UsdaResult = await _usdaService.SearchAsync(UsdaQuery, prevPage, 10);
            UsdaPage = prevPage;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"USDA search failed: {ex.Message}";
        }
        finally
        {
            IsUsdaSearching = false;
        }
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
            SodiumMgPer100g = item.SodiumMg,
        };

        try
        {
            Food added = _foodService.Add(food);
            Foods.Add(added);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to import: {ex.Message}";
        }
    }

    partial void OnUsdaResultChanged(UsdaSearchResult? value) => OnPropertyChanged(nameof(IsUsdaResultVisible));
}
