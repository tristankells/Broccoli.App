using Broccoli.App.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace Broccoli.App.Shared.Slices.Foods;

public partial class ImportFoodsPreviewDialog
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public List<ImportFoodPreviewItem> Items { get; set; } = new();
    [Parameter] public EventCallback<List<Food>> OnConfirmed { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private async Task Confirm()
    {
        var selected = Items.Where(i => i.IsSelected).Select(i => i.Incoming).ToList();
        await OnConfirmed.InvokeAsync(selected);
    }

    private async Task Cancel() => await OnCancelled.InvokeAsync();

    private static void SelectAll(List<ImportFoodPreviewItem> items, bool selected)
    {
        foreach (var item in items) item.IsSelected = selected;
    }
}
