using Broccoli.Data.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq; // Using Moq for mocking IFoodService
using Broccoli.App.Shared.IngredientParsing;
using Newtonsoft.Json; // Added for JSON deserialization

namespace Broccoli.App.Tests.Services;

[TestClass]
public class IngredientParserServiceTests
{
    // JSON data representing the full food database
    private const string FullFoodDatabaseJson = @"
[
{
    ""Id"": 1,
    ""Name"": ""Double Phoenix Asian Vermicelli"",
    ""Measure"": ""Serving"",
    ""GramsPerMeasure"": 100.0,
    ""Notes"": ""Standard noodles"",
    ""CaloriesPer100g"": 347.8,
    ""FatPer100g"": 0.0,
    ""SaturatedFatPer100g"": 0.0,
    ""TransFatPer100g"": 0.0,
    ""CarbohydratesPer100g"": 85.6,
    ""DietaryFiberPer100g"": 0.0,
    ""SugarsPer100g"": 0.0,
    ""ProteinPer100g"": 0.0,
    ""CholesterolMgPer100g"": 0.0,
    ""SodiumMgPer100g"": 30.0,
    ""VitaminAMcgPer100g"": 0.0,
    ""VitaminCMgPer100g"": 0.0,
    ""CalciumMgPer100g"": 0.0,
    ""IronMgPer100g"": 0.0
  },
  {
    ""Id"": 2,
    ""Name"": ""Olive Oil"",
    ""Measure"": ""Tablespoon"",
    ""GramsPerMeasure"": 13.0,
    ""Notes"": ""Calculation: (Value/13)*100"",
    ""CaloriesPer100g"": 917.69,
    ""FatPer100g"": 103.85,
    ""SaturatedFatPer100g"": 14.62,
    ""TransFatPer100g"": 0.0,
    ""CarbohydratesPer100g"": 0.0,
    ""DietaryFiberPer100g"": 0.0,
    ""SugarsPer100g"": 0.0,
    ""ProteinPer100g"": 0.0,
    ""CholesterolMgPer100g"": 0.0,
    ""SodiumMgPer100g"": 2.31,
    ""VitaminAMcgPer100g"": 0.0,
    ""VitaminCMgPer100g"": 0.0,
    ""CalciumMgPer100g"": 0.0,
    ""IronMgPer100g"": 0.0
  },
  {
    ""Id"": 3,
    ""Name"": ""Carrots, Raw"",
    ""Measure"": ""Medium Carrot"",
    ""GramsPerMeasure"": 61.0,
    ""Notes"": ""High Vitamin A"",
    ""CaloriesPer100g"": 45.08,
    ""FatPer100g"": 0.33,
    ""SaturatedFatPer100g"": 0.0,
    ""TransFatPer100g"": 0.0,
    ""CarbohydratesPer100g"": 10.33,
    ""DietaryFiberPer100g"": 3.11,
    ""SugarsPer100g"": 4.75,
    ""ProteinPer100g"": 0.98,
    ""CholesterolMgPer100g"": 0.0,
    ""SodiumMgPer100g"": 87.05,
    ""VitaminAMcgPer100g"": 501.64,
    ""VitaminCMgPer100g"": 8.85,
    ""CalciumMgPer100g"": 42.62,
    ""IronMgPer100g"": 0.3
  },
  {
    ""Id"": 4,
    ""Name"": ""Chicken Breast, Skinless, Raw"",
    ""Measure"": ""Gram"",
    ""GramsPerMeasure"": 1.0,
    ""Notes"": ""Lean protein source"",
    ""CaloriesPer100g"": 120.0,
    ""FatPer100g"": 0.0,
    ""SaturatedFatPer100g"": 0.0,
    ""TransFatPer100g"": 0.0,
    ""CarbohydratesPer100g"": 0.0,
    ""DietaryFiberPer100g"": 0.0,
    ""SugarsPer100g"": 0.0,
    ""ProteinPer100g"": 20.0,
    ""CholesterolMgPer100g"": 70.0,
    ""SodiumMgPer100g"": 50.0,
    ""VitaminAMcgPer100g"": 0.0,
    ""VitaminCMgPer100g"": 0.0,
    ""CalciumMgPer100g"": 0.0,
    ""IronMgPer100g"": 0.0
  },
  {
    ""Id"": 5,
    ""Name"": ""Ayam Brand Malaysian Laksa Paste"",
    ""Measure"": ""Gram"",
    ""GramsPerMeasure"": 1.0,
    ""Notes"": ""Sodium intense"",
    ""CaloriesPer100g"": 105.16,
    ""FatPer100g"": 0.0,
    ""SaturatedFatPer100g"": 0.0,
    ""TransFatPer100g"": 0.0,
    ""CarbohydratesPer100g"": 10.0,
    ""DietaryFiberPer100g"": 0.0,
    ""SugarsPer100g"": 0.0,
    ""ProteinPer100g"": 0.0,
    ""CholesterolMgPer100g"": 0.0,
    ""SodiumMgPer100g"": 1790.0,
    ""VitaminAMcgPer100g"": 0.0,
    ""VitaminCMgPer100g"": 0.0,
    ""CalciumMgPer100g"": 0.0,
    ""IronMgPer100g"": 0.0
  },
  {
    ""Id"": 6,
    ""Name"": ""Curry Powder"",
    ""Measure"": ""Teaspoon"",
    ""GramsPerMeasure"": 2.0,
    ""Notes"": ""Values scaled from 2g serving"",
    ""CaloriesPer100g"": 340.0,
    ""FatPer100g"": 15.0,
    ""SaturatedFatPer100g"": 0.0,
    ""TransFatPer100g"": 0.0,
    ""CarbohydratesPer100g"": 60.0,
    ""DietaryFiberPer100g"": 55.0,
    ""SugarsPer100g"": 5.0,
    ""ProteinPer100g"": 15.0,
    ""CholesterolMgPer100g"": 0.0,
    ""SodiumMgPer100g"": 55.0,
    ""VitaminAMcgPer100g"": 0.0,
    ""VitaminCMgPer100g"": 0.0,
    ""CalciumMgPer100g"": 650.0,
    ""IronMgPer100g"": 18.0
  },
  {
    ""Id"": 7,
    ""Name"": ""Thai Kitchen Coconut Milk Lite"",
    ""Measure"": ""Can"",
    ""GramsPerMeasure"": 400.0,
    ""Notes"": ""Calculation: (Value/400)*100"",
    ""CaloriesPer100g"": 66.67,
    ""FatPer100g"": 5.83,
    ""SaturatedFatPer100g"": 5.0,
    ""TransFatPer100g"": 0.0,
    ""CarbohydratesPer100g"": 3.33,
    ""DietaryFiberPer100g"": 0.0,
    ""SugarsPer100g"": 0.0,
    ""ProteinPer100g"": 0.68,
    ""CholesterolMgPer100g"": 8.32,
    ""SodiumMgPer100g"": 8.32,
    ""VitaminAMcgPer100g"": 0.0,
    ""VitaminCMgPer100g"": 1.58,
    ""CalciumMgPer100g"": 22.75,
    ""IronMgPer100g"": 3.29
  },
  {
    ""Id"": 8,
    ""Name"": ""Chinese Cabbage, Pak-Choi, Raw"",
    ""Measure"": ""Head"",
    ""GramsPerMeasure"": 840.0,
    ""Notes"": ""Massive Vitamin C source"",
    ""CaloriesPer100g"": 13.0,
    ""FatPer100g"": 0.2,
    ""SaturatedFatPer100g"": 0.02,
    ""TransFatPer100g"": 0.0,
    ""CarbohydratesPer100g"": 2.18,
    ""DietaryFiberPer100g"": 1.0,
    ""SugarsPer100g"": 1.18,
    ""ProteinPer100g"": 1.5,
    ""CholesterolMgPer100g"": 0.0,
    ""SodiumMgPer100g"": 65.0,
    ""VitaminAMcgPer100g"": 133.93,
    ""VitaminCMgPer100g"": 67.5,
    ""CalciumMgPer100g"": 136.19,
    ""IronMgPer100g"": 0.79
  },
  {
    ""Id"": 9,
    ""Name"": ""Fish Sauce"",
    ""Measure"": ""Tablespoon"",
    ""GramsPerMeasure"": 18.0,
    ""Notes"": ""Very high sodium"",
    ""CaloriesPer100g"": 35.0,
    ""FatPer100g"": 0.0,
    ""SaturatedFatPer100g"": 0.0,
    ""TransFatPer100g"": 0.0,
    ""CarbohydratesPer100g"": 3.89,
    ""DietaryFiberPer100g"": 0.0,
    ""SugarsPer100g"": 3.89,
    ""ProteinPer100g"": 5.0,
    ""CholesterolMgPer100g"": 0.0,
    ""SodiumMgPer100g"": 7851.11,
    ""VitaminAMcgPer100g"": 0.0,
    ""VitaminCMgPer100g"": 0.0,
    ""CalciumMgPer100g"": 72.22,
    ""IronMgPer100g"": 1.0
  },
  {
    ""Id"": 10,
    ""Name"": ""Mung Bean Sprouts, Raw"",
    ""Measure"": ""Cup"",
    ""GramsPerMeasure"": 104.0,
    ""Notes"": ""Fresh vegetables"",
    ""CaloriesPer100g"": 30.0,
    ""FatPer100g"": 0.19,
    ""SaturatedFatPer100g"": 0.0,
    ""TransFatPer100g"": 0.0,
    ""CarbohydratesPer100g"": 5.96,
    ""DietaryFiberPer100g"": 1.83,
    ""SugarsPer100g"": 2.12,
    ""ProteinPer100g"": 3.08,
    ""CholesterolMgPer100g"": 0.0,
    ""SodiumMgPer100g"": 5.96,
    ""VitaminAMcgPer100g"": 0.0,
    ""VitaminCMgPer100g"": 19.9,
    ""CalciumMgPer100g"": 12.5,
    ""IronMgPer100g"": 0.87
  }
]
";

