using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.Messaging;

namespace Broccoli.Avalonia.Seasonality;

/// <summary>
/// Scores recipe ingredients against the produce seasonality dataset held in SQLite (see
/// <see cref="ISeasonalityDataStore"/>). The dataset is cached in memory and reloaded whenever
/// <see cref="SeasonalityDataChangedMessage"/> is raised, so edits made on the Seasonality page
/// are reflected in recipe scores without a restart.
/// </summary>
public class SeasonalityService : ISeasonalityService
{
    private const double MinGrams = 5.0;

    private static readonly HashSet<string> s_stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "raw", "fresh", "free", "range", "diced", "sliced", "grated", "skinless",
        "lite", "baby", "chopped", "minced", "peeled", "deseeded", "rinsed",
        "drained", "cooked", "uncooked", "dried", "frozen", "canned", "tin", "tinned",
        "large", "small", "medium", "whole", "halved", "roughly", "finely", "thinly",
    };

    private static readonly Dictionary<string, string> s_pluralFixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["strawberries"] = "strawberry",
        ["raspberries"] = "raspberry",
        ["blackberries"] = "blackberry",
        ["boysenberries"] = "boysenberry",
        ["blueberries"] = "blueberry",
        ["cherries"] = "cherry",
        ["gooseberries"] = "gooseberry",
        ["redcurrants"] = "redcurrant",
        ["blackcurrants"] = "blackcurrant",
        ["apricots"] = "apricot",
        ["nectarines"] = "nectarine",
        ["peaches"] = "peach",
        ["plums"] = "plum",
        ["pears"] = "pear",
        ["lemons"] = "lemon",
        ["limes"] = "lime",
        ["mandarins"] = "mandarin",
        ["mushrooms"] = "mushroom",
        ["tomatoes"] = "tomato",
        ["potatoes"] = "potato",
    };

    private readonly ISeasonalityDataStore _dataStore;
    private readonly object _gate = new();
    private Dictionary<string, ProduceItem> _produceByNormalisedName = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    public SeasonalityService(ISeasonalityDataStore dataStore)
    {
        _dataStore = dataStore;
        WeakReferenceMessenger.Default.Register<SeasonalityDataChangedMessage>(this, (_, _) => Reload());
    }

    public static string NormaliseName(string name)
    {
        string result = name.ToLowerInvariant();
        int comma = result.IndexOf(',');
        if (comma >= 0)
        {
            result = result[..comma];
        }

        var tokens = result.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => !s_stopwords.Contains(t)).ToList();
        for (int i = 0; i < tokens.Count; i++)
        {
            if (s_pluralFixes.TryGetValue(tokens[i], out string? singular))
            {
                tokens[i] = singular;
            }
            else if (tokens[i].EndsWith('s') && tokens[i].Length >= 4)
            {
                tokens[i] = tokens[i][..^1];
            }
        }

        return string.Join(" ", tokens).Trim();
    }

    public SeasonalityResult Score(IEnumerable<ParsedIngredientMatch> matches, DateTime? asOf = null)
    {
        string season = SeasonHelper.GetCurrentSeason(asOf ?? DateTime.Now);
        Dictionary<string, ProduceItem> lookup = GetLookup();

        var matched = new List<(ProduceItem Produce, double Grams)>();
        foreach (ParsedIngredientMatch m in matches)
        {
            if (!m.IsMatched || m.MatchedFood is null)
            {
                continue;
            }

            double grams = m.GetWeightInGrams();
            if (grams < MinGrams)
            {
                continue;
            }

            ProduceItem? produce = LookupProduce(m.MatchedFood.Name, lookup);
            if (produce is null)
            {
                continue;
            }

            matched.Add((produce, grams));
        }

        if (matched.Count == 0)
        {
            return new SeasonalityResult { Score = null, Label = SeasonalityLabel.Unavailable, Breakdown = new List<IngredientSeasonalityDetail>(), BestSeasons = string.Empty };
        }

        (List<IngredientSeasonalityDetail>? breakdown, double totalWeighted, double totalPossible) = ComputeForSeason(matched, season);

        double score = totalPossible > 0 ? (totalWeighted / totalPossible) * 100.0 : 0;
        SeasonalityLabel label = score >= 75 ? SeasonalityLabel.PeakSeason
                               : score >= 40 ? SeasonalityLabel.PartiallyInSeason
                               : SeasonalityLabel.OffSeason;

        string bestSeasons = ComputeBestSeasons(matched);

        return new SeasonalityResult { Score = score, Label = label, Breakdown = breakdown, BestSeasons = bestSeasons };
    }

    private void Reload()
    {
        lock (_gate)
        {
            _loaded = false;
            LoadLocked();
        }
    }

    /// <summary>
    /// Returns the current produce lookup, loading it from the store on first use. Loading is
    /// deferred (rather than done in the constructor) so the singleton can be resolved before the
    /// database has been migrated at startup without touching the not-yet-created tables. A failed
    /// load is retried on the next call, so a pre-migration score naturally recovers afterwards.
    /// </summary>
    private Dictionary<string, ProduceItem> GetLookup()
    {
        lock (_gate)
        {
            LoadLocked();
            return _produceByNormalisedName;
        }
    }

    private void LoadLocked()
    {
        if (_loaded)
        {
            return;
        }

        try
        {
            List<ProduceItem> items = _dataStore.GetAll();
            _produceByNormalisedName = BuildLookup(items);
            _loaded = true;
        }
        catch (Exception)
        {
            // Database not ready yet (e.g. first run before migrations apply) - keep the previous
            // dataset and retry on the next score; nothing to match against yet.
        }
    }

    private static (List<IngredientSeasonalityDetail> Breakdown, double TotalWeighted, double TotalPossible)
        ComputeForSeason(List<(ProduceItem Produce, double Grams)> matched, string season)
    {
        var breakdown = new List<IngredientSeasonalityDetail>();
        double totalWeighted = 0, totalPossible = 0;
        foreach ((ProduceItem? produce, double grams) in matched)
        {
            bool inSeason = produce.YearRound || produce.Seasons.Contains(season, StringComparer.OrdinalIgnoreCase);
            double scarcity = SeasonHelper.GetScarcityWeight(produce);
            double possible = scarcity * grams;
            double contribution = (inSeason ? 1.0 : 0.0) * possible;
            breakdown.Add(new IngredientSeasonalityDetail { Name = produce.Name, IsInSeason = inSeason, ScarcityWeight = scarcity, WeightInGrams = grams });
            totalWeighted += contribution;
            totalPossible += possible;
        }

        return (breakdown, totalWeighted, totalPossible);
    }

    private static Dictionary<string, ProduceItem> BuildLookup(IEnumerable<ProduceItem> items)
    {
        var dict = new Dictionary<string, ProduceItem>(StringComparer.OrdinalIgnoreCase);
        foreach (ProduceItem item in items)
        {
            dict.TryAdd(NormaliseName(item.Name), item);
        }

        return dict;
    }

    private string ComputeBestSeasons(List<(ProduceItem Produce, double Grams)> matched)
    {
        if (matched.Count == 0)
        {
            return string.Empty;
        }

        var seasonScores = SeasonHelper.AllSeasons
            .Select(s =>
            {
                (List<IngredientSeasonalityDetail> _, double tw, double tp) = ComputeForSeason(matched, s);
                return (Season: s, Score: tp > 0 ? (tw / tp) * 100.0 : 0.0);
            })
            .OrderByDescending(x => x.Score).ToList();

        double topScore = seasonScores[0].Score;
        if (topScore <= 0)
        {
            return string.Empty;
        }

        var best = seasonScores.Where(x => x.Score >= topScore - 10.0).Select(x => x.Season).ToList();
        return best.Count == 1 ? $"Best in {best[0]}" : $"Best in {string.Join(", ", best.Take(best.Count - 1))} and {best.Last()}";
    }

    private static ProduceItem? LookupProduce(string foodName, Dictionary<string, ProduceItem> lookup)
    {
        string key = NormaliseName(foodName);
        if (lookup.TryGetValue(key, out ProduceItem? exact))
        {
            return exact;
        }

        ProduceItem? best = null;
        int bestLen = int.MaxValue;
        foreach ((string? produceKey, ProduceItem? item) in lookup)
        {
            bool match = key.Contains(produceKey, StringComparison.OrdinalIgnoreCase) || produceKey.Contains(key, StringComparison.OrdinalIgnoreCase);
            if (match && produceKey.Length < bestLen)
            {
                best = item;
                bestLen = produceKey.Length;
            }
        }

        return best;
    }
}
