using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class RecipesListViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;
    private readonly IngredientParserService? _parser;
    private readonly IFoodService? _foodService;

    [ObservableProperty]
    private ObservableObject _currentPage;

    private readonly RecipeListPageViewModel _listPage;

    public RecipesListViewModel() : this(new RecipeService(), null, null)
    {
    }

    public RecipesListViewModel(IRecipeService recipeService,
        IngredientParserService? parser, IFoodService? foodService)
    {
        _recipeService = recipeService;
        _parser = parser;
        _foodService = foodService;
        _listPage = new RecipeListPageViewModel(_recipeService)
        {
            AddRecipeRequested = ShowAdd,
            RecipeSelected = ShowDetail
        };
        _currentPage = _listPage;
    }

    private void ShowList()
    {
        _listPage.Reload();
        CurrentPage = _listPage;
    }

    private void ShowAdd() => ShowEdit(null);

    private void ShowDetail(Recipe recipe)
    {
        var detail = new RecipeDetailViewModel(_recipeService, _parser, recipe)
        {
            BackRequested = ShowList,
            EditRequested = ShowEdit,
            RecipeDeleted = ShowList
        };
        CurrentPage = detail;
    }

    private void ShowEdit(Recipe? existingRecipe)
    {
        var edit = new RecipeEditViewModel(_recipeService, existingRecipe, _parser, _foodService)
        {
            Saved = ShowList,
            Cancelled = ShowList
        };
        CurrentPage = edit;
    }
}
