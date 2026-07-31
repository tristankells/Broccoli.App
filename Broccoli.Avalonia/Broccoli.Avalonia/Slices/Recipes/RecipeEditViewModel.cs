using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Recipes;
public record RecipeImageItem(string FileName, string FullPath);

/// <summary>
/// Add/Edit form for a recipe. Pass an existing <see cref="Recipe"/> to edit it, or null to
/// create a new one.
/// </summary>
public partial class RecipeEditViewModel : ViewModelBase
{
    private readonly IRecipeService _recipeService;
    private readonly bool _wasExistingOnOpen;

    /// <summary>
    /// The recipe currently being edited. Starts as the existing recipe passed in, or a fresh
    /// unsaved <see cref="Recipe"/> for new ones; becomes the persisted instance after the first save.
    /// </summary>
    private Recipe _recipe;

    public bool IsNew => !_wasExistingOnOpen;

    public Action? Saved { get; set; }
    public Action? Cancelled { get; set; }

    /// <summary>
    /// Supplied by the view's code-behind (which has access to the platform file picker),
    /// so the view model can request an image file without depending on Avalonia's Visual tree.
    /// Returns the picked file's full path, or null if the user cancelled.
    /// </summary>
    public Func<Task<string?>>? PickImageFileAsync { get; set; }

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

    /// <summary>Images for the recipe currently being edited (empty until first save for new recipes).</summary>
    public ObservableCollection<RecipeImageItem> Images { get; } = new();

    public string RecipeIdForImages => _recipe.Id;

    public RecipeEditViewModel(IRecipeService recipeService, Recipe? existingRecipe)
    {
        _recipeService = recipeService;
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
    }

    public string GetImagePath(string fileName) => _recipeService.GetImagePath(RecipeIdForImages, fileName);

    private void RefreshImages(Recipe recipe)
    {
        Images.Clear();
        foreach (var image in recipe.Images)
        {
            Images.Add(new RecipeImageItem(image, _recipeService.GetImagePath(recipe.Id, image)));
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke();

    [RelayCommand]
    private async Task AddImage()
    {
        if (PickImageFileAsync is null)
        {
            return;
        }

        var path = await PickImageFileAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        // Recipes need to exist on disk before an image folder can hold anything, so make sure
        // this recipe (new or existing) is saved first, then attach the image immediately.
        var recipe = SaveCore();
        recipe = _recipeService.AddImage(recipe, path);
        RefreshImages(recipe);
    }

    [RelayCommand]
    private void RemoveImage(RecipeImageItem image)
    {
        var recipe = SaveCore();
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

    /// <summary>Persists the current form state and returns the saved recipe, without navigating away.</summary>
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
        _recipe.Tags = TagsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

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

    private bool _persisted;
}
