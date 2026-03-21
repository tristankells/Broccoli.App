using System.Net.Http.Json;
using System.Text.Json;
using Broccoli.App.Shared.Configuration;

namespace Broccoli.App.Shared.Services;

/// <summary>
/// Searches the USDA FoodData Central (FDC) API and maps results to
/// <see cref="UsdaFoodItem"/> instances with per-100g nutrient values.
/// </summary>
public class UsdaFoodSearchService : IUsdaFoodSearchService
{
    private readonly HttpClient _http;
    private readonly UsdaSettings _settings;

    private static readonly JsonSerializerOptions s_jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // USDA nutrient IDs we care about
    private const string NutrientParams =
        "nutrients=1008&nutrients=1004&nutrients=1003&nutrients=1005" +
        "&nutrients=1258&nutrients=1079&nutrients=1063&nutrients=1093";

    public UsdaFoodSearchService(HttpClient http, UsdaSettings settings)
    {
        _http = http;
        _settings = settings;
    }

    public async Task<UsdaSearchResult> SearchAsync(string query, int page = 1, int pageSize = 10)
    {
        var url = BuildUrl(query, page, pageSize);
        using var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadFromJsonAsync<FdcSearchResponse>(s_jsonOpts);
        if (raw == null) return new UsdaSearchResult();

        return new UsdaSearchResult
        {
            TotalHits   = raw.TotalHits,
            TotalPages  = raw.TotalPages,
            CurrentPage = raw.CurrentPage,
            Foods       = raw.Foods.Select(MapFood).ToList()
        };
    }

    private string BuildUrl(string query, int page, int pageSize) =>
        $"{_settings.BaseUrl}/foods/search" +
        $"?query={Uri.EscapeDataString(query)}" +
        $"&dataType=Foundation,SR%20Legacy" +
        $"&pageSize={pageSize}&pageNumber={page}" +
        $"&{NutrientParams}" +
        $"&api_key={_settings.ApiKey}";

    private static UsdaFoodItem MapFood(FdcFood f)
    {
        double Get(int id) =>
            f.FoodNutrients?.FirstOrDefault(n => n.NutrientId == id)?.Value ?? 0.0;

        return new UsdaFoodItem
        {
            FdcId         = f.FdcId,
            Description   = f.Description,
            DataType      = f.DataType,
            Calories      = Get(1008),
            Fat           = Get(1004),
            Protein       = Get(1003),
            Carbohydrates = Get(1005),
            SaturatedFat  = Get(1258),
            DietaryFiber  = Get(1079),
            Sugars        = Get(1063),
            SodiumMg      = Get(1093)
        };
    }

    // ── Private deserialization DTOs ────────────────────────────────────────

    private class FdcSearchResponse
    {
        public int TotalHits   { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages  { get; set; }
        public List<FdcFood> Foods { get; set; } = new();
    }

    private class FdcFood
    {
        public int    FdcId       { get; set; }
        public string Description { get; set; } = string.Empty;
        public string DataType    { get; set; } = string.Empty;
        public List<FdcNutrient> FoodNutrients { get; set; } = new();
    }

    private class FdcNutrient
    {
        public int    NutrientId { get; set; }
        public double Value      { get; set; }
    }
}

