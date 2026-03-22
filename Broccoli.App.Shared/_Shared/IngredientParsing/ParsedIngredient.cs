namespace Broccoli.App.Shared.IngredientParsing;

/// <summary>
/// Represents a parsed ingredient with quantity, unit, and food description.
/// </summary>
public class ParsedIngredient
{
    /// <summary>
    /// The original ingredient line as entered by the user.
    /// </summary>
    public required string RawLine { get; set; }

    /// <summary>
    /// Parsed quantity as a number (e.g., 1.5 for "1 1/2 cups")
    /// </summary>
    public required double Quantity { get; set; }

    /// <summary>
    /// The canonical unit after normalization (e.g., "cup" for "c", "cups", "Cup").
    /// May include informal units such as "drizzle", "pinch", "pack".
    /// </summary>
    public required string Unit { get; set; }

    /// <summary>
    /// The canonical unit form for internal calculations.
    /// </summary>
    public required string CanonicalUnit { get; set; }

    /// <summary>
    /// The extracted food description (e.g., "vermicelli noodles", "carrot grated").
    /// Notes and preparation instructions after a comma are stripped during parsing.
    /// </summary>
    public required string FoodDescription { get; set; }

    /// <summary>
    /// Alias for <see cref="FoodDescription"/> kept for backward compatibility.
    /// </summary>
    public string FoodName => FoodDescription;
}