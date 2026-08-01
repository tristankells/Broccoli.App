using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class RecipeListPageViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;
    private List<Recipe> _allRecipes;

    public Action? AddRecipeRequested { get; set; }
    public Action? ImportRecipeRequested { get; set; }

    public Action<Recipe>? RecipeSelected { get; set; }

    public ObservableCollection<Recipe> FilteredRecipes { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; }

    public RecipeListPageViewModel(IRecipeService recipeService)
    {
        _recipeService = recipeService;
        Reload();
    }

    public void Reload()
    {
        _allRecipes = [.. _recipeService.GetAll()];

        FilteredRecipes = new ObservableCollection<Recipe>(_allRecipes);
    }

    /// <summary>Fires on every keystroke in the search box; wire up filtering of <see cref="Recipes"/> here.</summary>
    partial void OnSearchTextChanged(string value)
    {
        FilterRecipes(value);
    }

    /// <summary>TODO: filter <see cref="Recipes"/> (or an underlying source collection) by <paramref name="searchText"/>.</summary>
    private void FilterRecipes(string searchText)
    {
        // FilteredRecipes = _allRecipes.
    }

    [RelayCommand]
    private void AddRecipe() => AddRecipeRequested?.Invoke();

    [RelayCommand]
    private void ImportRecipe() => ImportRecipeRequested?.Invoke();

    [RelayCommand]
    private void SelectRecipe(Recipe recipe) => RecipeSelected?.Invoke(recipe);
}

