namespace Broccoli.Avalonia.IngredientParsing;

public class ParsedIngredient
{
    public required string RawLine { get; set; }
    public required double Quantity { get; set; }
    public required string Unit { get; set; }
    public required string CanonicalUnit { get; set; }
    public required string FoodDescription { get; set; }
    public string FoodName => FoodDescription;
}
