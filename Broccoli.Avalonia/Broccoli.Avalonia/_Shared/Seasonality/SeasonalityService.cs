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

    public SeasonalityResult Score(IEnumerable<ParsedIngredientMatch> matches, DateTime? asOf = null, SeasonalityThresholds? thresholds = null)
    {
        SeasonalityThresholds t = thresholds ?? SeasonalityThresholds.Default;
        SeasonalityResult result = ScoreCore(matches, asOf, t.InfluenceMode);
        result.Band = Classify(result.Score, t);
        return result;
    }

    /// <inheritdoc />
    public List<SeasonalityResult> ScoreAll(
        IEnumerable<IReadOnlyList<ParsedIngredientMatch>> matchSets,
        DateTime? asOf = null,
        SeasonalityThresholds? thresholds = null)
    {
        SeasonalityThresholds t = thresholds ?? SeasonalityThresholds.Default;
        List<SeasonalityResult> results = matchSets.Select(set => ScoreCore(set, asOf, t.InfluenceMode)).ToList();
        List<double> libraryScores = results.Where(r => r.Score.HasValue).Select(r => r.Score!.Value).ToList();
        foreach (SeasonalityResult result in results)
        {
            result.Band = Classify(result.Score, t, libraryScores);
        }

        return results;
    }

    /// <inheritdoc />
    public SeasonalityState? GetProduceStateForFood(string foodName, DateTime? asOf = null)
    {
        Dictionary<string, ProduceItem> lookup = GetLookup();
        ProduceItem? produce = LookupProduce(foodName, lookup);
        if (produce is null)
        {
            return null;
        }

        return produce.GetStateForMonth((asOf ?? DateTime.Now).Month);
    }

    private SeasonalityResult ScoreCore(IEnumerable<ParsedIngredientMatch> matches, DateTime? asOf, SeasonalityInfluenceMode influenceMode)
    {
        int month = (asOf ?? DateTime.Now).Month;
        Dictionary<string, ProduceItem> lookup = GetLookup();

        int totalIngredientCount = 0;
        var matched = new List<(ProduceItem Produce, double Grams)>();
        foreach (ParsedIngredientMatch m in matches)
        {
            if (!m.IsMatched || m.MatchedFood is null)
            {
                continue;
            }

            totalIngredientCount++;

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
            return new SeasonalityResult { Score = null, Breakdown = new List<IngredientSeasonalityDetail>(), BestSeasons = string.Empty, TotalIngredientCount = totalIngredientCount };
        }

        (List<IngredientSeasonalityDetail>? breakdown, double totalWeighted, double totalPossible) = ComputeForMonth(matched, month, influenceMode);

        double score = totalPossible > 0 ? (totalWeighted / totalPossible) * 100.0 : 0;

        string bestSeasons = ComputeBestSeasons(matched, influenceMode);

        return new SeasonalityResult { Score = score, Breakdown = breakdown, BestSeasons = bestSeasons, MatchedProduceCount = breakdown.Count, TotalIngredientCount = totalIngredientCount };
    }

    /// <summary>
    /// Classifies a score into a band. With a library of scores the top third is green and the
    /// bottom third red, guarded by absolute thresholds: a score at/above
    /// <see cref="SeasonalityThresholds.GreenAlwaysHigh"/> is always green, one below
    /// <see cref="SeasonalityThresholds.GreenFloor"/> is never green; a score at/below
    /// <see cref="SeasonalityThresholds.RedAlwaysLow"/> is always red, one above
    /// <see cref="SeasonalityThresholds.RedCeiling"/> is never red. With too few library scores
    /// (or no library context) only the absolute thresholds apply.
    /// </summary>
    public static SeasonalityBand Classify(double? score, SeasonalityThresholds thresholds, IReadOnlyList<double>? libraryScores = null)
    {
        if (score is not double s)
        {
            return SeasonalityBand.Unavailable;
        }

        if (libraryScores is null || libraryScores.Count < 5)
        {
            return s >= thresholds.GreenFloor ? SeasonalityBand.Green
                 : s <= thresholds.RedCeiling ? SeasonalityBand.Red
                 : SeasonalityBand.Yellow;
        }

        double percentile = ComputePercentile(s, libraryScores);

        if (s >= thresholds.GreenAlwaysHigh)
        {
            return SeasonalityBand.Green;
        }

        if (s <= thresholds.RedAlwaysLow)
        {
            return SeasonalityBand.Red;
        }

        if (s >= thresholds.GreenFloor && percentile >= thresholds.TopPercentile)
        {
            return SeasonalityBand.Green;
        }

        if (s <= thresholds.RedCeiling && percentile <= thresholds.BottomPercentile)
        {
            return SeasonalityBand.Red;
        }

        return SeasonalityBand.Yellow;
    }

    private static double ComputePercentile(double score, IReadOnlyList<double> scores)
    {
        int countLess = 0;
        foreach (double s in scores)
        {
            if (s < score)
            {
                countLess++;
            }
        }

        return scores.Count <= 1 ? 0.5 : countLess / (double)(scores.Count - 1);
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
        ComputeForMonth(List<(ProduceItem Produce, double Grams)> matched, int month, SeasonalityInfluenceMode influenceMode)
    {
        var breakdown = new List<IngredientSeasonalityDetail>();
        double totalWeighted = 0, totalPossible = 0;
        foreach ((ProduceItem? produce, double grams) in matched)
        {
            SeasonalityState state = produce.GetStateForMonth(month);
            double factor = StateFactor(state);
            // Scarcity acts as an influence multiplier: short-season items count more than a
            // year-round staple, so an out-of-season strawberry matters more than an in-season
            // carrot. The weight sits in both numerator and denominator, so it reweights which
            // ingredients dominate the score rather than inflating it.
            double scarcity = SeasonHelper.GetScarcityWeight(produce, influenceMode);
            double possible = scarcity * grams;
            double contribution = factor * possible;
            breakdown.Add(new IngredientSeasonalityDetail { Name = produce.Name, State = state, ScarcityWeight = scarcity, WeightInGrams = grams });
            totalWeighted += contribution;
            totalPossible += possible;
        }

        return (breakdown, totalWeighted, totalPossible);
    }

    /// <summary>How much a state contributes toward the seasonality score.</summary>
    private static double StateFactor(SeasonalityState state) => state switch
    {
        SeasonalityState.InSeason => 1.0,
        SeasonalityState.PartiallyInSeason => 0.5,
        _ => 0.0,
    };

    private static Dictionary<string, ProduceItem> BuildLookup(IEnumerable<ProduceItem> items)
    {
        var dict = new Dictionary<string, ProduceItem>(StringComparer.OrdinalIgnoreCase);
        foreach (ProduceItem item in items)
        {
            dict.TryAdd(NormaliseName(item.Name), item);
        }

        return dict;
    }

    private string ComputeBestSeasons(List<(ProduceItem Produce, double Grams)> matched, SeasonalityInfluenceMode influenceMode)
    {
        if (matched.Count == 0)
        {
            return string.Empty;
        }

        var seasonScores = SeasonHelper.AllSeasons
            .Select(s =>
            {
                (double tw, double tp) = ComputeSeasonTotals(matched, s, influenceMode);
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

    /// <summary>
    /// Aggregates an ingredient set's contribution across the three months of a season, so the
    /// existing "Best in summer" style summary keeps working on top of monthly data.
    /// </summary>
    private static (double TotalWeighted, double TotalPossible) ComputeSeasonTotals(List<(ProduceItem Produce, double Grams)> matched, string season, SeasonalityInfluenceMode influenceMode)
    {
        double totalWeighted = 0, totalPossible = 0;
        foreach (int month in SeasonHelper.GetSeasonMonths(season))
        {
            foreach ((ProduceItem? produce, double grams) in matched)
            {
                double factor = StateFactor(produce.GetStateForMonth(month));
                double possible = SeasonHelper.GetScarcityWeight(produce, influenceMode) * grams;
                totalWeighted += factor * possible;
                totalPossible += possible;
            }
        }

        return (totalWeighted, totalPossible);
    }

    /// <summary>
    /// Matches a (normalised) food name to a produce dataset entry. Exact name match first; then a
    /// whole-token match so that e.g. "peas" never matches "Peach" and "butter" never matches
    /// "Butternut" the way a raw substring search would. A produce item is a match when the
    /// ingredient tokens fully describe it ("red onion" ⊇ "onion") or the ingredient is a generic
    /// name for a more specific produce ("cabbage" ⊆ "green cabbage").
    /// </summary>
    private static ProduceItem? LookupProduce(string foodName, Dictionary<string, ProduceItem> lookup)
    {
        string key = NormaliseName(foodName);
        if (lookup.TryGetValue(key, out ProduceItem? exact))
        {
            return exact;
        }

        string[] ingredientTokens = SplitTokens(key);
        if (ingredientTokens.Length == 0)
        {
            return null;
        }

        ProduceItem? best = null;
        int bestScore = -1;
        foreach ((string? produceKey, ProduceItem? item) in lookup)
        {
            string[] produceTokens = SplitTokens(produceKey);
            bool produceDescribedByIngredient = produceTokens.Length > 0 && produceTokens.All(ingredientTokens.Contains);
            bool ingredientDescribedByProduce = ingredientTokens.All(produceTokens.Contains);
            if (!produceDescribedByIngredient && !ingredientDescribedByProduce)
            {
                continue;
            }

            // Fully-described produce ("red onion" ⊇ "onion") is a stronger signal than a generic
            // ingredient name ("cabbage" ⊆ "green cabbage"); among same-direction matches prefer
            // the most specific (most tokens, then longest name).
            int score = produceDescribedByIngredient
                ? 1000 + (produceTokens.Length * 100) + produceKey.Length
                : 100 + (produceTokens.Length * 100) + produceKey.Length;
            if (score > bestScore)
            {
                best = item;
                bestScore = score;
            }
        }

        return best;
    }

    private static string[] SplitTokens(string name) =>
        name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
