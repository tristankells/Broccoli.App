using Broccoli.Data.Models;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Broccoli.App.Shared.Pages;

public partial class GroceryList
{
    private List<GroceryListItem> groceryItems = new();
    private string newItemName = string.Empty;
    private bool isLoading = true;
    private bool isAdding;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadItems();
    }

    private async Task LoadItems()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            var userId = AuthStateService.CurrentUserId!;
            groceryItems = await GroceryListService.GetAllAsync(userId);
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading grocery list: {ex.Message}";
            Console.WriteLine($"Error loading grocery list: {ex}");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task AddItem()
    {
        var name = newItemName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        isAdding = true;
        errorMessage = null;

        try
        {
            var userId = AuthStateService.CurrentUserId!;
            var item = new GroceryListItem
            {
                Name = name,
                UserId = userId,
                IsChecked = false
            };

            var created = await GroceryListService.AddAsync(item);
            groceryItems.Insert(0, created);
            newItemName = string.Empty;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error adding item: {ex.Message}";
            Console.WriteLine($"Error adding grocery item: {ex}");
        }
        finally
        {
            isAdding = false;
            StateHasChanged();
        }
    }

    private async Task OnNewItemKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await AddItem();
        }
    }

    private async Task ToggleItem(GroceryListItem item)
    {
        item.IsChecked = !item.IsChecked;
        StateHasChanged();

        try
        {
            await GroceryListService.UpdateAsync(item);
        }
        catch (Exception ex)
        {
            // Revert optimistic update on failure
            item.IsChecked = !item.IsChecked;
            errorMessage = $"Error updating item: {ex.Message}";
            Console.WriteLine($"Error toggling grocery item: {ex}");
            StateHasChanged();
        }
    }

    private async Task DeleteItem(GroceryListItem item)
    {
        try
        {
            var userId = AuthStateService.CurrentUserId!;
            await GroceryListService.DeleteAsync(item.Id, userId);
            groceryItems.Remove(item);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error deleting item: {ex.Message}";
            Console.WriteLine($"Error deleting grocery item: {ex}");
            StateHasChanged();
        }
    }

    private async Task CopyToClipboard()
    {
        try
        {
            var text = string.Join("\n", groceryItems.Select(i => i.Name));
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
        catch (Exception ex)
        {
            errorMessage = $"Error copying to clipboard: {ex.Message}";
            Console.WriteLine($"Error copying grocery list to clipboard: {ex}");
            StateHasChanged();
        }
    }

    private async Task ResetList()
    {
        if (!await JSRuntime.InvokeAsync<bool>("confirm",
                "Are you sure you want to clear your entire grocery list? This cannot be undone."))
        {
            return;
        }

        try
        {
            var userId = AuthStateService.CurrentUserId!;
            await GroceryListService.ResetAsync(userId);
            groceryItems.Clear();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error resetting list: {ex.Message}";
            Console.WriteLine($"Error resetting grocery list: {ex}");
            StateHasChanged();
        }
    }
}