    [TestMethod]
    public async Task ParseAndMatchIngredientsAsync_ExactMatch_ReturnsMatched()
    {
        // Arrange
        var mockFoodService = new Mock<IFoodService>();
        var food = new Food { Id = 1, Name = "Flour", Measure = "cup", GramsPerMeasure = 120 };
        Food? outFood = food; // Declare a nullable Food variable for out parameter
        mockFoodService.Setup(s => s.TryGetFood("flour", out outFood)).Returns(true);
        mockFoodService.Setup(s => s.TryGetFoodFuzzy(It.IsAny<string>(), out It.Ref<Food?>.IsAny)).Returns(false);
        mockFoodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns(new FoodMatchResult { Food = food, Score = 1.0, Method = "Exact" });

        string ingredientLine = "1 cup flour";

        IngredientParserService ingredientParser = CreateIngredientParserService(mockFoodService.Object);

        // Act
        var results = await ingredientParser.ParseAndMatchIngredientsAsync(ingredientLine);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(1, results.Count);
        Assert.IsTrue(results[0].IsMatched);
        Assert.AreEqual("Flour",
            results[0].MatchedFood!.Name); // Use null-forgiving operator as we assert IsMatched is true
        Assert.AreEqual(0, results[0].MatchDistance);
    }

    [TestMethod]
    public async Task ParseAndMatchIngredientsAsync_FuzzyMatch_ReturnsMatched()
    {
        // Arrange
        var mockFoodService = new Mock<IFoodService>();
        Food? exactFood = null; // No exact match
        mockFoodService.Setup(s => s.TryGetFood("flour", out exactFood)).Returns(false);

        var fuzzyFood = new Food { Id = 1, Name = "All-Purpose Flour", Measure = "cup", GramsPerMeasure = 120 };
        Food? outFuzzyFood = fuzzyFood; // Declare a nullable Food variable for out parameter
        mockFoodService.Setup(s => s.TryGetFoodFuzzy("flour", out outFuzzyFood)).Returns(true);
        mockFoodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns(new FoodMatchResult { Food = fuzzyFood, Score = 0.7, Method = "Fuzzy" });

        string ingredientLine = "1 cup flour";

        IngredientParserService ingredientParser = CreateIngredientParserService(mockFoodService.Object);

        // Act
        var results = await ingredientParser.ParseAndMatchIngredientsAsync(ingredientLine);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(1, results.Count);
        Assert.IsTrue(results[0].IsMatched);
        Assert.AreEqual("All-Purpose Flour", results[0].MatchedFood!.Name); // Use null-forgiving operator
        Assert.IsTrue(results[0].MatchDistance > 0); // Fuzzy match should have a distance > 0
    }

