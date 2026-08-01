using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Recipes;
public partial class RecipeDetailViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;
    private readonly IngredientParserService? _parser;

    public Action? BackRequested { get; set; }
    public Action<Recipe>? EditRequested { get; set; }
    public Action? RecipeDeleted { get; set; }

    [ObservableProperty] private Recipe _recipe;
    [ObservableProperty] private bool _isConfirmingDelete;

    public ObservableCollection<string> ImagePaths { get; } = new();

    public double TotalCalories { get; private set; }
    public double TotalProteinG { get; private set; }
    public double TotalCarbsG { get; private set; }
    public double TotalFatG { get; private set; }

    public double PerServingCalories => Recipe.Servings > 0 ? TotalCalories / Recipe.Servings.Value : 0;
    public double PerServingProteinG => Recipe.Servings > 0 ? TotalProteinG / Recipe.Servings.Value : 0;
    public double PerServingCarbsG => Recipe.Servings > 0 ? TotalCarbsG / Recipe.Servings.Value : 0;
    public double PerServingFatG => Recipe.Servings > 0 ? TotalFatG / Recipe.Servings.Value : 0;

    public RecipeDetailViewModel(IRecipeService recipeService, Recipe recipe)
        : this(recipeService, null, recipe)
    {
    }

    public RecipeDetailViewModel(IRecipeService recipeService, IngredientParserService? parser, Recipe recipe)
    {
        _recipeService = recipeService;
        _parser = parser;
        _recipe = recipe;
        foreach (var image in recipe.Images)
            ImagePaths.Add(recipeService.GetImagePath(recipe.Id, image));
        ParseIngredients();
    }

    private void ParseIngredients()
    {
        if (_parser is null || string.IsNullOrWhiteSpace(Recipe.Ingredients)) return;
        var matches = _parser.ParseAndMatchIngredients(Recipe.Ingredients);

        double cal = 0, pro = 0, carb = 0, fat = 0;
        foreach (var m in matches)
        {
            if (m.IsMatched)
            {
                cal  += m.GetCalories();
                pro  += m.GetProtein();
                carb += m.GetCarbohydrates();
                fat  += m.GetFat();
            }
        }
        TotalCalories = cal;
        TotalProteinG = pro;
        TotalCarbsG = carb;
        TotalFatG = fat;

        OnPropertyChanged(nameof(TotalCalories));
        OnPropertyChanged(nameof(TotalProteinG));
        OnPropertyChanged(nameof(TotalCarbsG));
        OnPropertyChanged(nameof(TotalFatG));
        OnPropertyChanged(nameof(PerServingCalories));
        OnPropertyChanged(nameof(PerServingProteinG));
        OnPropertyChanged(nameof(PerServingCarbsG));
        OnPropertyChanged(nameof(PerServingFatG));
    }

    [RelayCommand] private void Back() => BackRequested?.Invoke();
    [RelayCommand] private void Edit() => EditRequested?.Invoke(Recipe);
    [RelayCommand] private void RequestDelete() => IsConfirmingDelete = true;
    [RelayCommand] private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand]
    private void ConfirmDelete()
    {
        _recipeService.Delete(Recipe.Id);
        IsConfirmingDelete = false;
        RecipeDeleted?.Invoke();
    }
}
