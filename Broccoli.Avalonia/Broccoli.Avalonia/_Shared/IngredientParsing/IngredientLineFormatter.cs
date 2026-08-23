using System.Globalization;

namespace Broccoli.Avalonia.IngredientParsing;

/// <summary>
/// Renders an ingredient as a single canonical line ("250g Chicken Thigh", "1 drizzle Olive Oil").
/// Shared by the grocery cart and the recipe-editor match-correction flow so both format identically.
/// </summary>
public static class IngredientLineFormatter
{
    public static string Build(double quantity, string? unit, string food)
    {
        string quantityText = quantity.ToString("0.##", CultureInfo.InvariantCulture);

        if (string.IsNullOrEmpty(unit))
        {
            return $"{quantityText} {food}";
        }

        bool unitAttaches = unit is "g" or "kg" or "ml" or "l";
        return unitAttaches
            ? $"{quantityText}{unit} {food}"
            : $"{quantityText} {unit} {food}";
    }
}
