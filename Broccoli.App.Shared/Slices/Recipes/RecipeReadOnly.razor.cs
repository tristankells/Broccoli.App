using Broccoli.App.Shared.Platform;
using Broccoli.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Broccoli.App.Shared.Slices.Recipes;

public partial class RecipeReadOnly : IAsyncDisposable
{
    [Parameter] public string RecipeId { get; set; } = string.Empty;
    [Inject] private IWakeLockService WakeLockService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private Recipe? recipe;
    private bool isLoading = true;
    private string? errorMessage;
    private IReadOnlyList<string> ingredients = Array.Empty<string>();
    private string? directions;

    private bool _keepScreenOn;
    private bool _settingsDialogOpen;
    private bool _wakeLockActive;

    protected override async Task OnParametersSetAsync()
    {
        await LoadRecipe();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        try
        {
            var stored = await JSRuntime.InvokeAsync<string?>(
                "localStorage.getItem", "recipeReadOnlyKeepScreenOn");
            _keepScreenOn = stored == "true";
        }
        catch { _keepScreenOn = false; }

        if (_keepScreenOn)
        {
            await WakeLockService.AcquireAsync();
            _wakeLockActive = true;
        }

        StateHasChanged();
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
                directions = null;
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
            return Array.Empty<string>();

        return text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private void NavigateBack()   => Navigation.NavigateTo("/recipes");

    private void NavigateToEdit()
    {
        if (!string.IsNullOrWhiteSpace(RecipeId))
            Navigation.NavigateTo($"/recipes/{RecipeId}");
    }

    private void OpenSettings()  => _settingsDialogOpen = true;
    private void CloseSettings() => _settingsDialogOpen = false;

    private async Task OnSettingsSaved(bool keepScreenOn)
    {
        _keepScreenOn = keepScreenOn;
        _settingsDialogOpen = false;

        try
        {
            await JSRuntime.InvokeVoidAsync("localStorage.setItem",
                "recipeReadOnlyKeepScreenOn", keepScreenOn ? "true" : "false");
        }
        catch { /* ignore */ }

        if (keepScreenOn)
        {
            await WakeLockService.AcquireAsync();
            _wakeLockActive = true;
        }
        else
        {
            await WakeLockService.ReleaseAsync();
            _wakeLockActive = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_wakeLockActive)
        {
            await WakeLockService.ReleaseAsync();
            _wakeLockActive = false;
        }
    }
}

