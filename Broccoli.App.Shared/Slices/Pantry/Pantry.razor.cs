using Broccoli.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Broccoli.App.Shared.Slices.Pantry;

public partial class Pantry
{
    private List<PantryItem> pantryItems = new();
    private string newItemName = string.Empty;
    private PantryCategory newItemCategory = PantryCategory.CheckIfHave;
    private bool isLoading = true;
    private bool isAdding;
    private string? errorMessage;

    private IEnumerable<PantryItem> AlwaysHaveItems => pantryItems.Where(i => i.Category == PantryCategory.AlwaysHave);

    private IEnumerable<PantryItem> CheckIfHaveItems =>
        pantryItems.Where(i => i.Category == PantryCategory.CheckIfHave);

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
            pantryItems = await PantryService.GetAllAsync(userId);
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading pantry: {ex.Message}";
            Console.WriteLine($"Error loading pantry: {ex}");
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
            var item = new PantryItem
            {
                Name = name,
                Category = newItemCategory,
                UserId = userId
            };

            var created = await PantryService.AddAsync(item);
            pantryItems.Add(created);
            newItemName = string.Empty;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error adding item: {ex.Message}";
            Console.WriteLine($"Error adding pantry item: {ex}");
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

    private async Task ChangeCategoryAsync(PantryItem item, ChangeEventArgs e)
    {
        if (e.Value is null)
        {
            return;
        }

        var oldCategory = item.Category;

        try
        {
            if (Enum.TryParse<PantryCategory>(e.Value.ToString(), out var newCategory))
            {
                item.Category = newCategory;
                await PantryService.UpdateAsync(item);
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            item.Category = oldCategory;
            errorMessage = $"Error updating category: {ex.Message}";
            Console.WriteLine($"Error changing pantry item category: {ex}");
            StateHasChanged();
        }
    }

    private async Task DeleteItem(PantryItem item)
    {
        try
        {
            var userId = AuthStateService.CurrentUserId!;
            await PantryService.DeleteAsync(item.Id, userId);
            pantryItems.Remove(item);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error deleting item: {ex.Message}";
            Console.WriteLine($"Error deleting pantry item: {ex}");
            StateHasChanged();
        }
    }
}