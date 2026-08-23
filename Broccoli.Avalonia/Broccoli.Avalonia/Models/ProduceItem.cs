using System.Text.Json.Serialization;

namespace Broccoli.Avalonia.Models;

/// <summary>
/// Represents a single produce item from the bundled NZ seasonal produce dataset.
/// Seasonality is stored per month (1..12) rather than per season.
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
    /// Availability for each month, keyed by month number (1 = January .. 12 = December).
    /// Months not present are treated as <see cref="SeasonalityState.OutOfSeason"/>.
    /// </summary>
    [JsonPropertyName("months")]
    public Dictionary<int, SeasonalityState> Months { get; set; } = new();

    /// <summary>Optional human-readable note from the dataset.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>Returns the availability for a month (1..12), defaulting to out of season.</summary>
    public SeasonalityState GetStateForMonth(int month)
    {
        return Months.TryGetValue(month, out SeasonalityState state) ? state : SeasonalityState.OutOfSeason;
    }

    /// <summary>Sets the availability for a month (1..12).</summary>
    public void SetStateForMonth(int month, SeasonalityState state)
    {
        Months[month] = state;
    }
}
