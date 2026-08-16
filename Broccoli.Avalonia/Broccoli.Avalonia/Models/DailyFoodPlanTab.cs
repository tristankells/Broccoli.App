using System.Text.Json.Serialization;

namespace Broccoli.Avalonia.Models;

/// <summary>
/// A tab (e.g. one day or one meal set) within a <see cref="DailyFoodPlan"/>.
/// </summary>
public class DailyFoodPlanTab
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Display name of the tab (e.g. "Monday", "Week 1").</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional ID of a <see cref="MacroTarget"/> profile to compare
    /// this tab's totals against.
    /// </summary>
    [JsonPropertyName("macroTargetId")]
    public string? MacroTargetId { get; set; }

    /// <summary>Ordered list of rows in this tab.</summary>
    [JsonPropertyName("rows")]
    public List<DailyFoodPlanRow> Rows { get; set; } = new();
}
