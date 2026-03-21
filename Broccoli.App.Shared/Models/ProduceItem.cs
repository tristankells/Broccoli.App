using System.Text.Json.Serialization;

namespace Broccoli.Data.Models;

/// <summary>
/// Represents a single produce item from the bundled NZ seasonal produce dataset.
/// </summary>
public class ProduceItem
{
    /// <summary>Unique key, snake_case (e.g. "strawberry").</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name (e.g. "Strawberry").</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>"fruit" or "vegetable".</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Seasons this ingredient is in season: "spring", "summer", "autumn", "winter".
    /// Ignored for scoring when <see cref="YearRound"/> is true.
    /// </summary>
    [JsonPropertyName("seasons")]
    public List<string> Seasons { get; set; } = new();

    /// <summary>
    /// When true the ingredient is always considered in-season and receives
    /// scarcity weight 0.25 regardless of the <see cref="Seasons"/> list.
    /// </summary>
    [JsonPropertyName("year_round")]
    public bool YearRound { get; set; }

    /// <summary>Optional peak seasons noted in the dataset (not used for scoring).</summary>
    [JsonPropertyName("peak_seasons")]
    public List<string>? PeakSeasons { get; set; }

    /// <summary>Optional human-readable note from the dataset.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