    [TestMethod]
    public async Task ParseAndMatchIngredientsAsync_NoMatch_ReturnsUnmatched()
    {
        // Arrange
        var mockFoodService = new Mock<IFoodService>();
        Food? food = null; // Declare a nullable Food variable for out parameter
        mockFoodService.Setup(s => s.TryGetFood(It.IsAny<string>(), out food)).Returns(false);
        mockFoodService.Setup(s => s.TryGetFoodFuzzy(It.IsAny<string>(), out food)).Returns(false);
        mockFoodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns(new FoodMatchResult { Food = null, Score = 0, Method = "None" });

        string ingredientLine = "1 cup unknown_item";

        IngredientParserService ingredientParser = CreateIngredientParserService(mockFoodService.Object);

        // Act
        var results = await ingredientParser.ParseAndMatchIngredientsAsync(ingredientLine);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(1, results.Count);
        Assert.IsFalse(results[0].IsMatched);
        Assert.IsNull(results[0].MatchedFood);
        Assert.AreEqual(-1, results[0].MatchDistance);
    }

    [TestMethod]
    public async Task ParseAndMatchIngredientsAsync_MixedMatches_ReturnsCorrectly()
    {
        // Arrange
        var mockFoodService = new Mock<IFoodService>();

        var flourFood = new Food { Id = 1, Name = "Flour", Measure = "cup", GramsPerMeasure = 120 };
        var sugarFood = new Food { Id = 2, Name = "Granulated Sugar", Measure = "cup", GramsPerMeasure = 200 };
        Food? nullFood = null; // Declare a nullable Food variable for out parameter

        // Setup for "flour" (exact match)
        Food? outFlourFood = flourFood;
        mockFoodService.Setup(s => s.TryGetFood("flour", out outFlourFood)).Returns(true);
        mockFoodService.Setup(s => s.TryGetFoodFuzzy("flour", out It.Ref<Food?>.IsAny)).Returns(false);

        // Setup for "sugr" (fuzzy match to "Granulated Sugar")
        Food? outNullFood1 = null;
        mockFoodService.Setup(s => s.TryGetFood("sugr", out outNullFood1)).Returns(false);
        Food? outSugarFood = sugarFood;
        mockFoodService.Setup(s => s.TryGetFoodFuzzy("sugr", out outSugarFood)).Returns(true);

        // Setup for "unknown" (no match)
        Food? outNullFood2 = null;
        mockFoodService.Setup(s => s.TryGetFood("unknown", out outNullFood2)).Returns(false);
        Food? outNullFood3 = null;
        mockFoodService.Setup(s => s.TryGetFoodFuzzy("unknown", out outNullFood3)).Returns(false);

        // FindBestMatch for each ingredient
        // Fallback registered first — Moq evaluates last-registered first, so specifics below override it
        mockFoodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns(new FoodMatchResult { Food = null, Score = 0, Method = "None" });
        mockFoodService.Setup(s => s.FindBestMatch("flour"))
            .Returns(new FoodMatchResult { Food = flourFood, Score = 1.0, Method = "Exact" });
        mockFoodService.Setup(s => s.FindBestMatch("sugr"))
            .Returns(new FoodMatchResult { Food = sugarFood, Score = 0.65, Method = "Fuzzy" });
        mockFoodService.Setup(s => s.FindBestMatch("unknown"))
            .Returns(new FoodMatchResult { Food = null, Score = 0, Method = "None" });

        string ingredientLines = "1 cup flour\n0.5 cup sugr\n2 tsp unknown";

        IngredientParserService ingredientParser = CreateIngredientParserService(mockFoodService.Object);

        // Act
        var results = await ingredientParser.ParseAndMatchIngredientsAsync(ingredientLines);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(3, results.Count);

        // Flour - Exact Match
        Assert.IsTrue(results[0].IsMatched);
        Assert.AreEqual("Flour", results[0].MatchedFood!.Name);
        Assert.AreEqual(0, results[0].MatchDistance);

        // Sugar - Fuzzy Match
        Assert.IsTrue(results[1].IsMatched);
        Assert.AreEqual("Granulated Sugar", results[1].MatchedFood!.Name);
        Assert.IsTrue(results[1].MatchDistance > 0);

        // Unknown - No Match
        Assert.IsFalse(results[2].IsMatched);
        Assert.IsNull(results[2].MatchedFood);
        Assert.AreEqual(-1, results[2].MatchDistance);
    }

