using Broccoli.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Broccoli.App.Shared.Pages;

public partial class RecipeDetail
{
    [Parameter] public string? RecipeId { get; set; }

    private Recipe? recipe;
    private bool isLoading = true;
    private bool isSaving = false;
    private string? errorMessage;
    private string? successMessage;
    private string newTag = string.Empty;
    private string imageUrl = string.Empty;

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

    private async Task LoadRecipe()
    {
        isLoading = true;
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
            }
            else
            {
                recipe = await RecipeService.GetByIdAsync(RecipeId!);
                if (recipe == null)
                {
                    errorMessage = "Recipe not found.";
                }
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
    }

    private async Task SaveRecipe()
    {
        if (recipe == null) return;

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
        if (recipe == null || IsNewRecipe) return;

        var confirmed = await JSRuntime.InvokeAsync<bool>("confirm",
            $"Are you sure you want to delete '{recipe.Name}'? This action cannot be undone.");

        if (!confirmed) return;

        try
        {
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
        if (string.IsNullOrWhiteSpace(newTag) || recipe == null) return;

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

    private void AddImage()
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || recipe == null) return;

        if (!recipe.Images.Contains(imageUrl))
        {
            recipe.Images.Add(imageUrl);
        }

        imageUrl = string.Empty;
    }

    private void RemoveImage()
    {
        if (recipe == null || !recipe.Images.Any()) return;
        recipe.Images.RemoveAt(0);
    }

    private IEnumerable<string> ParseIngredients(string ingredients)
    {
        return ingredients
            .Split('\n')
            .Select(i => i.Trim())
            .Where(i => !string.IsNullOrWhiteSpace(i));
    }

    private void NavigateBack()
    {
        Navigation.NavigateTo("/recipes");
    }
}