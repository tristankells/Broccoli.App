using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Broccoli.App.Shared.Models;
using FuzzySharp;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Broccoli.App.Shared._Shared.IngredientParsing;

/// <summary>
/// CosmosDB-backed food lookup service with an in-memory cache for fast matching.
/// All food items share partitionKey = "food" (global shared database).
/// On first startup, if the container is empty and a seed file path is provided,
/// foods are seeded from the JSON file.
/// </summary>
public class CosmosFoodService : IFoodService
{
    private readonly CosmosClient _cosmosClient;
    private readonly string? _seedFilePath;
    private readonly ILogger<CosmosFoodService> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private Container? _container;
    private Dictionary<string, Food> _foodByName = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;

    private const string DatabaseId    = "BroccoliAppDb";
    private const string ContainerId   = "Foods";
    private const string PartitionKeyValue = "food";

    // -- Matching thresholds (mirrors LocalJsonFoodService) ------------------
    private const double TokenThreshold       = 0.7;
    private const double FuzzyThreshold       = 0.6;
    private const int    FuzzySharpThreshold  = 60;

    private static readonly HashSet<string> s_stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "raw", "fresh", "free", "range", "diced", "sliced", "grated", "skinless",
        "lite", "baby", "chopped", "minced", "peeled", "deseeded", "rinsed",
        "drained", "cooked", "uncooked", "dried", "frozen", "canned", "tin", "tinned",
        "large", "small", "medium", "whole", "halved", "roughly", "finely", "thinly"
    };

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CosmosFoodService(
        CosmosClient cosmosClient,
        string? seedFilePath,
        ILogger<CosmosFoodService> logger)
    {
        _cosmosClient = cosmosClient;
        _seedFilePath  = seedFilePath;
        _logger        = logger;
    }

    // -- Initialization ------------------------------------------------------

    public async Task InitializeAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            _logger.LogInformation("Initializing CosmosFoodService...");
            var database = _cosmosClient.GetDatabase(DatabaseId);
            var response = await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties { Id = ContainerId, PartitionKeyPath = "/partitionKey" });
            _container = response.Container;

            await LoadCacheAsync();

            if (_foodByName.Count == 0 && !string.IsNullOrWhiteSpace(_seedFilePath) && File.Exists(_seedFilePath))
            {
                _logger.LogInformation("Foods container is empty — seeding from {Path}", _seedFilePath);
                await SeedFromFileAsync(_seedFilePath);
                await LoadCacheAsync();
            }

            _initialized = true;
            _logger.LogInformation("CosmosFoodService ready. {Count} foods in cache.", _foodByName.Count);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task LoadCacheAsync()
    {
        var results = new List<FoodDocument>();
        using var iterator = _container!.GetItemQueryIterator<FoodDocument>(
            "SELECT * FROM c",
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(PartitionKeyValue) });

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync();
            results.AddRange(page);
        }

        var newCache = new Dictionary<string, Food>(StringComparer.OrdinalIgnoreCase);
        foreach (var doc in results)
        {
            var food = doc.ToFood();
            if (!string.IsNullOrWhiteSpace(food.Name))
                newCache.TryAdd(food.Name, food);
        }
        _foodByName = newCache;
        _logger.LogDebug("Cache loaded: {Count} foods", _foodByName.Count);
    }

    private async Task SeedFromFileAsync(string path)
    {
        string json = await File.ReadAllTextAsync(path);
        var foods = JsonSerializer.Deserialize<List<Food>>(json, s_jsonOptions);
        if (foods == null || foods.Count == 0) return;

        int nextId = 1;
        foreach (var food in foods.Where(f => !string.IsNullOrWhiteSpace(f.Name)))
        {
            food.Id = nextId++;
            var doc = FoodDocument.FromFood(food);
            await _container!.UpsertItemAsync(doc, new PartitionKey(PartitionKeyValue));
        }
        _logger.LogInformation("Seeded {Count} foods from {Path}", nextId - 1, path);
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("CosmosFoodService is not initialized. Call InitializeAsync() first.");
    }

    // -- IFoodService --------------------------------------------------------

    public bool TryGetFood(string name, out Food food)
    {
        if (string.IsNullOrWhiteSpace(name)) { food = null!; return false; }
        return _foodByName.TryGetValue(name, out food!);
    }

    public bool TryGetFoodFuzzy(string name, out Food food)
    {
        if (string.IsNullOrWhiteSpace(name) || _foodByName.Count == 0) { food = null!; return false; }
        var result = FindBestMatch(name);
        if (result.IsMatch && result.Score >= FuzzyThreshold) { food = result.Food!; return true; }
        food = null!;
        return false;
    }

    public Task<IEnumerable<Food>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Food>>(_foodByName.Values.OrderBy(f => f.Id));
    }

    public FoodMatchResult FindBestMatch(string foodDescription)
    {
        if (string.IsNullOrWhiteSpace(foodDescription) || _foodByName.Count == 0)
            return new FoodMatchResult { Score = 0, Method = "None" };

        string query = foodDescription.ToLowerInvariant().Trim();

        // Stage 1: Exact
        if (_foodByName.TryGetValue(query, out Food? exactFood))
            return new FoodMatchResult { Food = exactFood, Score = 1.0, Method = "Exact" };

        // Stage 2: Token / Jaccard
        var tokenResult = ScoreByTokens(query);
        if (tokenResult.Score >= TokenThreshold) return tokenResult;

        // Stage 3: Levenshtein
        var fuzzyResult = ScoreByLevenshtein(query);
        if (fuzzyResult.Score >= FuzzyThreshold) return fuzzyResult;

        // Stage 4: FuzzySharp
        var sharpResult = ScoreByFuzzySharp(query);
        if (sharpResult.Score * 100 >= FuzzySharpThreshold) return sharpResult;

        // Return best candidate below all thresholds
        var best = tokenResult.Score >= fuzzyResult.Score ? tokenResult : fuzzyResult;
        best = best.Score >= sharpResult.Score ? best : sharpResult;
        return best;
    }

    public async Task<Food> AddAsync(Food food)
    {
        EnsureInitialized();
        int nextId = _foodByName.Count > 0 ? _foodByName.Values.Max(f => f.Id) + 1 : 1;
        food.Id = nextId;
        var doc = FoodDocument.FromFood(food);
        await _container!.UpsertItemAsync(doc, new PartitionKey(PartitionKeyValue));
        _foodByName[food.Name] = food;
        _logger.LogInformation("AddAsync: food Id={Id} Name='{Name}'", food.Id, food.Name);
        return food;
    }

    public async Task UpdateAsync(Food food)
    {
        EnsureInitialized();
        // Handle name change: remove old cache entry
        var existing = _foodByName.Values.FirstOrDefault(f => f.Id == food.Id);
        if (existing != null && !string.Equals(existing.Name, food.Name, StringComparison.OrdinalIgnoreCase))
            _foodByName.Remove(existing.Name);

        var doc = FoodDocument.FromFood(food);
        await _container!.UpsertItemAsync(doc, new PartitionKey(PartitionKeyValue));
        _foodByName[food.Name] = food;
        _logger.LogInformation("UpdateAsync: food Id={Id} Name='{Name}'", food.Id, food.Name);
    }

    public async Task DeleteAsync(int id)
    {
        EnsureInitialized();
        var food = _foodByName.Values.FirstOrDefault(f => f.Id == id);
        if (food == null) return;

        await _container!.DeleteItemAsync<FoodDocument>(
            id.ToString(),
            new PartitionKey(PartitionKeyValue));
        _foodByName.Remove(food.Name);
        _logger.LogInformation("DeleteAsync: food Id={Id} Name='{Name}'", id, food.Name);
    }

    // -- Matching helpers (mirrors LocalJsonFoodService) ---------------------

    private FoodMatchResult ScoreByTokens(string input)
    {
        var inputTokens = Tokenise(input);
        inputTokens.ExceptWith(s_stopwords);

        Food? bestFood = null;
        double bestScore = -1;

        foreach (var candidate in _foodByName.Values)
        {
            if (string.IsNullOrWhiteSpace(candidate.Name)) continue;
            var candidateTokens = Tokenise(candidate.Name.Replace(",", " "));
            candidateTokens.ExceptWith(s_stopwords);
            double score = JaccardSimilarity(inputTokens, candidateTokens);
            if (score > bestScore)
            {
                bestScore = score;
                bestFood  = candidate;
                if (score >= 1.0) break;
            }
        }

        return new FoodMatchResult { Food = bestFood, Score = Math.Max(bestScore, 0), Method = "Token" };
    }

    private FoodMatchResult ScoreByLevenshtein(string input)
    {
        Food? bestFood = null;
        double bestScore = -1;

        foreach (var candidate in _foodByName.Values)
        {
            if (string.IsNullOrWhiteSpace(candidate.Name)) continue;
            string target  = candidate.Name.ToLowerInvariant();
            int distance   = LevenshteinDistance(input, target);
            int maxLen     = Math.Max(input.Length, target.Length);
            double score   = maxLen == 0 ? 1.0 : 1.0 - (double)distance / maxLen;
            if (score > bestScore) { bestScore = score; bestFood = candidate; }
        }

        return new FoodMatchResult { Food = bestFood, Score = Math.Max(bestScore, 0), Method = "Fuzzy" };
    }

    private FoodMatchResult ScoreByFuzzySharp(string input)
    {
        Food? bestFood = null;
        int bestRaw = -1;

        foreach (var candidate in _foodByName.Values)
        {
            if (string.IsNullOrWhiteSpace(candidate.Name)) continue;
            int score = Fuzz.TokenSetRatio(input, candidate.Name.ToLowerInvariant());
            if (score > bestRaw)
            {
                bestRaw  = score;
                bestFood = candidate;
                if (score == 100) break;
            }
        }

        double normalised = bestRaw < 0 ? 0 : bestRaw / 100.0;
        return new FoodMatchResult { Food = bestFood, Score = normalised, Method = "FuzzySharp" };
    }

    private static HashSet<string> Tokenise(string text) =>
        new(Regex.Split(text.ToLowerInvariant(), @"\W+").Where(t => t.Length > 1),
            StringComparer.OrdinalIgnoreCase);

    private static double JaccardSimilarity(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 && b.Count == 0) return 1.0;
        if (a.Count == 0 || b.Count == 0) return 0.0;
        int intersection = a.Count(t => b.Contains(t));
        int union = a.Count + b.Count - intersection;
        return (double)intersection / union;
    }

    private static int LevenshteinDistance(string source, string target)
    {
        if (source == target) return 0;
        if (source.Length == 0) return target.Length;
        if (target.Length == 0) return source.Length;

        int[] prev = new int[target.Length + 1];
        int[] curr = new int[target.Length + 1];
        for (int j = 0; j <= target.Length; j++) prev[j] = j;
        for (int i = 1; i <= source.Length; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                curr[j]  = Math.Min(Math.Min(prev[j] + 1, curr[j - 1] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }
        return prev[target.Length];
    }

    // -- CosmosDB document model ---------------------------------------------

    private class FoodDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("partitionKey")]
        public string PartitionKey { get; set; } = PartitionKeyValue;

        [JsonPropertyName("foodId")]
        public int FoodId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("measure")]
        public string Measure { get; set; } = string.Empty;

        [JsonPropertyName("gramsPerMeasure")]
        public double GramsPerMeasure { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("caloriesPer100g")]
        public double CaloriesPer100g { get; set; }

        [JsonPropertyName("fatPer100g")]
        public double FatPer100g { get; set; }

        [JsonPropertyName("saturatedFatPer100g")]
        public double SaturatedFatPer100g { get; set; }

        [JsonPropertyName("carbohydratesPer100g")]
        public double CarbohydratesPer100g { get; set; }

        [JsonPropertyName("dietaryFiberPer100g")]
        public double DietaryFiberPer100g { get; set; }

        [JsonPropertyName("sugarsPer100g")]
        public double SugarsPer100g { get; set; }

        [JsonPropertyName("proteinPer100g")]
        public double ProteinPer100g { get; set; }

        [JsonPropertyName("sodiumMgPer100g")]
        public double SodiumMgPer100g { get; set; }

        public static FoodDocument FromFood(Food f) => new()
        {
            Id                   = f.Id.ToString(),
            PartitionKey         = PartitionKeyValue,
            FoodId               = f.Id,
            Name                 = f.Name ?? string.Empty,
            Measure              = f.Measure ?? string.Empty,
            GramsPerMeasure      = f.GramsPerMeasure,
            Notes                = f.Notes ?? string.Empty,
            CaloriesPer100g      = f.CaloriesPer100g,
            FatPer100g           = f.FatPer100g,
            SaturatedFatPer100g  = f.SaturatedFatPer100g,
            CarbohydratesPer100g = f.CarbohydratesPer100g,
            DietaryFiberPer100g  = f.DietaryFiberPer100g,
            SugarsPer100g        = f.SugarsPer100g,
            ProteinPer100g       = f.ProteinPer100g,
            SodiumMgPer100g      = f.SodiumMgPer100g
        };

        public Food ToFood() => new()
        {
            Id                   = FoodId,
            Name                 = Name,
            Measure              = Measure,
            GramsPerMeasure      = GramsPerMeasure,
            Notes                = Notes,
            CaloriesPer100g      = CaloriesPer100g,
            FatPer100g           = FatPer100g,
            SaturatedFatPer100g  = SaturatedFatPer100g,
            CarbohydratesPer100g = CarbohydratesPer100g,
            DietaryFiberPer100g  = DietaryFiberPer100g,
            SugarsPer100g        = SugarsPer100g,
            ProteinPer100g       = ProteinPer100g,
            SodiumMgPer100g      = SodiumMgPer100g
        };
    }
}
