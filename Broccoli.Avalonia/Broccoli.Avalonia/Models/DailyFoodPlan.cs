using System.Text.Json.Serialization;

namespace Broccoli.Avalonia.Models;

/// <summary>
/// A named, reusable day-of-eating template scoped to a user.
/// Stored as a single CosmosDB document with all tabs and rows embedded.
/// Partition key: /userId.
/// </summary>
public class DailyFoodPlan
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>User-chosen name for this plan (e.g. "Cutting Day", "Rest Day").</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>ID of the owning user (partition key).</summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>Ordered tabs in this plan.</summary>
    [JsonPropertyName("tabs")]
    public List<DailyFoodPlanTab> Tabs { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
