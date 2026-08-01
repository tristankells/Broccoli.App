using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Planning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Recipes;
public partial class RecipeDetailViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;
    private readonly IngredientParserService? _parser;
    private readonly IMacroTargetService? _macroService;

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

    public bool IsComparisonEnabled { get; private set; }
    public string? ComparisonPersonName { get; private set; }

    public double MealTargetCalories { get; private set; }
    public double MealTargetProteinG { get; private set; }
    public double MealTargetCarbsG { get; private set; }
    public double MealTargetFatG { get; private set; }

    public double CalDelta => PerServingCalories - MealTargetCalories;
    public double ProteinDelta => PerServingProteinG - MealTargetProteinG;
    public double CarbsDelta => PerServingCarbsG - MealTargetCarbsG;
    public double FatDelta => PerServingFatG - MealTargetFatG;

    public double CalDeviationPct => MealTargetCalories > 0 ? Math.Abs(CalDelta) / MealTargetCalories * 100 : 0;
    public double ProteinDeviationPct => MealTargetProteinG > 0 ? Math.Abs(ProteinDelta) / MealTargetProteinG * 100 : 0;
    public double CarbsDeviationPct => MealTargetCarbsG > 0 ? Math.Abs(CarbsDelta) / MealTargetCarbsG * 100 : 0;
    public double FatDeviationPct => MealTargetFatG > 0 ? Math.Abs(FatDelta) / MealTargetFatG * 100 : 0;

    public string CalColor => DeviationColor(CalDeviationPct);
    public string ProteinColor => DeviationColor(ProteinDeviationPct);
    public string CarbsColor => DeviationColor(CarbsDeviationPct);
    public string FatColor => DeviationColor(FatDeviationPct);

    private static string DeviationColor(double pct) => pct <= 15 ? "#2ECC71" : pct <= 25 ? "#F39C12" : "#E74C3C";

    public RecipeDetailViewModel(IRecipeService recipeService, Recipe recipe)
        : this(recipeService, null, null, recipe)
    {
    }

    public RecipeDetailViewModel(IRecipeService recipeService, IngredientParserService? parser, Recipe recipe)
        : this(recipeService, parser, null, recipe)
    {
    }

    public RecipeDetailViewModel(IRecipeService recipeService, IngredientParserService? parser,
        IMacroTargetService? macroService, Recipe recipe)
    {
        _recipeService = recipeService;
        _parser = parser;
        _macroService = macroService;
        _recipe = recipe;
        foreach (var image in recipe.Images)
            ImagePaths.Add(recipeService.GetImagePath(recipe.Id, image));
        ParseIngredients();
        LoadMacroComparison();
    }

    private void LoadMacroComparison()
    {
        if (_macroService is null) return;
        try
        {
            var settings = _macroService.GetSettings();
            if (!settings.RecipeMealComparisonEnabled || string.IsNullOrWhiteSpace(settings.RecipeMealComparisonPersonId))
                return;

            var targets = _macroService.GetAll();
            var chosen = targets.FirstOrDefault(t => t.Id == settings.RecipeMealComparisonPersonId);
            if (chosen is null) return;

            IsComparisonEnabled = true;
            ComparisonPersonName = chosen.Name;
            MealTargetCalories = chosen.RecommendedCalories / 3.0;
            MealTargetProteinG = chosen.RecommendedProteinG / 3.0;
            MealTargetCarbsG = chosen.RecommendedCarbsG / 3.0;
            MealTargetFatG = chosen.RecommendedFatG / 3.0;
            RefreshComparison();
        }
        catch { }
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

        RefreshNutrition();
        RefreshComparison();
    }

    private void RefreshNutrition()
    {
        OnPropertyChanged(nameof(TotalCalories));
        OnPropertyChanged(nameof(TotalProteinG));
        OnPropertyChanged(nameof(TotalCarbsG));
        OnPropertyChanged(nameof(TotalFatG));
        OnPropertyChanged(nameof(PerServingCalories));
        OnPropertyChanged(nameof(PerServingProteinG));
        OnPropertyChanged(nameof(PerServingCarbsG));
        OnPropertyChanged(nameof(PerServingFatG));
    }

    private void RefreshComparison()
    {
        OnPropertyChanged(nameof(IsComparisonEnabled));
        OnPropertyChanged(nameof(ComparisonPersonName));
        OnPropertyChanged(nameof(MealTargetCalories));
        OnPropertyChanged(nameof(MealTargetProteinG));
        OnPropertyChanged(nameof(MealTargetCarbsG));
        OnPropertyChanged(nameof(MealTargetFatG));
        OnPropertyChanged(nameof(CalDelta));
        OnPropertyChanged(nameof(ProteinDelta));
        OnPropertyChanged(nameof(CarbsDelta));
        OnPropertyChanged(nameof(FatDelta));
        OnPropertyChanged(nameof(CalDeviationPct));
        OnPropertyChanged(nameof(ProteinDeviationPct));
        OnPropertyChanged(nameof(CarbsDeviationPct));
        OnPropertyChanged(nameof(FatDeviationPct));
        OnPropertyChanged(nameof(CalColor));
        OnPropertyChanged(nameof(ProteinColor));
        OnPropertyChanged(nameof(CarbsColor));
        OnPropertyChanged(nameof(FatColor));
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
