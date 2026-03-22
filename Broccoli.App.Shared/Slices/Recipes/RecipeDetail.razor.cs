using Broccoli.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Broccoli.App.Shared.Slices.Recipes;

public partial class RecipeDetail
{
    [Parameter] public string? RecipeId { get; set; }

    private Recipe? recipe;
    private bool isLoading = true;
    private bool isSaving = false;
    private bool isUploadingImage = false;
    private bool _jsInitialized;
    private string? errorMessage;
    private string? successMessage;
    private string? imageUploadError;
    private string newTag = string.Empty;

    private SeasonalityResult? _seasonality;
    private bool _seasonalityLoading;

    // Autocomplete state
    private HashSet<string> _allTags = new(StringComparer.OrdinalIgnoreCase);
    private List<string> _tagSuggestions = new();
    private bool _showSuggestions;
    private int _activeSuggestionIndex = -1; // -1 = nothing selected

    private bool IsNewRecipe => string.IsNullOrEmpty(RecipeId) || RecipeId == "new";

    protected override async Task OnInitializedAsync()
    {
        await LoadRecipe();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!isLoading)
        {
            await LoadRecipe();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Initialise the JS drop zone once the recipe form is visible.
        // _jsInitialized is reset to false by LoadRecipe() each time the recipe changes,
        // so the listener is always bound to the current DOM element.
        if (!isLoading && recipe != null && !_jsInitialized)
        {
            _jsInitialized = true;
            try
            {
                await JSRuntime.InvokeVoidAsync("imageDropZone.init", "recipe-drop-zone", "recipe-image-input");
            }
            catch
            {
                // imageDropZone.js is only loaded in the web host; silently ignore in MAUI.
            }
        }
    }

