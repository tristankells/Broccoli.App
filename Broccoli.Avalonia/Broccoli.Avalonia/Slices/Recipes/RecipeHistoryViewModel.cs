using System.Collections.ObjectModel;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class RecipeHistoryViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;
    private readonly Recipe _recipe;

    [ObservableProperty]
    private RecipeSnapshot? _selectedSnapshot;

    [ObservableProperty]
    private string _ingredientsPreview = string.Empty;

    [ObservableProperty]
    private string _directionsPreview = string.Empty;

    [ObservableProperty]
    private string? _capturedAtText;

    [ObservableProperty]
    private string? _errorMessage;

    public RecipeHistoryViewModel(IRecipeService recipeService, Recipe recipe)
    {
        _recipeService = recipeService;
        _recipe = recipe;
        History = new ObservableCollection<RecipeSnapshot>(_recipeService.GetHistory(recipe.Id));
    }

    public Recipe Recipe => _recipe;

    public ObservableCollection<RecipeSnapshot> History { get; }

    public Action? BackRequested { get; set; }

    public Action? Restored { get; set; }

    partial void OnSelectedSnapshotChanged(RecipeSnapshot? value)
    {
        if (value is null)
        {
            IngredientsPreview = string.Empty;
            DirectionsPreview = string.Empty;
            CapturedAtText = null;
            return;
        }

        IngredientsPreview = value.Ingredients;
        DirectionsPreview = value.Directions;
        CapturedAtText = $"Captured {value.CapturedAtDisplay}";
    }

    [RelayCommand]
    private void Back() => BackRequested?.Invoke();

    [RelayCommand]
    private void Restore()
    {
        if (SelectedSnapshot is null)
        {
            return;
        }

        Recipe? restored = _recipeService.Restore(_recipe.Id, SelectedSnapshot.Id);
        if (restored is null)
        {
            ErrorMessage = "Unable to restore this version.";
            return;
        }

        Restored?.Invoke();
    }
}
