using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Planning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Recipes;

internal partial class RecipeListPageViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;
    private readonly IngredientParserService? _parser;
    private readonly ISeasonalityService? _seasonalityService;
    private readonly IMacroTargetService? _macroService;
    private readonly SearchWordSource _searchWordSource = SearchWordSource.Tags | SearchWordSource.Title | SearchWordSource.Ingredients;

    private List<Recipe> _allRecipes = [];
    private readonly List<RecipeCardViewModel> _allCards = [];

    public Action? AddRecipeRequested { get; set; }
    public Action? ImportRecipeRequested { get; set; }
    public Action<Recipe>? RecipeSelected { get; set; }

    public ObservableCollection<RecipeCardViewModel> FilteredRecipes { get; set; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    public bool ShowImages { get; set; } = true;
    public bool ShowTags { get; set; } = true;
    public bool ShowSeasonality { get; set; } = true;
    public bool ShowNutrition { get; set; } = true;

    public RecipeListPageViewModel(IRecipeService recipeService)
        : this(recipeService, null, null, null) { }

    public RecipeListPageViewModel(
        IRecipeService recipeService,
        IngredientParserService? parser,
        ISeasonalityService? seasonalityService,
        IMacroTargetService? macroService = null)
    {
        _recipeService = recipeService;
        _parser = parser;
        _seasonalityService = seasonalityService;
        _macroService = macroService;
    }

    private void LoadCardSettings()
    {
        if (_macroService is null)
        {
            return;
        }

        MacroTargetSettings settings = _macroService.GetSettings();
        ShowImages = settings.ShowCardImage;
        ShowTags = settings.ShowCardTags;
        ShowSeasonality = settings.ShowCardSeasonality;
        ShowNutrition = settings.ShowCardNutrition;
    }

    public void Reload()
    {
        LoadCardSettings();
        _allRecipes = [.. _recipeService.GetAll()];
        _allCards.Clear();

        foreach (Recipe recipe in _allRecipes)
        {
            double cal = 0, pro = 0, carb = 0, fat = 0;
            SeasonalityResult? seasonality = null;
            string? imagePath = null;

            if (recipe.Images.Count > 0)
            {
                imagePath = _recipeService.GetImagePath(recipe.Id, recipe.Images[0]);
            }

            if (_parser is not null && !string.IsNullOrWhiteSpace(recipe.Ingredients))
            {
                List<ParsedIngredientMatch> matches = _parser.ParseAndMatchIngredients(recipe.Ingredients);
                foreach (ParsedIngredientMatch match in matches)
                {
                    if (match.IsMatched)
                    {
                        cal += match.GetCalories();
                        pro += match.GetProtein();
                        carb += match.GetCarbohydrates();
                        fat += match.GetFat();
                    }
                }

                if (_seasonalityService is not null)
                {
                    seasonality = _seasonalityService.Score(matches);
                }
            }

            double servings = recipe.Servings > 0 ? recipe.Servings.Value : 1;
            _allCards.Add(RecipeCardViewModel.FromRecipe(recipe, imagePath,
                cal / servings, pro / servings, carb / servings, fat / servings, seasonality));
        }

        FilteredRecipes = new ObservableCollection<RecipeCardViewModel>(_allCards);
    }

    // TODO: Us DynamicData if use case arises: https://docs.avaloniaui.net/docs/data-binding/collection-views#using-dynamicdata-recommended-for-complex-scenarios
    partial void OnSearchTextChanged(string value)
    {
        // TODO: Tokenize the search word, if it contains any spaces, then match recipes that... therefore can match multipe ingredients Chicken + Flour.

        FilteredRecipes.Clear();

        foreach (RecipeCardViewModel card in _allCards)
        {
            if (string.IsNullOrEmpty(value))
            {
                FilteredRecipes.Add(card);
            }
            else if (card.SearchWords.Any(searchWord => searchWord.Word.Contains(value, StringComparison.OrdinalIgnoreCase) && _searchWordSource.HasFlag(searchWord.Source)))
            {
                FilteredRecipes.Add(card);
            }
        }
    }

    [RelayCommand]
    private void AddRecipe() => AddRecipeRequested?.Invoke();

    [RelayCommand]
    private void ImportRecipe() => ImportRecipeRequested?.Invoke();

    [RelayCommand]
    private void SelectRecipe(RecipeCardViewModel card) => RecipeSelected?.Invoke(card.Recipe);
}
