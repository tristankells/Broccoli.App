namespace Broccoli.Avalonia.Slices.Settings;

public interface IUsdaFoodSearchService
{
    bool IsAvailable { get; }

    Task<UsdaSearchResult> SearchAsync(string query, int page = 1, int pageSize = 10);
}
