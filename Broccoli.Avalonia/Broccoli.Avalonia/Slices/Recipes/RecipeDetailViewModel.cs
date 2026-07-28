using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Recipes;
public partial class RecipeDetailViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;

    public Action? BackRequested { get; set; }
    public Action<Recipe>? EditRequested { get; set; }
    public Action? RecipeDeleted { get; set; }

    [ObservableProperty]
    private Recipe _recipe;

    [ObservableProperty]
    private bool _isConfirmingDelete;

    /// <summary>Full file paths to this recipe's images, for binding to <c>Image.Source</c>.</summary>
    public ObservableCollection<string> ImagePaths { get; } = new();

    public RecipeDetailViewModel(IRecipeService recipeService, Recipe recipe)
    {
        _recipeService = recipeService;
        _recipe = recipe;
        foreach (var image in recipe.Images)
        {
            ImagePaths.Add(recipeService.GetImagePath(recipe.Id, image));
        }
    }

    [RelayCommand]
    private void Back() => BackRequested?.Invoke();

    [RelayCommand]
    private void Edit() => EditRequested?.Invoke(Recipe);

    [RelayCommand]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand]
    private void ConfirmDelete()
    {
        _recipeService.Delete(Recipe.Id);
        IsConfirmingDelete = false;
        RecipeDeleted?.Invoke();
    }
}
