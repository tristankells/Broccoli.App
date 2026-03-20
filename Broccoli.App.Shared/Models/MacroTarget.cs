using System.Text.Json.Serialization;

namespace Broccoli.Data.Models;

public class MacroTarget
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; set; } = "macrotarget";

    // ── Editable fields ──────────────────────────────────────────────────────

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("gender")]
    public GenderType Gender { get; set; } = GenderType.Male;

    /// <summary>Always stored in kg regardless of UnitSystem setting.</summary>
    [JsonPropertyName("weightKg")]
    public double WeightKg { get; set; }

    /// <summary>Always stored in cm regardless of UnitSystem setting.</summary>
    [JsonPropertyName("heightCm")]
    public double HeightCm { get; set; }

    [JsonPropertyName("age")]
    public int Age { get; set; }

    [JsonPropertyName("activityLevel")]
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.ModeratelyActive;

    // ── Calculated fields (stored for display) ───────────────────────────────

    [JsonPropertyName("bmr")]
    public double Bmr { get; set; }

    [JsonPropertyName("tdee")]
    public double Tdee { get; set; }

    [JsonPropertyName("recommendedCalories")]
    public double RecommendedCalories { get; set; }

    [JsonPropertyName("recommendedProteinG")]
    public double RecommendedProteinG { get; set; }

    [JsonPropertyName("recommendedCarbsG")]
    public double RecommendedCarbsG { get; set; }

    [JsonPropertyName("recommendedFatG")]
    public double RecommendedFatG { get; set; }

    // ── Metadata ─────────────────────────────────────────────────────────────

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

