using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Platform;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Storage;
using Microsoft.EntityFrameworkCore;

namespace Broccoli.Avalonia.Seasonality;

/// <summary>
/// Reads the seed produce dataset from the embedded <c>nz-produce.json</c> and writes it into
/// SQLite. The JSON is used only to initialise/reset the database, never as the live store.
/// </summary>
public static class ProduceSeeder
{
    private static readonly Uri SeedUri = new("avares://Broccoli.Avalonia/Assets/nz-produce.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Dictionary<string, int> MonthNumbers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["jan"] = 1, ["feb"] = 2, ["mar"] = 3, ["apr"] = 4, ["may"] = 5, ["jun"] = 6,
        ["jul"] = 7, ["aug"] = 8, ["sep"] = 9, ["oct"] = 10, ["nov"] = 11, ["dec"] = 12,
    };

    public static List<ProduceItem> ReadSeedProduce()
    {
        try
        {
            using Stream stream = AssetLoader.Open(SeedUri);
            ProduceDataset? dataset = JsonSerializer.Deserialize<ProduceDataset>(stream, JsonOptions);
            if (dataset is null)
            {
                return [];
            }

            List<ProduceItem> items = new(dataset.Produce.Count);
            foreach (ProduceItemDto dto in dataset.Produce)
            {
                ProduceItem item = new()
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Type = dto.Type,
                    Notes = dto.Notes,
                };

                foreach ((string? monthKey, string? state) in dto.Months)
                {
                    if (MonthNumbers.TryGetValue(monthKey, out int month))
                    {
                        item.SetStateForMonth(month, ParseState(state));
                    }
                }

                items.Add(item);
            }

            return items;
        }
        catch (Exception)
        {
            // Design-time or missing resource — return an empty seed.
            return [];
        }
    }

    public static void SeedIfEmpty(BroccoliDbContext context)
    {
        if (context.ProduceItems.Any())
        {
            return;
        }

        context.ProduceItems.AddRange(ReadSeedProduce());
        context.SaveChanges();
    }

    public static void Reset(BroccoliDbContext context)
    {
        context.ProduceItems.ExecuteDelete();
        context.ProduceItems.AddRange(ReadSeedProduce());
        context.SaveChanges();
    }

    private static SeasonalityState ParseState(string? value) => value?.ToLowerInvariant() switch
    {
        "in" or "in_season" => SeasonalityState.InSeason,
        "partial" or "partially_in_season" => SeasonalityState.PartiallyInSeason,
        _ => SeasonalityState.OutOfSeason,
    };

    private sealed class ProduceDataset
    {
        [JsonPropertyName("produce")]
        public List<ProduceItemDto> Produce { get; set; } = new();
    }

    private sealed class ProduceItemDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("months")]
        public Dictionary<string, string> Months { get; set; } = new();

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
}
