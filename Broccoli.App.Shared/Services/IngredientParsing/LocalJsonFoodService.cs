using System.Text.Json;
using System.Text.RegularExpressions;
using Broccoli.Data.Models;
using Broccoli.Shared.Services;
using FuzzySharp;
using Microsoft.Extensions.Logging;

namespace Broccoli.App.Shared.Services.IngredientParsing;

/// <summary>
/// High-performance food lookup service that loads the food database once
/// and uses an in-memory dictionary for O(1) exact lookups and multi-stage
/// fuzzy matching for approximate matches.
/// </summary>
public class LocalJsonFoodService : IFoodService
{
    private readonly Dictionary<string, Food> _foodByName;
    private readonly ILogger<LocalJsonFoodService> _logger;

    // Stopwords stripped before token matching so they don't skew similarity scores.
    private static readonly HashSet<string> s_stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "raw", "fresh", "free", "range", "diced", "sliced", "grated", "skinless",
        "lite", "baby", "chopped", "minced", "peeled", "deseeded", "rinsed",
        "drained", "cooked", "uncooked", "dried", "frozen", "canned", "tin", "tinned",
        "large", "small", "medium", "whole", "halved", "roughly", "finely", "thinly"
    };

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Thresholds used by <see cref="FindBestMatch"/> to select the winning stage.
    /// </summary>
    private const double TokenThreshold    = 0.7;
    private const double FuzzyThreshold    = 0.6;
    private const int    FuzzySharpThreshold = 60; // out of 100

    public LocalJsonFoodService(string databasePath, ILogger<LocalJsonFoodService> logger)
    {
        _logger = logger;
        _logger.LogTrace("LocalJsonFoodService initialising. DatabasePath={DatabasePath}", databasePath);

        if (!File.Exists(databasePath))
        {
            _logger.LogWarning("Food database file not found at path: {DatabasePath}", databasePath);
            _foodByName = new Dictionary<string, Food>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        _logger.LogTrace("Reading food database file from disk");
        string jsonContent = File.ReadAllText(databasePath);
        _logger.LogTrace("Deserialising food database JSON ({Bytes} bytes)", jsonContent.Length);

        var foods = JsonSerializer.Deserialize<List<Food>>(jsonContent, _jsonOptions);
        _foodByName = new Dictionary<string, Food>(StringComparer.OrdinalIgnoreCase);

        if (foods == null)
        {
            _logger.LogWarning("Deserialisation of food database returned null. No food items loaded.");
            return;
        }

        _logger.LogTrace("Deserialised {Count} raw food records. Building lookup dictionary…", foods.Count);

        int added = 0, skipped = 0;
        foreach (Food food in foods.Where(f => !string.IsNullOrWhiteSpace(f.Name)))
        {
            if (_foodByName.TryAdd(food.Name, food))
            {
                added++;
            }
            else
            {
                skipped++;
                _logger.LogTrace("Duplicate food name skipped: '{FoodName}'", food.Name);
            }
        }

        _logger.LogInformation(
            "Food database loaded. Path={DatabasePath} Added={Added} Skipped={Skipped}",
            databasePath, added, skipped);
    }

    // ── IFoodService ────────────────────────────────────────────────────────

    /// <inheritdoc />
    public bool TryGetFood(string name, out Food food)
    {
        _logger.LogTrace("TryGetFood called. Name='{Name}'", name);

        if (string.IsNullOrWhiteSpace(name))
        {
            food = null!;
            return false;
        }

        bool found = _foodByName.TryGetValue(name, out food!);
        _logger.LogTrace("TryGetFood result. Name='{Name}' Found={Found}", name, found);
        return found;
    }

    /// <inheritdoc />
    public Task<IEnumerable<Food>> GetAllAsync()
    {
        _logger.LogTrace("GetAllAsync called. Returning {Count} food items", _foodByName.Count);
        return Task.FromResult<IEnumerable<Food>>(_foodByName.Values);
    }

    /// <summary>
    /// Legacy fuzzy helper — delegates to <see cref="FindBestMatch"/> and applies the
    /// original Jaccard threshold so existing call-sites continue to work unchanged.
    /// </summary>
    public bool TryGetFoodFuzzy(string name, out Food food)
    {
        _logger.LogTrace("TryGetFoodFuzzy called. Name='{Name}'", name);

        if (string.IsNullOrWhiteSpace(name) || _foodByName.Count == 0)
        {
            food = null!;
            return false;
        }

        FoodMatchResult result = FindBestMatch(name);
        if (result.IsMatch && result.Score >= FuzzyThreshold)
        {
            food = result.Food!;
            _logger.LogDebug(
                "TryGetFoodFuzzy: match found. Query='{Name}' Match='{Match}' Score={Score:F4} Method={Method}",
                name, result.Food!.Name, result.Score, result.Method);
            return true;
        }

        _logger.LogDebug(
            "TryGetFoodFuzzy: no match above threshold. Query='{Name}' BestScore={Score:F4}",
            name, result.Score);
        food = null!;
        return false;
    }

    /// <summary>
    /// Runs a 4-stage matching pipeline and returns the best result with its score and method.
    /// <list type="number">
    ///   <item>Exact case-insensitive lookup (score = 1.0)</item>
    ///   <item>Token overlap / Jaccard similarity with stopword removal (threshold = 0.7)</item>
    ///   <item>Normalised Levenshtein distance (threshold = 0.6)</item>
    ///   <item>FuzzySharp TokenSetRatio, handles word-order differences (threshold = 60/100)</item>
    /// </list>
    /// If no stage clears its threshold the best candidate found across all stages is returned
    /// with its (possibly low) score so the caller can surface it for manual review.
    /// </summary>
    public FoodMatchResult FindBestMatch(string foodDescription)
    {
        if (string.IsNullOrWhiteSpace(foodDescription) || _foodByName.Count == 0)
        {
            return new FoodMatchResult { Score = 0, Method = "None" };
        }

        string query = foodDescription.ToLowerInvariant().Trim();
        _logger.LogTrace("FindBestMatch: foodDescription parsed to '{Query}'", query);

        // ── Stage 1: Exact ────────────────────────────────────────────────
        if (_foodByName.TryGetValue(query, out Food? exactFood))
        {
            _logger.LogTrace("FindBestMatch: exact hit for '{Query}'", query);
            return new FoodMatchResult { Food = exactFood, Score = 1.0, Method = "Exact" };
        }

        // ── Stage 2: Token / Jaccard with stopword removal ────────────────
        FoodMatchResult tokenResult = ScoreByTokens(query);
        if (tokenResult.Score >= TokenThreshold)
        {
            _logger.LogTrace(
                "FindBestMatch: token match '{Match}' score={Score:F4}",
                tokenResult.Food?.Name, tokenResult.Score);
            return tokenResult;
        }

        // ── Stage 3: Normalised Levenshtein ───────────────────────────────
        FoodMatchResult fuzzyResult = ScoreByLevenshtein(query);
        if (fuzzyResult.Score >= FuzzyThreshold)
        {
            _logger.LogTrace(
                "FindBestMatch: levenshtein match '{Match}' score={Score:F4}",
                fuzzyResult.Food?.Name, fuzzyResult.Score);
            return fuzzyResult;
        }

        // ── Stage 4: FuzzySharp TokenSetRatio ────────────────────────────
        FoodMatchResult fuzzySharpResult = ScoreByFuzzySharp(query);
        if (fuzzySharpResult.Score * 100 >= FuzzySharpThreshold)
        {
            _logger.LogTrace(
                "FindBestMatch: FuzzySharp match '{Match}' score={Score:F4}",
                fuzzySharpResult.Food?.Name, fuzzySharpResult.Score);
            return fuzzySharpResult;
        }

        // Return the best we found, even if below all thresholds.
        FoodMatchResult best = tokenResult.Score >= fuzzyResult.Score
            ? tokenResult
            : fuzzyResult;

        best = best.Score >= fuzzySharpResult.Score ? best : fuzzySharpResult;

        _logger.LogDebug(
            "FindBestMatch: no confident match for '{Query}'. Returning best candidate '{Match}' score={Score:F4} method={Method}",
            query, 
            best.Food?.Name, 
            best.Score, 
            best.Method);
        
        return best;
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Tokenises <paramref name="input"/>, removes stopwords, and scores every food by
    /// Jaccard similarity. Returns the best match.
    /// </summary>
    private FoodMatchResult ScoreByTokens(string input)
    {
        HashSet<string> inputTokens = Tokenise(input);
        inputTokens.ExceptWith(s_stopwords);

        Food? bestFood = null;
        double bestScore = -1;

        foreach (Food candidate in _foodByName.Values)
        {
            if (string.IsNullOrWhiteSpace(candidate.Name))
            {
                continue;
            }

            HashSet<string> candidateTokens = Tokenise(candidate.Name.Replace(",", " "));
            candidateTokens.ExceptWith(s_stopwords);

            double score = JaccardSimilarity(inputTokens, candidateTokens);
            if (score > bestScore)
            {
                bestScore = score;
                bestFood  = candidate;
                if (score >= 1.0)
                {
                    break; // perfect — stop early
                }
            }
        }

        return new FoodMatchResult { Food = bestFood, Score = Math.Max(bestScore, 0), Method = "Token" };
    }

    /// <summary>
    /// Scores every food item by normalised Levenshtein distance (1.0 = identical, 0.0 = entirely different).
    /// Returns the best match.
    /// </summary>
    private FoodMatchResult ScoreByLevenshtein(string input)
    {
        Food? bestFood = null;
        double bestScore = -1;

        foreach (Food candidate in _foodByName.Values)
        {
            if (string.IsNullOrWhiteSpace(candidate.Name))
            {
                continue;
            }

            string target  = candidate.Name.ToLowerInvariant();
            int distance   = LevenshteinDistance(input, target);
            int maxLen     = Math.Max(input.Length, target.Length);
            double score   = maxLen == 0 ? 1.0 : 1.0 - (double)distance / maxLen;

            if (score > bestScore)
            {
                bestScore = score;
                bestFood  = candidate;
            }
        }

        return new FoodMatchResult { Food = bestFood, Score = Math.Max(bestScore, 0), Method = "Fuzzy" };
    }

    /// <summary>
    /// Uses FuzzySharp's TokenSetRatio which is robust against word-order differences,
    /// e.g. "diced free range chicken breast" vs "Chicken Breast, Skinless, Raw".
    /// Score is normalised to [0, 1].
    /// </summary>
    private FoodMatchResult ScoreByFuzzySharp(string input)
    {
        Food? bestFood = null;
        int bestRaw = -1;

        foreach (Food candidate in _foodByName.Values)
        {
            if (string.IsNullOrWhiteSpace(candidate.Name))
            {
                continue;
            }

            int score = Fuzz.TokenSetRatio(input, candidate.Name.ToLowerInvariant());
            if (score > bestRaw)
            {
                bestRaw  = score;
                bestFood = candidate;
                if (score == 100)
                {
                    break; // perfect
                }
            }
        }

        double normalised = bestRaw < 0 ? 0 : bestRaw / 100.0;
        return new FoodMatchResult { Food = bestFood, Score = normalised, Method = "FuzzySharp" };
    }

    // ── Static utilities ────────────────────────────────────────────────────

    /// <summary>
    /// Splits text into lowercase word tokens of length > 1, ignoring non-word characters.
    /// </summary>
    private static HashSet<string> Tokenise(string text) =>
        new(
            Regex.Split(text.ToLowerInvariant(), @"\W+").Where(t => t.Length > 1),
            StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Jaccard(A, B) = |A ∩ B| / |A ∪ B|. Returns a value in [0, 1].
    /// </summary>
    private static double JaccardSimilarity(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 && b.Count == 0)
        {
            return 1.0;
        }

        if (a.Count == 0 || b.Count == 0)
        {
            return 0.0;
        }

        int intersection = a.Count(t => b.Contains(t));
        int union        = a.Count + b.Count - intersection;
        return (double)intersection / union;
    }

    /// <summary>
    /// Classic Levenshtein distance (edit distance) between two strings.
    /// Uses two-row DP for O(m*n) time and O(n) space.
    /// </summary>
    private static int LevenshteinDistance(string source, string target)
    {
        if (source == target)
        {
            return 0;
        }

        if (source.Length == 0)
        {
            return target.Length;
        }

        if (target.Length == 0)
        {
            return source.Length;
        }

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
}
