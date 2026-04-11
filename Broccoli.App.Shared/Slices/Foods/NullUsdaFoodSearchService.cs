namespace Broccoli.App.Shared.Slices.Foods;

/// <summary>
/// No-op implementation used when no USDA API key is configured.
/// </summary>
public class NullUsdaFoodSearchService : IUsdaFoodSearchService
{
    public bool IsAvailable => false;

    public Task<UsdaSearchResult> SearchAsync(string query, int page = 1, int pageSize = 10)
        => Task.FromResult(new UsdaSearchResult());
}
