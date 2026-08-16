namespace Broccoli.Avalonia.Slices.Settings;

public class UsdaFoodItem
{
    public int FdcId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public double Calories { get; set; }

    public double Fat { get; set; }

    public double SaturatedFat { get; set; }

    public double Carbohydrates { get; set; }

    public double DietaryFiber { get; set; }

    public double Sugars { get; set; }

    public double Protein { get; set; }

    public double SodiumMg { get; set; }
}