    [TestMethod]
    public async Task ParseAndMatchIngredientsAsync_EmptyInput_ReturnsEmptyList()
    {
        // Arrange
        var mockFoodService = new Mock<IFoodService>();
        string ingredientLines = "";

        IngredientParserService ingredientParser = CreateIngredientParserService(mockFoodService.Object);

        // Act
        var results = await ingredientParser.ParseAndMatchIngredientsAsync(ingredientLines);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public async Task ParseAndMatchIngredientsAsync_NullInput_ReturnsEmptyList()
    {
        // Arrange
        var mockFoodService = new Mock<IFoodService>();
        string ingredientLines = null;

        IngredientParserService ingredientParser = CreateIngredientParserService(mockFoodService.Object);

        // Act
        var results = await ingredientParser.ParseAndMatchIngredientsAsync(ingredientLines);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public async Task ParseAndMatchIngredientsAsync_IngredientLineParsingFails_SkipsLine()
    {
        // Arrange
        var mockFoodService = new Mock<IFoodService>();
        Food? food = null;
        mockFoodService.Setup(s => s.TryGetFood(It.IsAny<string>(), out food)).Returns(false);
        mockFoodService.Setup(s => s.TryGetFoodFuzzy(It.IsAny<string>(), out food)).Returns(false);
        mockFoodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns(new FoodMatchResult { Food = null, Score = 0, Method = "None" });

        string ingredientLines = "1.5\n1 cup flour"; // First line will fail parsing

        IngredientParserService ingredientParser = CreateIngredientParserService(mockFoodService.Object);

        // Act
        var results = await ingredientParser.ParseAndMatchIngredientsAsync(ingredientLines);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(1, results.Count); // Only the second line should be processed
        Assert.AreEqual("flour", results[0].ParsedIngredient.FoodName);
    }

    [TestMethod]
    public async Task ParseAndMatchIngredientsAsync_WithFullDatabase_ReturnsCorrectMatches()
    {
        // Arrange
        var mockFoodService = CreateMockFoodService(FullFoodDatabaseJson);

        string ingredientLines = @"
100g Vermicelli
2 tbsp Olive Oil
1 medium Carrots, Raw
200g Chicken Breast, Skinless, Raw
50g Ayam Brand Malaysian Laksa Paste
1 tsp Curry Powder
1 can Thai Kitchen Coconut Milk Lite
1 head Chinese Cabbage, Pak-Choi, Raw
1 tbsp Fish Sauce
1 cup Mung Bean Sprouts, Raw
2 cups Unknown Item
";

        IngredientParserService ingredientParser = CreateIngredientParserService(mockFoodService);

        // Act
        var results = await ingredientParser.ParseAndMatchIngredientsAsync(ingredientLines);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(11, results.Count);

        // Vermicelli — "100g Vermicelli" → food="Vermicelli", mock Levenshtein best match = "Double Phoenix Asian Vermicelli"
        Assert.IsTrue(results[0].IsMatched,
            $"Vermicelli should be matched. RawLine: {results[0].ParsedIngredient.RawLine}, FoodName: {results[0].ParsedIngredient.FoodName}");
        Assert.AreEqual(100.0, results[0].ParsedIngredient.Quantity);
        Assert.AreEqual("g", results[0].ParsedIngredient.Unit);

        // Olive Oil (Exact Match via mock Levenshtein)
        Assert.IsTrue(results[1].IsMatched,
            $"Olive Oil should be matched. RawLine: {results[1].ParsedIngredient.RawLine}, FoodName: {results[1].ParsedIngredient.FoodName}");
        Assert.AreEqual("Olive Oil", results[1].MatchedFood!.Name);
        Assert.AreEqual(2.0, results[1].ParsedIngredient.Quantity);
        Assert.AreEqual("tbsp", results[1].ParsedIngredient.Unit);

        // Carrots — "1 medium Carrots, Raw" → comma-stripped → food="Carrots", fuzzy match to "Carrots, Raw"
        Assert.IsTrue(results[2].IsMatched,
            $"Carrots should be matched. RawLine: {results[2].ParsedIngredient.RawLine}, FoodName: {results[2].ParsedIngredient.FoodName}");
        Assert.AreEqual(1.0, results[2].ParsedIngredient.Quantity);
        Assert.AreEqual("medium", results[2].ParsedIngredient.Unit);

        // Chicken Breast — "200g Chicken Breast, Skinless, Raw" → comma-stripped → food="Chicken Breast"
        Assert.IsTrue(results[3].IsMatched,
            $"Chicken Breast should be matched. RawLine: {results[3].ParsedIngredient.RawLine}, FoodName: {results[3].ParsedIngredient.FoodName}");
        Assert.AreEqual(200.0, results[3].ParsedIngredient.Quantity);
        Assert.AreEqual("g", results[3].ParsedIngredient.Unit);

        // Ayam Brand Malaysian Laksa Paste (Exact Match)
        Assert.IsTrue(results[4].IsMatched,
            $"Ayam Brand Malaysian Laksa Paste should be matched. RawLine: {results[4].ParsedIngredient.RawLine}, FoodName: {results[4].ParsedIngredient.FoodName}");
        Assert.AreEqual("Ayam Brand Malaysian Laksa Paste", results[4].MatchedFood!.Name);
        Assert.AreEqual(50.0, results[4].ParsedIngredient.Quantity);
        Assert.AreEqual("g", results[4].ParsedIngredient.Unit);
        Assert.AreEqual(0, results[4].MatchDistance);

        // Curry Powder (Exact Match)
        Assert.IsTrue(results[5].IsMatched,
            $"Curry Powder should be matched. RawLine: {results[5].ParsedIngredient.RawLine}, FoodName: {results[5].ParsedIngredient.FoodName}");
        Assert.AreEqual("Curry Powder", results[5].MatchedFood!.Name);
        Assert.AreEqual(1.0, results[5].ParsedIngredient.Quantity);
        Assert.AreEqual("tsp", results[5].ParsedIngredient.Unit);
        Assert.AreEqual(0, results[5].MatchDistance);

        // Thai Kitchen Coconut Milk Lite (Exact Match)
        Assert.IsTrue(results[6].IsMatched,
            $"Thai Kitchen Coconut Milk Lite should be matched. RawLine: {results[6].ParsedIngredient.RawLine}, FoodName: {results[6].ParsedIngredient.FoodName}");
        Assert.AreEqual("Thai Kitchen Coconut Milk Lite", results[6].MatchedFood!.Name);
        Assert.AreEqual(1.0, results[6].ParsedIngredient.Quantity);
        Assert.AreEqual("can", results[6].ParsedIngredient.Unit);
        Assert.AreEqual(0, results[6].MatchDistance);

        // Chinese Cabbage — "1 head Chinese Cabbage, Pak-Choi, Raw" → comma-stripped → food="Chinese Cabbage"
        Assert.IsTrue(results[7].IsMatched,
            $"Chinese Cabbage should be matched. RawLine: {results[7].ParsedIngredient.RawLine}, FoodName: {results[7].ParsedIngredient.FoodName}");
        Assert.AreEqual(1.0, results[7].ParsedIngredient.Quantity);
        Assert.AreEqual("head", results[7].ParsedIngredient.Unit);

        // Fish Sauce (Exact Match)
        Assert.IsTrue(results[8].IsMatched,
            $"Fish Sauce should be matched. RawLine: {results[8].ParsedIngredient.RawLine}, FoodName: {results[8].ParsedIngredient.FoodName}");
        Assert.AreEqual("Fish Sauce", results[8].MatchedFood!.Name);
        Assert.AreEqual(1.0, results[8].ParsedIngredient.Quantity);
        Assert.AreEqual("tbsp", results[8].ParsedIngredient.Unit);
        Assert.AreEqual(0, results[8].MatchDistance);

        // Mung Bean Sprouts — "1 cup Mung Bean Sprouts, Raw" → comma-stripped → food="Mung Bean Sprouts"
        Assert.IsTrue(results[9].IsMatched,
            $"Mung Bean Sprouts should be matched. RawLine: {results[9].ParsedIngredient.RawLine}, FoodName: {results[9].ParsedIngredient.FoodName}");
        Assert.AreEqual(1.0, results[9].ParsedIngredient.Quantity);
        Assert.AreEqual("cup", results[9].ParsedIngredient.Unit);

        // Unknown Item — low confidence match or no match expected
        // The mock may return a low-score Levenshtein candidate; verify score is below high-confidence threshold
        Assert.IsTrue(
            !results[10].IsMatched || results[10].MatchScore < 0.85,
            $"Unknown Item should not be high-confidence matched. RawLine: {results[10].ParsedIngredient.RawLine}");
    }

    [TestMethod]
    public async Task ParseAndMatchIngredientsAsync_FailingTest_WithRealData()
    {
        // Arrange
        var mockFoodService = CreateMockFoodService(FullFoodDatabaseJson);

        string ingredientLines = @"
250g vermicelli noodles
1 drizzle of oil 
1 drizzle of oil 
600g carrot, grated
1400g diced free range chicken breast
150g Singapore laksa paste
1 1/2 pack Malaysian curry powder
400g lite coconut milk
1/2 cup water 
1 twin pack baby bok choy, sliced 3cm
1 tsp fish sauce, optional 
200g mung bean sprouts
1 pinch of chilli flakes, optional 
";

        IngredientParserService ingredientParser = CreateIngredientParserService(mockFoodService);

        // Act
        var results = await ingredientParser.ParseAndMatchIngredientsAsync(ingredientLines);

        // Assert
        // We expect 13 results — all lines should be parseable with the new parser.
        Assert.AreEqual(13, results.Count, "Expected 13 parsed ingredient lines.");

        // Every result must have a non-null, non-empty FoodDescription and a valid Quantity.
        for (int i = 0; i < results.Count; i++)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(results[i].ParsedIngredient.FoodDescription),
                $"FoodDescription should not be empty at index {i}. RawLine: {results[i].ParsedIngredient.RawLine}");
            Assert.IsTrue(results[i].ParsedIngredient.Quantity > 0,
                $"Quantity should be > 0 at index {i}. RawLine: {results[i].ParsedIngredient.RawLine}");
        }

        // Each result must have a MatchScore in [0,1] and a non-empty MatchMethod.
        foreach (var result in results)
        {
            Assert.IsTrue(result.MatchScore is >= 0 and <= 1,
                $"MatchScore out of range for '{result.ParsedIngredient.RawLine}': {result.MatchScore}");
            Assert.IsFalse(string.IsNullOrEmpty(result.MatchMethod),
                $"MatchMethod should not be empty for '{result.ParsedIngredient.RawLine}'");
        }
    }

