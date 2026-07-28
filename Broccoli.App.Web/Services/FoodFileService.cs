using Broccoli.App.Shared._Shared.Platform;
using Microsoft.JSInterop;

namespace Broccoli.App.Web.Services;

public class FoodFileService : IFoodFileService
{
    private readonly IJSRuntime _js;

    public FoodFileService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task ExportFoodsAsync(string filename, string jsonContent)
    {
        await _js.InvokeVoidAsync("foodFile.exportFoods", filename, jsonContent);
    }

    public async Task<string?> ImportFoodsAsync()
    {
        return await _js.InvokeAsync<string?>("foodFile.importFoods");
    }
}
