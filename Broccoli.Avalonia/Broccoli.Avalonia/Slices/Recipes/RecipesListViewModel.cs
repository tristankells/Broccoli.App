using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes.Import;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class RecipesListViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;
    private readonly IngredientParserService? _parser;
    private readonly IFoodService? _foodService;
    private readonly ISeasonalityService? _seasonalityService;
    private readonly IMacroTargetService? _macroService;
    private readonly ImportDialogViewModel? _importDialog;

    [ObservableProperty]
    private ObservableObject _currentPage;

    private readonly RecipeListPageViewModel _listPage;

    public RecipesListViewModel() : this(new RecipeService(), null, null, null, null, null) { }

    public RecipesListViewModel(IRecipeService recipeService,
        IngredientParserService? parser, IFoodService? foodService,
        ISeasonalityService? seasonalityService, IMacroTargetService? macroService,
        ImportDialogViewModel? importDialog = null)
    {
        _recipeService = recipeService;
        _parser = parser;
        _foodService = foodService;
        _seasonalityService = seasonalityService;
        _macroService = macroService;
        _importDialog = importDialog;
        _listPage = new RecipeListPageViewModel(_recipeService, _parser, _seasonalityService)
        {
            AddRecipeRequested = ShowAdd,
            ImportRecipeRequested = ShowImport,
            RecipeSelected = ShowDetail
        };
        LoadCardSettings();
        _listPage.Reload();
        _currentPage = _listPage;
    }

    private void LoadCardSettings()
    {
        if (_macroService is null) return;
        var settings = _macroService.GetSettings();
        _listPage.LoadCardSettings(settings);
    }

    private void ShowList()
    {
        LoadCardSettings();
        _listPage.Reload();
        CurrentPage = _listPage;
    }

    private void ShowAdd() => ShowEdit(null);

    private void ShowImport()
    {
        if (_importDialog is null) return;
        var existingNames = _recipeService.GetAll().Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _importDialog.Closed = ShowList;
        _importDialog.Open(existingNames);
        var window = new ImportDialog { DataContext = _importDialog };
        window.Show();
    }

    private void ShowDetail(Recipe recipe)
    {
        var detail = new RecipeDetailViewModel(_recipeService, _parser, _seasonalityService, _macroService, recipe)
        {
            BackRequested = ShowList,
            EditRequested = ShowEdit,
            RecipeDeleted = ShowList
        };
        CurrentPage = detail;
    }

    private void ShowEdit(Recipe? existingRecipe)
    {
        var edit = new RecipeEditViewModel(_recipeService, existingRecipe, _parser, _foodService, _macroService)
        {
            Saved = ShowList,
            Cancelled = ShowList
        };
        CurrentPage = edit;
    }
}
