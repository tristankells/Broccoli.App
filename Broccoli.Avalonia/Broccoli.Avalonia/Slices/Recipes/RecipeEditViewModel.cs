using System.Collections.ObjectModel;
using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;
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
    private readonly ISeasonalityService? _seasonalityService;
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
        IMacroTargetService? macroService,
        ISeasonalityService? seasonalityService = null)
    {
        _recipeService = recipeService;
        _parser = parser;
        _foodService = foodService;
        _macroService = macroService;
        _seasonalityService = seasonalityService;
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
        LoadSeasonalitySettings();
    }

    public bool IsNew => !_wasExistingOnOpen;

    public Action? Saved { get; set; }

    public Action? Cancelled { get; set; }

    public Action<Recipe>? HistoryRequested { get; set; }

    public Func<Task<string?>>? PickImageFileAsync { get; set; }

    public ObservableCollection<RecipeImageItem> Images { get; } = new();

    public string RecipeIdForImages => _recipe.Id;

    public ObservableCollection<ParsedIngredientRow> ParsedMatches { get; } = new();

    /// <summary>True when seasonality hints are shown in the editor (enabled in settings and a seasonality service is available).</summary>
    public bool ShowSeasonality { get; private set; }

    /// <summary>Label such as "Seasonality for March" shown above the ingredient list.</summary>
    public string SeasonalityMonthLabel { get; private set; } = string.Empty;

    /// <summary>Overall seasonality result for the current ingredients.</summary>
    public SeasonalityResult? Seasonality { get; private set; }

    /// <summary>True when the current ingredients produced a seasonality score.</summary>
    public bool HasSeasonality => ShowSeasonality && Seasonality?.Breakdown.Count > 0;

    /// <summary>Band label ("Green"/"Yellow"/"Red") for the recipe's seasonality.</summary>
    public string SeasonalityBandLabel => Seasonality is { Score: not null } && Seasonality.Band != SeasonalityBand.Unavailable
        ? SeasonalityBandVisuals.Label(Seasonality.Band)
        : string.Empty;

    public string SeasonScoreText => Seasonality?.Score is double s ? $"{s:0} / 100" : string.Empty;

    public string SeasonScoreColor => SeasonalityBandVisuals.Color(Seasonality?.Band ?? SeasonalityBand.Unavailable);

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

        RefreshSeasonality(matches);
        RefreshNutrition();
        RefreshComparison();
    }

    private void LoadSeasonalitySettings()
    {
        bool enabledInSettings = _macroService?.GetSettings().ShowRecipeEditorSeasonality ?? true;
        ShowSeasonality = _seasonalityService is not null && enabledInSettings;
        SeasonalityMonthLabel = $"Seasonality for {System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(DateTime.Today.Month)}";
    }

    /// <summary>
    /// Annotates each parsed ingredient with its seasonality detail for the current month: the
    /// produce it matched, its state, and how many points it adds to / costs the overall score.
    /// </summary>
    private void RefreshSeasonality(List<ParsedIngredientMatch> matches)
    {
        foreach (ParsedIngredientRow row in ParsedMatches)
        {
            row.ApplySeasonality(null, null);
        }

        Seasonality = null;
        if (!ShowSeasonality || _seasonalityService is null)
        {
            RaiseSeasonalityChanged();
            return;
        }

        SeasonalityThresholds thresholds = _macroService is null
            ? SeasonalityThresholds.Default
            : SeasonalityThresholds.FromSettings(_macroService.GetSettings());
        DateTime asOf = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        SeasonalityResult result = _seasonalityService.Score(matches, asOf, thresholds);
        Seasonality = result;
        double totalPossible = result.Breakdown.Sum(b => b.ScarcityWeight * b.WeightInGrams);

        for (int i = 0; i < ParsedMatches.Count && i < matches.Count; i++)
        {
            ParsedIngredientRow row = ParsedMatches[i];
            ParsedIngredientMatch match = matches[i];
            if (!row.IsMatched || match.MatchedFood is null)
            {
                continue;
            }

            IngredientSeasonalityDetail? detail = _seasonalityService.GetProduceDetailForFood(
                match.MatchedFood.Name, asOf, thresholds.InfluenceMode);
            if (detail is null)
            {
                continue;
            }

            double possible = detail.ScarcityWeight * row.WeightInGrams;
            double factor = detail.State switch
            {
                SeasonalityState.InSeason => 1.0,
                SeasonalityState.PartiallyInSeason => 0.5,
                _ => 0.0,
            };
            double? impact = totalPossible > 0
                ? detail.State == SeasonalityState.OutOfSeason
                    ? -possible / totalPossible * 100.0
                    : factor * possible / totalPossible * 100.0
                : null;
            row.ApplySeasonality(detail, impact);
        }

        RaiseSeasonalityChanged();
    }

    private void RaiseSeasonalityChanged()
    {
        OnPropertyChanged(nameof(Seasonality));
        OnPropertyChanged(nameof(HasSeasonality));
        OnPropertyChanged(nameof(SeasonalityBandLabel));
        OnPropertyChanged(nameof(SeasonScoreText));
        OnPropertyChanged(nameof(SeasonScoreColor));
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
        OpenAutoBalanceCommand.NotifyCanExecuteChanged();
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
    private void ResolveIngredient(ParsedIngredientRow row)
    {
        if (_foodService is null)
        {
            return;
        }

        IReadOnlyList<FoodMatchResult> matches = _foodService.FindMatches(row.FoodDescription, 10);
        var dialogViewModel = new IngredientFoodDialogViewModel(
            _foodService,
            row.FoodDescription,
            matches,
            row.MatchedFood)
        {
            FoodSelected = food => ApplyFoodFix(row, food),
        };

        var dialog = new IngredientFoodDialog { DataContext = dialogViewModel };
        dialog.Show();
    }

    private void ApplyFoodFix(ParsedIngredientRow row, Food selectedFood)
    {
        bool selectionChanged = row.MatchedFood is null ||
            selectedFood.Id != row.MatchedFood.Id ||
            !string.Equals(selectedFood.Name, row.MatchedFood.Name, StringComparison.OrdinalIgnoreCase);

        if (selectionChanged)
        {
            ApplyMatchFix(row, selectedFood);
        }
        else
        {
            ParseIngredients();
        }
    }

    private void ApplyMatchFix(ParsedIngredientRow row, Food food)
    {
        if (_parser is null)
        {
            return;
        }

        string[] lines = Ingredients.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            ParsedIngredient? parsed = _parser.ParseLine(lines[i].TrimEnd('\r'));
            if (parsed is null ||
                !string.Equals(parsed.FoodDescription, row.FoodDescription, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(parsed.CanonicalUnit, row.CanonicalUnit, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            lines[i] = IngredientLineFormatter.Build(parsed.Quantity, parsed.CanonicalUnit, food.Name);
        }

        Ingredients = string.Join("\n", lines);
    }

    private bool CanAutoBalance() => IsComparisonEnabled;

    [RelayCommand(CanExecute = nameof(CanAutoBalance))]
    private void OpenAutoBalance()
    {
        if (_parser is null || _macroService is null)
        {
            return;
        }

        List<AutoBalanceIngredient> ingredients = new();
        foreach (ParsedIngredientMatch match in _parser.ParseAndMatchIngredients(Ingredients))
        {
            AutoBalanceIngredient? ingredient = AutoBalanceIngredient.FromMatch(match);
            if (ingredient is not null)
            {
                ingredients.Add(ingredient);
            }
        }

        if (ingredients.Count == 0)
        {
            return;
        }

        double servings = EffectiveServings;
        var targets = new AutoBalanceTargets
        {
            Calories = MealTargetCalories * servings,
            ProteinG = MealTargetProteinG * servings,
            CarbsG = MealTargetCarbsG * servings,
            FatG = MealTargetFatG * servings,
        };

        MacroTargetSettings settings = _macroService.GetSettings();
        var dialogViewModel = new AutoBalanceDialogViewModel(
            ingredients,
            targets,
            servings,
            ComparisonPersonName ?? "target",
            settings.AutoBalanceStrategy,
            settings.CalorieMatchTolerancePercent)
        {
            ApplyRequested = ApplyAutoBalance,
        };

        var dialog = new AutoBalanceDialog { DataContext = dialogViewModel };
        dialog.Show();
    }

    private void ApplyAutoBalance(IReadOnlyList<AutoBalanceAdjustment> adjustments)
    {
        if (_parser is null || adjustments.Count == 0)
        {
            return;
        }

        string[] lines = Ingredients.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            ParsedIngredient? parsed = _parser.ParseLine(lines[i].TrimEnd('\r'));
            if (parsed is null)
            {
                continue;
            }

            AutoBalanceAdjustment? adjustment = adjustments.FirstOrDefault(a =>
                string.Equals(a.Ingredient.FoodDescription, parsed.FoodDescription, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.Ingredient.CanonicalUnit, parsed.CanonicalUnit, StringComparison.OrdinalIgnoreCase));

            if (adjustment is null || adjustment.BeforeGrams <= 0)
            {
                continue;
            }

            double ratio = adjustment.AfterGrams / adjustment.BeforeGrams;
            double newQuantity = parsed.Quantity * ratio;
            lines[i] = IngredientLineFormatter.Build(newQuantity, parsed.CanonicalUnit, adjustment.Ingredient.FoodName);
        }

        Ingredients = string.Join("\n", lines);
    }

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
