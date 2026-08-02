using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class RecipeListPageViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;
    private readonly IngredientParserService? _parser;
    private readonly ISeasonalityService? _seasonalityService;

    private List<Recipe> _allRecipes = [];
    private readonly List<RecipeCardViewModel> _allCards = [];

    public Action? AddRecipeRequested { get; set; }
    public Action? ImportRecipeRequested { get; set; }
    public Action<Recipe>? RecipeSelected { get; set; }

    public ObservableCollection<RecipeCardViewModel> FilteredRecipes { get; set; } = new();

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    public bool ShowImages { get; set; } = true;
    public bool ShowTags { get; set; } = true;
    public bool ShowSeasonality { get; set; } = true;
    public bool ShowNutrition { get; set; } = true;

    public RecipeListPageViewModel(IRecipeService recipeService)
        : this(recipeService, null, null) { }

    public RecipeListPageViewModel(IRecipeService recipeService,
        IngredientParserService? parser, ISeasonalityService? seasonalityService)
    {
        _recipeService = recipeService;
        _parser = parser;
        _seasonalityService = seasonalityService;
    }

    public void LoadCardSettings(MacroTargetSettings settings)
    {
        ShowImages = settings.ShowCardImage;
        ShowTags = settings.ShowCardTags;
        ShowSeasonality = settings.ShowCardSeasonality;
        ShowNutrition = settings.ShowCardNutrition;
    }

    public void Reload()
    {
        _allRecipes = [.. _recipeService.GetAll()];
        _allCards.Clear();

        foreach (var recipe in _allRecipes)
        {
            double cal = 0, pro = 0, carb = 0, fat = 0;
            SeasonalityResult? seasonality = null;
            string? imagePath = null;

            if (recipe.Images.Count > 0)
                imagePath = _recipeService.GetImagePath(recipe.Id, recipe.Images[0]);

            if (_parser is not null && !string.IsNullOrWhiteSpace(recipe.Ingredients))
            {
                var matches = _parser.ParseAndMatchIngredients(recipe.Ingredients);
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

                if (_seasonalityService is not null)
                    seasonality = _seasonalityService.Score(matches);
            }

            double servings = recipe.Servings > 0 ? recipe.Servings.Value : 1;
            _allCards.Add(RecipeCardViewModel.FromRecipe(recipe, imagePath,
                cal / servings, pro / servings, carb / servings, fat / servings, seasonality));
        }

        FilteredRecipes = new ObservableCollection<RecipeCardViewModel>(_allCards);
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            FilteredRecipes = new ObservableCollection<RecipeCardViewModel>(_allCards);
        else
            FilteredRecipes = new ObservableCollection<RecipeCardViewModel>(
                _allCards.Where(c => c.Name.Contains(value, StringComparison.OrdinalIgnoreCase)));
    }

    [RelayCommand] private void AddRecipe() => AddRecipeRequested?.Invoke();
    [RelayCommand] private void ImportRecipe() => ImportRecipeRequested?.Invoke();

    [RelayCommand]
    private void SelectRecipe(RecipeCardViewModel card) => RecipeSelected?.Invoke(card.Recipe);
}
