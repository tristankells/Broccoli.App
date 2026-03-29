using System.Text.Json;
using Broccoli.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Broccoli.App.Shared.Slices.Recipes;

public partial class RecipeDetail : IDisposable
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

    // ── Recipe settings / meal macro comparison ───────────────────────────────
    private MacroTargetSettings _recipeDetailSettings = new();
    private List<MacroTarget> _allTargets = new();
    private bool _recipeSettingsDialogOpen;

    // Nutrition totals from the last ingredient parse (updated by ScoreSeasonalityAsync).
    private double _totalCalories;
    private double _totalProteinG;
    private double _totalCarbsG;
    private double _totalFatG;

    // Per-serving values — automatically reflect the current Servings field.
    private double PerServingCalories => recipe?.Servings > 0 ? _totalCalories / recipe.Servings.Value : 0;
    private double PerServingProteinG => recipe?.Servings > 0 ? _totalProteinG  / recipe.Servings.Value : 0;
    private double PerServingCarbsG   => recipe?.Servings > 0 ? _totalCarbsG    / recipe.Servings.Value : 0;
    private double PerServingFatG     => recipe?.Servings > 0 ? _totalFatG      / recipe.Servings.Value : 0;

    /// <summary>The macro profile currently selected in the recipe settings dialog (null when none chosen).</summary>
    private MacroTarget? ChosenMacroTarget =>
        _allTargets.FirstOrDefault(t => t.Id == _recipeDetailSettings.RecipeMealComparisonPersonId);
    // ─────────────────────────────────────────────────────────────────────────

    // Tracks the ingredients text that has been committed to the nutrition table.
    // Updated only on Enter-key or blur so ParsedIngredientsTable doesn't re-parse
    // on every single keystroke.
    private string? _ingredientsDisplayText;

    // ── Autosave ──────────────────────────────────────────────────────────────
    private enum AutoSaveStatus { Idle, Pending, Saving, Saved, Error }
    private AutoSaveStatus _autoSaveStatus = AutoSaveStatus.Idle;
    private string? _autoSaveError;
    /// <summary>JSON snapshot of the recipe taken after the last successful save.</summary>
    private string? _savedSnapshot;
    private System.Threading.Timer? _autosaveTimer;
    private IDisposable? _locationChangingRegistration;

    private string SerializeRecipe() => JsonSerializer.Serialize(recipe);
    private bool IsDirty => _savedSnapshot is not null && SerializeRecipe() != _savedSnapshot;
    // ─────────────────────────────────────────────────────────────────────────

    // Autocomplete state
    private HashSet<string> _allTags = new(StringComparer.OrdinalIgnoreCase);
    private List<string> _tagSuggestions = new();
    private bool _showSuggestions;
    private int _activeSuggestionIndex = -1; // -1 = nothing selected

    private bool IsNewRecipe => string.IsNullOrEmpty(RecipeId) || RecipeId == "new";

    protected override async Task OnInitializedAsync()
    {
        _locationChangingRegistration = Navigation.RegisterLocationChangingHandler(OnLocationChangingAsync);
        await LoadRecipe();

        // Load macro settings and targets for the meal comparison panel.
        try
        {
            var userId = AuthStateService.CurrentUserId ?? string.Empty;
            _recipeDetailSettings = await MacroTargetService.GetSettingsAsync(userId);
            _allTargets = await MacroTargetService.GetAllAsync(userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load macro settings for recipe comparison: {ex.Message}");
        }
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

        // Capture the initial committed display text for ParsedIngredientsTable.
        _ingredientsDisplayText = recipe?.Ingredients;

        // Capture autosave baseline snapshot for existing recipes.
        if (!IsNewRecipe && recipe is not null)
        {
            _savedSnapshot = SerializeRecipe();
            _autoSaveStatus = AutoSaveStatus.Idle;
        }
        else
        {
            _savedSnapshot = null; // new recipes don't autosave
        }

        // Score seasonality for existing recipes once the recipe is loaded.
        if (recipe is not null && !string.IsNullOrWhiteSpace(recipe.Ingredients))
        {
            _ = ScoreSeasonalityAsync(recipe.Ingredients);
        }
    }

    private async Task OnIngredientsChanged()
    {
        // Commit the live model value to the display text so ParsedIngredientsTable
        // and the seasonality panel receive the updated ingredients.
        _ingredientsDisplayText = recipe?.Ingredients;

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

        HandleFieldChanged();
    }

    /// <summary>
    /// Keeps <c>recipe.Ingredients</c> in sync with the textarea on every keystroke
    /// without triggering an expensive re-parse of the nutrition table.
    /// </summary>
    private void OnIngredientsInput(ChangeEventArgs e)
    {
        if (recipe is not null)
            recipe.Ingredients = e.Value?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Triggers a nutrition/seasonality recalculation when the user presses Enter
    /// (i.e. moves to a new ingredient line), rather than waiting for blur.
    /// </summary>
    private async Task OnIngredientsKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await OnIngredientsChanged();
    }

    // ── Autosave ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Called whenever any editable field changes. Resets the 2-second debounce
    /// timer so autosave fires only after the user pauses. No-ops for new recipes.
    /// </summary>
    private void HandleFieldChanged()
    {
        if (IsNewRecipe) return;

        _autoSaveStatus = AutoSaveStatus.Pending;
        _autosaveTimer?.Dispose();
        _autosaveTimer = new System.Threading.Timer(
            _ => InvokeAsync(async () => await AutoSaveAsync()),
            null,
            dueTime: 2_000,
            period: System.Threading.Timeout.Infinite);
    }

    /// <summary>
    /// Persists the current recipe to Cosmos DB and updates the snapshot.
    /// All UI mutations go through <see cref="InvokeAsync"/> for thread safety.
    /// </summary>
    private async Task AutoSaveAsync()
    {
        _autosaveTimer?.Dispose();
        _autosaveTimer = null;

        if (!IsDirty) return;

        _autoSaveStatus = AutoSaveStatus.Saving;
        await InvokeAsync(StateHasChanged);

        try
        {
            // UpdateAsync mutates recipe.UpdatedAt in-place; capture the return
            // value to keep recipe in sync with what was persisted.
            recipe = await RecipeService.UpdateAsync(recipe!);
            _savedSnapshot = SerializeRecipe();
            _autoSaveStatus = AutoSaveStatus.Saved;
            await InvokeAsync(StateHasChanged);

            // Show "Saved" for 3 s, then silently revert to Idle.
            await Task.Delay(3_000);
            if (_autoSaveStatus == AutoSaveStatus.Saved)
            {
                _autoSaveStatus = AutoSaveStatus.Idle;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (Exception ex)
        {
            _autoSaveError = ex.Message;
            _autoSaveStatus = AutoSaveStatus.Error;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Flushes any pending autosave before Blazor router performs a navigation.
    /// Capped at 3 s so a slow Cosmos call never blocks navigation indefinitely.
    /// </summary>
    private async ValueTask OnLocationChangingAsync(LocationChangingContext ctx)
    {
        _autosaveTimer?.Dispose();
        _autosaveTimer = null;

        if (IsDirty)
            await Task.WhenAny(AutoSaveAsync(), Task.Delay(3_000));
    }

    public void Dispose()
    {
        _autosaveTimer?.Dispose();
        _locationChangingRegistration?.Dispose();
    }

    // ─────────────────────────────────────────────────────────────────────────

    private async Task ScoreSeasonalityAsync(string ingredientsText)
    {
        _seasonalityLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var matches = await IngredientParserService.ParseAndMatchIngredientsAsync(ingredientsText);
            _seasonality = SeasonalityService.Score(matches);

            // Reuse the same parsed matches to update nutrition totals for the meal comparison panel.
            _totalCalories = matches.Where(m => m.IsMatched).Sum(m => m.GetCalories());
            _totalProteinG = matches.Where(m => m.IsMatched).Sum(m => m.GetProtein());
            _totalCarbsG   = matches.Where(m => m.IsMatched).Sum(m => m.GetCarbohydrates());
            _totalFatG     = matches.Where(m => m.IsMatched).Sum(m => m.GetFat());
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

    // ── Recipe settings ───────────────────────────────────────────────────────

    private async Task OnRecipeSettingsSaved(MacroTargetSettings updated)
    {
        _recipeSettingsDialogOpen = false;
        try
        {
            _recipeDetailSettings = await MacroTargetService.SaveSettingsAsync(updated);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save recipe settings: {ex.Message}");
            // Still apply locally so the UI reflects the change even if persist failed.
            _recipeDetailSettings = updated;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    private async Task SaveRecipe()
    {
        if (recipe == null)
        {
            return;
        }

        // Cancel any pending autosave — manual save takes precedence.
        _autosaveTimer?.Dispose();
        _autosaveTimer = null;

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
                recipe = await RecipeService.UpdateAsync(recipe);
                // Refresh snapshot so the location-changing handler sees IsDirty = false
                // and doesn't attempt a redundant autosave during the navigation.
                _savedSnapshot = SerializeRecipe();
                _autoSaveStatus = AutoSaveStatus.Idle;
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
        HandleFieldChanged();
    }

    private void RemoveTag(string tag)
    {
        recipe?.Tags.Remove(tag);
        HandleFieldChanged();
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
            // Log but don't surface � image is already removed from the recipe
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