using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>
/// Recipes section shell: swaps between the recipe list, a read-only detail view, and the
/// add/edit form, all within the Recipes area of the app (the main nav stays on "Recipes").
/// </summary>
public partial class RecipesListViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;

    [ObservableProperty]
    private ObservableObject _currentPage;

    private readonly RecipeListPageViewModel _listPage;

    public RecipesListViewModel() : this(new RecipeService())
    {
    }

    public RecipesListViewModel(IRecipeService recipeService)
    {
        _recipeService = recipeService;
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
        var detail = new RecipeDetailViewModel(_recipeService, recipe)
        {
            BackRequested = ShowList,
            EditRequested = ShowEdit,
            RecipeDeleted = ShowList
        };
        CurrentPage = detail;
    }

    private void ShowEdit(Recipe? existingRecipe)
    {
        var edit = new RecipeEditViewModel(_recipeService, existingRecipe)
        {
            Saved = ShowList,
            Cancelled = ShowList
        };
        CurrentPage = edit;
    }
}

