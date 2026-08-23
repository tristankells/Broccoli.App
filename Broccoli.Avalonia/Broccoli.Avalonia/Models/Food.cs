namespace Broccoli.Avalonia.Models;

public class Food
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Measure { get; set; } = string.Empty;

    public double GramsPerMeasure { get; set; }

    public string Notes { get; set; } = string.Empty;

    /// <summary>True when the food was added/edited by the user rather than seeded from the embedded database.</summary>
    public bool IsCustom { get; set; }

    // Nutritional values based on 100g
    public double CaloriesPer100g { get; set; }

    public double FatPer100g { get; set; }

    public double SaturatedFatPer100g { get; set; }

    public double CarbohydratesPer100g { get; set; }

    public double DietaryFiberPer100g { get; set; }

    public double SugarsPer100g { get; set; }

    public double ProteinPer100g { get; set; }

    public double SodiumMgPer100g { get; set; }
}
