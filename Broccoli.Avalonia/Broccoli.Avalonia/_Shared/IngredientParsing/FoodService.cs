using System.Text.RegularExpressions;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Storage;
using FuzzySharp;
using Microsoft.EntityFrameworkCore;

namespace Broccoli.Avalonia.IngredientParsing;

/// <summary>
/// SQLite-backed food store. The full food list is cached in memory for fast fuzzy matching;
/// <see cref="Add"/>, <see cref="Update"/>, <see cref="Delete"/>, and <see cref="ResetToSeed"/>
/// write through to SQLite. The embedded <c>FoodDatabase.json</c> is used only as the initial seed.
/// </summary>
public class FoodService : IFoodService
{
    private const double TokenThreshold = 0.7;
    private const double FuzzyThreshold = 0.6;
    private const int FuzzySharpThreshold = 60;

    private static readonly HashSet<string> s_stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "raw", "fresh", "free", "range", "diced", "sliced", "grated", "skinless",
        "lite", "baby", "chopped", "minced", "peeled", "deseeded", "rinsed",
        "drained", "cooked", "uncooked", "dried", "frozen", "canned", "tin", "tinned",
        "large", "small", "medium", "whole", "halved", "roughly", "finely", "thinly",
    };

    private readonly Func<BroccoliDbContext> _contextFactory;
    private readonly Dictionary<string, Food> _foodByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private bool _loaded;

    public FoodService()
        : this(BroccoliDbContext.CreateForApp)
    {
    }

    public FoodService(Func<BroccoliDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public bool TryGetFood(string name, out Food food)
    {
        EnsureLoaded();
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                food = null!;
                return false;
            }

            return _foodByName.TryGetValue(name, out food!);
        }
    }

    public bool TryGetFoodFuzzy(string name, out Food food)
    {
        if (string.IsNullOrWhiteSpace(name))
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

    public List<Food> GetAll() => Snapshot();

    public Food Add(Food food)
    {
        EnsureLoaded();
        lock (_lock)
        {
            int nextId = _foodByName.Values.Count > 0 ? _foodByName.Values.Max(f => f.Id) + 1 : 1;
            food.Id = nextId;
            food.IsCustom = true;

            using BroccoliDbContext context = _contextFactory();
            context.Foods.Add(food);
            context.SaveChanges();

            _foodByName[food.Name] = food;
            return food;
        }
    }

    public void Update(Food food)
    {
        EnsureLoaded();
        lock (_lock)
        {
            Food? existing = _foodByName.Values.FirstOrDefault(f => f.Id == food.Id);
            if (existing is not null && !string.Equals(existing.Name, food.Name, StringComparison.OrdinalIgnoreCase))
            {
                _foodByName.Remove(existing.Name);
            }

            _foodByName[food.Name] = food;

            using BroccoliDbContext context = _contextFactory();
            context.Foods.Update(food);
            context.SaveChanges();
        }
    }

    public void Delete(int id)
    {
        EnsureLoaded();
        lock (_lock)
        {
            Food? food = _foodByName.Values.FirstOrDefault(f => f.Id == id);
            if (food is not null)
            {
                _foodByName.Remove(food.Name);
            }

            using BroccoliDbContext context = _contextFactory();
            Food? toDelete = context.Foods.Find(id);
            if (toDelete is not null)
            {
                context.Foods.Remove(toDelete);
                context.SaveChanges();
            }
        }
    }

    public void ResetToSeed()
    {
        lock (_lock)
        {
            using BroccoliDbContext context = _contextFactory();
            FoodDatabaseSeeder.Reset(context);

            _foodByName.Clear();
            foreach (Food food in context.Foods.AsNoTracking().OrderBy(f => f.Id))
            {
                _foodByName[food.Name] = food;
            }

            _loaded = true;
        }
    }

    public FoodMatchResult FindBestMatch(string foodDescription)
    {
        if (string.IsNullOrWhiteSpace(foodDescription))
        {
            return new FoodMatchResult { Score = 0, Method = "None" };
        }

        string query = foodDescription.ToLowerInvariant().Trim();

        (Food? exact, List<Food> foods) = SnapshotWithExact(query);

        if (exact is not null)
        {
            return new FoodMatchResult { Food = exact, Score = 1.0, Method = "Exact" };
        }

        if (foods.Count == 0)
        {
            return new FoodMatchResult { Score = 0, Method = "None" };
        }

        FoodMatchResult tokenResult = FirstOrEmpty(ScoreByTokens(foods, query, 1), "Token");
        if (tokenResult.Score >= TokenThreshold)
        {
            return tokenResult;
        }

        FoodMatchResult fuzzyResult = FirstOrEmpty(ScoreByLevenshtein(foods, query, 1), "Fuzzy");
        if (fuzzyResult.Score >= FuzzyThreshold)
        {
            return fuzzyResult;
        }

        FoodMatchResult fuzzySharpResult = FirstOrEmpty(ScoreByFuzzySharp(foods, query, 1), "FuzzySharp");
        if (fuzzySharpResult.Score * 100 >= FuzzySharpThreshold)
        {
            return fuzzySharpResult;
        }

        FoodMatchResult best = tokenResult.Score >= fuzzyResult.Score ? tokenResult : fuzzyResult;
        best = best.Score >= fuzzySharpResult.Score ? best : fuzzySharpResult;
        return best;
    }

    public IReadOnlyList<FoodMatchResult> FindMatches(string foodDescription, int maxResults = 10)
    {
        if (maxResults <= 0 || string.IsNullOrWhiteSpace(foodDescription))
        {
            return [];
        }

        string query = foodDescription.ToLowerInvariant().Trim();

        (Food? exact, List<Food> foods) = SnapshotWithExact(query);

        if (exact is not null)
        {
            return [new FoodMatchResult { Food = exact, Score = 1.0, Method = "Exact" }];
        }

        if (foods.Count == 0)
        {
            return [];
        }

        var merged = new List<FoodMatchResult>();
        merged.AddRange(ScoreByTokens(foods, query, maxResults));
        merged.AddRange(ScoreByLevenshtein(foods, query, maxResults));
        merged.AddRange(ScoreByFuzzySharp(foods, query, maxResults));

        return merged
            .Where(result => result.Food is not null && result.Score > 0)
            .GroupBy(result => result.Food!.Id)
            .Select(group => group.OrderByDescending(result => result.Score).First())
            .OrderByDescending(result => result.Score)
            .Take(maxResults)
            .ToList();
    }

    public FoodMatchResult ScoreMatch(string ingredientDescription, string candidateName)
    {
        if (string.IsNullOrWhiteSpace(ingredientDescription) || string.IsNullOrWhiteSpace(candidateName))
        {
            return new FoodMatchResult { Score = 0, Method = "None" };
        }

        string query = ingredientDescription.ToLowerInvariant().Trim();

        if (string.Equals(query, candidateName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return new FoodMatchResult { Score = 1.0, Method = "Exact" };
        }

        var foods = new List<Food> { new Food { Id = -1, Name = candidateName } };

        FoodMatchResult tokenResult = FirstOrEmpty(ScoreByTokens(foods, query, 1), "Token");
        FoodMatchResult fuzzyResult = FirstOrEmpty(ScoreByLevenshtein(foods, query, 1), "Fuzzy");
        FoodMatchResult fuzzySharpResult = FirstOrEmpty(ScoreByFuzzySharp(foods, query, 1), "FuzzySharp");

        FoodMatchResult best = tokenResult.Score >= fuzzyResult.Score ? tokenResult : fuzzyResult;
        best = best.Score >= fuzzySharpResult.Score ? best : fuzzySharpResult;
        return best;
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
        int union = a.Count + b.Count - intersection;
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
                curr[j] = Math.Min(Math.Min(prev[j] + 1, curr[j - 1] + 1), prev[j - 1] + cost);
            }

            (prev, curr) = (curr, prev);
        }

        return prev[target.Length];
    }

    private static List<FoodMatchResult> ScoreByTokens(List<Food> foods, string input, int maxResults)
    {
        HashSet<string> inputTokens = Tokenise(input);
        inputTokens.ExceptWith(s_stopwords);

        var scored = new List<FoodMatchResult>();

        foreach (Food candidate in foods)
        {
            if (string.IsNullOrWhiteSpace(candidate.Name))
            {
                continue;
            }

            HashSet<string> candidateTokens = Tokenise(candidate.Name.Replace(",", " "));
            candidateTokens.ExceptWith(s_stopwords);

            double score = JaccardSimilarity(inputTokens, candidateTokens);
            scored.Add(new FoodMatchResult { Food = candidate, Score = score, Method = "Token" });
        }

        return scored.OrderByDescending(result => result.Score).Take(maxResults).ToList();
    }

    private static List<FoodMatchResult> ScoreByLevenshtein(List<Food> foods, string input, int maxResults)
    {
        var scored = new List<FoodMatchResult>();

        foreach (Food candidate in foods)
        {
            if (string.IsNullOrWhiteSpace(candidate.Name))
            {
                continue;
            }

            string target = candidate.Name.ToLowerInvariant();
            int distance = LevenshteinDistance(input, target);
            int maxLen = Math.Max(input.Length, target.Length);
            double score = maxLen == 0 ? 1.0 : 1.0 - ((double)distance / maxLen);

            scored.Add(new FoodMatchResult { Food = candidate, Score = score, Method = "Fuzzy" });
        }

        return scored.OrderByDescending(result => result.Score).Take(maxResults).ToList();
    }

    private static List<FoodMatchResult> ScoreByFuzzySharp(List<Food> foods, string input, int maxResults)
    {
        var scored = new List<FoodMatchResult>();

        foreach (Food candidate in foods)
        {
            if (string.IsNullOrWhiteSpace(candidate.Name))
            {
                continue;
            }

            int score = Fuzz.TokenSetRatio(input, candidate.Name.ToLowerInvariant());
            scored.Add(new FoodMatchResult { Food = candidate, Score = score / 100.0, Method = "FuzzySharp" });
        }

        return scored.OrderByDescending(result => result.Score).Take(maxResults).ToList();
    }

    private static FoodMatchResult FirstOrEmpty(List<FoodMatchResult> results, string method) =>
        results.Count > 0 ? results[0] : new FoodMatchResult { Score = 0, Method = method };

    private void EnsureLoaded()
    {
        lock (_lock)
        {
            if (_loaded)
            {
                return;
            }

            try
            {
                using BroccoliDbContext context = _contextFactory();
                List<Food> foods = context.Foods.AsNoTracking().OrderBy(f => f.Id).ToList();
                if (foods.Count == 0)
                {
                    FoodDatabaseSeeder.SeedIfEmpty(context);
                    foods = context.Foods.AsNoTracking().OrderBy(f => f.Id).ToList();
                }

                foreach (Food food in foods)
                {
                    _foodByName[food.Name] = food;
                }
            }
            catch (Exception)
            {
                // Database not yet migrated/available (e.g. design-time) — leave the cache empty.
            }

            _loaded = true;
        }
    }

    private List<Food> Snapshot()
    {
        EnsureLoaded();
        lock (_lock)
        {
            return _foodByName.Values.ToList();
        }
    }

    private (Food? Exact, List<Food> Foods) SnapshotWithExact(string query)
    {
        EnsureLoaded();
        lock (_lock)
        {
            _foodByName.TryGetValue(query, out Food? exact);
            return (exact, _foodByName.Values.ToList());
        }
    }
}
