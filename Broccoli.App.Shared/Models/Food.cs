// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Broccoli.Data.Models
{
    public class Food
    {
        // Unique identifier for the food item
        public int Id { get; set; }

        // Name of the food (e.g., "Apple", "Peanut Butter")
        public string Name { get; set; }

        // The unit of measure (e.g., "Cup", "Tablespoon", "Piece")
        public string Measure { get; set; }

        // Weight in grams for a single 'Measure' unit
        public double GramsPerMeasure { get; set; }

        // General information or descriptions
        public string Notes { get; set; }

        // Nutritional values based on 100g
        public double CaloriesPer100g { get; set; }
        public double FatPer100g { get; set; }
        public double CarbohydratesPer100g { get; set; }
        public double ProteinPer100g { get; set; }
    }
}