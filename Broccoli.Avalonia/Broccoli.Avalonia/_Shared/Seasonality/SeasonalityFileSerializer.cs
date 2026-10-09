using System.Text.Json;
using System.Text.Json.Serialization;
using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Seasonality;

/// <summary>
/// Reads and writes the produce seasonality dataset as JSON in the same human-readable format as
/// the bundled <c>nz-produce.json</c> seed: <c>"months": { "jan": "in", "feb": "partial" }</c>.
/// Used by the Seasonality page's Import/Export, so an exported file can be re-imported (or edited
/// by hand) and a tweaked copy of the seed file can be imported too.
/// </summary>
public static class SeasonalityFileSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private static readonly Dictionary<string, int> MonthNumbers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["jan"] = 1, ["feb"] = 2, ["mar"] = 3, ["apr"] = 4, ["may"] = 5, ["jun"] = 6,
        ["jul"] = 7, ["aug"] = 8, ["sep"] = 9, ["oct"] = 10, ["nov"] = 11, ["dec"] = 12,
    };

    private static readonly string[] MonthNames =
        ["jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"];

    /// <summary>Serializes the dataset to the seed-format JSON (all 12 months always written).</summary>
    public static string Serialize(IReadOnlyList<ProduceItem> items)
    {
        var dataset = new ProduceDatasetFile
        {
            Meta = new DatasetMeta { Region = "NZ", Notes = "Exported from Broccoli" },
            Produce = items.Select(ToDto).ToList(),
        };

        return JsonSerializer.Serialize(dataset, JsonOptions);
    }

    /// <summary>
    /// Parses seed-format JSON into produce items. Returns true when the file was readable at all;
    /// individual invalid items are skipped and their problems reported in <paramref name="errors"/>.
    /// </summary>
    public static bool TryParse(string json, out List<ProduceItem> items, out List<string> errors)
    {
        items = new List<ProduceItem>();
        errors = new List<string>();

        ProduceDatasetFile? dataset;
        try
        {
            dataset = JsonSerializer.Deserialize<ProduceDatasetFile>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            errors.Add($"The file isn't valid JSON: {ex.Message}");
            return false;
        }

        if (dataset?.Produce is null || dataset.Produce.Count == 0)
        {
            errors.Add("The file contains no produce items.");
            return false;
        }

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ProduceItemDto dto in dataset.Produce)
        {
            string label = string.IsNullOrWhiteSpace(dto.Name) ? (dto.Id ?? "unnamed") : dto.Name;
            List<string> itemErrors = ValidateItem(dto, seenIds);
            if (itemErrors.Count > 0)
            {
                errors.Add($"{label}: {string.Join("; ", itemErrors)}");
                continue;
            }

            seenIds.Add(dto.Id!);
            items.Add(BuildItem(dto));
        }

        return true;
    }

    private static List<string> ValidateItem(ProduceItemDto dto, HashSet<string> seenIds)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            errors.Add("missing 'id'");
        }
        else if (!seenIds.Add(dto.Id))
        {
            errors.Add($"duplicate id '{dto.Id}'");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            errors.Add("missing 'name'");
        }

        if (string.IsNullOrWhiteSpace(dto.Type))
        {
            errors.Add("missing 'type'");
        }

        if (dto.Months is not null)
        {
            foreach ((string? monthKey, string? state) in dto.Months)
            {
                if (!MonthNumbers.ContainsKey(monthKey ?? string.Empty))
                {
                    errors.Add($"unknown month '{monthKey}'");
                }
                else if (!TryParseState(state, out _))
                {
                    errors.Add($"unknown state '{state}' for {monthKey}");
                }
            }
        }

        return errors;
    }

    private static ProduceItem BuildItem(ProduceItemDto dto)
    {
        var item = new ProduceItem
        {
            Id = dto.Id!,
            Name = dto.Name!,
            Type = dto.Type!,
            Notes = dto.Notes,
        };

        if (dto.Months is not null)
        {
            foreach ((string? monthKey, string? state) in dto.Months)
            {
                if (MonthNumbers.TryGetValue(monthKey!, out int month) && TryParseState(state, out SeasonalityState parsed))
                {
                    item.SetStateForMonth(month, parsed);
                }
            }
        }

        return item;
    }

    private static ProduceItemDto ToDto(ProduceItem item)
    {
        var months = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int month = 1; month <= 12; month++)
        {
            months[MonthNames[month - 1]] = StateToString(item.GetStateForMonth(month));
        }

        return new ProduceItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Type = item.Type,
            Months = months,
            Notes = item.Notes,
        };
    }

    private static bool TryParseState(string? value, out SeasonalityState state)
    {
        switch (value?.ToLowerInvariant())
        {
            case "in":
            case "in_season":
                state = SeasonalityState.InSeason;
                return true;
            case "partial":
            case "partially_in_season":
                state = SeasonalityState.PartiallyInSeason;
                return true;
            case "out":
            case "out_of_season":
                state = SeasonalityState.OutOfSeason;
                return true;
            default:
                state = SeasonalityState.OutOfSeason;
                return false;
        }
    }

    private static string StateToString(SeasonalityState state) => state switch
    {
        SeasonalityState.InSeason => "in",
        SeasonalityState.PartiallyInSeason => "partial",
        _ => "out",
    };

    private sealed class ProduceDatasetFile
    {
        [JsonPropertyName("meta")]
        public DatasetMeta? Meta { get; set; }

        [JsonPropertyName("produce")]
        public List<ProduceItemDto> Produce { get; set; } = new();
    }

    private sealed class DatasetMeta
    {
        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    private sealed class ProduceItemDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("months")]
        public Dictionary<string, string>? Months { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
}
