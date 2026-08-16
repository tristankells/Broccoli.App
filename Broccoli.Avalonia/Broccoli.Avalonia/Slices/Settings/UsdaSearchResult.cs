namespace Broccoli.Avalonia.Slices.Settings;

public class UsdaSearchResult
{
    public int TotalHits { get; set; }

    public int TotalPages { get; set; }

    public int CurrentPage { get; set; }

    public List<UsdaFoodItem> Foods { get; set; } = new();
}
