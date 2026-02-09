using Broccoli.Data.Models;
using Microsoft.AspNetCore.Components;

namespace Broccoli.App.Shared.Pages;

public partial class RecipeReadOnly
{
    [Parameter] public string RecipeId { get; set; } = string.Empty;

    private Recipe? recipe;
    private bool isLoading = true;
    private string? errorMessage;
    private IReadOnlyList<string> ingredients = Array.Empty<string>();
    private string directions;

    protected override async Task OnParametersSetAsync()
    {
        await LoadRecipe();
    }

    private async Task LoadRecipe()
    {
        if (string.IsNullOrWhiteSpace(RecipeId))
        {
            errorMessage = "Recipe not found.";
            isLoading = false;
            return;
        }

        isLoading = true;
        errorMessage = null;

        try
        {
            recipe = await RecipeService.GetByIdAsync(RecipeId);
            if (recipe == null)
            {
                errorMessage = "Recipe not found.";
                ingredients = Array.Empty<string>();
                directions = string.Empty;
                return;
            }

            ingredients = ParseLines(recipe.Ingredients);
            directions = recipe.Directions;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading recipe: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private static IReadOnlyList<string> ParseLines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        return text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private void NavigateBack()
    {
        Navigation.NavigateTo("/recipes");
    }

    private void NavigateToEdit()
    {
        if (!string.IsNullOrWhiteSpace(RecipeId))
        {
            Navigation.NavigateTo($"/recipes/{RecipeId}");
        }
    }
}

