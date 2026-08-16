namespace Broccoli.Avalonia.Models;

/// <summary>
/// Discriminates a row in a <see cref="DailyFoodPlanTab"/> between a visual section header
/// and an actual food/recipe entry.
/// </summary>
public enum DailyFoodPlanRowType
{
    /// <summary>A full-width styled label row (e.g. "Breakfast", "Lunch").</summary>
    Header,

    /// <summary>A food or recipe entry row with macro values.</summary>
    FoodEntry,
}