    /// <summary>
    /// Regression tests for word-boundary enforcement on single-letter units.
    /// Before the fix, inputs like "2 lemon" were mis-parsed as qty=2, unit=l (liter),
    /// food=emon — because the unit regex had no word-boundary anchor.
    /// </summary>
    [TestMethod]
    public async Task ParseAndMatchIngredientsAsync_UnitWordBoundary_ParsesCorrectly()
    {
        // Arrange — use a mock that always returns "no match" so we can focus on parsing
        var mockFoodService = new Mock<IFoodService>();
        mockFoodService
            .Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns(new FoodMatchResult { Food = null, Score = 0, Method = "None" });

        IngredientParserService parser = CreateIngredientParserService(mockFoodService.Object);

        // --- Cases where a single-letter unit must NOT be stolen from the food word ---

        // "2 lemon"  →  qty=2, unit="", food="lemon"   (was: unit=l, food=emon)
        var r = await parser.ParseAndMatchIngredientsAsync("2 lemon");
        Assert.AreEqual(1, r.Count, "2 lemon: expected 1 result");
        Assert.AreEqual(2.0, r[0].ParsedIngredient.Quantity, "2 lemon: wrong quantity");
        Assert.AreEqual("", r[0].ParsedIngredient.Unit, "2 lemon: unit should be empty, not 'l'");
        Assert.AreEqual("lemon", r[0].ParsedIngredient.FoodDescription, "2 lemon: wrong food description");

        // "3 lemons"  →  qty=3, unit="", food="lemons"
        r = await parser.ParseAndMatchIngredientsAsync("3 lemons");
        Assert.AreEqual(1, r.Count, "3 lemons: expected 1 result");
        Assert.AreEqual(3.0, r[0].ParsedIngredient.Quantity, "3 lemons: wrong quantity");
        Assert.AreEqual("", r[0].ParsedIngredient.Unit, "3 lemons: unit should be empty, not 'l'");
        Assert.AreEqual("lemons", r[0].ParsedIngredient.FoodDescription, "3 lemons: wrong food description");

        // "1 lime"  →  qty=1, unit="", food="lime"  (l must not match)
        r = await parser.ParseAndMatchIngredientsAsync("1 lime");
        Assert.AreEqual(1, r.Count, "1 lime: expected 1 result");
        Assert.AreEqual(1.0, r[0].ParsedIngredient.Quantity, "1 lime: wrong quantity");
        Assert.AreEqual("", r[0].ParsedIngredient.Unit, "1 lime: unit should be empty, not 'l'");
        Assert.AreEqual("lime", r[0].ParsedIngredient.FoodDescription, "1 lime: wrong food description");

        // "2 garlic cloves"  →  qty=2, unit="", food="garlic cloves"  (g must not match)
        r = await parser.ParseAndMatchIngredientsAsync("2 garlic cloves");
        Assert.AreEqual(1, r.Count, "2 garlic cloves: expected 1 result");
        Assert.AreEqual(2.0, r[0].ParsedIngredient.Quantity, "2 garlic cloves: wrong quantity");
        Assert.AreEqual("", r[0].ParsedIngredient.Unit, "2 garlic cloves: unit should be empty, not 'g'");
        Assert.AreEqual("garlic cloves", r[0].ParsedIngredient.FoodDescription, "2 garlic cloves: wrong food description");

        // "4 small onions"  →  qty=4, unit="small", food="onions"  (legitimate word unit)
        r = await parser.ParseAndMatchIngredientsAsync("4 small onions");
        Assert.AreEqual(1, r.Count, "4 small onions: expected 1 result");
        Assert.AreEqual(4.0, r[0].ParsedIngredient.Quantity, "4 small onions: wrong quantity");
        Assert.AreEqual("small", r[0].ParsedIngredient.Unit, "4 small onions: wrong unit");
        Assert.AreEqual("onions", r[0].ParsedIngredient.FoodDescription, "4 small onions: wrong food description");

        // --- Cases where a single-letter unit MUST still be captured correctly ---

        // "1 l water"  →  qty=1, unit="l", food="water"  (l followed by space is a word boundary)
        r = await parser.ParseAndMatchIngredientsAsync("1 l water");
        Assert.AreEqual(1, r.Count, "1 l water: expected 1 result");
        Assert.AreEqual(1.0, r[0].ParsedIngredient.Quantity, "1 l water: wrong quantity");
        Assert.AreEqual("l", r[0].ParsedIngredient.Unit, "1 l water: wrong unit");
        Assert.AreEqual("water", r[0].ParsedIngredient.FoodDescription, "1 l water: wrong food description");

        // "250g flour"  →  qty=250, unit="g", food="flour"  (g attached to number)
        r = await parser.ParseAndMatchIngredientsAsync("250g flour");
        Assert.AreEqual(1, r.Count, "250g flour: expected 1 result");
        Assert.AreEqual(250.0, r[0].ParsedIngredient.Quantity, "250g flour: wrong quantity");
        Assert.AreEqual("g", r[0].ParsedIngredient.Unit, "250g flour: wrong unit");
        Assert.AreEqual("flour", r[0].ParsedIngredient.FoodDescription, "250g flour: wrong food description");

        // "500 ml water"  →  qty=500, unit="ml", food="water"
        r = await parser.ParseAndMatchIngredientsAsync("500 ml water");
        Assert.AreEqual(1, r.Count, "500 ml water: expected 1 result");
        Assert.AreEqual(500.0, r[0].ParsedIngredient.Quantity, "500 ml water: wrong quantity");
        Assert.AreEqual("ml", r[0].ParsedIngredient.Unit, "500 ml water: wrong unit");
        Assert.AreEqual("water", r[0].ParsedIngredient.FoodDescription, "500 ml water: wrong food description");
    }

