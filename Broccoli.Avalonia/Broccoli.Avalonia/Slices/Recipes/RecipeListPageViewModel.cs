using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Recipes;
public partial class RecipeListPageViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;

    public Action? AddRecipeRequested { get; set; }
    public Action<Recipe>? RecipeSelected { get; set; }

    public ObservableCollection<Recipe> Recipes { get; } = new();

    public RecipeListPageViewModel(IRecipeService recipeService)
    {
        _recipeService = recipeService;
        Reload();
    }

    public void Reload()
    {
        Recipes.Clear();
        foreach (var recipe in _recipeService.GetAll())
        {
            Recipes.Add(recipe);
        }
    }

    [RelayCommand]
    private void AddRecipe() => AddRecipeRequested?.Invoke();

    [RelayCommand]
    private void SelectRecipe(Recipe recipe) => RecipeSelected?.Invoke(recipe);
}