    private async Task LoadRecipe()
    {
        isLoading = true;
        _jsInitialized = false;
        errorMessage = null;

        try
        {
            if (IsNewRecipe)
            {
                recipe = new Recipe
                {
                    Tags = new List<string>(),
                    Images = new List<string>()
                };
                // Still fetch tags so the user gets suggestions on a brand-new recipe
                var allForNew = await RecipeService.GetAllAsync();
                _allTags = allForNew
                    .SelectMany(r => r.Tags)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                // Fire both requests in parallel
                var recipeTask = RecipeService.GetByIdAsync(RecipeId!);
                var allTask = RecipeService.GetAllAsync();
                await Task.WhenAll(recipeTask, allTask);
                recipe = recipeTask.Result;
                _allTags = allTask.Result
                    .SelectMany(r => r.Tags)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (recipe == null)
                    errorMessage = "Recipe not found.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading recipe: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }

        // Score seasonality for existing recipes once the recipe is loaded.
        if (recipe is not null && !string.IsNullOrWhiteSpace(recipe.Ingredients))
        {
            _ = ScoreSeasonalityAsync(recipe.Ingredients);
        }
    }

    private async Task OnIngredientsChanged()
    {
        // Re-render so ParsedIngredientsTable sees the new value, then rescore.
        StateHasChanged();
        if (!string.IsNullOrWhiteSpace(recipe?.Ingredients))
        {
            _ = ScoreSeasonalityAsync(recipe.Ingredients);
        }
        else
        {
            _seasonality = null;
        }
    }

    private async Task ScoreSeasonalityAsync(string ingredientsText)
    {
        _seasonalityLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var matches = await IngredientParserService.ParseAndMatchIngredientsAsync(ingredientsText);
            _seasonality = SeasonalityService.Score(matches);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Seasonality scoring error: {ex.Message}");
            _seasonality = null;
        }
        finally
        {
            _seasonalityLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task SaveRecipe()
    {
        if (recipe == null)
        {
            return;
        }

        isSaving = true;
        errorMessage = null;
        successMessage = null;

        try
        {
            if (IsNewRecipe)
            {
                var created = await RecipeService.AddAsync(recipe);
                successMessage = "Recipe created successfully!";
                await Task.Delay(1000);
                Navigation.NavigateTo($"/recipes/{created.Id}");
            }
            else
            {
                await RecipeService.UpdateAsync(recipe);
                successMessage = "Recipe updated successfully!";
                await Task.Delay(1500);
                Navigation.NavigateTo("/recipes");
            }
        }
        catch (UnauthorizedAccessException)
        {
            errorMessage = "You don't have permission to save this recipe.";
        }
        catch (Exception ex)
        {
            errorMessage = $"Error saving recipe: {ex.Message}";
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task DeleteRecipe()
    {
        if (recipe == null || IsNewRecipe)
        {
            return;
        }

        var confirmed = await JSRuntime.InvokeAsync<bool>("confirm",
            $"Are you sure you want to delete '{recipe.Name}'? This action cannot be undone.");

        if (!confirmed)
        {
            return;
        }

        try
        {
            // Delete any uploaded images from Supabase Storage first
            foreach (var imageUrl in recipe.Images.ToList())
            {
                try { await RecipeImageService.DeleteAsync(imageUrl); }
                catch (Exception ex) { Console.WriteLine($"Failed to delete image during recipe deletion: {ex.Message}"); }
            }

            await RecipeService.DeleteAsync(recipe.Id);
            Navigation.NavigateTo("/recipes");
        }
        catch (Exception ex)
        {
            errorMessage = $"Error deleting recipe: {ex.Message}";
        }
    }

    private void AddTag()
    {
        if (string.IsNullOrWhiteSpace(newTag) || recipe == null)
        {
            return;
        }

        var tag = newTag.Trim();
        if (!recipe.Tags.Contains(tag))
        {
            recipe.Tags.Add(tag);
        }

        newTag = string.Empty;
    }

    private void RemoveTag(string tag)
    {
        recipe?.Tags.Remove(tag);
    }

    private void OnTagKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            AddTag();
        }
    }

    private void OnTagInputChanged(ChangeEventArgs e)
    {
        newTag = e.Value?.ToString() ?? string.Empty;
        _activeSuggestionIndex = -1;

        if (string.IsNullOrWhiteSpace(newTag))
        {
            _tagSuggestions.Clear();
            _showSuggestions = false;
            return;
        }

        // Suggestions: tags that start with the typed text, excluding tags
        // already on this recipe, ordered alphabetically, capped at 8.
        _tagSuggestions = _allTags
            .Where(t => t.StartsWith(newTag, StringComparison.OrdinalIgnoreCase)
                        && !(recipe?.Tags.Contains(t, StringComparer.OrdinalIgnoreCase) ?? false))
            .OrderBy(t => t)
            .Take(8)
            .ToList();

        _showSuggestions = _tagSuggestions.Count > 0;
    }

    private void SelectSuggestion(string tag)
    {
        newTag = tag;
        _showSuggestions = false;
        _activeSuggestionIndex = -1;
        AddTag(); // reuses existing duplicate-check + clear logic
    }

    private async Task HideSuggestions()
    {
        // Delay lets a mousedown on a list item fire before focus leaves the wrapper.
        await Task.Delay(150);
        _showSuggestions = false;
        _activeSuggestionIndex = -1;
    }

    private void OnTagKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "ArrowDown":
                if (_tagSuggestions.Count == 0) break;
                _showSuggestions = true;
                _activeSuggestionIndex = Math.Min(_activeSuggestionIndex + 1, _tagSuggestions.Count - 1);
                break;
            case "ArrowUp":
                if (_tagSuggestions.Count == 0) break;
                _activeSuggestionIndex = Math.Max(_activeSuggestionIndex - 1, -1);
                if (_activeSuggestionIndex == -1) _showSuggestions = _tagSuggestions.Count > 0;
                break;
            case "Enter":
                if (_activeSuggestionIndex >= 0 && _activeSuggestionIndex < _tagSuggestions.Count)
                {
                    SelectSuggestion(_tagSuggestions[_activeSuggestionIndex]);
                }
                else
                {
                    // Fallback: enter with no selection uses the typed text (existing behaviour)
                    AddTag();
                    _showSuggestions = false;
                }
                break;
            case "Escape":
                _showSuggestions = false;
                _activeSuggestionIndex = -1;
                break;
        }
    }

    private async Task HandleImageUpload(InputFileChangeEventArgs e)
    {
        if (recipe == null) return;

        isUploadingImage = true;
        imageUploadError = null;
        StateHasChanged();

        try
        {
            const long MaxBytes = 5 * 1024 * 1024; // 5 MB
            var file = e.File;

            await using var stream = file.OpenReadStream(MaxBytes);
            var url = await RecipeImageService.UploadAsync(stream, file.Name, recipe.Id);

            // Replace any existing image with the new one
            recipe.Images.Clear();
            recipe.Images.Add(url);
        }
        catch (Exception ex)
        {
            imageUploadError = $"Upload failed: {ex.Message}";
            Console.WriteLine($"Image upload error: {ex}");
        }
        finally
        {
            isUploadingImage = false;
            StateHasChanged();
        }
    }

    private async Task RemoveImage()
    {
        if (recipe == null || !recipe.Images.Any()) return;

        var url = recipe.Images[0];
        recipe.Images.RemoveAt(0);

        try
        {
            await RecipeImageService.DeleteAsync(url);
        }
        catch (Exception ex)
        {
            // Log but don't surface — image is already removed from the recipe
            Console.WriteLine($"Failed to delete image from storage: {ex.Message}");
        }
    }


    private void NavigateToReadRecipe()
    {
        if (!string.IsNullOrWhiteSpace(RecipeId))
        {
            Navigation.NavigateTo($"/recipes/{RecipeId}/read");
        }
    }

    private void NavigateBack()
    {
        Navigation.NavigateTo($"/recipes");
    }
}