    [TestMethod]
    public void LevenshteinDistance_IdenticalStrings_ReturnsZero()
    {
        // Arrange
        string source = "flour";
        string target = "flour";

        // Act
        int distance = GetLevenshteinDistance(source, target);

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
        int distance = GetLevenshteinDistance(source, target);

        // Assert
        Assert.AreEqual(1, distance);
    }

    [TestMethod]
    public void LevenshteinDistance_MissingCharacter_ReturnsOne()
    {
        // Arrange
        string source = "flour";
        string target = "four"; // Missing 'l'

        // Act
        int distance = GetLevenshteinDistance(source, target);

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
        int distance = GetLevenshteinDistance(source, target);

        // Assert
        Assert.AreEqual(1, distance);
    }

    [TestMethod]
    public void LevenshteinDistance_Typos_WithinThresholdOfThree()
    {
        // Arrange
        var testCases = new[]
        {
            ("apple", "aple"), // 1 deletion
            ("banana", "bananna"), // 1 insertion
            ("chicken", "chiken"), // 1 deletion
            ("flour", "flur"), // 1 deletion
        };

        foreach (var (source, target) in testCases)
        {
            // Act
            int distance = GetLevenshteinDistance(source, target);

            // Assert
            Assert.IsTrue(distance <= 3, $"Failed for variant: {source}");
        }
    }

