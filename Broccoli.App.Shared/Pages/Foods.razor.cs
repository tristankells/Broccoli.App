using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Pages;

public partial class Foods
{
    private IEnumerable<Food>? _foods;

    protected override async Task OnInitializedAsync()
    {
        _foods = await FoodService.GetAllAsync();
    }
}