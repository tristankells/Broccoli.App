using System.Text.Json.Serialization;

namespace Broccoli.Data.Models;

/// <summary>
/// Discriminates a row in a <see cref="DailyFoodPlanTab"/> between a visual section header
/// and an actual food/recipe entry.
/// </summary>
public enum DailyFoodPlanRowType
{
    /// <summary>A full-width styled label row (e.g. "Breakfast", "Lunch").</summary>
    Header,
    /// <summary>A food or recipe entry row with macro values.</summary>
    FoodEntry
}

/// <summary>
/// A single row in a <see cref="DailyFoodPlanTab"/>.
/// Use <see cref="RowType"/> to determine which fields are populated.
/// </summary>
public class DailyFoodPlanRow
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Determines whether this row is a section header or a food/recipe entry.</summary>
    [JsonPropertyName("rowType")]
    public DailyFoodPlanRowType RowType { get; set; } = DailyFoodPlanRowType.FoodEntry;

    // ── Header row fields ────────────────────────────────────────────────────

    /// <summary>The label text for a Header row (e.g. "Breakfast", "Snacks").</summary>
    [JsonPropertyName("headerName")]
    public string? HeaderName { get; set; }

    // ── Food entry row fields ─────────────────────────────────────────────────

    /// <summary>
    /// Primary key of the selected food or recipe.
    /// For foods this is the <c>Food.Id</c> as a string; for recipes it is the <c>Recipe.Id</c> GUID.
    /// </summary>
    [JsonPropertyName("foodOrRecipeId")]
    public string? FoodOrRecipeId { get; set; }

    /// <summary>True if <see cref="FoodOrRecipeId"/> refers to a Recipe; false if it refers to a Food.</summary>
    [JsonPropertyName("isRecipe")]
    public bool IsRecipe { get; set; }

    /// <summary>
    /// User-visible unit label (e.g. "Cup", "tbsp", "serving").
    /// Defaults to the food's <c>Food.Measure</c> when a food is first selected.
    /// </summary>
    [JsonPropertyName("servingName")]
    public string ServingName { get; set; } = string.Empty;

    /// <summary>How many servings/units the user intends to eat.</summary>
    [JsonPropertyName("quantity")]
    public double Quantity { get; set; } = 1;

    // ── Pre-calculated macro values (refreshed on every selection/qty change) ──

    [JsonPropertyName("calories")]
    public double Calories { get; set; }

    [JsonPropertyName("fat")]
    public double Fat { get; set; }

    [JsonPropertyName("proteinG")]
    public double ProteinG { get; set; }

    [JsonPropertyName("carbsG")]
    public double CarbsG { get; set; }
}

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
    /// Optional ID of a <see cref="Broccoli.Data.Models.MacroTarget"/> profile to compare
    /// this tab's totals against.
    /// </summary>
    [JsonPropertyName("macroTargetId")]
    public string? MacroTargetId { get; set; }

    /// <summary>Ordered list of rows in this tab.</summary>
    [JsonPropertyName("rows")]
    public List<DailyFoodPlanRow> Rows { get; set; } = new();
}

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

