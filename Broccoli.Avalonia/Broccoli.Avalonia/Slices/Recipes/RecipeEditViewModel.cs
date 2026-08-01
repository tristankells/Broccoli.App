using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Recipes;
public record RecipeImageItem(string FileName, string FullPath);

public partial class RecipeEditViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;
    private readonly IngredientParserService? _parser;
    private readonly IFoodService? _foodService;
    private readonly bool _wasExistingOnOpen;

    private Recipe _recipe;

    public bool IsNew => !_wasExistingOnOpen;

    public Action? Saved { get; set; }
    public Action? Cancelled { get; set; }
    public Func<Task<string?>>? PickImageFileAsync { get; set; }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _ingredients = string.Empty;
    [ObservableProperty] private string _directions = string.Empty;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private int? _servings;
    [ObservableProperty] private int? _prepTimeMinutes;
    [ObservableProperty] private int? _cookTimeMinutes;
    [ObservableProperty] private string? _source;
    [ObservableProperty] private string? _url;
    [ObservableProperty] private string _tagsText = string.Empty;
    [ObservableProperty] private bool _isFavorite;
    [ObservableProperty] private string? _errorMessage;

    public ObservableCollection<RecipeImageItem> Images { get; } = new();
    public string RecipeIdForImages => _recipe.Id;

    public ObservableCollection<ParsedIngredientMatch> ParsedMatches { get; } = new();

    public double TotalCalories { get; private set; }
    public double TotalProteinG { get; private set; }
    public double TotalCarbsG { get; private set; }
    public double TotalFatG { get; private set; }

    public double PerServingCalories => Servings > 0 ? TotalCalories / Servings.Value : 0;
    public double PerServingProteinG => Servings > 0 ? TotalProteinG / Servings.Value : 0;
    public double PerServingCarbsG => Servings > 0 ? TotalCarbsG / Servings.Value : 0;
    public double PerServingFatG => Servings > 0 ? TotalFatG / Servings.Value : 0;

    public RecipeEditViewModel(IRecipeService recipeService, Recipe? existingRecipe)
        : this(recipeService, existingRecipe, null, null)
    {
    }

    public RecipeEditViewModel(IRecipeService recipeService, Recipe? existingRecipe,
        IngredientParserService? parser, IFoodService? foodService)
    {
        _recipeService = recipeService;
        _parser = parser;
        _foodService = foodService;
        _wasExistingOnOpen = existingRecipe is not null;
        _persisted = _wasExistingOnOpen;
        _recipe = existingRecipe ?? new Recipe();

        if (existingRecipe is not null)
        {
            Name = existingRecipe.Name;
            Ingredients = existingRecipe.Ingredients;
            Directions = existingRecipe.Directions;
            Notes = existingRecipe.Notes;
            Servings = existingRecipe.Servings;
            PrepTimeMinutes = existingRecipe.PrepTimeMinutes;
            CookTimeMinutes = existingRecipe.CookTimeMinutes;
            Source = existingRecipe.Source;
            Url = existingRecipe.Url;
            TagsText = string.Join(", ", existingRecipe.Tags);
            IsFavorite = existingRecipe.IsFavorite;
            RefreshImages(existingRecipe);
        }
    }

    partial void OnIngredientsChanged(string value)
    {
        ParseIngredients();
    }

    private void ParseIngredients()
    {
        if (_parser is null || string.IsNullOrWhiteSpace(Ingredients))
        {
            ParsedMatches.Clear();
            RefreshNutrition();
            return;
        }

        var matches = _parser.ParseAndMatchIngredients(Ingredients);

        ParsedMatches.Clear();
        double cal = 0, pro = 0, carb = 0, fat = 0;
        foreach (var m in matches)
        {
            ParsedMatches.Add(m);
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

    public string GetImagePath(string fileName) => _recipeService.GetImagePath(RecipeIdForImages, fileName);

    private void RefreshImages(Recipe recipe)
    {
        Images.Clear();
        foreach (var image in recipe.Images)
            Images.Add(new RecipeImageItem(image, _recipeService.GetImagePath(recipe.Id, image)));
    }

    [RelayCommand] private void Cancel() => Cancelled?.Invoke();

    [RelayCommand]
    private async Task AddImage()
    {
        if (PickImageFileAsync is null) return;
        var path = await PickImageFileAsync();
        if (string.IsNullOrWhiteSpace(path)) return;
        var recipe = SaveCore();
        recipe = _recipeService.AddImage(recipe, path);
        RefreshImages(recipe);
    }

    [RelayCommand]
    private void RemoveImage(RecipeImageItem image)
    {
        var recipe = SaveCore();
        recipe = _recipeService.RemoveImage(recipe, image.FileName);
        RefreshImages(recipe);
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Name is required.";
            return;
        }
        SaveCore();
        Saved?.Invoke();
    }

    private Recipe SaveCore()
    {
        _recipe.Name = Name.Trim();
        _recipe.Ingredients = Ingredients;
        _recipe.Directions = Directions;
        _recipe.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes;
        _recipe.Servings = Servings;
        _recipe.PrepTimeMinutes = PrepTimeMinutes;
        _recipe.CookTimeMinutes = CookTimeMinutes;
        _recipe.Source = string.IsNullOrWhiteSpace(Source) ? null : Source;
        _recipe.Url = string.IsNullOrWhiteSpace(Url) ? null : Url;
        _recipe.IsFavorite = IsFavorite;
        _recipe.Tags = TagsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        if (!_persisted) { _recipe = _recipeService.Create(_recipe); _persisted = true; }
        else _recipe = _recipeService.Update(_recipe);

        return _recipe;
    }

    private bool _persisted;
}