    [TestMethod]
    public void LevenshteinDistance_CompletelyDifferent_GreaterThanThreshold()
    {
        // Arrange
        string source = "apple";
        string target = "xyz";

        // Act
        int distance = GetLevenshteinDistance(source, target);

        // Assert
        Assert.IsTrue(distance > 3, "Completely different strings should have distance > 3");
    }

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
            FoodDescription = "Apple"
        };

        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = food,
            MatchDistance = 0,
            MatchScore = 1.0,
            IsMatched = true
        };

        // Act
        double calories = match.GetCalories();

        // Assert
        // 1 apple * 182g * (52 cal / 100g) = ~94.64 calories
        Assert.AreEqual(94.64, calories, 0.01);
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
            FoodDescription = "Butter"
        };

        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = food,
            MatchDistance = 0,
            MatchScore = 1.0,
            IsMatched = true
        };

        // Act
        double fat = match.GetFat();

        // Assert
        // 1 tbsp uses the canonical 15g/tbsp conversion: 15g * (81g fat / 100g) = 12.15g fat
        Assert.AreEqual(12.15, fat, 0.01);
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
            FoodDescription = "Chicken Breast"
        };

        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = food,
            MatchDistance = 0,
            MatchScore = 1.0,
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
            FoodDescription = "Banana"
        };

        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = food,
            MatchDistance = 0,
            MatchScore = 1.0,
            IsMatched = true
        };

        // Act
        double carbs = match.GetCarbohydrates();

        // Assert
        // 1 banana * 118g * (23g / 100g) = ~27.14g carbs
        Assert.AreEqual(27.14, carbs, 0.01);
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
            FoodDescription = "Milk"
        };

        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = food,
            MatchDistance = 0,
            MatchScore = 1.0,
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
            FoodDescription = "unknown ingredient"
        };

        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = parsed,
            MatchedFood = null,
            MatchDistance = -1,
            MatchScore = 0,
            IsMatched = false
        };

        // Act
        double calories = match.GetCalories();

        // Assert
        Assert.AreEqual(0.0, calories);
    }

    /// <summary>
    /// Helper method to create a mock IFoodService with the provided JSON data.
    /// </summary>
    private static IFoodService CreateMockFoodService(string jsonData)
    {
        var foods = JsonConvert.DeserializeObject<List<Food>>(jsonData) ?? new List<Food>();
        var mockFoodService = new Mock<IFoodService>();

        mockFoodService.Setup(s => s.TryGetFood(It.IsAny<string>(), out It.Ref<Food?>.IsAny))
            .Returns((string name, out Food? food) =>
            {
                food = foods.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                return food != null;
            });

        mockFoodService.Setup(s => s.TryGetFoodFuzzy(It.IsAny<string>(), out It.Ref<Food?>.IsAny))
            .Returns((string name, out Food? food) =>
            {
                var best = foods
                    .Select(f => new
                    {
                        Food = f,
                        Distance = GetLevenshteinDistance(name.ToLowerInvariant(), f.Name.ToLowerInvariant())
                    })
                    .OrderBy(x => x.Distance)
                    .FirstOrDefault();
                food = best?.Food;
                return food != null;
            });

        // FindBestMatch: exact first, then best Levenshtein
        mockFoodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns((string name) =>
            {
                // Stage 1: exact
                var exact = foods.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                {
                    return new FoodMatchResult { Food = exact, Score = 1.0, Method = "Exact" };
                }

                // Stage 2: best Levenshtein — only return a match when score >= 0.3
                if (foods.Count == 0)
                {
                    return new FoodMatchResult { Score = 0, Method = "None" };
                }

                var best = foods
                    .Select(f => new
                    {
                        Food = f,
                        Distance = GetLevenshteinDistance(name.ToLowerInvariant(), f.Name.ToLowerInvariant())
                    })
                    .OrderBy(x => x.Distance)
                    .First();

                int maxLen = Math.Max(name.Length, best.Food.Name.Length);
                double score = maxLen == 0 ? 1.0 : 1.0 - (double)best.Distance / maxLen;

                // Don't return a match for extremely low scores
                if (score < 0.1)
                {
                    return new FoodMatchResult { Score = score, Method = "Fuzzy" };
                }

                return new FoodMatchResult { Food = best.Food, Score = score, Method = "Fuzzy" };
            });

        return mockFoodService.Object;
    }

    private static IngredientParserService CreateIngredientParserService(IFoodService foodService) =>
        new(foodService);

    // Helper method for Levenshtein Distance (moved from ParsedIngredientsTableTests)
    private static int GetLevenshteinDistance(string source, string target)
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
}