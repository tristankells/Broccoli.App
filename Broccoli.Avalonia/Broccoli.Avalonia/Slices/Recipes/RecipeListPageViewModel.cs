using System.Collections.ObjectModel;
using Avalonia.Controls;
using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Groceries;
using Broccoli.Avalonia.Slices.Pantry;
using Broccoli.Avalonia.Slices.Planning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Broccoli.Avalonia.Slices.Recipes;

internal partial class RecipeListPageViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;
    private readonly IngredientParserService? _parser;
    private readonly ISeasonalityService? _seasonalityService;
    private readonly IMacroTargetService? _macroService;
    private readonly IngredientCartService? _cartService;
    private readonly IPantryService? _pantryService;
    private readonly IRecipeIngredientSearchService? _ingredientSearchService;

    private readonly List<RecipeCardViewModel> _allCards = [];

    private List<Recipe> _allRecipes = [];

    public RecipeListPageViewModel(IRecipeService recipeService)
        : this(recipeService, null, null, null, null, null, null)
    {
    }

    public RecipeListPageViewModel(
        IRecipeService recipeService,
        IngredientParserService? parser,
        ISeasonalityService? seasonalityService,
        IMacroTargetService? macroService = null,
        IngredientCartService? cartService = null,
        IPantryService? pantryService = null,
        IRecipeIngredientSearchService? ingredientSearchService = null)
    {
        _recipeService = recipeService;
        _parser = parser;
        _seasonalityService = seasonalityService;
        _macroService = macroService;
        _cartService = cartService;
        _pantryService = pantryService;
        _ingredientSearchService = ingredientSearchService;

        WeakReferenceMessenger.Default.Register<CardSettingsChangedMessage>(this, (_, _) => _ = ReloadAsync());
    }

    public Action? AddRecipeRequested { get; set; }

    public Action? ImportRecipeRequested { get; set; }

    public Action? UseUpIngredientsRequested { get; set; }

    public Action<Recipe>? RecipeSelected { get; set; }

    public Action<Recipe>? EditRecipeRequested { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private ObservableCollection<RecipeCardViewModel> _filteredRecipes = new();

    /// <summary>
    /// True until the first recipe load completes. Starts true because this page is the initial
    /// shell page, shown before <see cref="ReloadAsync"/> runs on startup.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading = true;

    /// <summary>True after the first load completes, so the empty state isn't shown while loading.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _hasLoaded;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsTitleSearchEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsTagSearchEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsIngredientSearchEnabled { get; set; } = true;

    /// <summary>True shows the compact list view (no images); false shows the image cards.</summary>
    [ObservableProperty]
    private bool _isListView;

    /// <summary>The visible list-view columns, in display order (loaded from settings).</summary>
    [ObservableProperty]
    private ObservableCollection<RecipeListColumnDefinition> _listColumns = new();

    /// <summary>Star-sized grid definitions matching <see cref="ListColumns"/>, for the table header.</summary>
    public ColumnDefinitions ListColumnDefinitions { get; } = new();

    /// <summary>The list-view rows, filtered and sorted by the active column.</summary>
    [ObservableProperty]
    private ObservableCollection<RecipeListRowViewModel> _sortedListItems = new();

    /// <summary>The column the list view is currently sorted by.</summary>
    [ObservableProperty]
    private RecipeListColumn _sortColumn = RecipeListColumn.Name;

    /// <summary>Sort direction for <see cref="SortColumn"/>.</summary>
    [ObservableProperty]
    private bool _sortAscending = true;

    /// <summary>
    /// Set when the recipe list fails to load (e.g. the local database can't be read). The view
    /// shows an inline banner with a Retry button instead of leaving the loading spinner stuck.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    /// <summary>True when <see cref="ErrorMessage"/> is set, so the error banner is visible.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    private bool _suppressViewModePersistence;

    /// <summary>True only after the first load finished and there are no recipes to show.</summary>
    public bool ShowEmptyState => HasLoaded && FilteredRecipes.Count == 0;

    /// <summary>
    /// True until the view has played the staggered entrance once. The view clears this after the
    /// first batch animates in, so re-showing the page (or reloading it) never replays the stagger.
    /// </summary>
    public bool EntranceAnimationPending { get; internal set; } = true;

    public bool ShowImages { get; set; } = true;

    public bool ShowTags { get; set; } = true;

    public bool ShowSeasonality { get; set; } = true;

    public bool ShowNutrition { get; set; } = true;

    public bool ShowCalorieMatch { get; set; }

    public double CalorieMatchTolerancePercent { get; set; } = 15;

    public double? CompareTargetCaloriesPerMeal { get; private set; }

    public void Reload()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            LoadListViewSettings();
            (List<Recipe> recipes, List<RecipeCardViewModel> cards) = BuildCards();
            ApplyResults(recipes, cards);
        }
        catch (Exception ex)
        {
            // Never leave the page stuck on its loading spinner: clear it and surface the
            // failure inline so the user can retry (or investigate) rather than waiting forever.
            System.Diagnostics.Trace.TraceError($"Recipe list load failed: {ex}");
            ErrorMessage = $"Recipes couldn't be loaded: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Builds the recipe cards on a background thread so the UI thread stays free to render the
    /// window, then swaps the results into the bound collections. Used by the startup path.
    /// </summary>
    public async Task ReloadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            LoadListViewSettings();
            (List<Recipe> recipes, List<RecipeCardViewModel> cards) = await Task.Run(BuildCards);
            ApplyResults(recipes, cards);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Recipe list load failed: {ex}");
            ErrorMessage = $"Recipes couldn't be loaded: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Retry() => await ReloadAsync();

    /// <summary>
    /// Restores the list-view display settings (cards-vs-list choice and the visible columns).
    /// Called on the UI thread (a display concern; must not run on the background card-building
    /// thread).
    /// </summary>
    private void LoadListViewSettings()
    {
        if (_macroService is null)
        {
            return;
        }

        _suppressViewModePersistence = true;
        try
        {
            MacroTargetSettings settings = _macroService.GetSettings();
            IsListView = settings.ShowRecipesAsList;
            LoadColumns(settings.RecipeListColumns);
        }
        finally
        {
            _suppressViewModePersistence = false;
        }
    }

    /// <summary>Rebuilds the visible list columns and the rows derived from them.</summary>
    private void LoadColumns(string? serialized)
    {
        RecipeListColumn[] columns = RecipeListColumnDefinitions.Parse(serialized);
        ListColumns.Clear();
        ListColumnDefinitions.Clear();
        for (int i = 0; i < columns.Length; i++)
        {
            RecipeListColumnDefinition definition = new(columns[i], i);
            ListColumns.Add(definition);
            ListColumnDefinitions.Add(new ColumnDefinition(definition.Width));
        }

        RebuildSortedList();
    }

    /// <summary>Persists the cards-vs-list choice so it survives reloads and app restarts.</summary>
    partial void OnIsListViewChanged(bool value)
    {
        if (_suppressViewModePersistence || _macroService is null)
        {
            return;
        }

        MacroTargetSettings settings = _macroService.GetSettings();
        settings.ShowRecipesAsList = value;
        _macroService.SaveSettings(settings);
    }

    /// <summary>Sorts (or reverses) the list view by the active column.</summary>
    [RelayCommand]
    private void Sort(RecipeListColumn column)
    {
        if (SortColumn == column)
        {
            SortAscending = !SortAscending;
        }
        else
        {
            SortColumn = column;
            SortAscending = true;
        }

        RebuildSortedList();
    }

    /// <summary>Rebuilds the list-view rows from the filtered cards using the active sort.</summary>
    private void RebuildSortedList()
    {
        List<RecipeCardViewModel> sorted = SortCards(FilteredRecipes);
        SortedListItems = new ObservableCollection<RecipeListRowViewModel>(
            sorted.Select(card => new RecipeListRowViewModel(card, ListColumns)));
        UpdateSortIndicators();
    }

    private List<RecipeCardViewModel> SortCards(IEnumerable<RecipeCardViewModel> cards)
    {
        List<RecipeCardViewModel> list = cards.ToList();
        Comparison<RecipeCardViewModel> comparison = SortColumn switch
        {
            RecipeListColumn.Name => (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase),
            RecipeListColumn.CookingTime => (a, b) => TotalMinutes(a).CompareTo(TotalMinutes(b)),
            RecipeListColumn.Servings => (a, b) => (a.Recipe.Servings ?? int.MaxValue).CompareTo(b.Recipe.Servings ?? int.MaxValue),
            RecipeListColumn.Source => (a, b) => string.Compare(a.Recipe.Source, b.Recipe.Source, StringComparison.OrdinalIgnoreCase),
            RecipeListColumn.DateAdded => (a, b) => a.Recipe.CreatedAt.CompareTo(b.Recipe.CreatedAt),
            RecipeListColumn.Calories => (a, b) => a.CaloriesPerServing.CompareTo(b.CaloriesPerServing),
            RecipeListColumn.Protein => (a, b) => a.ProteinPerServingG.CompareTo(b.ProteinPerServingG),
            RecipeListColumn.Carbs => (a, b) => a.CarbsPerServingG.CompareTo(b.CarbsPerServingG),
            RecipeListColumn.Fat => (a, b) => a.FatPerServingG.CompareTo(b.FatPerServingG),
            _ => (a, b) => 0,
        };

        list.Sort(comparison);
        if (!SortAscending)
        {
            list.Reverse();
        }

        return list;
    }

    private static int TotalMinutes(RecipeCardViewModel card)
    {
        int prep = card.Recipe.PrepTimeMinutes ?? 0;
        int cook = card.Recipe.CookTimeMinutes ?? 0;
        return prep + cook;
    }

    private void UpdateSortIndicators()
    {
        foreach (RecipeListColumnDefinition definition in ListColumns)
        {
            definition.SortSuffix = definition.Column == SortColumn
                ? SortAscending
                    ? " ▲"
                    : " ▼"
                : string.Empty;
        }
    }

    private (List<Recipe> Recipes, List<RecipeCardViewModel> Cards) BuildCards()
    {
        LoadCardSettings();

        List<Recipe> recipes = _recipeService.GetAll()
            .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var cards = new List<RecipeCardViewModel>(recipes.Count);

        foreach (Recipe recipe in recipes)
        {
            double cal = 0, pro = 0, carb = 0, fat = 0;
            SeasonalityResult? seasonality = null;
            string? imagePath = null;

            if (ShowImages && recipe.Images.Count > 0)
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
            RecipeCardViewModel card = RecipeCardViewModel.FromRecipe(
                recipe,
                imagePath,
                cal / servings,
                pro / servings,
                carb / servings,
                fat / servings,
                seasonality,
                CompareTargetCaloriesPerMeal,
                CalorieMatchTolerancePercent,
                ShowImages,
                ShowTags,
                ShowSeasonality,
                ShowNutrition);
            card.AddToCartRequested = AddToCart;
            card.EditRequested = EditRecipe;
            cards.Add(card);
        }

        return (recipes, cards);
    }

    private void ApplyResults(List<Recipe> recipes, List<RecipeCardViewModel> cards)
    {
        _allRecipes = recipes;
        _allCards.Clear();
        _allCards.AddRange(cards);
        HasLoaded = true;
        ApplyFilter();
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
        ShowCalorieMatch = settings.ShowCardCalorieMatch;
        CalorieMatchTolerancePercent = settings.CalorieMatchTolerancePercent;

        CompareTargetCaloriesPerMeal = null;
        if (ShowCalorieMatch && !string.IsNullOrWhiteSpace(settings.RecipeMealComparisonPersonId))
        {
            List<MacroTarget> targets = _macroService.GetAll();
            MacroTarget? chosen = targets.FirstOrDefault(t => t.Id == settings.RecipeMealComparisonPersonId);
            if (chosen is not null && chosen.RecommendedCalories > 0)
            {
                CompareTargetCaloriesPerMeal = chosen.RecommendedCalories / 3.0;
            }
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnIsTitleSearchEnabledChanged(bool value) => ApplyFilter();

    partial void OnIsTagSearchEnabledChanged(bool value) => ApplyFilter();

    partial void OnIsIngredientSearchEnabledChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            FilteredRecipes = new ObservableCollection<RecipeCardViewModel>(_allCards);
            RebuildSortedList();
            return;
        }

        SearchWordSource enabledSources = DetermineEnabledSearchSource();

        string[] tokens = string.IsNullOrEmpty(SearchText) ?
            [] :
            SearchText.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        FilteredRecipes =
            new ObservableCollection<RecipeCardViewModel>(_allCards.Where(card => IsMatch(tokens, card.SearchWords)));
        RebuildSortedList();

        SearchWordSource DetermineEnabledSearchSource()
        {
            SearchWordSource enabledSources = SearchWordSource.None;
            if (IsTitleSearchEnabled)
            {
                enabledSources |= SearchWordSource.Title;
            }

            if (IsTagSearchEnabled)
            {
                enabledSources |= SearchWordSource.Tags;
            }

            if (IsIngredientSearchEnabled)
            {
                enabledSources |= SearchWordSource.Ingredients;
            }

            return enabledSources;
        }

        bool IsMatch(string[] searchTokens, HashSet<SearchWord> searchWords)
        {
            return searchTokens.All(
                    token => searchWords.Any(
                        searchWord =>
                            searchWord.Word.Contains(token, StringComparison.OrdinalIgnoreCase)
                            && enabledSources.HasFlag(searchWord.Source)));
        }
    }

    [RelayCommand]
    private void AddRecipe() => AddRecipeRequested?.Invoke();

    [RelayCommand]
    private void ImportRecipe() => ImportRecipeRequested?.Invoke();

    [RelayCommand]
    private void UseUpIngredients() => UseUpIngredientsRequested?.Invoke();

    [RelayCommand]
    private void SelectRecipe(RecipeCardViewModel card) => RecipeSelected?.Invoke(card.Recipe);

    [RelayCommand]
    private void EditRecipe(RecipeCardViewModel card) => EditRecipeRequested?.Invoke(card.Recipe);

    [RelayCommand]
    private void AddToCart(RecipeCardViewModel card)
    {
        if (_cartService is null || _pantryService is null || string.IsNullOrWhiteSpace(card.Recipe.Ingredients))
        {
            return;
        }

        List<string> ingredientLines = card.Recipe.Ingredients
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var dialogViewModel = new AddToCartDialogViewModel(_cartService, _pantryService, card.Recipe.Name, ingredientLines);
        var dialog = new AddToCartDialog { DataContext = dialogViewModel };
        dialog.Show();
    }
}
