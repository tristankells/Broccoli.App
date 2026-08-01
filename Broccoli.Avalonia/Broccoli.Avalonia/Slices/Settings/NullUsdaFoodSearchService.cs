namespace Broccoli.Avalonia.Slices.Settings;

public class NullUsdaFoodSearchService : IUsdaFoodSearchService
{
    public bool IsAvailable => false;

    public Task<UsdaSearchResult> SearchAsync(string query, int page = 1, int pageSize = 10)
        => Task.FromResult(new UsdaSearchResult());
}
