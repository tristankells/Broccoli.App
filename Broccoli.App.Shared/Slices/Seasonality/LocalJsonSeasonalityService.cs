using System.Text.Json;
using System.Text.Json.Serialization;
using Broccoli.App.Shared.IngredientParsing;
using Broccoli.Data.Models;
using Microsoft.Extensions.Logging;

namespace Broccoli.App.Shared.Slices.Seasonality;

/// <summary>
/// Scores recipe seasonality using the bundled <c>nz-produce.json</c> embedded resource.
/// The dataset is loaded once at construction time; all scoring is pure in-memory computation.
/// </summary>
public class LocalJsonSeasonalityService : ISeasonalityService
{
    private const double MinGrams = 5.0;

    private readonly List<ProduceItem> _allProduce;
    private readonly Dictionary<string, ProduceItem> _produceByNormalisedName;
    private readonly ILogger<LocalJsonSeasonalityService> _logger;

    // Stopwords stripped during name normalisation — same set used by LocalJsonFoodService
    // to ensure consistent matching across both datasets.
    private static readonly HashSet<string> s_stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "raw", "fresh", "free", "range", "diced", "sliced", "grated", "skinless",
        "lite", "baby", "chopped", "minced", "peeled", "deseeded", "rinsed",
        "drained", "cooked", "uncooked", "dried", "frozen", "canned", "tin", "tinned",
        "large", "small", "medium", "whole", "halved", "roughly", "finely", "thinly"
    };

    // Explicit irregular-plural fixes for produce names in the NZ dataset.
    private static readonly Dictionary<string, string> s_pluralFixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["strawberries"]  = "strawberry",
        ["raspberries"]   = "raspberry",
        ["blackberries"]  = "blackberry",
        ["boysenberries"] = "boysenberry",
        ["blueberries"]   = "blueberry",
        ["cherries"]      = "cherry",
        ["gooseberries"]  = "gooseberry",
        ["redcurrants"]   = "redcurrant",
        ["blackcurrants"] = "blackcurrant",
        ["apricots"]      = "apricot",
        ["nectarines"]    = "nectarine",
        ["peaches"]       = "peach",
        ["plums"]         = "plum",
        ["pears"]         = "pear",
        ["lemons"]        = "lemon",
        ["limes"]         = "lime",
        ["mandarins"]     = "mandarin",
        ["mushrooms"]     = "mushroom",
        ["tomatoes"]      = "tomato",
        ["potatoes"]      = "potato",
    };

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Production constructor — loads <c>nz-produce.json</c> from the embedded resource
    /// in the <c>Broccoli.App.Shared</c> assembly.
    /// </summary>
    public LocalJsonSeasonalityService(ILogger<LocalJsonSeasonalityService> logger)
    {
        _logger = logger;

        var assembly = typeof(LocalJsonSeasonalityService).Assembly;
        using var stream = assembly.GetManifestResourceStream("Broccoli.App.Shared.Data.nz-produce.json");

        if (stream is null)
        {
            _logger.LogWarning("Embedded resource 'nz-produce.json' not found. Seasonality scoring disabled.");
            _allProduce = new List<ProduceItem>();
            _produceByNormalisedName = new Dictionary<string, ProduceItem>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        var dataset = JsonSerializer.Deserialize<ProduceDataset>(stream, s_jsonOptions);
        _allProduce = dataset?.Produce ?? new List<ProduceItem>();
        _produceByNormalisedName = BuildLookup(_allProduce);

        _logger.LogInformation("NZ produce dataset loaded. {Count} items.", _allProduce.Count);
    }

    /// <summary>
    /// Testing constructor — supply produce items directly without loading the embedded resource.
    /// </summary>
    public LocalJsonSeasonalityService(IEnumerable<ProduceItem> produce, ILogger<LocalJsonSeasonalityService> logger)
    {
        _logger = logger;
        _allProduce = produce.ToList();
        _produceByNormalisedName = BuildLookup(_allProduce);
    }

    // -- ISeasonalityService -------------------------------------------------

    /// <inheritdoc />
    public SeasonalityResult Score(IEnumerable<ParsedIngredientMatch> matches, DateTime? asOf = null)
    {
        string season = SeasonHelper.GetCurrentSeason(asOf ?? DateTime.Now);

        // Build the list of (ProduceItem, grams) pairs that will drive all calculations.
        var matched = new List<(ProduceItem Produce, double Grams)>();

        foreach (var m in matches)
        {
            if (!m.IsMatched || m.MatchedFood is null) continue;

            double grams = m.GetWeightInGrams();
            if (grams < MinGrams) continue;

            var produce = LookupProduce(m.MatchedFood.Name);
            if (produce is null) continue;

            matched.Add((produce, grams));
        }

        if (matched.Count == 0)
        {
            _logger.LogDebug("No produce ingredients matched. Returning Unavailable.");
            return new SeasonalityResult
            {
                Score      = null,
                Label      = SeasonalityLabel.Unavailable,
                Breakdown  = new List<IngredientSeasonalityDetail>(),
                BestSeasons = string.Empty
            };
        }

        var (breakdown, totalWeighted, totalPossible) = ComputeForSeason(matched, season);

        double score = (totalWeighted / totalPossible) * 100.0;
        SeasonalityLabel label = score >= 75 ? SeasonalityLabel.PeakSeason
                               : score >= 40 ? SeasonalityLabel.PartiallyInSeason
                               : SeasonalityLabel.OffSeason;

        string bestSeasons = ComputeBestSeasons(matched);

        _logger.LogDebug(
            "Seasonality scored. Season={Season} Score={Score:F1} Label={Label} ProduceItems={Count}",
            season, score, label, matched.Count);

        return new SeasonalityResult
        {
            Score       = score,
            Label       = label,
            Breakdown   = breakdown,
            BestSeasons = bestSeasons
        };
    }

    // -- Private helpers -----------------------------------------------------

    private static (List<IngredientSeasonalityDetail> breakdown, double totalWeighted, double totalPossible)
        ComputeForSeason(List<(ProduceItem Produce, double Grams)> matched, string season)
    {
        var breakdown = new List<IngredientSeasonalityDetail>(matched.Count);
        double totalWeighted = 0;
        double totalPossible = 0;

        foreach (var (produce, grams) in matched)
        {
            bool inSeason = produce.YearRound
                || produce.Seasons.Contains(season, StringComparer.OrdinalIgnoreCase);

            double scarcityWeight = SeasonHelper.GetScarcityWeight(produce);
            double possible       = scarcityWeight * grams;
            double contribution   = (inSeason ? 1.0 : 0.0) * possible;

            breakdown.Add(new IngredientSeasonalityDetail
            {
                Name          = produce.Name,
                IsInSeason    = inSeason,
                ScarcityWeight = scarcityWeight,
                WeightInGrams = grams
            });

            totalWeighted += contribution;
            totalPossible += possible;
        }

        return (breakdown, totalWeighted, totalPossible);
    }

    private string ComputeBestSeasons(List<(ProduceItem Produce, double Grams)> matched)
    {
        if (matched.Count == 0) return string.Empty;

        var seasonScores = SeasonHelper.AllSeasons
            .Select(s =>
            {
                var (_, tw, tp) = ComputeForSeason(matched, s);
                double sc = tp > 0 ? (tw / tp) * 100.0 : 0.0;
                return (Season: s, Score: sc);
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        double topScore = seasonScores[0].Score;
        if (topScore <= 0) return string.Empty;

        // Include all seasons within 10 points of the top score
        var best = seasonScores
            .Where(x => x.Score >= topScore - 10.0)
            .Select(x => x.Season)
            .ToList();

        return best.Count == 1
            ? $"Best in {best[0]}"
            : $"Best in {string.Join(", ", best.Take(best.Count - 1))} and {best.Last()}";
    }

    private ProduceItem? LookupProduce(string foodName)
    {
        string key = NormaliseName(foodName);

        // 1. Exact normalised lookup
        if (_produceByNormalisedName.TryGetValue(key, out var exact))
            return exact;

        // 2. Contains fallback — prefer the match with the shortest produce name
        //    to reduce false positives (e.g. "pea" matching "snow pea" before "green pea")
        ProduceItem? best = null;
        int bestLen = int.MaxValue;

        foreach (var (produceKey, item) in _produceByNormalisedName)
        {
            bool match = key.Contains(produceKey, StringComparison.OrdinalIgnoreCase)
                      || produceKey.Contains(key, StringComparison.OrdinalIgnoreCase);

            if (match && produceKey.Length < bestLen)
            {
                best    = item;
                bestLen = produceKey.Length;
            }
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

    /// <summary>
    /// Normalises a food/produce name for matching:
    /// lower-case ? strip from comma ? remove stopwords ? fix irregular plurals ? de-pluralise.
    /// </summary>
    public static string NormaliseName(string name)
    {
        // 1. Lower-case
        string result = name.ToLowerInvariant();

        // 2. Strip everything after the first comma ("carrots, raw" ? "carrots")
        int comma = result.IndexOf(',');
        if (comma >= 0) result = result[..comma];

        // 3. Tokenise and remove stopwords
        var tokens = result
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => !s_stopwords.Contains(t))
            .ToList();

        // 4. Apply irregular-plural fixes and naive trailing-s de-pluralisation
        for (int i = 0; i < tokens.Count; i++)
        {
            if (s_pluralFixes.TryGetValue(tokens[i], out var singular))
                tokens[i] = singular;
            else if (tokens[i].EndsWith('s') && tokens[i].Length >= 4)
                tokens[i] = tokens[i][..^1];
        }

        return string.Join(" ", tokens).Trim();
    }

    // -- Private DTO used only for JSON deserialisation ----------------------

    private sealed class ProduceDataset
    {
        [JsonPropertyName("produce")]
        public List<ProduceItem> Produce { get; set; } = new();
    }
}


