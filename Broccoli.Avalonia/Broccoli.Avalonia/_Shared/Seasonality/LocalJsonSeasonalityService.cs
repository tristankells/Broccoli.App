using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Platform;
using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Seasonality;

public class LocalJsonSeasonalityService : ISeasonalityService
{
    private const double MinGrams = 5.0;

    private readonly List<ProduceItem> _allProduce;
    private readonly Dictionary<string, ProduceItem> _produceByNormalisedName;

    private static readonly HashSet<string> s_stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "raw", "fresh", "free", "range", "diced", "sliced", "grated", "skinless",
        "lite", "baby", "chopped", "minced", "peeled", "deseeded", "rinsed",
        "drained", "cooked", "uncooked", "dried", "frozen", "canned", "tin", "tinned",
        "large", "small", "medium", "whole", "halved", "roughly", "finely", "thinly"
    };

    private static readonly Dictionary<string, string> s_pluralFixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["strawberries"] = "strawberry", ["raspberries"] = "raspberry", ["blackberries"] = "blackberry",
        ["boysenberries"] = "boysenberry", ["blueberries"] = "blueberry", ["cherries"] = "cherry",
        ["gooseberries"] = "gooseberry", ["redcurrants"] = "redcurrant", ["blackcurrants"] = "blackcurrant",
        ["apricots"] = "apricot", ["nectarines"] = "nectarine", ["peaches"] = "peach",
        ["plums"] = "plum", ["pears"] = "pear", ["lemons"] = "lemon", ["limes"] = "lime",
        ["mandarins"] = "mandarin", ["mushrooms"] = "mushroom", ["tomatoes"] = "tomato", ["potatoes"] = "potato",
    };

    private static readonly JsonSerializerOptions s_jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public LocalJsonSeasonalityService()
    {
        try
        {
            var uri = new Uri("avares://Broccoli.Avalonia/Assets/nz-produce.json");
            using var stream = AssetLoader.Open(uri);
            var dataset = JsonSerializer.Deserialize<ProduceDataset>(stream, s_jsonOpts);
            _allProduce = dataset?.Produce ?? new List<ProduceItem>();
        }
        catch
        {
            _allProduce = new List<ProduceItem>();
        }
        _produceByNormalisedName = BuildLookup(_allProduce);
    }

    public SeasonalityResult Score(IEnumerable<ParsedIngredientMatch> matches, DateTime? asOf = null)
    {
        string season = SeasonHelper.GetCurrentSeason(asOf ?? DateTime.Now);

        var matched = new List<(ProduceItem Produce, double Grams)>();
        foreach (var m in matches)
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

            var produce = LookupProduce(m.MatchedFood.Name);
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

        var (breakdown, totalWeighted, totalPossible) = ComputeForSeason(matched, season);

        double score = totalPossible > 0 ? (totalWeighted / totalPossible) * 100.0 : 0;
        SeasonalityLabel label = score >= 75 ? SeasonalityLabel.PeakSeason
                               : score >= 40 ? SeasonalityLabel.PartiallyInSeason
                               : SeasonalityLabel.OffSeason;

        string bestSeasons = ComputeBestSeasons(matched);

        return new SeasonalityResult { Score = score, Label = label, Breakdown = breakdown, BestSeasons = bestSeasons };
    }

    private static (List<IngredientSeasonalityDetail> breakdown, double totalWeighted, double totalPossible)
        ComputeForSeason(List<(ProduceItem Produce, double Grams)> matched, string season)
    {
        var breakdown = new List<IngredientSeasonalityDetail>();
        double totalWeighted = 0, totalPossible = 0;
        foreach (var (produce, grams) in matched)
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

    private string ComputeBestSeasons(List<(ProduceItem Produce, double Grams)> matched)
    {
        if (matched.Count == 0)
        {
            return string.Empty;
        }

        var seasonScores = SeasonHelper.AllSeasons
            .Select(s => { var (_, tw, tp) = ComputeForSeason(matched, s); return (Season: s, Score: tp > 0 ? (tw / tp) * 100.0 : 0.0); })
            .OrderByDescending(x => x.Score).ToList();

        double topScore = seasonScores[0].Score;
        if (topScore <= 0)
        {
            return string.Empty;
        }

        var best = seasonScores.Where(x => x.Score >= topScore - 10.0).Select(x => x.Season).ToList();
        return best.Count == 1 ? $"Best in {best[0]}" : $"Best in {string.Join(", ", best.Take(best.Count - 1))} and {best.Last()}";
    }

    private ProduceItem? LookupProduce(string foodName)
    {
        string key = NormaliseName(foodName);
        if (_produceByNormalisedName.TryGetValue(key, out var exact))
        {
            return exact;
        }

        ProduceItem? best = null;
        int bestLen = int.MaxValue;
        foreach (var (produceKey, item) in _produceByNormalisedName)
        {
            bool match = key.Contains(produceKey, StringComparison.OrdinalIgnoreCase) || produceKey.Contains(key, StringComparison.OrdinalIgnoreCase);
            if (match && produceKey.Length < bestLen) { best = item; bestLen = produceKey.Length; }
        }
        return best;
    }

    private static Dictionary<string, ProduceItem> BuildLookup(IEnumerable<ProduceItem> items)
    {
        var dict = new Dictionary<string, ProduceItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            dict.TryAdd(NormaliseName(item.Name), item);
        }

        return dict;
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
            if (s_pluralFixes.TryGetValue(tokens[i], out var singular))
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

    private sealed class ProduceDataset
    {
        [JsonPropertyName("produce")]
        public List<ProduceItem> Produce { get; set; } = new();
    }
}
