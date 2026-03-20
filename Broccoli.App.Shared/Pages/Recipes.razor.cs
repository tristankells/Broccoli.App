using Broccoli.Data.Models;
using Microsoft.JSInterop;

namespace Broccoli.App.Shared.Pages;

public partial class Recipes
{
    private List<Recipe> allRecipes = new();
    private IEnumerable<Recipe> filteredRecipes = new List<Recipe>();
    private HashSet<string> availableTags = new();
    private HashSet<string> selectedTags = new();
    private string searchTerm = string.Empty;
    private bool isLoading = true;
    private System.Threading.Timer? searchDebounceTimer;
    private bool showIngredientDialog;
    private Recipe? selectedRecipe;
    private List<PantryItem> userPantryItems = new();
    private bool showImportDialog;

    protected override async Task OnInitializedAsync()
    {
        await LoadRecipes();
    }

    private async Task LoadRecipes()
    {
        isLoading = true;
        StateHasChanged();

        try
        {
            var recipes = await RecipeService.GetAllAsync();
            allRecipes = recipes.OrderByDescending(r => r.CreatedAt).ToList();

            // Extract all unique tags
            availableTags = allRecipes
                .SelectMany(r => r.Tags)
                .Distinct()
                .OrderBy(t => t)
                .ToHashSet();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            // Log error
            Console.WriteLine($"Error loading recipes: {ex.Message}");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void ApplyFilters()
    {
        var results = allRecipes.AsEnumerable();

        // Apply tag filters
        if (selectedTags.Any())
        {
            results = results.Where(r => r.Tags.Any(t => selectedTags.Contains(t)));
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            results = results.Where(r =>
                r.Name.ToLower().Contains(searchLower) ||
                (r.Ingredients?.ToLower().Contains(searchLower) ?? false) ||
                (r.Source?.ToLower().Contains(searchLower) ?? false));
        }

        filteredRecipes = results.ToList();
    }

    private void ToggleTag(string tag)
    {
        if (selectedTags.Contains(tag))
        {
            selectedTags.Remove(tag);
        }
        else
        {
            selectedTags.Add(tag);
        }

        ApplyFilters();
    }

    private void ClearFilters()
    {
        selectedTags.Clear();
        searchTerm = string.Empty;
        ApplyFilters();
    }

    private void OnSearchChanged()
    {
        // Debounce search input
        searchDebounceTimer?.Dispose();
        searchDebounceTimer = new System.Threading.Timer(_ =>
        {
            InvokeAsync(() =>
            {
                ApplyFilters();
                StateHasChanged();
            });
        }, null, 300, Timeout.Infinite);
    }

    private void NavigateToAddRecipe()
    {
        Navigation.NavigateTo("/recipes/new");
    }

    private void OpenImportDialog()
    {
        showImportDialog = true;
        StateHasChanged();
    }

    private void CloseImportDialog()
    {
        showImportDialog = false;
        StateHasChanged();
    }

    private async Task HandleImportConfirm()
    {
        showImportDialog = false;
        await LoadRecipes(); // Refresh the list to include newly imported recipes
    }

    private void NavigateToEditRecipe(string recipeId)
    {
        Navigation.NavigateTo($"/recipes/{recipeId}");
    }

    private void NavigateToReadRecipe(string recipeId)
    {
        Navigation.NavigateTo($"/recipes/{recipeId}/read");
    }

    private async Task DeleteRecipe(Recipe recipe)
    {
        // Simple confirmation - in production, use a proper modal
        if (await JSRuntime.InvokeAsync<bool>("confirm", $"Are you sure you want to delete '{recipe.Name}'?"))
        {
            try
            {
                await RecipeService.DeleteAsync(recipe.Id);
                await LoadRecipes();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting recipe: {ex.Message}");
            }
        }
    }

    private async Task OpenAddIngredientsDialog(Recipe recipe)
    {
        selectedRecipe = recipe;

        // Load pantry items so the dialog can pre-check/uncheck appropriately
        try
        {
            var userId = AuthStateService.CurrentUserId!;
            userPantryItems = await PantryService.GetAllAsync(userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading pantry for dialog: {ex.Message}");
            userPantryItems = new();
        }

        showIngredientDialog = true;
        StateHasChanged();
    }

    private void CloseIngredientDialog()
    {
        showIngredientDialog = false;
        selectedRecipe = null;
        StateHasChanged();
    }

    private async Task HandleAddIngredientsConfirm(List<string> selectedLines)
    {
        showIngredientDialog = false;

        if (!selectedLines.Any())
        {
            return;
        }

        try
        {
            var userId = AuthStateService.CurrentUserId!;
            var items = selectedLines.Select(line => new GroceryListItem
            {
                Name = line,
                UserId = userId,
                IsChecked = false
            }).ToList();

            await GroceryListService.AddMultipleAsync(items);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding ingredients to grocery list: {ex.Message}");
        }
        finally
        {
            selectedRecipe = null;
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        searchDebounceTimer?.Dispose();
    }
}