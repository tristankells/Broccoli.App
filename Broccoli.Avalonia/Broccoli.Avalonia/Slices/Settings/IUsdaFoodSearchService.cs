namespace Broccoli.Avalonia.Slices.Settings;

public interface IUsdaFoodSearchService
{
    bool IsAvailable { get; }
    Task<UsdaSearchResult> SearchAsync(string query, int page = 1, int pageSize = 10);
}

public class UsdaSearchResult
{
    public int TotalHits { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public List<UsdaFoodItem> Foods { get; set; } = new();
}

public class UsdaFoodItem
{
    public int FdcId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public double Calories { get; set; }
    public double Fat { get; set; }
    public double SaturatedFat { get; set; }
    public double Carbohydrates { get; set; }
    public double DietaryFiber { get; set; }
    public double Sugars { get; set; }
    public double Protein { get; set; }
    public double SodiumMg { get; set; }
}
