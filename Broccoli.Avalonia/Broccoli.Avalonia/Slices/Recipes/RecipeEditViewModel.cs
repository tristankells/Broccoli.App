using System.Collections.ObjectModel;
using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Planning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class RecipeEditViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;
    private readonly IngredientParserService? _parser;
    private readonly IFoodService? _foodService;
    private readonly IMacroTargetService? _macroService;
    private readonly bool _wasExistingOnOpen;

    private Recipe _recipe;
    private bool _persisted;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _ingredients = string.Empty;

    [ObservableProperty]
    private string _directions = string.Empty;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private int? _servings;

    [ObservableProperty]
    private int? _prepTimeMinutes;

    [ObservableProperty]
    private int? _cookTimeMinutes;

    [ObservableProperty]
    private string? _source;

    [ObservableProperty]
    private string? _url;

    [ObservableProperty]
    private string _tagsText = string.Empty;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private string? _errorMessage;

    public RecipeEditViewModel(IRecipeService recipeService, Recipe? existingRecipe)
        : this(recipeService, existingRecipe, null, null, null)
    {
    }

    public RecipeEditViewModel(
        IRecipeService recipeService,
        Recipe? existingRecipe,
        IngredientParserService? parser,
        IFoodService? foodService)
        : this(recipeService, existingRecipe, parser, foodService, null)
    {
    }

    public RecipeEditViewModel(
        IRecipeService recipeService,
        Recipe? existingRecipe,
        IngredientParserService? parser,
        IFoodService? foodService,
        IMacroTargetService? macroService)
    {
        _recipeService = recipeService;
        _parser = parser;
        _foodService = foodService;
        _macroService = macroService;
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

        LoadMacroComparison();
    }

    public bool IsNew => !_wasExistingOnOpen;

    public Action? Saved { get; set; }

    public Action? Cancelled { get; set; }

    public Action<Recipe>? HistoryRequested { get; set; }

    public Func<Task<string?>>? PickImageFileAsync { get; set; }

    public ObservableCollection<RecipeImageItem> Images { get; } = new();

    public string RecipeIdForImages => _recipe.Id;

    public ObservableCollection<ParsedIngredientRow> ParsedMatches { get; } = new();

    public double TotalCalories { get; private set; }

    public double TotalProteinG { get; private set; }

    public double TotalCarbsG { get; private set; }

    public double TotalFatG { get; private set; }

    public double PerServingCalories => TotalCalories / EffectiveServings;

    public double PerServingProteinG => TotalProteinG / EffectiveServings;

    public double PerServingCarbsG => TotalCarbsG / EffectiveServings;

    public double PerServingFatG => TotalFatG / EffectiveServings;

    private int EffectiveServings => Servings > 0 ? Servings.Value : 1;

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

    public string GetImagePath(string fileName) => _recipeService.GetImagePath(RecipeIdForImages, fileName);

    private static string DeviationColor(double pct) => pct <= 15 ? "#2ECC71" : pct <= 25 ? "#F39C12" : "#E74C3C";

    private void LoadMacroComparison()
    {
        if (_macroService is null)
        {
            return;
        }

        MacroTargetSettings settings = _macroService.GetSettings();
        if (!settings.RecipeMealComparisonEnabled || string.IsNullOrWhiteSpace(settings.RecipeMealComparisonPersonId))
        {
            return;
        }

        List<MacroTarget> targets = _macroService.GetAll();
        MacroTarget? chosen = targets.FirstOrDefault(t => t.Id == settings.RecipeMealComparisonPersonId);
        if (chosen is null)
        {
            return;
        }

        IsComparisonEnabled = true;
        ComparisonPersonName = chosen.Name;
        MealTargetCalories = chosen.RecommendedCalories / 3.0;
        MealTargetProteinG = chosen.RecommendedProteinG / 3.0;
        MealTargetCarbsG = chosen.RecommendedCarbsG / 3.0;
        MealTargetFatG = chosen.RecommendedFatG / 3.0;
        RefreshComparison();
    }

    partial void OnIngredientsChanged(string value) => ParseIngredients();

    partial void OnServingsChanged(int? value)
    {
        RefreshNutrition();
        RefreshComparison();
    }

    private void ParseIngredients()
    {
        if (_parser is null || string.IsNullOrWhiteSpace(Ingredients))
        {
            ParsedMatches.Clear();
            RefreshNutrition();
            RefreshComparison();
            return;
        }

        List<ParsedIngredientMatch> matches = _parser.ParseAndMatchIngredients(Ingredients);
        ParsedMatches.Clear();
        double cal = 0, pro = 0, carb = 0, fat = 0;
        foreach (ParsedIngredientMatch m in matches)
        {
            ParsedMatches.Add(ParsedIngredientRow.FromMatch(m));
            if (m.IsMatched)
            {
                cal += m.GetCalories();
                pro += m.GetProtein();
                carb += m.GetCarbohydrates();
                fat += m.GetFat();
            }
        }

        TotalCalories = cal;
        TotalProteinG = pro;
        TotalCarbsG = carb;
        TotalFatG = fat;

        int topCalIdx = -1, topProIdx = -1, topCarbIdx = -1, topFatIdx = -1;
        double topCal = 0, topPro = 0, topCarb = 0, topFat = 0;
        for (int i = 0; i < matches.Count; i++)
        {
            ParsedIngredientMatch m = matches[i];
            if (!m.IsMatched)
            {
                continue;
            }

            double matchCal = m.GetCalories();
            double matchPro = m.GetProtein();
            double matchCarb = m.GetCarbohydrates();
            double matchFat = m.GetFat();

            if (matchCal > topCal)
            {
                topCal = matchCal;
                topCalIdx = i;
            }

            if (matchPro > topPro)
            {
                topPro = matchPro;
                topProIdx = i;
            }

            if (matchCarb > topCarb)
            {
                topCarb = matchCarb;
                topCarbIdx = i;
            }

            if (matchFat > topFat)
            {
                topFat = matchFat;
                topFatIdx = i;
            }
        }

        if (topCalIdx >= 0)
        {
            ParsedMatches[topCalIdx].IsTopCalories = true;
        }

        if (topProIdx >= 0)
        {
            ParsedMatches[topProIdx].IsTopProtein = true;
        }

        if (topCarbIdx >= 0)
        {
            ParsedMatches[topCarbIdx].IsTopCarbs = true;
        }

        if (topFatIdx >= 0)
        {
            ParsedMatches[topFatIdx].IsTopFat = true;
        }

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

    private void RefreshImages(Recipe recipe)
    {
        Images.Clear();
        foreach (string image in recipe.Images)
        {
            Images.Add(new RecipeImageItem(image, _recipeService.GetImagePath(recipe.Id, image)));
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke();

    [RelayCommand]
    private void OpenHistory() => HistoryRequested?.Invoke(_recipe);

    [RelayCommand]
    private async Task AddImage()
    {
        if (PickImageFileAsync is null)
        {
            return;
        }

        string? path = await PickImageFileAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        Recipe recipe = SaveCore();
        recipe = _recipeService.AddImage(recipe, path);
        RefreshImages(recipe);
    }

    [RelayCommand]
    private void RemoveImage(RecipeImageItem image)
    {
        Recipe recipe = SaveCore();
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

        if (!_persisted)
        {
            _recipe = _recipeService.Create(_recipe);
            _persisted = true;
        }
        else
        {
            _recipe = _recipeService.Update(_recipe);
        }

        return _recipe;
    }
}
