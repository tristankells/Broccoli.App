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

    public static List<ProduceItem> ReadSeedProduce()
    {
        try
        {
            using Stream stream = AssetLoader.Open(SeedUri);
            ProduceDataset? dataset = JsonSerializer.Deserialize<ProduceDataset>(stream, JsonOptions);
            return dataset?.Produce ?? [];
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

    private sealed class ProduceDataset
    {
        [JsonPropertyName("produce")]
        public List<ProduceItem> Produce { get; set; } = new();
    }
}
