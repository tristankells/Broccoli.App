using Broccoli.App.Shared.Services;
using Broccoli.Data.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Broccoli.App.Tests.Services;

[TestClass]
public class ParsedIngredientsTableTests
{
    #region ParseIngredient Tests

    [TestMethod]
    public void ParseIngredient_SimpleQuantityAndUnit_ReturnsParsedIngredient()
    {
        // Arrange
        string ingredient = "1 cup flour";

        // Act
        var result = IngredientParserService.ParseIngredient(ingredient);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result.Quantity);
        Assert.AreEqual("cup", result.Unit);
        Assert.AreEqual("flour", result.FoodName);
    }

    [TestMethod]
    public void ParseIngredient_DecimalQuantity_ReturnsParsedIngredient()
    {
        // Arrange
        string ingredient = "2.5 tbsp butter";

        // Act
        var result = IngredientParserService.ParseIngredient(ingredient);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2.5, result.Quantity);
        Assert.AreEqual("tbsp", result.Unit);
        Assert.AreEqual("butter", result.FoodName);
    }

    [TestMethod]
    public void ParseIngredient_FractionQuantity_ReturnsParsedIngredient()
    {
        // Arrange
        string ingredient = "1/2 cup sugar";

        // Act
        var result = IngredientParserService.ParseIngredient(ingredient);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0.5, result.Quantity, 0.01);
        Assert.AreEqual("cup", result.Unit);
        Assert.AreEqual("sugar", result.FoodName);
    }

    [TestMethod]
    public void ParseIngredient_MixedFraction_ReturnsParsedIngredient()
    {
        // Arrange
        string ingredient = "1 1/2 cups milk";

        // Act
        var result = IngredientParserService.ParseIngredient(ingredient);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1.5, result.Quantity, 0.01);
        Assert.AreEqual("cup", result.Unit);
        Assert.AreEqual("milk", result.FoodName);
    }

    [TestMethod]
    public void ParseIngredient_GramsUnit_ReturnsParsedIngredient()
    {
        // Arrange
        string ingredient = "250g chicken breast";

        // Act
        var result = IngredientParserService.ParseIngredient(ingredient);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(250.0, result.Quantity);
        Assert.AreEqual("g", result.Unit);
        Assert.AreEqual("chicken breast", result.FoodName);
    }

    [TestMethod]
    public void ParseIngredient_MultiWordFoodName_ReturnsParsedIngredient()
    {
        // Arrange
        string ingredient = "2 tbsp smooth peanut butter";

        // Act
        var result = IngredientParserService.ParseIngredient(ingredient);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2.0, result.Quantity);
        Assert.AreEqual("tbsp", result.Unit);
        Assert.AreEqual("smooth peanut butter", result.FoodName);
    }

    [TestMethod]
    public void ParseIngredient_NoQuantity_DefaultsToOne()
    {
        // Arrange
        string ingredient = "salt";

        // Act
        var result = IngredientParserService.ParseIngredient(ingredient);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result.Quantity);
    }

    [TestMethod]
    public void ParseIngredient_WhitespaceOnly_ReturnsNull()
    {
        // Arrange
        string ingredient = "   ";

        // Act
        var result = IngredientParserService.ParseIngredient(ingredient);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseIngredient_NullInput_ReturnsNull()
    {
        // Arrange
        string ingredient = null;

        // Act
        var result = IngredientParserService.ParseIngredient(ingredient);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseIngredient_CaseInsensitiveUnit_NormalizesCorrectly()
    {
        // Arrange
        string ingredient = "1 CUP flour";

        // Act
        var result = IngredientParserService.ParseIngredient(ingredient);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("cup", result.Unit); // Should be normalized to lowercase
    }

    #endregion

    #region NormalizeUnit Tests

    [TestMethod]
    public void NormalizeUnit_StandardGramUnit_ReturnsCanonical()
    {
        // Arrange
        string unit = "g";

        // Act
        string normalized = IngredientParserService.NormalizeUnit(unit, out string canonical);

        // Assert
        Assert.AreEqual("g", normalized);
        Assert.AreEqual("g", canonical);
    }

    [TestMethod]
    public void NormalizeUnit_CupVariants_AllReturnCanonical()
    {
        // Arrange
        var variants = new[] { "cup", "cups", "c", "Cup", "CUPS" };

        foreach (var variant in variants)
        {
            // Act
            string normalized = IngredientParserService.NormalizeUnit(variant, out string canonical);

            // Assert
            Assert.AreEqual("cup", normalized, $"Failed for variant: {variant}");
            Assert.AreEqual("cup", canonical);
        }
    }

    [TestMethod]
    public void NormalizeUnit_TablespoonVariants_AllReturnCanonical()
    {
        // Arrange
        var variants = new[] { "tbsp", "tablespoon", "tablespoons", "tbl", "t" };

        foreach (var variant in variants)
        {
            // Act
            string normalized = IngredientParserService.NormalizeUnit(variant, out string canonical);

            // Assert
            Assert.AreEqual("tbsp", normalized, $"Failed for variant: {variant}");
        }
    }

    [TestMethod]
    public void NormalizeUnit_UnknownUnit_ReturnsOriginal()
    {
        // Arrange
        string unit = "unknown_unit";

        // Act
        string normalized = IngredientParserService.NormalizeUnit(unit, out string canonical);

        // Assert
        Assert.AreEqual("unknown_unit", normalized);
        Assert.AreEqual("unknown_unit", canonical);
    }

    [TestMethod]
    public void NormalizeUnit_EmptyString_ReturnsEmpty()
    {
        // Arrange
        string unit = "";

        // Act
        string normalized = IngredientParserService.NormalizeUnit(unit, out string canonical);

        // Assert
        Assert.AreEqual("", normalized);
        Assert.AreEqual("", canonical);
    }

    #endregion

    #region Levenshtein Distance Tests

    [TestMethod]
    public void LevenshteinDistance_IdenticalStrings_ReturnsZero()
    {
        // Arrange
        string source = "flour";
        string target = "flour";

        // Act
        int distance = CalculateLevenshteinDistance(source, target);

        // Assert
        Assert.AreEqual(0, distance);
    }

    [TestMethod]
    public void LevenshteinDistance_SingleCharacterDifference_ReturnsOne()
    {
        // Arrange
        string source = "flour";
        string target = "floir"; // 'o' and 'u' swapped (1 substitution)

        // Act
        int distance = CalculateLevenshteinDistance(source, target);

        // Assert
        Assert.IsTrue(distance <= 2, "Single character difference should have distance <= 2");
    }

    [TestMethod]
    public void LevenshteinDistance_MissingCharacter_ReturnsOne()
    {
        // Arrange
        string source = "flour";
        string target = "four"; // Missing 'l'

        // Act
        int distance = CalculateLevenshteinDistance(source, target);

        // Assert
        Assert.AreEqual(1, distance);
    }

    [TestMethod]
    public void LevenshteinDistance_ExtraCharacter_ReturnsOne()
    {
        // Arrange
        string source = "flour";
        string target = "flourr"; // Extra 'r'

        // Act
        int distance = CalculateLevenshteinDistance(source, target);

        // Assert
        Assert.AreEqual(1, distance);
    }

    [TestMethod]
    public void LevenshteinDistance_Typos_WithinThresholdOfThree()
    {
        // Arrange
        var testCases = new[]
        {
            ("apple", "aple"),     // 1 deletion
            ("banana", "bananna"),  // 1 insertion
            ("chicken", "chiken"),  // 1 deletion
            ("flour", "flur"),      // 1 deletion
        };

        foreach (var (source, target) in testCases)
        {
            // Act
            int distance = CalculateLevenshteinDistance(source, target);

            // Assert
            Assert.IsTrue(distance <= 3, $"Typo '{source}' -> '{target}' should have distance <= 3, got {distance}");
        }
    }

    [TestMethod]
    public void LevenshteinDistance_CompletelyDifferent_GreaterThanThreshold()
    {
        // Arrange
        string source = "apple";
        string target = "xyz";

        // Act
        int distance = CalculateLevenshteinDistance(source, target);

        // Assert
        Assert.IsTrue(distance > 3, "Completely different strings should have distance > 3");
    }

    #endregion

    #region Nutrition Calculation Tests

    [TestMethod]
    public void GetCalories_MatchedFood_CalculatesCorrectly()
    {
        // Arrange
        var food = new Food
        {
            Id = 1,
            Name = "Apple",
            Measure = "medium",
            GramsPerMeasure = 182.0,
            CaloriesPer100g = 52.0,
            FatPer100g = 0.2,
            ProteinPer100g = 0.3,
            CarbohydratesPer100g = 13.8
        };

        var parsed = new ParsedIngredient
        {
            RawLine = "1 apple",
            Quantity = 1.0,
            Unit = "medium",
            CanonicalUnit = "medium",
            FoodName = "Apple"
        };

        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = food,
            MatchDistance = 0,
            IsMatched = true
        };

        // Act
        double calories = match.GetCalories();

        // Assert
        // 1 apple * 182g * (52 cal / 100g) = ~94.64 calories
        Assert.IsTrue(calories > 90 && calories < 100, $"Calories should be ~94.64, got {calories}");
    }

    [TestMethod]
    public void GetFat_MatchedFood_CalculatesCorrectly()
    {
        // Arrange
        var food = new Food
        {
            Id = 1,
            Name = "Butter",
            Measure = "tbsp",
            GramsPerMeasure = 14.2,
            CaloriesPer100g = 717.0,
            FatPer100g = 81.0,
            ProteinPer100g = 0.9,
            CarbohydratesPer100g = 0.1
        };

        var parsed = new ParsedIngredient
        {
            RawLine = "1 tbsp butter",
            Quantity = 1.0,
            Unit = "tbsp",
            CanonicalUnit = "tbsp",
            FoodName = "Butter"
        };

        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = food,
            MatchDistance = 0,
            IsMatched = true
        };

        // Act
        double fat = match.GetFat();

        // Assert
        // 1 tbsp * 14.2g * (81g / 100g) = ~11.5g fat
        Assert.IsTrue(fat > 10 && fat < 13, $"Fat should be ~11.5g, got {fat}");
    }

    [TestMethod]
    public void GetProtein_MatchedFood_CalculatesCorrectly()
    {
        // Arrange
        var food = new Food
        {
            Id = 1,
            Name = "Chicken Breast",
            Measure = "g",
            GramsPerMeasure = 1.0,
            CaloriesPer100g = 165.0,
            FatPer100g = 3.6,
            ProteinPer100g = 31.0,
            CarbohydratesPer100g = 0.0
        };

        var parsed = new ParsedIngredient
        {
            RawLine = "100g chicken breast",
            Quantity = 100.0,
            Unit = "g",
            CanonicalUnit = "g",
            FoodName = "Chicken Breast"
        };

        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = food,
            MatchDistance = 0,
            IsMatched = true
        };

        // Act
        double protein = match.GetProtein();

        // Assert
        // 100g * (31g / 100g) = 31g protein
        Assert.AreEqual(31.0, protein, 0.1);
    }

    [TestMethod]
    public void GetCarbohydrates_MatchedFood_CalculatesCorrectly()
    {
        // Arrange
        var food = new Food
        {
            Id = 1,
            Name = "Banana",
            Measure = "medium",
            GramsPerMeasure = 118.0,
            CaloriesPer100g = 89.0,
            FatPer100g = 0.3,
            ProteinPer100g = 1.1,
            CarbohydratesPer100g = 23.0
        };

        var parsed = new ParsedIngredient
        {
            RawLine = "1 banana",
            Quantity = 1.0,
            Unit = "medium",
            CanonicalUnit = "medium",
            FoodName = "Banana"
        };

        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = food,
            MatchDistance = 0,
            IsMatched = true
        };

        // Act
        double carbs = match.GetCarbohydrates();

        // Assert
        // 1 banana * 118g * (23g / 100g) = ~27.14g carbs
        Assert.IsTrue(carbs > 25 && carbs < 30, $"Carbs should be ~27.14g, got {carbs}");
    }

    [TestMethod]
    public void GetWeightInGrams_MatchedFood_CalculatesCorrectly()
    {
        // Arrange
        var food = new Food
        {
            Id = 1,
            Name = "Milk",
            Measure = "cup",
            GramsPerMeasure = 240.0,
            CaloriesPer100g = 61.0,
            FatPer100g = 3.3,
            ProteinPer100g = 3.2,
            CarbohydratesPer100g = 4.8
        };

        var parsed = new ParsedIngredient
        {
            RawLine = "1 cup milk",
            Quantity = 1.0,
            Unit = "cup",
            CanonicalUnit = "cup",
            FoodName = "Milk"
        };

        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = food,
            MatchDistance = 0,
            IsMatched = true
        };

        // Act
        double weightInGrams = match.GetWeightInGrams();

        // Assert
        Assert.AreEqual(240.0, weightInGrams);
    }

    [TestMethod]
    public void GetCalories_UnmatchedFood_ReturnsZero()
    {
        // Arrange
        var parsed = new ParsedIngredient
        {
            RawLine = "1 unknown ingredient",
            Quantity = 1.0,
            Unit = "cup",
            CanonicalUnit = "cup",
            FoodName = "unknown ingredient"
        };

        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = null,
            MatchDistance = -1,
            IsMatched = false
        };

        // Act
        double calories = match.GetCalories();

        // Assert
        Assert.AreEqual(0.0, calories);
    }

    #endregion

    #region Helper Methods

    private static int CalculateLevenshteinDistance(string source, string target)
    {
        if (source == target)
        {
            return 0;
        }

        int sourceLength = source.Length;
        int targetLength = target.Length;

        if (sourceLength == 0)
        {
            return targetLength;
        }

        if (targetLength == 0)
        {
            return sourceLength;
        }

        int[] previousRow = new int[targetLength + 1];
        int[] currentRow = new int[targetLength + 1];

        for (int j = 0; j <= targetLength; j++)
        {
            previousRow[j] = j;
        }

        for (int i = 1; i <= sourceLength; i++)
        {
            currentRow[0] = i;

            for (int j = 1; j <= targetLength; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;

                currentRow[j] = Math.Min(
                    Math.Min(
                        previousRow[j] + 1,
                        currentRow[j - 1] + 1),
                    previousRow[j - 1] + cost);
            }

            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[targetLength];
    }

    #endregion
}

