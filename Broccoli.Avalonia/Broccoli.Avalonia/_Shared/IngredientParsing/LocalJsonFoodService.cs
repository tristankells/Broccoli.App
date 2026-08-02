using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia.Platform;
using Broccoli.Avalonia.Models;
using FuzzySharp;

namespace Broccoli.Avalonia.IngredientParsing;

public class LocalJsonFoodService : IFoodService
{
    private readonly Dictionary<string, Food> _foodByName;

    private static readonly HashSet<string> s_stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "raw", "fresh", "free", "range", "diced", "sliced", "grated", "skinless",
        "lite", "baby", "chopped", "minced", "peeled", "deseeded", "rinsed",
        "drained", "cooked", "uncooked", "dried", "frozen", "canned", "tin", "tinned",
        "large", "small", "medium", "whole", "halved", "roughly", "finely", "thinly"
    };

    private const double TokenThreshold    = 0.7;
    private const double FuzzyThreshold    = 0.6;
    private const int    FuzzySharpThreshold = 60;

    public LocalJsonFoodService()
    {
        _foodByName = new Dictionary<string, Food>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var uri = new Uri("avares://Broccoli.Avalonia/Assets/FoodDatabase.json");
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            string jsonContent = reader.ReadToEnd();

            var foods = JsonSerializer.Deserialize<List<Food>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (foods == null) return;

            foreach (Food food in foods.Where(f => !string.IsNullOrWhiteSpace(f.Name)))
            {
                _foodByName.TryAdd(food.Name, food);
            }
        }
        catch (Exception)
        {
            // Design-time or missing resource — service works with empty dict
        }
    }

    public bool TryGetFood(string name, out Food food)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            food = null!;
            return false;
        }

        return _foodByName.TryGetValue(name, out food!);
    }

    public bool TryGetFoodFuzzy(string name, out Food food)
    {
        if (string.IsNullOrWhiteSpace(name) || _foodByName.Count == 0)
        {
            food = null!;
            return false;
        }

        FoodMatchResult result = FindBestMatch(name);
        if (result.IsMatch && result.Score >= FuzzyThreshold)
        {
            food = result.Food!;
            return true;
        }

        food = null!;
        return false;
    }

    public List<Food> GetAll() => _foodByName.Values.ToList();

    public Food Add(Food food)
    {
        int nextId = _foodByName.Count > 0 ? _foodByName.Values.Max(f => f.Id) + 1 : 1;
        food.Id = nextId;
        _foodByName[food.Name] = food;
        return food;
    }

    public void Update(Food food)
    {
        var existing = _foodByName.Values.FirstOrDefault(f => f.Id == food.Id);
        if (existing != null && !string.Equals(existing.Name, food.Name, StringComparison.OrdinalIgnoreCase))
        {
            _foodByName.Remove(existing.Name);
        }
        _foodByName[food.Name] = food;
    }

    public void Delete(int id)
    {
        var food = _foodByName.Values.FirstOrDefault(f => f.Id == id);
        if (food != null) _foodByName.Remove(food.Name);
    }

    public FoodMatchResult FindBestMatch(string foodDescription)
    {
        if (string.IsNullOrWhiteSpace(foodDescription) || _foodByName.Count == 0)
        {
            return new FoodMatchResult { Score = 0, Method = "None" };
        }

        string query = foodDescription.ToLowerInvariant().Trim();

        if (_foodByName.TryGetValue(query, out Food? exactFood))
        {
            return new FoodMatchResult { Food = exactFood, Score = 1.0, Method = "Exact" };
        }

        FoodMatchResult tokenResult = ScoreByTokens(query);
        if (tokenResult.Score >= TokenThreshold) return tokenResult;

        FoodMatchResult fuzzyResult = ScoreByLevenshtein(query);
        if (fuzzyResult.Score >= FuzzyThreshold) return fuzzyResult;

        FoodMatchResult fuzzySharpResult = ScoreByFuzzySharp(query);
        if (fuzzySharpResult.Score * 100 >= FuzzySharpThreshold) return fuzzySharpResult;

        FoodMatchResult best = tokenResult.Score >= fuzzyResult.Score ? tokenResult : fuzzyResult;
        best = best.Score >= fuzzySharpResult.Score ? best : fuzzySharpResult;
        return best;
    }

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
                    break;
                }
            }
        }

        return new FoodMatchResult { Food = bestFood, Score = Math.Max(bestScore, 0), Method = "Token" };
    }

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
                    break;
                }
            }
        }

        double normalised = bestRaw < 0 ? 0 : bestRaw / 100.0;
        return new FoodMatchResult { Food = bestFood, Score = normalised, Method = "FuzzySharp" };
    }

    private static HashSet<string> Tokenise(string text) =>
        new(
            Regex.Split(text.ToLowerInvariant(), @"\W+").Where(t => t.Length > 1),
            StringComparer.OrdinalIgnoreCase);

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

        for (int j = 0; j <= target.Length; j++)
        {
            prev[j] = j;
        }

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
