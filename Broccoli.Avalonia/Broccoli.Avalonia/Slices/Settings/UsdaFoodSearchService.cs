using System.Net.Http.Json;
using System.Text.Json;

namespace Broccoli.Avalonia.Slices.Settings;

public class UsdaFoodSearchService : IUsdaFoodSearchService
{
    private const string NutrientParams =
        "nutrients=1008&nutrients=1004&nutrients=1003&nutrients=1005" +
        "&nutrients=1258&nutrients=1079&nutrients=1063&nutrients=1093";

    private static readonly JsonSerializerOptions s_jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly UsdaSettings _settings;

    public UsdaFoodSearchService(HttpClient http, UsdaSettings settings)
    {
        _http = http;
        _settings = settings;
    }

    public bool IsAvailable => true;

    public async Task<UsdaSearchResult> SearchAsync(string query, int page = 1, int pageSize = 10)
    {
        string url = BuildUrl(query, page, pageSize);
        using HttpResponseMessage response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        FdcSearchResponse? raw = await response.Content.ReadFromJsonAsync<FdcSearchResponse>(s_jsonOpts);
        if (raw == null)
        {
            return new UsdaSearchResult();
        }

        return new UsdaSearchResult
        {
            TotalHits = raw.TotalHits,
            TotalPages = raw.TotalPages,
            CurrentPage = raw.CurrentPage,
            Foods = raw.Foods.Select(MapFood).ToList(),
        };
    }

    private static UsdaFoodItem MapFood(FdcFood f)
    {
        double Get(int id) =>
            f.FoodNutrients?.FirstOrDefault(n => n.NutrientId == id)?.Value ?? 0.0;

        return new UsdaFoodItem
        {
            FdcId = f.FdcId,
            Description = f.Description,
            DataType = f.DataType,
            Calories = Get(1008),
            Fat = Get(1004),
            Protein = Get(1003),
            Carbohydrates = Get(1005),
            SaturatedFat = Get(1258),
            DietaryFiber = Get(1079),
            Sugars = Get(1063),
            SodiumMg = Get(1093),
        };
    }

    private string BuildUrl(string query, int page, int pageSize) =>
        $"{_settings.BaseUrl}/foods/search" +
        $"?query={Uri.EscapeDataString(query)}" +
        $"&dataType=Foundation,SR%20Legacy" +
        $"&pageSize={pageSize}&pageNumber={page}" +
        $"&{NutrientParams}" +
        $"&api_key={_settings.ApiKey}";

    private sealed class FdcSearchResponse
    {
        public int TotalHits { get; set; }

        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public List<FdcFood> Foods { get; set; } = new();
    }

    private sealed class FdcFood
    {
        public int FdcId { get; set; }

        public string Description { get; set; } = string.Empty;

        public string DataType { get; set; } = string.Empty;

        public List<FdcNutrient> FoodNutrients { get; set; } = new();
    }

    private sealed class FdcNutrient
    {
        public int NutrientId { get; set; }

        public double Value { get; set; }
    }
}
