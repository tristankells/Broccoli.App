using System.Text.Json.Serialization;

namespace Broccoli.Avalonia.Models;

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
